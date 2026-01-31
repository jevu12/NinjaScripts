# NinjaScripts

A collection of custom trading strategies built for NinjaTrader 8 using NinjaScript (C#). These scripts are designed to automate entries, exits, and risk management for futures trading, with a focus on compliance with prop firm evaluation rules (Apex, TopStep, etc.).

📌 Features

**ApexPro_NQStrategy - Advanced Features:**
- Professional-level indicators (WaveTrend Oscillator, Modified Bollinger Bands)
- Institutional-grade liquidity and order flow analysis
- Multi-layer confirmation system for high-probability trades
- Advanced volatility expansion detection
- Dynamic position sizing with dual risk limits
- Complete Apex Trader Funding compliance
- Session-based risk management

**General Features:**
- Pre-built strategies for futures trading (NQ, ES, YM, etc.)
- Risk management tools: ATR stops, trailing stops, daily loss limits
- Auto-flatten logic for prop firm compliance
- Entry logic examples: moving averages, momentum indicators, volume filters
- Modular structure for easy customization
- Optimized for prop firm evaluations (Apex, TopStep, etc.)

⚡ Installation

**For ApexPro_NQStrategy (Advanced):**

1. Download these 3 files:
   - `ApexPro_NQStrategy.cs` (main strategy)
   - `WaveTrendOscillator.cs` (custom indicator)
   - `BollingerBandsModified.cs` (custom indicator)

2. Open NinjaTrader 8 → Tools → Import → NinjaScript Add-On

3. Import each file one at a time

4. Compile: Tools → NinjaScript Editor → F5

5. Apply to chart: Right-click chart → Strategies → Add → ApexPro_NQStrategy

6. **See [Quick Start Guide](QUICK_START.md) for detailed setup**

**For Other Strategies:**

Open NinjaTrader 8 → Tools → Import → NinjaScript Add-On → Select .cs file → Compile (F5) → Apply to chart

🛠 Usage

Load a strategy from Strategies tab on your chart.

Adjust parameters (ATR period, stop loss, profit target, etc.) in the properties window.

Enable live simulation or real trading (at your own risk).

📖 Available Strategies

**ApexPro_NQStrategy** (NEW) → Advanced institutional-grade strategy for Apex Trader Funding evaluations
- WaveTrend Oscillator for momentum confirmation
- Modified Bollinger Bands for volatility expansion
- Liquidity zone analysis and order flow dynamics
- Multi-layer entry confirmation system
- Dynamic position sizing with ATR-based stops
- Complete Apex compliance (daily guards, flat-before-close)
- Optimized for NQ futures on 1-5 minute timeframes
- [📘 Full Documentation](APEXPRO_DOCUMENTATION.md) | [⚡ Quick Start Guide](QUICK_START.md)

**ApexNQ_EvalGuard** → Conservative strategy with Apex evaluation risk guards
- ATR-based stops with EMA/ADX trend filtering
- Daily loss cap and profit lock protections
- Flat-before-close automation
- Per-trade risk sizing
- Simple, proven approach for passing evaluations

⚠️ Disclaimer

These strategies are provided for educational purposes only. Trading futures and derivatives involves substantial risk of loss and is not suitable for all investors. Use these scripts at your own discretion and always test in simulation before live trading.

🤝 Contributions

Pull requests are welcome for bug fixes, new setups, or optimization improvements.

Open an issue if you find a problem or have a feature request.
