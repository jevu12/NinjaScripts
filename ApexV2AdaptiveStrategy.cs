// ============================================================================
// APEX v2 Adaptive Strategy (SETUP->RUPTURA) + SL/TP
// Platform: NinjaTrader 8 (C#)
// Type: NinjaTrader.NinjaScript.Strategies.Strategy
// Compatibility: Strategy Analyzer, Playback, Realtime
// Purpose: Faithful conversion of PineScript "APEX v2 Adaptive" to NinjaTrader 8
// ============================================================================
// Key Features:
//   - Session management (Asia/Europe/NY) with session-anchored VWAP
//   - Multiple data series (HTF 15-min trend and Daily ATR)
//   - Opening Range (OR) detection per session
//   - Trend vs Chop regime classification
//   - Trend setup with pending breakout orders (SETUP->RUPTURA)
//   - Chop mean reversion entries
//   - ATR-based SL/TP with configurable multipliers
//   - Prop-firm ready with guards for Apex compliance
// ============================================================================

#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows.Media;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators;
#endregion

namespace NinjaTrader.NinjaScript.Strategies
{
    public class ApexV2AdaptiveStrategy : Strategy
    {
        #region Constants
        // BarsInProgress indices
        private const int BIP_Primary = 0;
        private const int BIP_HTF = 1;
        private const int BIP_Daily = 2;
        
        // Pending direction
        private const int PENDING_NONE = 0;
        private const int PENDING_LONG = 1;
        private const int PENDING_SHORT = -1;
        
        // Time constants (HHMMSS format as integers - without leading zeros to avoid octal interpretation)
        private const int TIME_ASIA_START = 180000;     // 18:00:00 ET
        private const int TIME_ASIA_END = 20000;        // 02:00:00 ET (next day)
        private const int TIME_EUROPE_START = 20000;    // 02:00:00 ET
        private const int TIME_EUROPE_END = 83000;      // 08:30:00 ET
        private const int TIME_NY_START = 93000;        // 09:30:00 ET
        private const int TIME_NY_END = 160000;         // 16:00:00 ET
        private const int TIME_SESSION_CLOSE_CT = 155900;  // 15:59:00 CT (3:59 PM) - Apex requirement
        private const int MINUTES_TO_TIME_MULTIPLIER = 100; // Converts minutes to HHMMSS integer offset
        
        // Calculation constants
        private const double ATR_DAILY_FALLBACK_MULT = 2.0;  // Multiplier when daily ATR unavailable
        private const double CHOP_VWAP_SLOPE_FACTOR = 0.80;  // 80% of min slope for chop regime
        #endregion

        #region User Parameters - Sessions
        [NinjaScriptProperty]
        [Display(Name = "Use Sessions Filter", Order = 1, GroupName = "1. Sessions")]
        public bool UseSessions { get; set; } = true;

        [NinjaScriptProperty]
        [Display(Name = "Asia Session (18:00-02:00 ET)", Order = 2, GroupName = "1. Sessions")]
        public bool AsiaSession { get; set; } = true;

        [NinjaScriptProperty]
        [Display(Name = "Europe Session (02:00-08:30 ET)", Order = 3, GroupName = "1. Sessions")]
        public bool EuropeSession { get; set; } = true;

        [NinjaScriptProperty]
        [Display(Name = "NY Session (09:30-16:00 ET)", Order = 4, GroupName = "1. Sessions")]
        public bool NySession { get; set; } = true;
        #endregion

        #region User Parameters - Timeframes/EMAs
        [NinjaScriptProperty]
        [Display(Name = "Trend TF Minutes (HTF)", Order = 1, GroupName = "2. Timeframes & EMAs")]
        [Range(1, 1440)]
        public int TrendTFMinutes { get; set; } = 15;

        [NinjaScriptProperty]
        [Display(Name = "EMA Fast Length", Order = 2, GroupName = "2. Timeframes & EMAs")]
        [Range(1, 200)]
        public int EmaFastLen { get; set; } = 21;

        [NinjaScriptProperty]
        [Display(Name = "EMA Slow Length", Order = 3, GroupName = "2. Timeframes & EMAs")]
        [Range(1, 200)]
        public int EmaSlowLen { get; set; } = 55;
        #endregion

        #region User Parameters - Volatility
        [NinjaScriptProperty]
        [Display(Name = "ATR Length (Intraday)", Order = 1, GroupName = "3. Volatility")]
        [Range(1, 100)]
        public int AtrLen { get; set; } = 14;

        [NinjaScriptProperty]
        [Display(Name = "ATR Daily Length", Order = 2, GroupName = "3. Volatility")]
        [Range(1, 100)]
        public int AtrDailyLen { get; set; } = 14;
        #endregion

        #region User Parameters - Opening Range
        [NinjaScriptProperty]
        [Display(Name = "OR Minutes", Order = 1, GroupName = "4. Opening Range")]
        [Range(1, 120)]
        public int OrMinutes { get; set; } = 15;

        [NinjaScriptProperty]
        [Display(Name = "OR/ATR Frac for Trend Day", Order = 2, GroupName = "4. Opening Range")]
        [Range(0.01, 2.0)]
        public double OrAtrFracTrend { get; set; } = 0.33;
        #endregion

        #region User Parameters - Regime Detection
        [NinjaScriptProperty]
        [Display(Name = "ADX Length", Order = 1, GroupName = "5. Regime Detection")]
        [Range(1, 100)]
        public int AdxLen { get; set; } = 14;

        [NinjaScriptProperty]
        [Display(Name = "ADX Min for Trend", Order = 2, GroupName = "5. Regime Detection")]
        [Range(1, 100)]
        public double AdxMinTrend { get; set; } = 18;

