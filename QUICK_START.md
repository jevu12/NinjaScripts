# ApexPro_NQStrategy Quick Start Guide

## Installation (5 Minutes)

### Step 1: Download Files
Download these three files from the repository:
- `ApexPro_NQStrategy.cs`
- `WaveTrendOscillator.cs`
- `BollingerBandsModified.cs`

### Step 2: Import to NinjaTrader 8
1. Open NinjaTrader 8
2. Click **Tools → Import → NinjaScript Add-On**
3. Select `WaveTrendOscillator.cs` → Import
4. Select `BollingerBandsModified.cs` → Import
5. Select `ApexPro_NQStrategy.cs` → Import
6. Click **Tools → NinjaScript Editor** → Press **F5** to compile

### Step 3: Apply to Chart
1. Open a chart for **NQ** (E-mini Nasdaq-100)
2. Set timeframe to **3 minutes** (recommended for evaluation)
3. Right-click chart → **Strategies → Add Strategy**
4. Select **ApexPro_NQStrategy**
5. Click **OK**

## Quick Configuration for Apex 50K Plan

Use these settings for a standard Apex Trader Funding 50K evaluation:

### 1. Apex Plan Tab
- **Account Start Balance**: $50,000
- **Profit Target**: $3,000
- **Trailing Threshold**: $2,500
- **Daily Loss Cap**: $600
- **Daily Profit Lock**: $800
- **Halt After Daily Guard**: ✓ (checked)

### 2. Position Sizing Tab
- **Max Contracts**: 1 (conservative start)
- **Risk Per Trade**: $750
- **Risk % of Threshold**: 0.30 (30%)

### 3. Leave Other Tabs at Defaults
The default settings are optimized for NQ evaluation trading.

## Your First Trading Day

### Before Market Open (Before 8:30 AM CT)
1. ✓ Verify strategy is applied to NQ 3-min chart
2. ✓ Check parameters match your Apex plan
3. ✓ Enable the strategy (right-click chart → Strategies → Enable)
4. ✓ Verify "Calculate" is set to "On bar close"

### During Market Hours (8:30 AM - 3:57 PM CT)
The strategy will:
- Automatically identify high-probability setups
- Enter trades with defined stops and targets
- Monitor daily loss and profit limits
- Exit all positions 2 minutes before close (3:57 PM CT)

**You should**:
- Monitor the Apex dashboard (R|Trader/Tradovate)
- Watch for entry signals (green/red arrows on chart)
- Let the strategy manage trades automatically
- Avoid manual intervention unless emergency

### After Market Close
1. Review trades in **Trade Performance** window
2. Check daily P&L in strategy log
3. Note any adjustments needed for tomorrow
4. Verify account balance and drawdown on Apex dashboard

## Understanding Entry Signals

### Long Entry (Green Arrow) Requires:
- ✓ Fast EMA above Slow EMA (uptrend)
- ✓ Price above Fast EMA
- ✓ ADX above 25 (trending market)
- ✓ WaveTrend showing bullish momentum
- ✓ Bollinger Bands expanding (volatility)
- ✓ Volume above 120% of average
- ✓ Price near liquidity zone

### Short Entry (Red Arrow) Requires:
- ✓ Fast EMA below Slow EMA (downtrend)
- ✓ Price below Fast EMA
- ✓ ADX above 25 (trending market)
- ✓ WaveTrend showing bearish momentum
- ✓ Bollinger Bands expanding (volatility)
- ✓ Volume above 120% of average
- ✓ Price near liquidity zone

**Note**: All confirmations must align for an entry. This keeps trades high-probability.

## Daily Guard Protection

### Daily Loss Cap ($600)
- Strategy stops trading if session loss reaches $600
- Automatically exits open positions
- Protects your trailing threshold
- **Action**: Let it protect you, restart fresh tomorrow

### Daily Profit Lock ($800)
- Strategy stops trading after +$800 session profit
- Automatically exits open positions
- Locks in your daily gain
- **Action**: Celebrate! You're $800 closer to target

### Flat-Before-Close (2 minutes)
- All positions closed at 3:57 PM CT
- Ensures no overnight risk
- Required by Apex rules
- **Action**: None needed, automatic

## Common Scenarios

### Scenario 1: No Trades All Day
**Likely reasons**:
- Market is choppy (ADX < 25)
- No volatility expansion
- Volume too low
- No clear trend

**What to do**: Be patient. The strategy filters for quality, not quantity.

### Scenario 2: Hit Daily Loss Cap
**What happened**: Lost $600 in the session

**What to do**:
1. Strategy auto-halts trading
2. Review trades to understand what happened
3. Don't trade manually to "make it back"
4. Start fresh tomorrow with lessons learned

### Scenario 3: Hit Daily Profit Lock
**What happened**: Made $800 profit in the session

