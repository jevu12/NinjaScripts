# ApexPro_NQStrategy Documentation

## Overview

**ApexPro_NQStrategy** is an advanced NinjaTrader 8 trading strategy specifically designed for passing prop firm evaluations like Apex Trader Funding. It employs professional-level indicators and institutional-grade precision for high-probability trade identification.

## Key Features

### 1. Advanced Indicators
- **WaveTrend Oscillator (WTO)**: Custom momentum oscillator providing cleaner signals than traditional RSI
  - Dual EMA smoothing for precise momentum detection
  - Identifies exhaustion points and momentum reversals
  - Configurable overbought/oversold levels

- **Modified Bollinger Bands**: Enhanced volatility indicator with expansion detection
  - Dynamic standard deviation multipliers
  - Squeeze detection for consolidation phases
  - Expansion bands for breakout identification

### 2. Liquidity and Order Flow Analysis
- Automatic liquidity zone identification based on volume and price consolidation
- High-volume bar detection for institutional activity
- Zone filtering to prioritize trades near significant price levels

### 3. Multi-Layer Entry Confirmation
The strategy requires multiple confirmations before entering trades:
- **Trend Filter**: EMA crossover and price position relative to EMAs
- **Trend Strength**: ADX threshold to avoid choppy markets
- **Momentum**: WaveTrend crossovers and momentum direction
- **Volatility**: Bollinger Band expansion for breakout mechanics
- **Volume**: Above-average volume confirmation
- **Liquidity**: Price near identified liquidity zones

### 4. Apex Compliance Features
- **Daily Loss Cap**: Automatic halt when daily loss limit is reached
- **Daily Profit Lock**: Stops trading after reaching daily profit target
- **Flat-Before-Close**: Automatically exits all positions minutes before session close
- **Trailing Threshold Protection**: Conservative risk sizing to protect against drawdown rules

### 5. Dynamic Position Sizing
- Risk-based position sizing using ATR
- Dual risk limits: fixed dollar amount and percentage of trailing threshold
- Automatic adjustment based on market volatility
- Maximum contract limits for risk control

## Installation

1. **Download Files**:
   - `ApexPro_NQStrategy.cs` (Main strategy)
   - `WaveTrendOscillator.cs` (Custom indicator)
   - `BollingerBandsModified.cs` (Custom indicator)

2. **Import to NinjaTrader 8**:
   - Open NinjaTrader 8
   - Go to Tools → Import → NinjaScript Add-On
   - Select each .cs file and import
   - Go to Tools → NinjaScript Editor → Compile (F5)

3. **Apply to Chart**:
   - Right-click on chart → Strategies → Add strategy
   - Select "ApexPro_NQStrategy" from the list
   - Configure parameters (see below)

## Parameter Configuration

### 1. Apex Plan Parameters
Configure these based on your specific Apex evaluation plan:

| Parameter | Default | Description |
|-----------|---------|-------------|
| Account Start Balance | $50,000 | Initial account balance for your plan |
| Profit Target | $3,000 | Required profit to pass evaluation |
| Trailing Threshold | $2,500 | Maximum allowed drawdown from peak |
| Daily Loss Cap | $600 | Maximum daily loss before halting |
| Daily Profit Lock | $800 | Daily profit target before halting |
| Halt After Daily Guard | true | Stop all trading when guard triggers |

**Common Apex Plans**:
- **25K Plan**: Balance $25,000, Target $1,500, Threshold $1,250
- **50K Plan**: Balance $50,000, Target $3,000, Threshold $2,500
- **100K Plan**: Balance $100,000, Target $6,000, Threshold $5,000

### 2. Position Sizing Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| Max Contracts | 2 | Maximum contracts per trade |
| Risk Per Trade | $750 | Maximum dollar risk per trade |
| Risk % of Threshold | 30% | Max risk as percentage of threshold |

**Recommendations**:
- Start with 1-2 contracts during evaluation
- Keep risk per trade at 20-30% of trailing threshold
- Increase gradually as you build cushion

### 3. WaveTrend Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| Channel Length | 10 | Period for channel calculation |
| Average Length | 21 | Smoothing period for momentum |
| Signal Length | 4 | Period for signal line |
| Overbought Level | 60 | Upper threshold for momentum |
| Oversold Level | -60 | Lower threshold for momentum |

**Tuning Tips**:
- Lower Channel Length (8-10) for faster signals
- Higher Channel Length (12-15) for smoother signals
- Adjust Overbought/Oversold for different markets

### 4. Bollinger Bands Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| Period | 20 | Moving average period |
| Std Dev Multiplier | 2.0 | Standard band width |
| Expansion Multiplier | 2.5 | Expansion band width |
| Squeeze Threshold | 0.02 | Bandwidth threshold for squeeze |