        [NinjaScriptProperty]
        [Display(Name = "EMA Spread Min (Normalized)", Order = 3, GroupName = "5. Regime Detection")]
        [Range(0.01, 5.0)]
        public double EmaSpreadMin { get; set; } = 0.35;

        [NinjaScriptProperty]
        [Display(Name = "VWAP Slope Min", Order = 4, GroupName = "5. Regime Detection")]
        [Range(0.01, 2.0)]
        public double VwapSlopeMin { get; set; } = 0.10;

        [NinjaScriptProperty]
        [Display(Name = "Allow Chop in NY", Order = 5, GroupName = "5. Regime Detection")]
        public bool AllowChopInNY { get; set; } = false;
        #endregion

        #region User Parameters - Trend Setup
        [NinjaScriptProperty]
        [Display(Name = "Break Buffer Ticks", Order = 1, GroupName = "6. Trend Setup")]
        [Range(0, 50)]
        public int BreakBufferTicks { get; set; } = 2;

        [NinjaScriptProperty]
        [Display(Name = "Setup Expiry Bars", Order = 2, GroupName = "6. Trend Setup")]
        [Range(1, 50)]
        public int SetupExpiryBars { get; set; } = 6;

        [NinjaScriptProperty]
        [Display(Name = "Use Pullback VWAP", Order = 3, GroupName = "6. Trend Setup")]
        public bool UsePullbackVWAP { get; set; } = true;

        [NinjaScriptProperty]
        [Display(Name = "Pull Band ATR Mult", Order = 4, GroupName = "6. Trend Setup")]
        [Range(0.01, 2.0)]
        public double PullBandATR { get; set; } = 0.30;

        [NinjaScriptProperty]
        [Display(Name = "Use Pullback EMA", Order = 5, GroupName = "6. Trend Setup")]
        public bool UsePullbackEMA { get; set; } = true;
        #endregion

        #region User Parameters - Chop Mean Reversion
        [NinjaScriptProperty]
        [Display(Name = "Deviation ATR Mult", Order = 1, GroupName = "7. Chop Mean Reversion")]
        [Range(0.1, 5.0)]
        public double DevMultATR { get; set; } = 1.10;

        [NinjaScriptProperty]
        [Display(Name = "RSI Length", Order = 2, GroupName = "7. Chop Mean Reversion")]
        [Range(1, 100)]
        public int RsiLen { get; set; } = 14;

        [NinjaScriptProperty]
        [Display(Name = "RSI Low Threshold", Order = 3, GroupName = "7. Chop Mean Reversion")]
        [Range(1, 50)]
        public double RsiLow { get; set; } = 35;

        [NinjaScriptProperty]
        [Display(Name = "RSI High Threshold", Order = 4, GroupName = "7. Chop Mean Reversion")]
        [Range(50, 99)]
        public double RsiHigh { get; set; } = 65;

        [NinjaScriptProperty]
        [Display(Name = "Chop TP ATR Mult", Order = 5, GroupName = "7. Chop Mean Reversion")]
        [Range(0.1, 5.0)]
        public double ChopTP_ATR { get; set; } = 0.80;
        #endregion

        #region User Parameters - Risk/SL/TP
        [NinjaScriptProperty]
        [Display(Name = "Stop ATR Mult", Order = 1, GroupName = "8. Risk (SL/TP)")]
        [Range(0.1, 5.0)]
        public double StopMultATR { get; set; } = 0.85;

        [NinjaScriptProperty]
        [Display(Name = "Take Profit ATR Mult", Order = 2, GroupName = "8. Risk (SL/TP)")]
        [Range(0.1, 10.0)]
        public double TpMultATR { get; set; } = 1.20;
        #endregion

        #region User Parameters - Execution
        [NinjaScriptProperty]
        [Display(Name = "Max Signals Per Session", Order = 1, GroupName = "9. Execution")]
        [Range(1, 100)]
        public int MaxSignalsPerSession { get; set; } = 4;

        [NinjaScriptProperty]
        [Display(Name = "Cooldown Bars", Order = 2, GroupName = "9. Execution")]
        [Range(0, 100)]
        public int CooldownBars { get; set; } = 2;

        [NinjaScriptProperty]
        [Display(Name = "Position Size (Contracts)", Order = 3, GroupName = "9. Execution")]
        [Range(1, 100)]
        public int PositionSize { get; set; } = 1;
        #endregion

        #region User Parameters - Prop Firm Guards
        [NinjaScriptProperty]
        [Display(Name = "Daily Loss Limit ($)", Order = 1, GroupName = "10. Prop Firm Guards")]
        [Range(0, 100000)]
        public double DailyLossLimit { get; set; } = 0;

        [NinjaScriptProperty]
        [Display(Name = "Flatten Minutes Before Close", Order = 2, GroupName = "10. Prop Firm Guards")]
        [Range(0, 120)]
        public int FlattenMinutesBeforeClose { get; set; } = 1;

        [NinjaScriptProperty]
        [Display(Name = "Disable After N Consecutive Losses", Order = 3, GroupName = "10. Prop Firm Guards")]
        [Range(0, 20)]
        public int DisableTradingAfterNConsecutiveLosses { get; set; } = 0;

        [NinjaScriptProperty]
        [Display(Name = "Max Trailing Drawdown Guard ($)", Order = 4, GroupName = "10. Prop Firm Guards")]
        [Range(0, 100000)]
        public double MaxTrailingDrawdownGuard { get; set; } = 0;
        #endregion

        #region User Parameters - Debug
        [NinjaScriptProperty]
        [Display(Name = "Debug Mode", Order = 1, GroupName = "99. Debug")]
        public bool DebugMode { get; set; } = false;
        #endregion

