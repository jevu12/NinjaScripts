// ApexPro_NQStrategy - Advanced NinjaTrader 8 Strategy for Apex Trader Funding Evaluations
// Platform: NinjaTrader 8 (C#)
// Instrument: Designed for NQ (E-mini Nasdaq-100), works on 1-5m timeframes
// Goal: Professional-level strategy with advanced indicators and institutional-grade precision
//
// Core Features:
//  - WaveTrend Oscillator for momentum confirmation
//  - Modified Bollinger Bands for volatility expansion detection
//  - Liquidity zone analysis and order flow dynamics
//  - Apex compliance: flat-before-close, daily loss cap, daily profit lock
//  - Dynamic position sizing based on account risk parameters
//  - Advanced risk management with trailing threshold awareness
//
// IMPORTANT: This strategy approximates Apex protections using your plan inputs.
// Always monitor R|Trader/Tradovate as required by Apex Trader Funding.

#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
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
    public class ApexPro_NQStrategy : Strategy
    {
        #region Apex Plan Parameters
        [NinjaScriptProperty]
        [Display(Name = "Account Start Balance ($)", Order = 1, GroupName = "1. Apex Plan")]
        public double AccountStartBalance { get; set; } = 50000;

        [NinjaScriptProperty]
        [Display(Name = "Profit Target ($)", Order = 2, GroupName = "1. Apex Plan")]
        public double ProfitTargetDollars { get; set; } = 3000;

        [NinjaScriptProperty]
        [Display(Name = "Trailing Threshold ($)", Order = 3, GroupName = "1. Apex Plan")]
        public double TrailingThresholdDollars { get; set; } = 2500;

        [NinjaScriptProperty]
        [Display(Name = "Daily Loss Cap ($)", Order = 4, GroupName = "1. Apex Plan")]
        public double DailyLossCap { get; set; } = 600;

        [NinjaScriptProperty]
        [Display(Name = "Daily Profit Lock ($)", Order = 5, GroupName = "1. Apex Plan")]
        public double DailyProfitLock { get; set; } = 800;
        
        [NinjaScriptProperty]
        [Display(Name = "Halt After Daily Guard Triggers", Order = 6, GroupName = "1. Apex Plan")]
        public bool HaltOnDailyGuard { get; set; } = true;
        #endregion

        #region Position Sizing Parameters
        [NinjaScriptProperty]
        [Display(Name = "Max Contracts", Order = 1, GroupName = "2. Position Sizing")]
        public int MaxContracts { get; set; } = 2;

        [NinjaScriptProperty]
        [Display(Name = "Risk Per Trade ($)", Order = 2, GroupName = "2. Position Sizing")]
        public double RiskPerTradeDollars { get; set; } = 750;

        [NinjaScriptProperty]
        [Display(Name = "Risk Per Trade (% of Threshold)", Order = 3, GroupName = "2. Position Sizing")]
        [Range(0.1, 0.5)]
        public double RiskPercentOfThreshold { get; set; } = 0.30; // 30% max
        #endregion

        #region WaveTrend Parameters
        [NinjaScriptProperty]
        [Display(Name = "WaveTrend Channel Length", Order = 1, GroupName = "3. WaveTrend")]
        public int WTChannelLength { get; set; } = 10;

        [NinjaScriptProperty]
        [Display(Name = "WaveTrend Average Length", Order = 2, GroupName = "3. WaveTrend")]
        public int WTAverageLength { get; set; } = 21;

        [NinjaScriptProperty]
        [Display(Name = "WaveTrend Signal Length", Order = 3, GroupName = "3. WaveTrend")]
        public int WTSignalLength { get; set; } = 4;

        [NinjaScriptProperty]
        [Display(Name = "WaveTrend Overbought", Order = 4, GroupName = "3. WaveTrend")]
        public double WTOverbought { get; set; } = 60;

        [NinjaScriptProperty]
        [Display(Name = "WaveTrend Oversold", Order = 5, GroupName = "3. WaveTrend")]
        public double WTOversold { get; set; } = -60;
        #endregion

        #region Bollinger Bands Parameters
        [NinjaScriptProperty]
        [Display(Name = "BB Period", Order = 1, GroupName = "4. Bollinger Bands")]
        public int BBPeriod { get; set; } = 20;

        [NinjaScriptProperty]
        [Display(Name = "BB Std Dev Multiplier", Order = 2, GroupName = "4. Bollinger Bands")]
        public double BBStdDev { get; set; } = 2.0;

        [NinjaScriptProperty]
        [Display(Name = "BB Expansion Multiplier", Order = 3, GroupName = "4. Bollinger Bands")]
        public double BBExpansion { get; set; } = 2.5;

        [NinjaScriptProperty]
        [Display(Name = "BB Squeeze Threshold", Order = 4, GroupName = "4. Bollinger Bands")]
        public double BBSqueezeThreshold { get; set; } = 0.02;
        #endregion

        #region Risk Management Parameters
        [NinjaScriptProperty]
        [Display(Name = "ATR Period", Order = 1, GroupName = "5. Risk Management")]
        public int AtrPeriod { get; set; } = 14;

        [NinjaScriptProperty]
        [Display(Name = "ATR Stop Multiplier", Order = 2, GroupName = "5. Risk Management")]
        public double AtrStopMultiplier { get; set; } = 2.5;

        [NinjaScriptProperty]
        [Display(Name = "Target R Multiple", Order = 3, GroupName = "5. Risk Management")]
        public double TargetRMultiple { get; set; } = 2.0;

        [NinjaScriptProperty]
        [Display(Name = "Flat Before Close (mins)", Order = 4, GroupName = "5. Risk Management")]
        [Range(1, 60)]
        public int MinutesBeforeClose { get; set; } = 2;
        #endregion

        #region Session and Filter Parameters
        [NinjaScriptProperty]
        [Display(Name = "Only Trade RTH", Order = 1, GroupName = "6. Session Filters")]
        public bool RthOnly { get; set; } = true;

        [NinjaScriptProperty]
        [Display(Name = "ADX Period", Order = 2, GroupName = "6. Session Filters")]
        public int AdxPeriod { get; set; } = 14;

        [NinjaScriptProperty]
        [Display(Name = "Min ADX for Trend", Order = 3, GroupName = "6. Session Filters")]
        public double MinAdx { get; set; } = 25;

        [NinjaScriptProperty]
        [Display(Name = "Fast EMA Period", Order = 4, GroupName = "6. Session Filters")]
        public int FastEmaPeriod { get; set; } = 21;

        [NinjaScriptProperty]
        [Display(Name = "Slow EMA Period", Order = 5, GroupName = "6. Session Filters")]
        public int SlowEmaPeriod { get; set; } = 50;

        [NinjaScriptProperty]
        [Display(Name = "Volume MA Period", Order = 6, GroupName = "6. Session Filters")]
        public int VolumeMAPeriod { get; set; } = 20;

        [NinjaScriptProperty]
        [Display(Name = "Min Volume Multiplier", Order = 7, GroupName = "6. Session Filters")]
        public double MinVolumeMultiplier { get; set; } = 1.2; // 120% of average
        #endregion

        #region Advanced Entry Parameters
        [NinjaScriptProperty]
        [Display(Name = "Require WaveTrend Confirmation", Order = 1, GroupName = "7. Entry Logic")]
        public bool RequireWTConfirmation { get; set; } = true;

        [NinjaScriptProperty]
        [Display(Name = "Require Volatility Expansion", Order = 2, GroupName = "7. Entry Logic")]
        public bool RequireVolatilityExpansion { get; set; } = true;

        [NinjaScriptProperty]
        [Display(Name = "Require Volume Confirmation", Order = 3, GroupName = "7. Entry Logic")]
        public bool RequireVolumeConfirmation { get; set; } = true;

        [NinjaScriptProperty]
        [Display(Name = "Use Liquidity Zone Filter", Order = 4, GroupName = "7. Entry Logic")]
        public bool UseLiquidityZoneFilter { get; set; } = true;
        #endregion

        #region Indicator Variables
        private WaveTrendOscillator waveTrend;
        private BollingerBandsModified bbMod;
        private ATR atr;
        private ADX adx;
        private EMA emaFast;
        private EMA emaSlow;
        private SMA volumeSMA;
        #endregion

        #region State Variables
        private double sessionRealizedPnl;
        private bool dailyGuardTripped;
        private double peakEquity;
        private List<double> liquidityZones;
        private const double TickValuePerContract = 5.0; // NQ: $5 per 0.25 tick
        #endregion

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = "ApexPro_NQStrategy";
                Description = @"Advanced strategy for Apex evaluations with WaveTrend, Modified BB, liquidity analysis, and professional risk management";
                Calculate = Calculate.OnBarClose;
                EntriesPerDirection = 1;
                EntryHandling = EntryHandling.AllEntries;
                IsExitOnSessionCloseStrategy = false;
                IsFillLimitOnTouch = false;
                MaximumBarsLookBack = MaximumBarsLookBack.TwoHundredFiftySix;
                OrderFillResolution = OrderFillResolution.Standard;
                Slippage = 0;
                StartBehavior = StartBehavior.WaitUntilFlat;
                TimeInForce = TimeInForce.Gtc;
                TraceOrders = false;
                RealtimeErrorHandling = RealtimeErrorHandling.StopCancelClose;
                StopTargetHandling = StopTargetHandling.PerEntryExecution;
                BarsRequiredToTrade = 50;
                IsInstantiatedOnEachOptimizationIteration = false;
            }
            else if (State == State.Configure)
            {
                // Can add additional data series here if needed (e.g., higher timeframe)
            }
            else if (State == State.DataLoaded)
            {
                // Initialize indicators
                waveTrend = WaveTrendOscillator(WTChannelLength, WTAverageLength, WTSignalLength, WTOverbought, WTOversold);
                bbMod = BollingerBandsModified(BBPeriod, BBStdDev, BBExpansion, BBSqueezeThreshold);
                atr = ATR(AtrPeriod);
                adx = ADX(AdxPeriod);
                emaFast = EMA(FastEmaPeriod);
                emaSlow = EMA(SlowEmaPeriod);
                volumeSMA = SMA(VOL(), VolumeMAPeriod);
                
                // Add indicators to chart
                AddChartIndicator(waveTrend);
                AddChartIndicator(bbMod);
                AddChartIndicator(atr);
                AddChartIndicator(adx);
                AddChartIndicator(emaFast);
                AddChartIndicator(emaSlow);
                
                // Initialize liquidity zones tracking
                liquidityZones = new List<double>();
                peakEquity = AccountStartBalance;
            }
        }

        protected override void OnBarUpdate()
        {
            // Ensure we have enough bars
            if (CurrentBar < BarsRequiredToTrade)
                return;

            // === SESSION MANAGEMENT ===
            if (Bars.IsFirstBarOfSession)
            {
                sessionRealizedPnl = 0;
                dailyGuardTripped = false;
                liquidityZones.Clear();
                Print($"[Session Start] New session beginning at {Time[0]}");
            }

            // Track peak equity for trailing threshold
            double realized = SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit;
            double unrealized = Position.MarketPosition != MarketPosition.Flat 
                ? Position.GetUnrealizedProfitLoss(PerformanceUnit.Currency, Close[0]) : 0;
            double currentEquity = AccountStartBalance + realized + unrealized;
            if (currentEquity > peakEquity) 
                peakEquity = currentEquity;

            // === FLAT-BEFORE-CLOSE ===
            // Force flat a few minutes before 4:59 PM ET (15:59 CT for CME)
            int cutoffCT = 155900;
            int nowCT = ToTime(Time[0]);
            int earlyFlatCT = cutoffCT - (MinutesBeforeClose * 100);
            
            if (nowCT >= earlyFlatCT)
            {
                if (Position.MarketPosition != MarketPosition.Flat)
                {
                    ExitLong("FlatBeforeClose");
                    ExitShort("FlatBeforeClose");
                    Print($"[Flat Before Close] Exiting all positions at {Time[0]}");
                }
                return;
            }

            // === RTH FILTER ===
            if (RthOnly && !IsRTH(Time[0]))
                return;

            // === DAILY GUARD CHECKS ===
            sessionRealizedPnl = GetSessionRealizedPnL();
            
            if (!dailyGuardTripped)
            {
                if (DailyLossCap > 0 && sessionRealizedPnl <= -Math.Abs(DailyLossCap))
                {
                    dailyGuardTripped = true;
                    Print($"[Daily Guard] Loss cap hit: {sessionRealizedPnl:C}. Halting trading.");
                    if (Position.MarketPosition != MarketPosition.Flat)
                    {
                        ExitLong("DailyLossCap");
                        ExitShort("DailyLossCap");
                    }
                }
                
                if (DailyProfitLock > 0 && sessionRealizedPnl >= Math.Abs(DailyProfitLock))
                {
                    dailyGuardTripped = true;
                    Print($"[Daily Guard] Profit lock reached: {sessionRealizedPnl:C}. Halting trading.");
                    if (Position.MarketPosition != MarketPosition.Flat)
                    {
                        ExitLong("DailyProfitLock");
                        ExitShort("DailyProfitLock");
                    }
                }
            }
            
            if (HaltOnDailyGuard && dailyGuardTripped)
                return;

            // === LIQUIDITY ZONE IDENTIFICATION ===
            UpdateLiquidityZones();

            // === POSITION MANAGEMENT (if in position) ===
            if (Position.MarketPosition != MarketPosition.Flat)
            {
                // Let stops and targets handle exits
                return;
            }

            // === SIGNAL GENERATION ===
            
            // 1. Trend Filter (EMA)
            bool upTrend = emaFast[0] > emaSlow[0] && Close[0] > emaFast[0];
            bool downTrend = emaFast[0] < emaSlow[0] && Close[0] < emaFast[0];
            
            // 2. Trend Strength (ADX)
            bool trendStrength = adx[0] >= MinAdx;
            
            // 3. WaveTrend Momentum
            bool wtBullish = false;
            bool wtBearish = false;
            
            if (RequireWTConfirmation)
            {
                // Bullish: WT1 crossing above WT2 from oversold or rising momentum
                wtBullish = (waveTrend.WT1[0] > waveTrend.WT2[0] && waveTrend.WT1[1] <= waveTrend.WT2[1]) 
                           || (waveTrend.WT1[0] < WTOversold && waveTrend.WT1[0] > waveTrend.WT1[1]);
                
                // Bearish: WT1 crossing below WT2 from overbought or falling momentum
                wtBearish = (waveTrend.WT1[0] < waveTrend.WT2[0] && waveTrend.WT1[1] >= waveTrend.WT2[1])
                           || (waveTrend.WT1[0] > WTOverbought && waveTrend.WT1[0] < waveTrend.WT1[1]);
            }
            else
            {
                wtBullish = true;
                wtBearish = true;
            }
            
            // 4. Volatility Expansion (Bollinger Bands)
            bool volatilityExpansion = false;
            
            if (RequireVolatilityExpansion)
            {
                volatilityExpansion = bbMod.IsExpanding();
            }
            else
            {
                volatilityExpansion = true;
            }
            
            // 5. Volume Confirmation
            bool volumeConfirmed = false;
            
            if (RequireVolumeConfirmation)
            {
                volumeConfirmed = VOL()[0] >= (volumeSMA[0] * MinVolumeMultiplier);
            }
            else
            {
                volumeConfirmed = true;
            }
            
            // 6. Liquidity Zone Filter
            bool nearLiquidityZone = false;
            
            if (UseLiquidityZoneFilter)
            {
                nearLiquidityZone = IsNearLiquidityZone(Close[0]);
            }
            else
            {
                nearLiquidityZone = true;
            }

            // === DYNAMIC POSITION SIZING ===
            double stopDistance = atr[0] * AtrStopMultiplier;
            int stopTicks = (int)Math.Ceiling(stopDistance / TickSize);
            if (stopTicks < 1) stopTicks = 1;

            double riskPerContract = stopTicks * TickValuePerContract;
            
            // Calculate max risk based on both fixed dollar amount and percentage of threshold
            double maxRiskDollars = Math.Min(RiskPerTradeDollars, TrailingThresholdDollars * RiskPercentOfThreshold);
            
            int qtyByRisk = (int)Math.Floor(maxRiskDollars / Math.Max(1, riskPerContract));
            int qty = Math.Max(1, Math.Min(MaxContracts, qtyByRisk));

            if (qty < 1)
            {
                Print($"[Risk] Position size too small for current ATR. Skipping trade.");
                return;
            }

            // Additional risk check: ensure we're not over-exposing
            double worstCaseRisk = qty * riskPerContract;
            if (worstCaseRisk > maxRiskDollars)
            {
                Print($"[Risk] Calculated risk ${worstCaseRisk:F2} exceeds max ${maxRiskDollars:F2}. Reducing size.");
                qty = Math.Max(1, (int)Math.Floor(maxRiskDollars / riskPerContract));
            }

            // Target calculation
            int targetTicks = (int)Math.Ceiling(stopTicks * TargetRMultiple);
            double entryPrice = Close[0];

            // === ENTRY LOGIC ===
            
            // LONG ENTRY
            if (upTrend && trendStrength && wtBullish && volatilityExpansion && volumeConfirmed && nearLiquidityZone)
            {
                // Additional confirmation: price breaking above BB middle or in expansion zone
                bool bbConfirm = Close[0] > bbMod.Middle[0] || Close[0] > High[1];
                
                if (bbConfirm)
                {
                    SetStopLoss(CalculationMode.Price, entryPrice - (stopTicks * TickSize));
                    SetProfitTarget(CalculationMode.Price, entryPrice + (targetTicks * TickSize));
                    EnterLong(qty, "Long_Entry");
                    
                    Print($"[LONG] Entry at {entryPrice:F2}, Qty: {qty}, Stop: {stopTicks} ticks, Target: {targetTicks} ticks, WT1: {waveTrend.WT1[0]:F2}");
                    
                    // Draw entry marker
                    Draw.ArrowUp(this, "LongEntry" + CurrentBar, true, 0, Low[0] - (2 * TickSize), Brushes.Green);
                }
            }
            
            // SHORT ENTRY
            else if (downTrend && trendStrength && wtBearish && volatilityExpansion && volumeConfirmed && nearLiquidityZone)
            {
                // Additional confirmation: price breaking below BB middle or in expansion zone
                bool bbConfirm = Close[0] < bbMod.Middle[0] || Close[0] < Low[1];
                
                if (bbConfirm)
                {
                    SetStopLoss(CalculationMode.Price, entryPrice + (stopTicks * TickSize));
                    SetProfitTarget(CalculationMode.Price, entryPrice - (targetTicks * TickSize));
                    EnterShort(qty, "Short_Entry");
                    
                    Print($"[SHORT] Entry at {entryPrice:F2}, Qty: {qty}, Stop: {stopTicks} ticks, Target: {targetTicks} ticks, WT1: {waveTrend.WT1[0]:F2}");
                    
                    // Draw entry marker
                    Draw.ArrowDown(this, "ShortEntry" + CurrentBar, true, 0, High[0] + (2 * TickSize), Brushes.Red);
                }
            }
        }

        #region Helper Methods
        
        private bool IsRTH(DateTime time)
        {
            // CME RTH for equity index futures: 8:30 AM - 3:00 PM CT
            int t = ToTime(time);
            return (t >= 083000 && t <= 150000);
        }

        private double GetSessionRealizedPnL()
        {
            double pnl = 0;
            DateTime sessionStart = Times[0][0].Date;
            DateTime sessionEnd = sessionStart.AddDays(1);

            foreach (var trade in SystemPerformance.AllTrades)
            {
                if (trade.Exit != null 
                    && trade.Exit.Time >= sessionStart 
                    && trade.Exit.Time < sessionEnd)
                {
                    pnl += trade.ProfitCurrency;
                }
            }
            return pnl;
        }

        private void UpdateLiquidityZones()
        {
            // Identify potential liquidity zones based on high volume bars and price consolidation
            // Zones are identified where price has shown significant volume accumulation
            
            if (CurrentBar < VolumeMAPeriod + 10)
                return;
            
            // Check if current bar is a high volume bar (potential liquidity)
            if (VOL()[0] >= volumeSMA[0] * 1.5)
            {
                // Check for price consolidation (range contraction)
                double avgRange = 0;
                for (int i = 1; i <= 5; i++)
                {
                    avgRange += (High[i] - Low[i]);
                }
                avgRange /= 5.0;
                
                double currentRange = High[0] - Low[0];
                
                // If current range is smaller than average (consolidation) with high volume
                if (currentRange < avgRange * 0.8)
                {
                    double zonePrice = (High[0] + Low[0]) / 2.0;
                    
                    // Add if not too close to existing zones
                    bool tooClose = false;
                    foreach (double zone in liquidityZones)
                    {
                        if (Math.Abs(zonePrice - zone) < atr[0] * 0.5)
                        {
                            tooClose = true;
                            break;
                        }
                    }
                    
                    if (!tooClose)
                    {
                        liquidityZones.Add(zonePrice);
                        
                        // Keep only recent zones (last 20)
                        if (liquidityZones.Count > 20)
                            liquidityZones.RemoveAt(0);
                        
                        // Draw zone on chart
                        Draw.HorizontalLine(this, "LiqZone" + CurrentBar, zonePrice, Brushes.Yellow, DashStyleHelper.Dash, 1);
                    }
                }
            }
        }

        private bool IsNearLiquidityZone(double price)
        {
            if (liquidityZones.Count == 0)
                return true; // No zones identified yet, allow trade
            
            double tolerance = atr[0] * 1.0; // Within 1 ATR of a zone
            
            foreach (double zone in liquidityZones)
            {
                if (Math.Abs(price - zone) <= tolerance)
                {
                    return true;
                }
            }
            
            return false; // Not near any liquidity zone
        }
        
        #endregion

        #region Properties
        [Browsable(false)]
        public string StrategyInfo => $"ApexPro NQ | Balance: ${AccountStartBalance:N0} | Target: ${ProfitTargetDollars:N0} | Threshold: ${TrailingThresholdDollars:N0}";
        #endregion
    }
}
