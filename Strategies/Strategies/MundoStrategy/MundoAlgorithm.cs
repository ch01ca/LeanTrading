using System;
using System.Collections.Generic;
using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace Strategies.MundoStrategy
{

    public class MundoAlgorithm : QCAlgorithm
    {
        //List of Currencies you would like to trade and get the indexes for 
        List<string> _symbols = new List<string>() { "EURUSD", "GBPUSD", "AUDUSD", "NZDUSD" };

        TradeBars _bars = new TradeBars();
        private Dictionary<string, SymbolData> _symbolData = new Dictionary<string, SymbolData>();
        public readonly int RollingWindowSize = 2;
        public readonly TimeSpan BarPeriod = TimeSpan.FromDays(1);

        //parameters
        int _normPeriod = 60;
        int _period = 20;

        //Initialize the data and resolution you require for your strategy:
        public override void Initialize()
        {
            SetStartDate(2007, 4, 1);
            SetEndDate(DateTime.Now);

            SetCash(1000);

            // initialize data for all the symbols
            foreach (var symbol in _symbols)
            {
                _symbolData.Add(symbol, new SymbolData(symbol, SecurityType.Forex, BarPeriod, RollingWindowSize));
            }

            //Set forex securities and Consolidate all the data
            foreach (var kvp in _symbolData)
            {
                var symbolData = kvp.Value;

                //AddSecurity(symbolData.SecurityType, symbolData.Symbol, Resolution.Hour);
                AddForex(symbolData.Symbol, Resolution.Hour, "fxcm");

                // define a consolidator to consolidate data for this symbol on the requested period
                var consolidator = new TradeBarConsolidator(BarPeriod);

                // define indicators
                symbolData._sd = new StandardDeviation(_period);
                symbolData._min = new Minimum(_normPeriod);
                symbolData._max = new Maximum(_normPeriod);

                //update indicators
                consolidator.DataConsolidated += (sender, bar) =>
                {
                    // 'bar' here is our newly consolidated data
                    symbolData._min.Update(bar.Time, symbolData._portfolio);
                    symbolData._max.Update(bar.Time, symbolData._portfolio);
                    symbolData._sd.Update(bar.Time, bar.Close);

                    // we're also going to add this bar to our rolling window so we have access to it later
                    symbolData.Bars.Add(bar);

                };

                // we need to add this consolidator so it gets auto updates
                SubscriptionManager.AddConsolidator(symbolData.Symbol, consolidator);

            }
        }

        public void OnData(TradeBars data)
        {
            UpdateBars(data);
            if (_bars.Count != _symbols.Count) return;

            decimal totalSd = 0;
            decimal beta = 0;
            decimal prt = 0;

            // Calculate total SD
            foreach (var symbolData in _symbolData.Values)
            {
                if (!symbolData._sd.IsReady) return;

                if (symbolData._sd != 0)
                {
                    totalSd += symbolData._sd;
                    beta += symbolData._sd;
                }
            }

            foreach (var symbolData in _symbolData.Values)
            {
                //make portfolio with Mundo index beta weights
                if (beta != 0) prt += _bars[symbolData.Symbol].Close * symbolData._sd / (beta / 4m);
            }

            foreach (var symbolData in _symbolData.Values)
            {
                symbolData._portfolio = prt;

                // Do a basic 0-1 Min-Max Normalization to normalize all the values
                if (symbolData._max - symbolData._min != 0)
                {
                    symbolData._norm = (prt - symbolData._min) / (symbolData._max - symbolData._min);
                }

                if (Portfolio[symbolData.Symbol].IsLong && symbolData._norm > 1m)
                {
                    Liquidate();
                }

                if (Portfolio[symbolData.Symbol].IsShort && symbolData._norm < 0m)
                {
                    Liquidate();
                }

                if (!Portfolio[symbolData.Symbol].Invested && symbolData._norm > 0 && symbolData._prenorm < 0)
                {
                    if (beta / 4m != 0 && symbolData._sd != 0) SetHoldings(symbolData.Symbol, 0.4m / (symbolData._sd / (beta / 4m)));
                }

                if (!Portfolio[symbolData.Symbol].Invested && symbolData._norm < 1m && symbolData._prenorm > 1m)
                {
                    if (beta / 4m != 0 && symbolData._sd != 0) SetHoldings(symbolData.Symbol, -0.6m / (symbolData._sd / (beta / 4m)));
                }

                symbolData._prenorm = symbolData._norm; //Keep track of the previous normalized values
            }
        }

        private void UpdateBars(TradeBars data)
        {
            foreach (var bar in data.Values)
            {
                if (!_bars.ContainsKey(bar.Symbol))
                {
                    _bars.Add(bar.Symbol, bar);
                }

                _bars[bar.Symbol] = bar;
            }
        }

        private class SymbolData
        {
            public readonly string Symbol;
            private readonly SecurityType SecurityType;
            public readonly RollingWindow<TradeBar> Bars;
            private readonly TimeSpan BarPeriod;

            //indcators
            public StandardDeviation _sd;
            public Minimum _min;
            public Maximum _max;

            public decimal _portfolio;
            public decimal _norm;
            public decimal _prenorm;


            // Constructor for the class
            public SymbolData(string symbol, SecurityType securityType, TimeSpan barPeriod, int windowSize)
            {
                Symbol = symbol;
                SecurityType = securityType;
                BarPeriod = barPeriod;
                Bars = new RollingWindow<TradeBar>(windowSize);
            }

            public bool IsReady => Bars.IsReady
                                   && _sd.IsReady
                                   && _min.IsReady
                                   && _max.IsReady;

            //   public bool WasJustUpdated(DateTime current)
            //  {
            //     return Bars.Count > 0 && Bars[0].Time == current - BarPeriod;
            //}
        }
    }
}