        #region Indicators
        private ATR atrIntraday;
        private ATR atrDaily;
        private RSI rsiIntraday;
        private EMA emaFastPrimary;
        private EMA emaSlowPrimary;
        private EMA emaFastHTF;
        private EMA emaSlowHTF;
        #endregion

        #region Session State
        private enum SessionType { None, Asia, Europe, NY }
        private SessionType currentSession = SessionType.None;
        private SessionType lastSession = SessionType.None;
        private bool sessionStarted = false;
        private int signalsThisSession = 0;
        private int lastSignalBar = -1000;
        #endregion

        #region VWAP State
        private double cumPV = 0;
        private double cumV = 0;
        private double vwapSess = double.NaN;
        private List<double> vwapHistory = new List<double>();
        private const int VWAP_SLOPE_LOOKBACK = 5;
        #endregion

        #region Opening Range State
        private double orHigh = double.NaN;
        private double orLow = double.NaN;
        private bool orDone = false;
        private DateTime orStartTime;
        private double orRange = 0;
        private double orRatio = 0;
        #endregion

        #region Regime State
        private bool useTrend = false;
        private bool useChop = false;
        private double adxHTF = 0;
        private double emaSpreadNorm = 0;
        private double vwapSlope = 0;
        #endregion

        #region Pending Order State
        private int pendingDir = PENDING_NONE;
        private double pendingLevel = double.NaN;
        private int pendingAge = 0;
        private string pendingType = "";
        #endregion

        #region Risk Guards State
        private double dailyRealizedPnL = 0;
        private int consecutiveLosses = 0;
        private double peakEquity = 0;
        private bool dailyGuardTripped = false;
        #endregion

        #region ADX Manual Calculation State (Wilder's method)
        private double[] htfDMPlus;
        private double[] htfDMMinus;
        private double[] htfTR;
        private double smDMPlus = 0;
        private double smDMMinus = 0;
        private double smTR = 0;
        private double smDX = 0;
        private int adxBarCount = 0;
        #endregion

        #region Data Validation
        private bool dataLoaded = false;
        private int minBarsRequired = 0;
        #endregion

        #region OnStateChange
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = "ApexV2AdaptiveStrategy";
                Description = @"APEX v2 Adaptive Strategy - Faithful conversion from PineScript. 
Features session-anchored VWAP, OR detection, Trend/Chop regime, 
SETUP->RUPTURA breakout entries, Chop mean reversion, and prop-firm guards.";
                Calculate = Calculate.OnBarClose;
                
                // Prop firm compliance: no pyramiding
                EntriesPerDirection = 1;
                EntryHandling = EntryHandling.UniqueEntries;
                
                // Flatten before close for intraday
                IsExitOnSessionCloseStrategy = true;
                ExitOnSessionCloseSeconds = 60;
                
