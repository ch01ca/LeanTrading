using QuantConnect.Data.Market;

namespace Strategies.TrendVolatilityMultiCurrencyPortfolioStrategy
{
    public class ProfitTargetSignalExit : IExitSignal
    {
        private TradeProfile _tradeProfile;
        private decimal _targetProfitLossRatio;

        public ProfitTargetSignalExit() { }

        public ProfitTargetSignalExit(TradeProfile tradeProfile, decimal targetProfitLossRatio)
        {
            _tradeProfile = tradeProfile;
            _targetProfitLossRatio = targetProfitLossRatio;
        }

        public void Scan(TradeBar data)
        {
            if (_tradeProfile.ProfitLossRatio > _targetProfitLossRatio)
            {
                Signal = SignalType.Exit;
            }
            else
            {
                Signal = SignalType.NoSignal;
            }
        }

        public SignalType Signal { get; private set; }

        public ISignal ExitSignalFactory(TradeProfile tradeProfile)
        {
            return new ProfitTargetSignalExit(tradeProfile, _targetProfitLossRatio);
        }
    }
}