**What to do**:
1. Strategy auto-halts trading
2. Great job! You're $800 toward your $3,000 target
3. Let the lock protect your gains
4. Come back tomorrow to build on success

### Scenario 4: Strategy Keeps Getting Stopped Out
**Possible issues**:
- ATR Stop Multiplier too tight (try 2.5-3.0)
- Market very volatile today
- Wrong timeframe (try 3-5 min instead of 1 min)

**What to do**:
1. Review stop placement on chart
2. Consider slightly wider stop multiplier
3. Check if Market is unusually volatile today
4. May need to sit out highly volatile sessions

## Performance Expectations

### Realistic Targets for 50K Evaluation
- **Target**: $3,000 profit
- **Typical timeframe**: 5-15 trading days
- **Expected win rate**: 45-60%
- **Average daily gain**: $200-$400
- **Average trades/day**: 2-6 trades

### Key Metrics to Track
1. **Daily P&L**: Should be trending positive
2. **Win Rate**: Above 45% is good
3. **Average Winner**: Should be larger than average loser
4. **Max Drawdown**: Should stay well below $2,500 threshold
5. **Consistency**: Prefer steady gains over huge swings

## Troubleshooting Quick Fixes

| Problem | Quick Fix |
|---------|-----------|
| No trades executing | Lower Min ADX to 20, disable one confirmation |
| Too many losing trades | Increase Min ADX to 30, enable all confirmations |
| Stops too tight | Increase ATR Stop Multiplier to 2.5-3.0 |
| Stops too wide | Decrease ATR Stop Multiplier to 2.0-2.5 |
| Not enough profit per win | Increase Target R Multiple to 2.5 |
| Strategy won't compile | Re-import all 3 files, then compile |

## Safety Checklist

Before every trading session, verify:
- [ ] Strategy is applied to correct chart (NQ)
- [ ] Parameters match your Apex plan size
- [ ] "Halt on Daily Guard" is enabled
- [ ] RTH only is enabled (no overnight trading)
- [ ] Max contracts appropriate for account size
- [ ] Apex dashboard shows you're in good standing

## Progression Path

### Week 1: Conservative
- **Max Contracts**: 1
- **Goal**: Learn the strategy, small consistent gains
- **Risk Per Trade**: $500-$750

### Week 2: Building Confidence
- **Max Contracts**: 1-2
- **Goal**: Build profit cushion above threshold
- **Risk Per Trade**: $750

### Week 3+: Scaling Up
- **Max Contracts**: 2-3 (only if comfortable)
- **Goal**: Accelerate toward profit target
- **Risk Per Trade**: Up to $1,000

**Important**: Only scale up when you have a solid profit cushion above your trailing threshold.

## Emergency Procedures

### If Something Goes Wrong
1. **Disable Strategy**: Right-click chart → Strategies → Disable
2. **Flatten All**: Press F7 or use "Flatten Everything" button
3. **Review Logs**: Check NinjaScript Output window for errors
4. **Contact Support**: Check strategy log messages for clues

### If You're Approaching Trailing Threshold
1. **Reduce Position Size**: Lower Max Contracts to 1
2. **Tighten Guards**: Reduce Daily Loss Cap to $300-$400
3. **Be More Selective**: Enable all confirmations
4. **Consider Break**: Sometimes stepping away helps

## Tips for Success

### Do's ✓
- Start with 1 contract in evaluation
- Let the strategy filter trades
- Trust the daily guards
- Review trades daily to learn
- Stay within RTH (8:30 AM - 3:00 PM CT)
- Keep detailed notes on performance
- Maintain discipline and patience

### Don'ts ✗
- Don't manually override strategy trades
- Don't disable daily guards
- Don't scale up too quickly
- Don't trade outside RTH in evaluation
- Don't over-optimize parameters
- Don't chase losses with manual trades
- Don't ignore the Apex dashboard

## Getting Help

### Strategy Not Working as Expected?
1. Check all 3 files imported correctly
2. Verify compilation successful (no errors)
3. Review parameters match your plan
4. Check NinjaScript Output for error messages
5. Try restarting NinjaTrader

### Questions or Issues?
- Review the full **APEXPRO_DOCUMENTATION.md**
- Check NinjaTrader 8 Help documentation
- Post issues on GitHub repository
- Join NinjaTrader community forums

## Next Steps

Once comfortable with the strategy:
1. Review **APEXPRO_DOCUMENTATION.md** for advanced configuration
2. Experiment with parameters in Market Replay mode
3. Track which sessions perform best for you
4. Consider adding your own filters or logic
5. Share your experience and results

---

**Remember**: This is a tool to help you pass evaluations, but you are the trader. Use it wisely, monitor it closely, and always respect risk management principles.

**Good luck with your Apex evaluation!** 🚀
