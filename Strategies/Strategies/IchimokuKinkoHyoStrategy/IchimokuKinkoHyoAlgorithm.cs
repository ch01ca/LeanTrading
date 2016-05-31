using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using System;
using static Strategies.IchimokuKinkoHyoStrategy.IchimokuKinkoHyoSignal;

namespace Strategies.IchimokuKinkoHyoStrategy
{
    class IchimokuKinkoHyoAlgorithm : QCAlgorithm
    {
        private enum SignalType
        {
            Bearish, Bullish
        }

        private const string Symbol = "SPY";

        private IchimokuKinkoHyo _indicator;

        private Chart plotter;

        private const int _windowSize = 2;

        private IchimokuKinkoHyoConsolidated _consolidation;

        public override void Initialize()
        {
            SetCash(3000);
            SetStartDate(2007, 1, 1);
            SetEndDate(DateTime.Now);

            // Request SPY data with minute resolution
            AddSecurity(SecurityType.Equity, Symbol, Resolution.Daily);

            _indicator = new IchimokuKinkoHyo(Symbol, 7, 22, 22, 44, 22, 22);

            _consolidation = new IchimokuKinkoHyoConsolidated(null, null);

            SetWarmup(52);

            plotter = new Chart("ICH", ChartType.Overlay);
            plotter.AddSeries(new Series("Price", SeriesType.Line));
            plotter.AddSeries(new Series("Tenkan", SeriesType.Line));
            plotter.AddSeries(new Series("Kijun", SeriesType.Line));
            plotter.AddSeries(new Series("SenkouA", SeriesType.Line));
            plotter.AddSeries(new Series("SenkouB", SeriesType.Line));
        }

        public void OnData(TradeBars data)
        {
            _indicator.Update(data[Symbol]);

            _consolidation.RollConsolidationWindow();
            _consolidation.Consolidate(data[Symbol], _indicator);

            if (IsWarmingUp) return;

            if (!_consolidation.IsReady) return;

            var holdings = Portfolio[Symbol].Quantity;

            if (holdings <= 0)
            {
                var tenkanKijunSignal = _consolidation.BullishTenkanKijunCross();
                var kijunSignal = _consolidation.BullishKijunCross(data);
                var kumoSignal = _consolidation.BullishKumoBreakout(data);
                var senkouSignal = _consolidation.BullishSenkouCross(data);

                var neutralSignal = tenkanKijunSignal == SignalStrength.Neutral
                    || kijunSignal == SignalStrength.Neutral
                    || senkouSignal == SignalStrength.Neutral;

                var strongSignal = tenkanKijunSignal == SignalStrength.Strong
                    || kijunSignal == SignalStrength.Strong
                    || senkouSignal == SignalStrength.Strong;

                if (neutralSignal || strongSignal)
                {
                    Log("Buy >> " + Securities[Symbol].Price);
                    SetHoldings(Symbol, 1.0);
                }
            }

            if (holdings >= 0)
            {
                var tenkanKijunSignal = _consolidation.BearishTenkanKijunCross();
                var kijunSignal = _consolidation.BearishKijunCross(data);
                var kumoSignal = _consolidation.BearishKumoBreakout(data);
                var senkouSignal = _consolidation.BearishSenkouCross(data);

                var neutralSignal = tenkanKijunSignal == SignalStrength.Neutral
                    || kijunSignal == SignalStrength.Neutral
                    || senkouSignal == SignalStrength.Neutral;

                var strongSignal = tenkanKijunSignal == SignalStrength.Strong
                    || kijunSignal == SignalStrength.Strong
                    || senkouSignal == SignalStrength.Strong;

                if (neutralSignal || strongSignal)
                {
                    Log("Sell >> " + Securities[Symbol].Price);
                    SetHoldings(Symbol, -1.0);
                    //Liquidate(Symbol);
                }
            }

            Plot("ICH", "Price", data[Symbol].Price);
            Plot("ICH", "Tenkan", _indicator.Tenkan);
            Plot("ICH", "Kijun", _indicator.Kijun);
            Plot("ICH", "SenkouA", _indicator.SenkouA);
            Plot("ICH", "SenkouB", _indicator.SenkouB);
        }
    }
}