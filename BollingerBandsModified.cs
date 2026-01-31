// Modified Bollinger Bands - Enhanced volatility indicator for NinjaTrader 8
// This indicator uses dynamic standard deviation multipliers and includes
// additional squeeze detection for identifying volatility expansion phases.
// Usage: Detect volatility expansion for breakout mechanics and squeeze conditions.

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
    public class BollingerBandsModified : Indicator
    {
        private SMA sma;
        private StdDev stdDev;
        private Series<double> bandwidth;
        
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Period", Order = 1, GroupName = "Parameters")]
        public int Period { get; set; }
        
        [NinjaScriptProperty]
        [Range(0.1, double.MaxValue)]
        [Display(Name = "Std Dev Multiplier", Order = 2, GroupName = "Parameters")]
        public double StdDevMultiplier { get; set; }
        
        [NinjaScriptProperty]
        [Range(0.1, double.MaxValue)]
        [Display(Name = "Expansion Multiplier", Order = 3, GroupName = "Parameters")]
        public double ExpansionMultiplier { get; set; }
        
        [NinjaScriptProperty]
        [Range(0.0, 1.0)]
        [Display(Name = "Squeeze Threshold", Order = 4, GroupName = "Parameters")]
        public double SqueezeThreshold { get; set; }
        
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"Modified Bollinger Bands with volatility expansion detection";
                Name = "BollingerBandsModified";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DisplayInDataBox = true;
                DrawOnPricePanel = true;
                DrawHorizontalGridLines = true;
                DrawVerticalGridLines = true;
                PaintPriceMarkers = true;
                ScaleJustification = NinjaTrader.Gui.Chart.ScaleJustification.Right;
                IsSuspendedWhileInactive = true;
                
                // Default parameters
                Period = 20;
                StdDevMultiplier = 2.0;
                ExpansionMultiplier = 2.5;
                SqueezeThreshold = 0.02; // 2% bandwidth threshold
                
                AddPlot(new Stroke(Brushes.Blue, 2), PlotStyle.Line, "Middle");
                AddPlot(new Stroke(Brushes.Red, 2), PlotStyle.Line, "Upper");
                AddPlot(new Stroke(Brushes.Green, 2), PlotStyle.Line, "Lower");
                AddPlot(new Stroke(Brushes.Orange, 2), PlotStyle.Line, "ExpansionUpper");
                AddPlot(new Stroke(Brushes.Purple, 2), PlotStyle.Line, "ExpansionLower");
            }
            else if (State == State.Configure)
            {
            }
            else if (State == State.DataLoaded)
            {
                sma = SMA(Period);
                stdDev = StdDev(Period);
                bandwidth = new Series<double>(this);
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < Period)
                return;
            
            double middle = sma[0];
            double stdDevValue = stdDev[0];
            
            // Standard Bollinger Bands
            double upper = middle + (StdDevMultiplier * stdDevValue);
            double lower = middle - (StdDevMultiplier * stdDevValue);
            
            // Expansion bands (wider for breakout detection)
            double expansionUpper = middle + (ExpansionMultiplier * stdDevValue);
            double expansionLower = middle - (ExpansionMultiplier * stdDevValue);
            
            // Calculate bandwidth (for squeeze detection)
            if (middle != 0)
            {
                bandwidth[0] = (upper - lower) / middle;
            }
            else
            {
                bandwidth[0] = 0;
            }
            
            Values[0][0] = middle;
            Values[1][0] = upper;
            Values[2][0] = lower;
            Values[3][0] = expansionUpper;
            Values[4][0] = expansionLower;
        }

        #region Properties
        [Browsable(false)]
        [XmlIgnore]
        public Series<double> Middle
        {
            get { return Values[0]; }
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> Upper
        {
            get { return Values[1]; }
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> Lower
        {
            get { return Values[2]; }
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> ExpansionUpper
        {
            get { return Values[3]; }
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> ExpansionLower
        {
            get { return Values[4]; }
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> Bandwidth
        {
            get { return bandwidth; }
        }
        
        // Helper method to detect if bands are in squeeze
        public bool IsInSqueeze()
        {
            if (CurrentBar < Period)
                return false;
            
            return bandwidth[0] < SqueezeThreshold;
        }
        
        // Helper method to detect volatility expansion
        public bool IsExpanding()
        {
            if (CurrentBar < Period + 1)
                return false;
            
            return bandwidth[0] > bandwidth[1] && bandwidth[1] < bandwidth[2];
        }
        
        // Helper method to detect breakout above upper band
        public bool IsBreakoutAbove(double price)
        {
            if (CurrentBar < Period)
                return false;
            
            return price > Values[1][0]; // Above upper band
        }
        
        // Helper method to detect breakout below lower band
        public bool IsBreakoutBelow(double price)
        {
            if (CurrentBar < Period)
                return false;
            
            return price < Values[2][0]; // Below lower band
        }
        #endregion
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private BollingerBandsModified[] cacheBollingerBandsModified;
		public BollingerBandsModified BollingerBandsModified(int period, double stdDevMultiplier, double expansionMultiplier, double squeezeThreshold)
		{
			return BollingerBandsModified(Input, period, stdDevMultiplier, expansionMultiplier, squeezeThreshold);
		}

		public BollingerBandsModified BollingerBandsModified(ISeries<double> input, int period, double stdDevMultiplier, double expansionMultiplier, double squeezeThreshold)
		{
			if (cacheBollingerBandsModified != null)
				for (int idx = 0; idx < cacheBollingerBandsModified.Length; idx++)
					if (cacheBollingerBandsModified[idx] != null && cacheBollingerBandsModified[idx].Period == period && cacheBollingerBandsModified[idx].StdDevMultiplier == stdDevMultiplier && cacheBollingerBandsModified[idx].ExpansionMultiplier == expansionMultiplier && cacheBollingerBandsModified[idx].SqueezeThreshold == squeezeThreshold && cacheBollingerBandsModified[idx].EqualsInput(input))
						return cacheBollingerBandsModified[idx];
			return CacheIndicator<BollingerBandsModified>(new BollingerBandsModified(){ Period = period, StdDevMultiplier = stdDevMultiplier, ExpansionMultiplier = expansionMultiplier, SqueezeThreshold = squeezeThreshold }, input, ref cacheBollingerBandsModified);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.BollingerBandsModified BollingerBandsModified(int period, double stdDevMultiplier, double expansionMultiplier, double squeezeThreshold)
		{
			return indicator.BollingerBandsModified(Input, period, stdDevMultiplier, expansionMultiplier, squeezeThreshold);
		}

		public Indicators.BollingerBandsModified BollingerBandsModified(ISeries<double> input , int period, double stdDevMultiplier, double expansionMultiplier, double squeezeThreshold)
		{
			return indicator.BollingerBandsModified(input, period, stdDevMultiplier, expansionMultiplier, squeezeThreshold);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.BollingerBandsModified BollingerBandsModified(int period, double stdDevMultiplier, double expansionMultiplier, double squeezeThreshold)
		{
			return indicator.BollingerBandsModified(Input, period, stdDevMultiplier, expansionMultiplier, squeezeThreshold);
		}

		public Indicators.BollingerBandsModified BollingerBandsModified(ISeries<double> input , int period, double stdDevMultiplier, double expansionMultiplier, double squeezeThreshold)
		{
			return indicator.BollingerBandsModified(input, period, stdDevMultiplier, expansionMultiplier, squeezeThreshold);
		}
	}
}

#endregion
