using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace Strategies
{
    public class SchaffTrendCycleIndicator : TradeBarIndicator
    {
        private MovingAverageConvergenceDivergence _macd;

        private Stochastic _frac1;
        private Stochastic _frac2;

        public decimal Factor => 0.5m;

        public override bool IsReady => _macd.IsReady && _frac1.IsReady && _frac2.IsReady;

        public SchaffTrendCycleIndicator(string name) : this(name, 23, 50, 10, MovingAverageType.Exponential)
        {
        }

        public SchaffTrendCycleIndicator(string name, int fastPeriod, int slowPeriod, int signalPeriod, MovingAverageType movingAverageType) : base(name)
        {
            _macd = new MovingAverageConvergenceDivergence(name + "_MACD", fastPeriod, slowPeriod, signalPeriod, movingAverageType);
            _frac1 = new Stochastic(name + "Frac1", 10, 10, 10);
            _frac2 = new Stochastic(name + "Frac2", 10, 10, 10);
        }

        protected override decimal ComputeNextValue(TradeBar input)
        {
            _macd.Update(input.EndTime, input.Close);

            if (!IsReady) return 0m;

            var macdBar = new TradeBar
            {
                EndTime = input.EndTime,
                Close = _macd
            };
            _frac1.Update(macdBar);

            var pfBar = new TradeBar
            {
                EndTime = input.EndTime,
                Close = _frac1
            };
            _frac2.Update(pfBar);

            return _frac2.StochD;
        }
    }
}