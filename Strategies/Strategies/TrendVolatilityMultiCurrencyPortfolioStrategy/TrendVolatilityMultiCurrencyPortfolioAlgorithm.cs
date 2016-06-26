using System;
using System.Collections.Generic;
using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;

namespace Strategies.TrendVolatilityMultiCurrencyPortfolioStrategy
{
    /// <summary>
    /// Basic template algorithm simply initializes the date range and cash
    /// </summary>
    public class TrendVolatilityMultiCurrencyPortfolioAlgorithm : QCAlgorithm, IRequiredOrderMethods
    {
        //Configure which securities you'd like to use:
        public string[] Symbols = { "EURUSD", "GBPUSD", "NZDUSD", "AUDUSD" };

        //Risk in dollars per trade ($ or the quote currency of the assets)
        public decimal RiskPerTrade = 40;

        //Sets the profit to loss ratio we want to hit before we exit
        public decimal TargetProfitLossRatio = 0.1m;

        //Cap the investment maximum size ($).
        public decimal MaximumTradeSize = 10000;

        private Resolution _dataResolution = Resolution.Minute;
        private Dictionary<Symbol, TradingAsset> _tradingAssets;

        //List to store the RSI value for each asset
        private RelativeStrengthIndex _rsi;

        public override void Initialize()
        {
            SetStartDate(2007, 4, 1);
            SetEndDate(DateTime.Now);
            SetCash(2000);
            _tradingAssets = new Dictionary<Symbol, TradingAsset>();

            //Add as many securities as you like. All the data will be passed into the event handler:
            foreach (var symbol in Symbols)
            {
                AddSecurity(SecurityType.Forex, symbol, _dataResolution);
                Securities[symbol].FeeModel = new ConstantFeeModel(0.04m);
                //SetBrokerageModel(BrokerageName.FxcmBrokerage);

                _rsi = RSI(symbol, 60, MovingAverageType.Exponential, _dataResolution);

                var tradeBarHistory = History(symbol, TimeSpan.FromDays(2), _dataResolution);

                foreach (var tradeBar in tradeBarHistory)
                {
                    _rsi.Update(tradeBar.EndTime, tradeBar.Close);
                }

                Securities[symbol].VolatilityModel = new ThreeSigmaVolatilityModel(STD(symbol, 390, _dataResolution));
                _tradingAssets.Add(symbol,
                    new TradingAsset(Securities[symbol],
                        new OneShotTrigger(new RsiSignal(_rsi, Portfolio[symbol])),
                        new ProfitTargetSignalExit(null, TargetProfitLossRatio),
                        RiskPerTrade,
                        MaximumTradeSize,
                        this
                    ));
            }
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        public void OnData(TradeBars data)
        {

            foreach (var symbol in Symbols)
            {
                //Create a trading asset package for each symbol 
                _tradingAssets[symbol].Scan(data[symbol]);
            }
        }

        public override void OnEndOfDay()
        {
            foreach (var symbol in Symbols)
            {
                Plot("Charts", "Holdings", Portfolio[symbol].Quantity);
                Plot("Charts", "RSI", _rsi);
            }
        }

    }

    /// <summary>
    /// Interface for the two types of orders required to make the trade
    /// </summary>
    public interface IRequiredOrderMethods
    {
        OrderTicket StopMarketOrder(Symbol symbol, int quantity, decimal stopPrice, string tag = "");
        OrderTicket MarketOrder(Symbol symbol, int quantity, bool asynchronous = false, string tag = "");
    }
}