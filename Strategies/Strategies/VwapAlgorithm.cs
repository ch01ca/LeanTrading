using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Indicators;
using Strategies.TrendVolatilityMultiCurrencyPortfolioStrategy;

namespace Strategies
{
    public class VwapAlgorithm : QCAlgorithm
    {
        public string Symbol = "SPY";

        private VolumeWeightedAveragePriceIndicator _vwap;

        public override void Initialize()
        {
            SetStartDate(2007, 1, 1);
            SetEndDate(DateTime.Now);
            SetCash(10000);

            AddSecurity(SecurityType.Equity, Symbol, Resolution.Daily);

            _vwap = new VolumeWeightedAveragePriceIndicator(Symbol, 14);
        }

        public override void OnData(Slice slice)
        {
            _vwap.Update(slice[Symbol]);

            if (!_vwap.IsReady) return;

            if (slice[Symbol].Price < _vwap)
            {
                SetHoldings(Symbol, 1.0);
            }
            else if (slice[Symbol].Price > _vwap)
            {
                SetHoldings(Symbol, -1.0);
            }
        }
    }
}