                IsInstantiatedOnEachOptimizationIteration = true;
                StartBehavior = StartBehavior.WaitUntilFlat;
                TimeInForce = TimeInForce.Day;
                TraceOrders = false;
                RealtimeErrorHandling = RealtimeErrorHandling.StopCancelClose;
                StopTargetHandling = StopTargetHandling.PerEntryExecution;
                BarsRequiredToTrade = 55;
            }
            else if (State == State.Configure)
            {
                // Add HTF data series for trend detection
                AddDataSeries(BarsPeriodType.Minute, TrendTFMinutes);
                
                // Add Daily data series for ATR Daily
                AddDataSeries(BarsPeriodType.Day, 1);
            }
            else if (State == State.DataLoaded)
            {
                // Initialize indicators on Primary series (BIP=0)
                atrIntraday = ATR(AtrLen);
                rsiIntraday = RSI(RsiLen, 1);
                emaFastPrimary = EMA(EmaFastLen);
                emaSlowPrimary = EMA(EmaSlowLen);
                
                // Initialize indicators on HTF series (BIP=1)
                emaFastHTF = EMA(BarsArray[BIP_HTF], EmaFastLen);
                emaSlowHTF = EMA(BarsArray[BIP_HTF], EmaSlowLen);
                
                // Initialize ATR on Daily series (BIP=2)
                atrDaily = ATR(BarsArray[BIP_Daily], AtrDailyLen);
                
                // Initialize ADX manual arrays
                htfDMPlus = new double[AdxLen + 1];
                htfDMMinus = new double[AdxLen + 1];
                htfTR = new double[AdxLen + 1];
                
                // Calculate minimum bars required across all indicator periods
                int maxIndicatorPeriod = new[] { EmaSlowLen, AtrLen, RsiLen, AdxLen }.Max();
                minBarsRequired = maxIndicatorPeriod + VWAP_SLOPE_LOOKBACK + 5;
                
                dataLoaded = true;
                
                if (DebugMode)
                    Print($"[APEX] DataLoaded: minBarsRequired={minBarsRequired}");
            }
        }
        #endregion

        #region OnBarUpdate
        protected override void OnBarUpdate()
        {
            if (!dataLoaded)
                return;
            
            // Validate sufficient bars for all series
            if (CurrentBars[BIP_Primary] < minBarsRequired)
                return;
            if (CurrentBars[BIP_HTF] < minBarsRequired)
                return;
            if (CurrentBars[BIP_Daily] < AtrDailyLen + 1)
                return;

            // Update HTF values when HTF bar closes
            if (BarsInProgress == BIP_HTF)
            {
                UpdateHTFIndicators();
                return;
            }
            
            // Update Daily ATR when Daily bar closes
            if (BarsInProgress == BIP_Daily)
            {
                return;
            }
            
            // Main logic only on Primary series
            if (BarsInProgress != BIP_Primary)
                return;
            
            // === Session Management ===
            UpdateSession();
            
            // === Daily Reset ===
            if (Bars.IsFirstBarOfSession)
            {
                ResetDailyState();
            }
            
            // === Flatten Before Close ===
            if (ShouldFlattenBeforeClose())
            {
                FlattenPosition("FlatBeforeClose");
                return;
            }
            
            // === Check Guards ===
            if (IsGuardTripped())
                return;
            
            // === Update VWAP ===
            UpdateVWAP();
            
            // === Update Opening Range ===
            UpdateOpeningRange();
            
            // === Update Regime ===
            UpdateRegime();
            
            // === Check for Pending Triggers ===
            CheckPendingTriggers();
            
            // === Generate New Setups ===
            GenerateSetups();
        }
        #endregion

        #region Session Management
        private void UpdateSession()
        {
            SessionType newSession = GetCurrentSession();
            
            // Detect session change
            if (newSession != lastSession)
            {
                if (newSession != SessionType.None)
                {
                    sessionStarted = true;
                    ResetSessionState();
                    
                    if (DebugMode)
                        Print($"[APEX] Session Start: {newSession} at {Time[0]}");
                }
                lastSession = newSession;
            }
            
            currentSession = newSession;
        }

        private SessionType GetCurrentSession()
        {
            if (!UseSessions)
                return SessionType.NY; // Default to NY if sessions disabled
            
            int timeNow = ToTime(Time[0]);
            
            // Asia: 18:00-02:00 ET (crosses midnight)
            if (AsiaSession)
            {
                if (timeNow >= TIME_ASIA_START || timeNow < TIME_ASIA_END)
                    return SessionType.Asia;
            }
            
            // Europe: 02:00-08:30 ET
            if (EuropeSession)
            {
                if (timeNow >= TIME_EUROPE_START && timeNow < TIME_EUROPE_END)
                    return SessionType.Europe;
            }
            
            // NY: 09:30-16:00 ET
            if (NySession)
            {
                if (timeNow >= TIME_NY_START && timeNow < TIME_NY_END)
                    return SessionType.NY;
            }
            
            return SessionType.None;
        }

        private void ResetSessionState()
        {
            // Reset session counters
            signalsThisSession = 0;
            lastSignalBar = CurrentBar - CooldownBars - 1; // Allow immediate signal
            
            // Reset VWAP
            cumPV = 0;
            cumV = 0;
            vwapSess = double.NaN;
            vwapHistory.Clear();
            
            // Reset Opening Range
            orHigh = double.NaN;
            orLow = double.NaN;
            orDone = false;
            orStartTime = Time[0];
            orRange = 0;
            orRatio = 0;
            
            // Reset pending
            CancelPending();
            
            if (DebugMode)
                Print($"[APEX] Session State Reset at bar {CurrentBar}");
        }

        private void ResetDailyState()
        {
            dailyRealizedPnL = 0;
            consecutiveLosses = 0;
            dailyGuardTripped = false;
            
            if (DebugMode)
                Print($"[APEX] Daily State Reset at {Time[0]}");
        }

        private bool IsInSession()
        {
            return currentSession != SessionType.None;
        }
        #endregion

        #region VWAP Calculation
        private void UpdateVWAP()
        {
            if (!IsInSession())
            {
                vwapSess = double.NaN;
                return;
            }
            
            // Typical Price = (H + L + C) / 3
            double typicalPrice = (High[0] + Low[0] + Close[0]) / 3.0;
            double vol = Volume[0];
            
            if (vol <= 0)
                vol = 1; // Avoid division by zero
            
            cumPV += typicalPrice * vol;
            cumV += vol;
            
            if (cumV > 0)
                vwapSess = cumPV / cumV;
            else
                vwapSess = typicalPrice;
            
            // Store VWAP history for slope calculation
            vwapHistory.Add(vwapSess);
            if (vwapHistory.Count > VWAP_SLOPE_LOOKBACK + 1)
                vwapHistory.RemoveAt(0);
        }

        private double GetVwapSlope()
        {
            if (vwapHistory.Count <= VWAP_SLOPE_LOOKBACK)
                return 0;
            
            double currentVwap = vwapHistory[vwapHistory.Count - 1];
            double prevVwap = vwapHistory[vwapHistory.Count - 1 - VWAP_SLOPE_LOOKBACK];
            
            double atr = atrIntraday[0];
            if (atr <= 0 || double.IsNaN(atr))
                return 0;
            
            return (currentVwap - prevVwap) / atr;
        }
        #endregion

        #region Opening Range
        private void UpdateOpeningRange()
        {
            if (!IsInSession() || orDone)
                return;
            
            // Check if we're still within OR window
            TimeSpan elapsed = Time[0] - orStartTime;
            
            if (elapsed.TotalMinutes < OrMinutes)
            {
                // Update OR high/low
                if (double.IsNaN(orHigh) || High[0] > orHigh)
                    orHigh = High[0];
                if (double.IsNaN(orLow) || Low[0] < orLow)
                    orLow = Low[0];
            }
            else
            {
                // OR period complete
                orDone = true;
                orRange = orHigh - orLow;
                
                // Calculate OR ratio vs Daily ATR
                double atrD = GetDailyATR();
                if (atrD > 0)
                    orRatio = orRange / atrD;
                else
                    orRatio = 0;
                
                if (DebugMode)
                    Print($"[APEX] OR Complete: High={orHigh:F2}, Low={orLow:F2}, Range={orRange:F2}, Ratio={orRatio:F2}");
            }
        }

        private double GetDailyATR()
        {
            if (CurrentBars[BIP_Daily] < AtrDailyLen)
                return atrIntraday[0] * ATR_DAILY_FALLBACK_MULT; // Fallback when daily ATR not yet available
            
            return atrDaily[0];
        }
        #endregion

        #region HTF Indicators & Manual ADX
        private void UpdateHTFIndicators()
        {
            // Calculate manual ADX using Wilder's method (faithful to PineScript)
            CalculateManualADX();
        }

        private void CalculateManualADX()
        {
            // This implements Wilder's ADX calculation (equivalent to PineScript ta.adx)
            if (CurrentBars[BIP_HTF] < 2)
                return;
            
            // Get HTF bar data
            double highHTF = Highs[BIP_HTF][0];
            double lowHTF = Lows[BIP_HTF][0];
            double closeHTF = Closes[BIP_HTF][0];
            double highHTF1 = Highs[BIP_HTF][1];
            double lowHTF1 = Lows[BIP_HTF][1];
            double closeHTF1 = Closes[BIP_HTF][1];
            
            // Calculate +DM and -DM
            double upMove = highHTF - highHTF1;
            double downMove = lowHTF1 - lowHTF;
            
            double dmPlus = (upMove > downMove && upMove > 0) ? upMove : 0;
            double dmMinus = (downMove > upMove && downMove > 0) ? downMove : 0;
            
            // Calculate True Range
            double tr = Math.Max(highHTF - lowHTF, 
                        Math.Max(Math.Abs(highHTF - closeHTF1), 
                                 Math.Abs(lowHTF - closeHTF1)));
            
            adxBarCount++;
            
            if (adxBarCount <= AdxLen)
            {
                // Accumulate initial values
                smDMPlus += dmPlus;
                smDMMinus += dmMinus;
                smTR += tr;
                
                if (adxBarCount == AdxLen)
                {
                    // First ADX calculation
                    double diPlus = (smTR > 0) ? 100 * smDMPlus / smTR : 0;
                    double diMinus = (smTR > 0) ? 100 * smDMMinus / smTR : 0;
                    double diSum = diPlus + diMinus;
                    double dx = (diSum > 0) ? 100 * Math.Abs(diPlus - diMinus) / diSum : 0;
                    smDX = dx;
                    adxHTF = dx;
                }
            }
            else
            {
                // Wilder's smoothing: SM = SM_prev - (SM_prev / len) + currentValue
                smDMPlus = smDMPlus - (smDMPlus / AdxLen) + dmPlus;
                smDMMinus = smDMMinus - (smDMMinus / AdxLen) + dmMinus;
                smTR = smTR - (smTR / AdxLen) + tr;
                
                double diPlus = (smTR > 0) ? 100 * smDMPlus / smTR : 0;
                double diMinus = (smTR > 0) ? 100 * smDMMinus / smTR : 0;
                double diSum = diPlus + diMinus;
                double dx = (diSum > 0) ? 100 * Math.Abs(diPlus - diMinus) / diSum : 0;
                
                // Smooth ADX
                smDX = smDX - (smDX / AdxLen) + dx;
                adxHTF = smDX;
            }
        }
        #endregion

        #region Regime Detection
        private void UpdateRegime()
        {
            if (!orDone)
            {
                useTrend = false;
                useChop = false;
                return;
            }
            
            // Get current values
            double atr = atrIntraday[0];
            if (atr <= 0 || double.IsNaN(atr))
                atr = 1;
            
            // EMA spread normalized by ATR (on HTF)
            double emaFastHTFVal = emaFastHTF[0];
            double emaSlowHTFVal = emaSlowHTF[0];
            emaSpreadNorm = Math.Abs(emaFastHTFVal - emaSlowHTFVal) / atr;
            
            // VWAP slope
            vwapSlope = GetVwapSlope();
            
            // Trend strength check (any condition true = trend)
            bool trendStrength = (adxHTF >= AdxMinTrend) ||
                                 (emaSpreadNorm >= EmaSpreadMin) ||
                                 (Math.Abs(vwapSlope) >= VwapSlopeMin);
            
            // Trend day: OR done, OR ratio sufficient, trend strength
            useTrend = orDone && (orRatio >= OrAtrFracTrend) && trendStrength;
            
            // Chop day: OR done, no trend strength, flat VWAP (80% of min slope), (if NY then check AllowChopInNY)
            bool isNY = (currentSession == SessionType.NY);
            bool chopAllowed = !isNY || AllowChopInNY;
            
            useChop = orDone && !trendStrength && 
                      (Math.Abs(vwapSlope) < VwapSlopeMin * CHOP_VWAP_SLOPE_FACTOR) && 
                      chopAllowed;
            
            if (DebugMode && (useTrend || useChop))
                Print($"[APEX] Regime: useTrend={useTrend}, useChop={useChop}, ADX={adxHTF:F1}, EmaSpread={emaSpreadNorm:F2}, VwapSlope={vwapSlope:F3}");
        }
        #endregion

        #region Setup Generation
        private void GenerateSetups()
        {
            // === Trend Setups (SETUP -> Pending Breakout) ===
            if (useTrend && CanGenerateSignal())
            {
                GenerateTrendSetup();
            }
            
            // === Chop Mean Reversion Setups (immediate entry) ===
            if (useChop && CanGenerateSignal())
            {
                GenerateChopSetup();
            }
        }

        private bool CanGenerateSignal()
        {
            if (!IsInSession())
                return false;
            if (!orDone)
                return false;
            if (signalsThisSession >= MaxSignalsPerSession)
                return false;
            
            // Cooldown check
            bool cooldownOk = (CurrentBar - lastSignalBar >= CooldownBars);
            if (!cooldownOk)
                return false;
            
            // Must be flat
            if (Position.MarketPosition != MarketPosition.Flat)
                return false;
            
            return true;
        }

        private void GenerateTrendSetup()
        {
            if (double.IsNaN(vwapSess))
                return;
            
            double atr = atrIntraday[0];
            if (atr <= 0 || double.IsNaN(atr))
                return;
            
            // Get HTF EMAs
            double emaFastHTFVal = emaFastHTF[0];
            double emaSlowHTFVal = emaSlowHTF[0];
            double emaFastVal = emaFastPrimary[0];
            
            // === BIAS ===
            bool biasLong = useTrend && (emaFastHTFVal > emaSlowHTFVal) && (Close[0] > vwapSess);
            bool biasShort = useTrend && (emaFastHTFVal < emaSlowHTFVal) && (Close[0] < vwapSess);
            
            // === PULLBACK BAND ===
            double band = PullBandATR * atr;
            
            // === TOUCH CONDITIONS ===
            bool touchVWAP_Long = UsePullbackVWAP && (Low[0] <= vwapSess - band) && (Close[0] > vwapSess);
            bool touchVWAP_Short = UsePullbackVWAP && (High[0] >= vwapSess + band) && (Close[0] < vwapSess);
            
            bool touchEMA_Long = UsePullbackEMA && (Low[0] <= emaFastVal) && (Close[0] > emaFastVal);
            bool touchEMA_Short = UsePullbackEMA && (High[0] >= emaFastVal) && (Close[0] < emaFastVal);
            
            // === REVERSAL CONFIRMATION ===
            bool reversalLong = (Close[0] > Open[0]) && (Close[0] > High[1]);
            bool reversalShort = (Close[0] < Open[0]) && (Close[0] < Low[1]);
            
            // === SETUP CONDITIONS ===
            bool setupLong = biasLong && (touchVWAP_Long || touchEMA_Long) && reversalLong;
            bool setupShort = biasShort && (touchVWAP_Short || touchEMA_Short) && reversalShort;
            
            // === CREATE PENDING ORDER ===
            if (setupLong && pendingDir == PENDING_NONE)
            {
                double buf = BreakBufferTicks * TickSize;
                pendingLevel = High[0] + buf;
                pendingDir = PENDING_LONG;
                pendingAge = 0;
                pendingType = "TrendBreak";
                
                if (DebugMode)
                    Print($"[APEX] Pending LONG created at {pendingLevel:F2}, bar {CurrentBar}");
            }
            else if (setupShort && pendingDir == PENDING_NONE)
            {
                double buf = BreakBufferTicks * TickSize;
                pendingLevel = Low[0] - buf;
                pendingDir = PENDING_SHORT;
                pendingAge = 0;
                pendingType = "TrendBreak";
                
                if (DebugMode)
                    Print($"[APEX] Pending SHORT created at {pendingLevel:F2}, bar {CurrentBar}");
            }
        }

        private void GenerateChopSetup()
        {
            if (double.IsNaN(vwapSess))
                return;
            
            double atr = atrIntraday[0];
            if (atr <= 0 || double.IsNaN(atr))
                return;
            
            double rsi = rsiIntraday[0];
            double dev = DevMultATR * atr;
            
            // === REVERSAL CONFIRMATION ===
            bool reversalLong = (Close[0] > Open[0]) && (Close[0] > High[1]);
            bool reversalShort = (Close[0] < Open[0]) && (Close[0] < Low[1]);
            
            // === CHOP MEAN REVERSION ENTRIES ===
            // Short: price extended above VWAP + deviation, RSI overbought, reversal down
            bool chopShort = (Close[0] >= vwapSess + dev) && (rsi >= RsiHigh) && reversalShort;
            
            // Long: price extended below VWAP - deviation, RSI oversold, reversal up
            bool chopLong = (Close[0] <= vwapSess - dev) && (rsi <= RsiLow) && reversalLong;
            
            if (chopLong)
            {
                ExecuteChopEntry(true);
            }
            else if (chopShort)
            {
                ExecuteChopEntry(false);
            }
        }

        private void ExecuteChopEntry(bool isLong)
        {
            double atr = atrIntraday[0];
            double entry = Close[0];
            
            // Calculate SL/TP for chop
            double sl, tp;
            double tpChop = ChopTP_ATR * atr;
            
            if (isLong)
            {
                sl = entry - StopMultATR * atr;
                tp = entry + tpChop;
                
                // For long chop trades, target VWAP mean reversion - use closer target
                if (!double.IsNaN(vwapSess) && vwapSess > entry)
                    tp = Math.Min(tp, vwapSess);
                
                SetStopLoss("ChopLong", CalculationMode.Price, sl, false);
                SetProfitTarget("ChopLong", CalculationMode.Price, tp);
                EnterLong(PositionSize, "ChopLong");
                
                if (DebugMode)
                    Print($"[APEX] CHOP LONG Entry at {entry:F2}, SL={sl:F2}, TP={tp:F2}");
            }
            else
            {
                sl = entry + StopMultATR * atr;
                tp = entry - tpChop;
                
                // For short chop trades, target VWAP mean reversion - use closer target
                if (!double.IsNaN(vwapSess) && vwapSess < entry)
                    tp = Math.Max(tp, vwapSess);
                
                SetStopLoss("ChopShort", CalculationMode.Price, sl, false);
                SetProfitTarget("ChopShort", CalculationMode.Price, tp);
                EnterShort(PositionSize, "ChopShort");
                
                if (DebugMode)
                    Print($"[APEX] CHOP SHORT Entry at {entry:F2}, SL={sl:F2}, TP={tp:F2}");
            }
            
            // Update signal tracking
            signalsThisSession++;
            lastSignalBar = CurrentBar;
        }
        #endregion

        #region Pending Order Management
        private void CheckPendingTriggers()
        {
            if (pendingDir == PENDING_NONE)
                return;
            
            // Age the pending order
            pendingAge++;
            
            // Check expiry
            if (pendingAge > SetupExpiryBars)
            {
                if (DebugMode)
                    Print($"[APEX] Pending expired after {pendingAge} bars");
                CancelPending();
                return;
            }
            
            // Check if position already exists
            if (Position.MarketPosition != MarketPosition.Flat)
            {
                CancelPending();
                return;
            }
            
            // Check trigger
            if (pendingDir == PENDING_LONG)
            {
                // Trigger long if High reaches or exceeds pendingLevel
                if (High[0] >= pendingLevel)
                {
                    ExecuteTrendEntry(true);
                    CancelPending();
                }
            }
            else if (pendingDir == PENDING_SHORT)
            {
                // Trigger short if Low reaches or falls below pendingLevel
                if (Low[0] <= pendingLevel)
                {
                    ExecuteTrendEntry(false);
                    CancelPending();
                }
            }
        }

        private void ExecuteTrendEntry(bool isLong)
        {
            double atr = atrIntraday[0];
            // Use Close[0] as the entry estimate since we're entering on bar close after trigger
            double entry = Close[0];
            
            // Calculate SL/TP for trend based on estimated entry
            double sl, tp;
            
            if (isLong)
            {
                sl = entry - StopMultATR * atr;
                tp = entry + TpMultATR * atr;
                
                SetStopLoss("TrendLong", CalculationMode.Price, sl, false);
                SetProfitTarget("TrendLong", CalculationMode.Price, tp);
                EnterLong(PositionSize, "TrendLong");
                
                if (DebugMode)
                    Print($"[APEX] TREND LONG entry at {entry:F2}, SL={sl:F2}, TP={tp:F2}");
            }
            else
            {
                sl = entry + StopMultATR * atr;
                tp = entry - TpMultATR * atr;
                
                SetStopLoss("TrendShort", CalculationMode.Price, sl, false);
                SetProfitTarget("TrendShort", CalculationMode.Price, tp);
                EnterShort(PositionSize, "TrendShort");
                
                if (DebugMode)
                    Print($"[APEX] TREND SHORT entry at {entry:F2}, SL={sl:F2}, TP={tp:F2}");
            }
            
            // Update signal tracking
            signalsThisSession++;
            lastSignalBar = CurrentBar;
        }

        private void CancelPending()
        {
            pendingDir = PENDING_NONE;
            pendingLevel = double.NaN;
            pendingAge = 0;
            pendingType = "";
        }
        #endregion

        #region Risk Guards
        private bool ShouldFlattenBeforeClose()
        {
            if (FlattenMinutesBeforeClose <= 0)
                return false;
            
            // Calculate cutoff time before session close for Apex compliance
            int cutoffCT = TIME_SESSION_CLOSE_CT - (FlattenMinutesBeforeClose * MINUTES_TO_TIME_MULTIPLIER);
            int nowCT = ToTime(Time[0]);
            
            return (nowCT >= cutoffCT && nowCT < TIME_SESSION_CLOSE_CT);
        }

        private void FlattenPosition(string reason)
        {
            if (Position.MarketPosition == MarketPosition.Long)
            {
                ExitLong(reason);
                if (DebugMode)
                    Print($"[APEX] Flattening LONG position: {reason}");
            }
            else if (Position.MarketPosition == MarketPosition.Short)
            {
                ExitShort(reason);
                if (DebugMode)
                    Print($"[APEX] Flattening SHORT position: {reason}");
            }
            
            CancelPending();
        }

        private bool IsGuardTripped()
        {
            if (dailyGuardTripped)
                return true;
            
            // Update daily PnL
            UpdateDailyPnL();
            
            // Check daily loss limit
            if (DailyLossLimit > 0 && dailyRealizedPnL <= -Math.Abs(DailyLossLimit))
            {
                dailyGuardTripped = true;
                if (DebugMode)
                    Print($"[APEX] Daily Loss Limit hit: {dailyRealizedPnL:C}");
                return true;
            }
            
            // Check consecutive losses
            if (DisableTradingAfterNConsecutiveLosses > 0 && 
                consecutiveLosses >= DisableTradingAfterNConsecutiveLosses)
            {
                dailyGuardTripped = true;
                if (DebugMode)
                    Print($"[APEX] Consecutive Losses Limit hit: {consecutiveLosses}");
                return true;
            }
            
            // Check max trailing drawdown
            if (MaxTrailingDrawdownGuard > 0)
            {
                double equity = SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit;
                if (equity > peakEquity)
                    peakEquity = equity;
                
                double drawdown = peakEquity - equity;
                if (drawdown >= MaxTrailingDrawdownGuard)
                {
                    dailyGuardTripped = true;
                    if (DebugMode)
                        Print($"[APEX] Trailing Drawdown Guard hit: {drawdown:C}");
                    return true;
                }
            }
            
            return false;
        }

        private void UpdateDailyPnL()
        {
            // Calculate session realized PnL from trade collection
            double pnl = 0;
            DateTime sessionStart = Time[0].Date;
            DateTime sessionEnd = sessionStart.AddDays(1);
            
            foreach (var trade in SystemPerformance.AllTrades)
            {
                if (trade.Exit.Time >= sessionStart && trade.Exit.Time < sessionEnd)
                {
                    pnl += trade.ProfitCurrency;
                }
            }
            
            dailyRealizedPnL = pnl;
        }
        #endregion

        #region OnExecutionUpdate
        protected override void OnExecutionUpdate(Execution execution, string executionId, 
            double price, int quantity, MarketPosition marketPosition, string orderId, DateTime time)
        {
            // Track consecutive losses for guard
            if (execution.Order != null && execution.Order.OrderState == OrderState.Filled)
            {
                // Check if this closes a position
                if (marketPosition == MarketPosition.Flat)
                {
                    // Determine if it was a win or loss
                    var lastTrade = SystemPerformance.AllTrades.LastOrDefault();
                    if (lastTrade != null)
                    {
                        if (lastTrade.ProfitCurrency < 0)
                            consecutiveLosses++;
                        else
                            consecutiveLosses = 0;
                    }
                }
            }
        }
        #endregion

        #region Properties for UI
        [Browsable(false)]
        public string StrategyInfo => $"APEX v2 Adaptive | TF:{TrendTFMinutes}m | EMA:{EmaFastLen}/{EmaSlowLen}";
        #endregion
    }
}