**Usage Notes**:
- Standard settings work well for NQ on 1-5min charts
- Increase Period (25-30) for less sensitive signals
- Adjust Expansion Multiplier based on volatility regime

### 5. Risk Management Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| ATR Period | 14 | Period for volatility calculation |
| ATR Stop Multiplier | 2.5 | Stop distance in ATR multiples |
| Target R Multiple | 2.0 | Profit target as R multiple |
| Flat Before Close | 2 | Minutes before close to exit |

**Risk Guidelines**:
- 2.0-2.5 ATR stop provides good balance
- 1.5-2.0 R multiple recommended for prop evaluations
- Increase "Flat Before Close" to 5-10 minutes for safety

### 6. Session and Filter Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| Only Trade RTH | true | Trade only regular hours (8:30-3:00 CT) |
| Min ADX | 25 | Minimum ADX for trend strength |
| Fast EMA | 21 | Fast EMA period for trend |
| Slow EMA | 50 | Slow EMA period for trend |
| Volume MA Period | 20 | Period for volume average |
| Min Volume Multiplier | 1.2 | Required volume vs average (120%) |

**Session Recommendations**:
- RTH only recommended for Apex evaluations
- Min ADX of 20-30 filters choppy markets
- Volume filter helps avoid low-liquidity traps

### 7. Entry Logic Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| Require WT Confirmation | true | Must have WaveTrend signal |
| Require Volatility Expansion | true | Must have BB expansion |
| Require Volume Confirmation | true | Must have above-average volume |
| Use Liquidity Zone Filter | true | Must be near liquidity zone |

**Conservative vs Aggressive**:
- **Conservative**: Enable all confirmations (default)
- **Aggressive**: Disable 1-2 confirmations for more trades
- **Very Conservative**: Increase thresholds and enable all filters

## Trading Sessions

### Regular Trading Hours (RTH)
- **NQ RTH**: 8:30 AM - 3:00 PM CT (Monday-Friday)
- **Recommended**: Trade only RTH for Apex evaluations
- **Avoid**: First 15-30 minutes for settlement volatility

### Optimal Trading Windows
1. **9:30-11:00 AM CT**: High volume, strong trends
2. **1:00-2:30 PM CT**: Afternoon session moves
3. **Avoid 11:00-1:00 PM CT**: Lunch chop, lower volume

## Strategy Logic Flow

### Entry Process
1. **Check Session**: Verify RTH and time before close
2. **Check Guards**: Ensure daily limits not exceeded
3. **Identify Trend**: Fast EMA > Slow EMA (bullish) or vice versa
4. **Confirm Strength**: ADX > minimum threshold
5. **Check Momentum**: WaveTrend crossover or extreme reading
6. **Detect Volatility**: Bollinger Band expansion
7. **Verify Volume**: Above 120% of 20-period average
8. **Check Liquidity**: Price near identified zone
9. **Calculate Size**: Dynamic sizing based on ATR and risk parameters
10. **Enter Trade**: Set stop loss and profit target
11. **Monitor**: Track unrealized P&L against thresholds

### Exit Process
- **Profit Target**: Automatic exit at Target R Multiple
- **Stop Loss**: Automatic exit at ATR-based stop
- **Daily Guards**: Exit if daily loss/profit limits reached
- **Flat-Before-Close**: Exit all positions before session end
- **Manual Override**: Can manually exit via platform

## Risk Management

### Daily Risk Controls
The strategy implements multiple layers of daily protection:

1. **Daily Loss Cap** ($600 default)
   - Tracks realized P&L for the session
   - Automatically exits and halts when reached
   - Protects trailing threshold from single-day damage

2. **Daily Profit Lock** ($800 default)
   - Locks in profits after strong session
   - Prevents giving back gains in late trading
   - Ensures consistent progress toward target

3. **Trailing Threshold Awareness**
   - Tracks peak equity throughout evaluation
   - Limits per-trade risk to percentage of threshold
   - Double-checks position size against worst-case scenarios

### Per-Trade Risk Controls
1. **ATR-Based Stops**: Dynamic stops adapt to market volatility
2. **Position Sizing**: Automatically scales based on risk parameters
3. **Max Contracts**: Hard limit prevents over-leveraging
4. **Risk Verification**: Double-checks before entry

### Session Risk Controls
1. **Flat-Before-Close**: Ensures no overnight risk
2. **RTH Only**: Avoids illiquid overnight sessions
3. **Volume Filter**: Prevents trading in low liquidity
4. **Trend Filter**: Reduces chop and whipsaw trades

## Performance Tips

### For Evaluation Success
1. **Start Conservative**: Use default parameters first
2. **One Contract**: Begin with single contracts
3. **Monitor Daily**: Track daily P&L carefully
4. **Respect Guards**: Let guards protect you
5. **Build Cushion**: Scale up after establishing profit buffer

