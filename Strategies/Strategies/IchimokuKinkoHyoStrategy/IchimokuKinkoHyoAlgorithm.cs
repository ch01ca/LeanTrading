using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Data.Market;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Strategies.IchimokuKinkoHyoStrategy
{
    public class IchimokuKinkoHyoAlgorithm : QCAlgorithm
    {
        private const int NumberOfSymbols = 10;
        private const decimal FractionOfPortfolio = 0.25m;

        private readonly ConcurrentDictionary<Symbol, TrendSelectionData> _selectionDatas =
            new ConcurrentDictionary<Symbol, TrendSelectionData>();

        private readonly List<string> _symbols = new List<string>
        {
            "SPY",
            "GOOGL",
            "AAPL",
            //"EDV",
            //"CAT",
            "NVO",
            //"INTC"
        };

        private Chart _chart;

        public override void Initialize()
        {
            SetCash(10000);
            SetStartDate(2005, 1, 1);
            SetEndDate(DateTime.Now);
            SetBenchmark("SPY");

            SetWarmup(44);

            foreach (var symbol in _symbols)
            {
                AddSecurity(SecurityType.Equity, symbol, Resolution.Daily);
                _selectionDatas.AddOrUpdate(symbol, new TrendSelectionData(symbol));
            }

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
            foreach (var symbol in _symbols)
            {
                _selectionDatas[symbol].Update(data[symbol], Portfolio[symbol].Quantity);
                Securities[symbol].IsTradable = true;
            }

            if (IsWarmingUp)
            {
                return;
            }

            foreach (var symbol in _symbols.Where(symbol => Portfolio[symbol].Invested))
            {
                var bullishToBearish = Portfolio[symbol].IsLong
                    && _selectionDatas[symbol].TrendDirection == TrendSelectionData.Direction.Bearish;
                var bearishToBullish = Portfolio[symbol].IsShort
                    && _selectionDatas[symbol].TrendDirection == TrendSelectionData.Direction.Bullish;
                var takeProfit = Portfolio[symbol].UnrealizedProfitPercent >= 0.3m;
                var stopLoss = Portfolio[symbol].UnrealizedProfitPercent < -0.1m;
                if (bullishToBearish || bearishToBullish || takeProfit || stopLoss)
                {
                    Log("Flat >> " + Securities[symbol].Price + " Profit: " + Portfolio[symbol].UnrealizedProfitPercent);
                    Liquidate(symbol);
                    Securities[symbol].IsTradable = false;
                }
            }

            var trending = _selectionDatas.Where(kvp => kvp.Value.IsReady && Securities[kvp.Key].IsTradable).Select(kvp => kvp).ToList();
            trending.Sort((a, b) => a.Value.CompareTo(b.Value));

            var topTrending = trending.Take(NumberOfSymbols - Portfolio.Count);

            foreach (var security in topTrending)
            {
                if (security.Value.TrendDirection == TrendSelectionData.Direction.Bullish)
                {
                    Log("Buy >> " + Securities[security.Key].Price + " Profit: " + Portfolio[security.Key].UnrealizedProfitPercent);
                    SetHoldings(security.Key, FractionOfPortfolio);
                }
                else if (!security.Key.Value.Equals("NVO") && security.Value.TrendDirection == TrendSelectionData.Direction.Bearish)
                {
                    Log("Sell >> " + Securities[security.Key].Price + " Profit: " + Portfolio[security.Key].UnrealizedProfitPercent);
                    SetHoldings(security.Key, -FractionOfPortfolio);
                }
            }

            Plot("ICH", "Price", data["SPY"].Price);
            Plot("ICH", "SenkouA", _selectionDatas["SPY"]._ich.SenkouA);
            Plot("ICH", "SenkouB", _selectionDatas["SPY"]._ich.SenkouB);

            Plot("ICH", "ADX", _selectionDatas["SPY"]._adx);
            Plot("ICH", "ADX.NegativeDirectionalIndex", _selectionDatas["SPY"]._adx.NegativeDirectionalIndex);
            Plot("ICH", "ADX.PositiveDirectionalIndex", _selectionDatas["SPY"]._adx.PositiveDirectionalIndex);
        }
    }
}