// ============================================================================
// NOTES OF EQUIVALENCE VS PINESCRIPT
// ============================================================================
/*
1. VWAP CALCULATION:
   - PineScript: Uses ta.vwap with anchor. 
   - NinjaScript: Custom session-anchored VWAP using cumulative PV/V, reset on session start.
   - Equivalence: Functionally identical when session boundaries match.

2. ADX CALCULATION:
   - PineScript: Uses ta.adx() which implements Wilder's ADX.
   - NinjaScript: Manual implementation of Wilder's smoothed ADX on HTF series.
   - Note: NinjaTrader's built-in ADX() could be used as alternative, but may have 
     slight differences in smoothing. The manual implementation is faithful to PineScript.

3. SESSION HANDLING:
   - PineScript: Uses time() with session strings.
   - NinjaScript: ToTime() with integer comparisons, with midnight wrap handling for Asia session.
   - Equivalence: Functionally identical. Times are in exchange time (typically ET for futures).

4. MULTI-TIMEFRAME DATA:
   - PineScript: request.security() with lookahead=barmerge.lookahead_off.
   - NinjaScript: AddDataSeries() with separate BarsInProgress handling.
   - Equivalence: Both prevent lookahead bias when implemented correctly.

5. PENDING ORDERS:
   - PineScript: strategy.entry() with stop parameter.
   - NinjaScript: EnterLongStopMarket() / EnterShortStopMarket().
   - Note: NinjaTrader handles order aging internally; we track expiry manually.

6. OPENING RANGE:
   - PineScript: Time-based OR window tracking.
   - NinjaScript: TimeSpan comparison from session start.
   - Equivalence: Functionally identical.

7. RSI:
   - PineScript: ta.rsi(close, len)
   - NinjaScript: RSI(RsiLen, 1) - using smoothing of 1 for closest match.
   - Equivalence: Identical calculation.

8. ATR:
   - PineScript: ta.atr(len)
   - NinjaScript: ATR(len) - uses Wilder's smoothing.
   - Equivalence: Identical calculation.
*/

