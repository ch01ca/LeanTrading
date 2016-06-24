using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Orders;
using System;
using System.Collections.Generic;
using System.Linq;
using static Strategies.IchimokuKinkoHyoStrategy.IchimokuKinkoHyoSignal;

namespace Strategies.IchimokuKinkoHyoStrategy
{
    class IchimokuKinkoHyoAlgorithm : QCAlgorithm
    {
        private const string Symbol = "SPY";

        private IchimokuKinkoHyo _indicator;

        private Chart _chart;

        private Chart _priceChart;

        private IchimokuKinkoHyoConsolidated _consolidation;

        private AverageDirectionalIndex _adx;

        private VolumeWeightedAveragePriceIndicator _vwap;

        private OrderTicket _openStopMarketOrder;

        private bool _isTraded;

        private readonly RollingWindow<decimal> _volume = new RollingWindow<decimal>(250);

        public override void Initialize()
        {
            SetCash(10000);
            SetStartDate(1998, 1, 1);
            SetEndDate(DateTime.Now);

            // Request SPY data with minute resolution
            AddSecurity(SecurityType.Equity, Symbol, Resolution.Daily);

            _indicator = new IchimokuKinkoHyo(Symbol, 7, 22, 22, 44, 22, 22);

            _adx = new AverageDirectionalIndex(Symbol, 22);

            _vwap = new VolumeWeightedAveragePriceIndicator(Symbol, 22);

            _consolidation = new IchimokuKinkoHyoConsolidated(null, null);

            SetWarmup(44);

            _chart = new Chart("ICH", ChartType.Overlay);
            _chart.AddSeries(new Series("Price", SeriesType.Line));
            _chart.AddSeries(new Series("Tenkan", SeriesType.Line));
            _chart.AddSeries(new Series("Kijun", SeriesType.Line));
            _chart.AddSeries(new Series("SenkouA", SeriesType.Line));
            _chart.AddSeries(new Series("SenkouB", SeriesType.Line));
            _chart.AddSeries(new Series("ADX", SeriesType.Line));
            _chart.AddSeries(new Series("ADX.NegativeDirectionalIndex", SeriesType.Line));
            _chart.AddSeries(new Series("ADX.PositiveDirectionalIndex", SeriesType.Line));
        }

        public void OnData(TradeBars data)
        {
            _indicator.Update(data[Symbol]);
            _adx.Update(data[Symbol]);
            _vwap.Update(data[Symbol]);

            _consolidation.RollConsolidationWindow();
            _consolidation.Consolidate(data[Symbol], _indicator);

            if (IsWarmingUp)
            {
                _volume.Add(Securities[Symbol].Volume);
                return;
            }

            if (!_consolidation.IsReady)
            {
                _volume.Add(Securities[Symbol].Volume);
                return;
            }

            _isTraded = false;

            var holdings = Portfolio[Symbol].Quantity;

            if (holdings != 0 && _adx < 20)
            {
                Liquidate(Symbol);
                _isTraded = true;
            }

            if (holdings <= 0 && !_isTraded)
            {
                var tenkanKijunSignal = _consolidation.BullishTenkanKijunCross();
                var kijunSignal = _consolidation.BullishKijunCross(data);
                var kumoSignal = Math.Max(_indicator.SenkouA, _indicator.SenkouB) < Securities[Symbol].Price;
                var senkouSignal = _consolidation.BullishSenkouCross(data);

                var averageVolume = _volume.Sum() / _volume.Count;
                var volumeSignal = Securities[Symbol].Volume > averageVolume
                    || _volume.Skip(_volume.Count - 5).Any(av => av > averageVolume);

                var strongSignal = tenkanKijunSignal == SignalStrength.Strong
                    || kijunSignal == SignalStrength.Strong
                    || senkouSignal == SignalStrength.Strong;

                var adxSignal = _adx > 20 && _adx.PositiveDirectionalIndex > _adx.NegativeDirectionalIndex;

                var vwapSignal = Securities[Symbol].Price < _vwap;

                if (strongSignal && kumoSignal && volumeSignal && adxSignal && vwapSignal)
                {
                    Log("Buy >> " + Securities[Symbol].Price);
                    SetHoldings(Symbol, 1.0);

                    _isTraded = true;
                }
            }

            if (holdings >= 0 && !_isTraded)
            {
                var tenkanKijunSignal = _consolidation.BearishTenkanKijunCross();
                var kijunSignal = _consolidation.BearishKijunCross(data);
                var kumoSignal = Math.Min(_indicator.SenkouA, _indicator.SenkouB) > Securities[Symbol].Price;
                var senkouSignal = _consolidation.BearishSenkouCross(data);

                var averageVolume = _volume.Sum() / _volume.Count;
                var volumeSignal = Securities[Symbol].Volume > averageVolume
                    || _volume.Skip(_volume.Count - 5).Any(av => av > averageVolume);

                var strongSignal = tenkanKijunSignal == SignalStrength.Strong
                    || kijunSignal == SignalStrength.Strong
                    || senkouSignal == SignalStrength.Strong;

                var adxSignal = _adx > 20 && _adx.NegativeDirectionalIndex < _adx.PositiveDirectionalIndex;

                var vwapSignal = Securities[Symbol].Price > _vwap;

                if (strongSignal && kumoSignal && volumeSignal && adxSignal && vwapSignal)
                {
                    Log("Sell >> " + Securities[Symbol].Price);
                    SetHoldings(Symbol, -1.0);
                }
            }

            _volume.Add(Securities[Symbol].Volume);

            Plot("ICH", "Price", data[Symbol].Price);
            //Plot("ICH", "Tenkan", _indicator.Tenkan);
            //Plot("ICH", "Kijun", _indicator.Kijun);
            Plot("ICH", "SenkouA", _indicator.SenkouA);
            Plot("ICH", "SenkouB", _indicator.SenkouB);

            Plot("ICH", "ADX", _adx);
            Plot("ICH", "ADX.NegativeDirectionalIndex", _adx.NegativeDirectionalIndex);
            Plot("ICH", "ADX.PositiveDirectionalIndex", _adx.PositiveDirectionalIndex);
        }
    }
}