using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Securities;

namespace Strategies.TrendVolatilityMultiCurrencyPortfolioStrategy
{
    public class RsiSignal : ISignal
    {
        private RelativeStrengthIndex _rsi;
        private SecurityHolding _securityHolding;

        public RsiSignal(RelativeStrengthIndex rsi, SecurityHolding securityHolding)
        {
            _rsi = rsi;
            _securityHolding = securityHolding;
        }

        public void Scan(TradeBar data)
        {
            if (_rsi > 70 && !_securityHolding.Invested)
            {
                Signal = SignalType.Short;
            }
            else if (_rsi < 30 && !_securityHolding.Invested)
            {
                Signal = SignalType.Long;
            }
            else
            {
                Signal = SignalType.NoSignal;
            }
        }

        public SignalType Signal { get; private set; }
    }
}