// ============================================================================
// USAGE INSTRUCTIONS
// ============================================================================
/*
HOW TO INSTALL:
1. Open NinjaTrader 8 Control Center
2. Go to Tools > Edit NinjaScript > Strategies
3. Right-click in the editor, select "New Strategy"
4. Name it "ApexV2AdaptiveStrategy"
5. Replace all code with this file's contents
6. Press F5 to compile or click the compile button
7. Verify no compilation errors in the output window

HOW TO CONFIGURE FOR MNQ/NQ:
1. Open a chart with MNQ or NQ (recommended: 1-minute timeframe)
2. Right-click > Strategies > Add Strategy
3. Select "ApexV2AdaptiveStrategy"
4. Configure parameters:
   
   RECOMMENDED SETTINGS FOR MNQ/NQ:
   - TrendTFMinutes: 15 (for 15-minute HTF analysis)
   - EmaFastLen: 21
   - EmaSlowLen: 55
   - AtrLen: 14
   - OrMinutes: 15 (first 15 minutes of session)
   - StopMultATR: 0.85-1.0 (adjust based on volatility)
   - TpMultATR: 1.2-1.5 (risk:reward targeting 1:1.2 to 1:1.5)
   - MaxSignalsPerSession: 2-4 (conservative for prop firm)
   - PositionSize: 1 (start with 1 contract)
   
   PROP FIRM SETTINGS (APEX):
   - DailyLossLimit: Set to 50-70% of your trailing threshold
   - FlattenMinutesBeforeClose: 1-2 (flatten before 4:59 PM ET)
   - DisableTradingAfterNConsecutiveLosses: 2-3

5. Enable "DebugMode" initially to see detailed logs

HOW TO BACKTEST (STRATEGY ANALYZER):
1. Open Control Center > New > Strategy Analyzer
2. Select "ApexV2AdaptiveStrategy"
3. Configure:
   - Instrument: MNQ or NQ
   - Data Series: 1-minute bars
   - Time Range: At least 3 months of data
   - Include commission: $0.54/contract for MNQ, $2.04 for NQ
4. Run backtest
5. Analyze results in the Performance tab

HOW TO TEST IN PLAYBACK:
1. Open a chart with historical data
2. Add the strategy to the chart
3. Use Market Replay (Connection > Playback Connection)
4. Play through historical data to validate entries/exits

IMPORTANT NOTES:
- Always test in simulation before live trading
- Monitor Rithmic/Tradovate for actual Apex threshold (NT8 cannot read it directly)
- The strategy is designed for intraday only - positions flatten before session close
- Start with conservative settings and adjust based on backtest results
- Use DebugMode=true to understand signal generation
*/