### Optimization Guidelines
1. **Backtest First**: Test on historical data before live
2. **Forward Test**: Paper trade for 1-2 weeks minimum
3. **Small Tweaks**: Make incremental parameter changes
4. **Track Metrics**: Monitor win rate, R-multiple, drawdown
5. **Session Analysis**: Note which sessions perform best

### Common Pitfalls to Avoid
1. **Over-Trading**: Let the strategy filter, don't force trades
2. **Parameter Fitting**: Don't over-optimize to past data
3. **Ignoring Guards**: Always respect daily limits
4. **Too Aggressive**: Start conservative, scale gradually
5. **No Testing**: Always backtest and paper trade first

## Monitoring and Logging

The strategy provides detailed logging for monitoring:

### Log Messages
- `[Session Start]`: New trading session beginning
- `[LONG]` / `[SHORT]`: Entry signals with details
- `[Daily Guard]`: Daily limit triggers
- `[Flat Before Close]`: End-of-session exit
- `[Risk]`: Position sizing decisions

### Chart Indicators
- **Green Arrow**: Long entry marker
- **Red Arrow**: Short entry marker
- **Yellow Dashed Lines**: Liquidity zones
- **Bollinger Bands**: Volatility visualization
- **WaveTrend Panel**: Momentum oscillator (separate panel)

## Troubleshooting

### No Trades Executing
1. Check all confirmations are being met
2. Verify ATR not too wide for risk parameters
3. Ensure within RTH if enabled
4. Check daily guards haven't triggered
5. Verify liquidity zones are being identified

### Too Many Trades
1. Enable more confirmations (WT, volume, liquidity)
2. Increase Min ADX threshold
3. Reduce trading hours to optimal windows
4. Increase Min Volume Multiplier

### Stops Too Wide/Narrow
1. Adjust ATR Stop Multiplier (2.0-3.0 range)
2. Modify ATR Period for different sensitivity
3. Check if position sizing is limiting entries

### Strategy Not Compiling
1. Ensure all three files imported correctly
2. Verify NinjaTrader 8 (not NT7)
3. Check for any syntax errors in NinjaScript Editor
4. Try Tools → NinjaScript Editor → File → Utilities → Remove Unused → OK → Compile

## Best Practices

### Pre-Market Routine
1. Review previous day's performance
2. Check market news and calendar
3. Verify strategy parameters correct for your plan
4. Ensure "Halt on Daily Guard" is enabled
5. Start strategy at market open (8:30 AM CT)

### During Market Hours
1. Monitor position entries and exits
2. Track unrealized P&L against thresholds
3. Watch for daily guard triggers
4. Note market conditions and behavior
5. Avoid manual overrides unless necessary

### Post-Market Routine
1. Review all trades in Trade Performance
2. Analyze log messages for insights
3. Check daily P&L against guards
4. Note any parameter adjustments needed
5. Plan for next session

## Version History

### Version 1.0 (Current)
- Initial release with full feature set
- WaveTrend Oscillator integration
- Modified Bollinger Bands with expansion detection
- Liquidity zone identification
- Multi-layer confirmation system
- Complete Apex compliance features
- Dynamic position sizing
- Comprehensive risk management

## Support and Disclaimer

### Educational Use
This strategy is provided for educational purposes only. It demonstrates advanced NinjaScript techniques and trading concepts.

### Risk Warning
Trading futures involves substantial risk of loss and is not suitable for all investors. Past performance is not indicative of future results. Always test strategies in simulation before live trading.

### Apex Compliance
This strategy implements approximations of Apex rules using local calculations. Always monitor your actual Apex dashboard (R|Trader/Tradovate) as the authoritative source for evaluation status.

### Customization
Users are encouraged to:
- Backtest with their specific instruments and timeframes
- Adjust parameters for their risk tolerance
- Add additional filters or confirmations
- Modify for their trading style

## Additional Resources

### Recommended Reading
- NinjaTrader 8 Strategy Development documentation
- Apex Trader Funding rules and requirements
- WaveTrend Oscillator methodology
- Bollinger Band trading strategies
- ATR-based position sizing

### Testing Recommendations
1. **Backtest Period**: Minimum 6 months historical data
2. **Replay Mode**: Use Market Replay for realistic testing
3. **Paper Trading**: 2-4 weeks minimum before live
4. **Small Size**: Start with 1 contract in live evaluation
5. **Daily Review**: Track performance metrics daily

---

**Strategy Developer Note**: This strategy represents Phase 1 implementation of the ApexPro system. Future enhancements may include adaptive parameter optimization, machine learning integration, and multi-timeframe analysis.

For questions, issues, or contributions, please use the GitHub repository issue tracker.
