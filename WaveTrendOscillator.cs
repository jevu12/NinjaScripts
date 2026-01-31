// WaveTrend Oscillator (WTO) - Custom momentum indicator for NinjaTrader 8
// This indicator provides cleaner signals than RSI by using dual EMA smoothing
// and detecting momentum exhaustion points with precision.
// Usage: Identify overbought/oversold conditions and momentum reversals.

#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    public class WaveTrendOscillator : Indicator
    {
        private Series<double> hlc3;
        private Series<double> esa;
        private Series<double> d;
        private Series<double> ci;
        private Series<double> tci;
        private Series<double> wt1;
        
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Channel Length", Order = 1, GroupName = "Parameters")]
        public int ChannelLength { get; set; }
        
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Average Length", Order = 2, GroupName = "Parameters")]
        public int AverageLength { get; set; }
        
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Signal Length", Order = 3, GroupName = "Parameters")]
        public int SignalLength { get; set; }
        
        [NinjaScriptProperty]
        [Range(30, int.MaxValue)]
        [Display(Name = "Overbought Level", Order = 4, GroupName = "Levels")]
        public double OverboughtLevel { get; set; }
        
        [NinjaScriptProperty]
        [Range(-100, 0)]
        [Display(Name = "Oversold Level", Order = 5, GroupName = "Levels")]
        public double OversoldLevel { get; set; }
        
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"WaveTrend Oscillator - Advanced momentum indicator with dual smoothing";
                Name = "WaveTrendOscillator";
                Calculate = Calculate.OnBarClose;
                IsOverlay = false;
                DisplayInDataBox = true;
                DrawOnPricePanel = false;
                DrawHorizontalGridLines = true;
                DrawVerticalGridLines = true;
                PaintPriceMarkers = true;
                ScaleJustification = NinjaTrader.Gui.Chart.ScaleJustification.Right;
                IsSuspendedWhileInactive = true;
                
                // Default parameters based on standard WaveTrend settings
                ChannelLength = 10;
                AverageLength = 21;
                SignalLength = 4;
                OverboughtLevel = 60;
                OversoldLevel = -60;
                
                AddPlot(new Stroke(Brushes.Cyan, 2), PlotStyle.Line, "WT1");
                AddPlot(new Stroke(Brushes.Red, 2), PlotStyle.Line, "WT2");
                AddLine(new Stroke(Brushes.Green, DashStyleHelper.Dash), OverboughtLevel, "Overbought");
                AddLine(new Stroke(Brushes.Red, DashStyleHelper.Dash), OversoldLevel, "Oversold");
                AddLine(new Stroke(Brushes.Gray, DashStyleHelper.Dot), 0, "Zero");
            }
            else if (State == State.Configure)
            {
            }
            else if (State == State.DataLoaded)
            {
                hlc3 = new Series<double>(this);
                esa = new Series<double>(this);
                d = new Series<double>(this);
                ci = new Series<double>(this);
                tci = new Series<double>(this);
                wt1 = new Series<double>(this);
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < AverageLength)
                return;
            
            // Calculate HLC3 (average of high, low, close)
            hlc3[0] = (High[0] + Low[0] + Close[0]) / 3.0;
            
            // ESA: Exponential moving average of source (HLC3)
            if (CurrentBar == 0)
            {
                esa[0] = hlc3[0];
            }
            else
            {
                double alpha = 2.0 / (ChannelLength + 1.0);
                esa[0] = alpha * hlc3[0] + (1.0 - alpha) * esa[1];
            }
            
            // D: Absolute difference between source and ESA, exponentially smoothed
            double absDiff = Math.Abs(hlc3[0] - esa[0]);
            if (CurrentBar == 0)
            {
                d[0] = absDiff;
            }
            else
            {
                double alpha = 2.0 / (ChannelLength + 1.0);
                d[0] = alpha * absDiff + (1.0 - alpha) * d[1];
            }
            
            // CI: Channel Index
            if (d[0] != 0)
            {
                ci[0] = (hlc3[0] - esa[0]) / (0.015 * d[0]);
            }
            else
            {
                ci[0] = 0;
            }
            
            // TCI: Exponentially smoothed CI
            if (CurrentBar == 0)
            {
                tci[0] = ci[0];
            }
            else
            {
                double alpha = 2.0 / (AverageLength + 1.0);
                tci[0] = alpha * ci[0] + (1.0 - alpha) * tci[1];
            }
            
            // WT1: Main line (TCI)
            wt1[0] = tci[0];
            
            // WT2: Signal line (SMA of WT1)
            double wt2Value = 0;
            if (CurrentBar >= SignalLength - 1)
            {
                double sum = 0;
                for (int i = 0; i < SignalLength; i++)
                {
                    sum += wt1[i];
                }
                wt2Value = sum / SignalLength;
            }
            else
            {
                wt2Value = wt1[0];
            }
            
            Values[0][0] = wt1[0];  // WT1
            Values[1][0] = wt2Value; // WT2
        }

        #region Properties
        [Browsable(false)]
        [XmlIgnore]
        public Series<double> WT1
        {
            get { return Values[0]; }
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> WT2
        {
            get { return Values[1]; }
        }
        #endregion
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private WaveTrendOscillator[] cacheWaveTrendOscillator;
		public WaveTrendOscillator WaveTrendOscillator(int channelLength, int averageLength, int signalLength, double overboughtLevel, double oversoldLevel)
		{
			return WaveTrendOscillator(Input, channelLength, averageLength, signalLength, overboughtLevel, oversoldLevel);
		}

		public WaveTrendOscillator WaveTrendOscillator(ISeries<double> input, int channelLength, int averageLength, int signalLength, double overboughtLevel, double oversoldLevel)
		{
			if (cacheWaveTrendOscillator != null)
				for (int idx = 0; idx < cacheWaveTrendOscillator.Length; idx++)
					if (cacheWaveTrendOscillator[idx] != null && cacheWaveTrendOscillator[idx].ChannelLength == channelLength && cacheWaveTrendOscillator[idx].AverageLength == averageLength && cacheWaveTrendOscillator[idx].SignalLength == signalLength && cacheWaveTrendOscillator[idx].OverboughtLevel == overboughtLevel && cacheWaveTrendOscillator[idx].OversoldLevel == oversoldLevel && cacheWaveTrendOscillator[idx].EqualsInput(input))
						return cacheWaveTrendOscillator[idx];
			return CacheIndicator<WaveTrendOscillator>(new WaveTrendOscillator(){ ChannelLength = channelLength, AverageLength = averageLength, SignalLength = signalLength, OverboughtLevel = overboughtLevel, OversoldLevel = oversoldLevel }, input, ref cacheWaveTrendOscillator);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.WaveTrendOscillator WaveTrendOscillator(int channelLength, int averageLength, int signalLength, double overboughtLevel, double oversoldLevel)
		{
			return indicator.WaveTrendOscillator(Input, channelLength, averageLength, signalLength, overboughtLevel, oversoldLevel);
		}

		public Indicators.WaveTrendOscillator WaveTrendOscillator(ISeries<double> input , int channelLength, int averageLength, int signalLength, double overboughtLevel, double oversoldLevel)
		{
			return indicator.WaveTrendOscillator(input, channelLength, averageLength, signalLength, overboughtLevel, oversoldLevel);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.WaveTrendOscillator WaveTrendOscillator(int channelLength, int averageLength, int signalLength, double overboughtLevel, double oversoldLevel)
		{
			return indicator.WaveTrendOscillator(Input, channelLength, averageLength, signalLength, overboughtLevel, oversoldLevel);
		}

		public Indicators.WaveTrendOscillator WaveTrendOscillator(ISeries<double> input , int channelLength, int averageLength, int signalLength, double overboughtLevel, double oversoldLevel)
		{
			return indicator.WaveTrendOscillator(input, channelLength, averageLength, signalLength, overboughtLevel, oversoldLevel);
		}
	}
}

#endregion
