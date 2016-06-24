using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Data.Market;
using Strategies.ROICFundamentalsStrategy;

namespace Strategies.RoicFundamentalsStrategy
{
    public class RoicFundamentalsAlgorithm : QCAlgorithm
    {
        // Notes

        /* */

        // User-editable Fields

        private readonly Dictionary<string, decimal> _tickerDictionary = new Dictionary<string, decimal> // ticker, weighted holdings
	    {
            { "AAPL", 1.0m },
            { "NFLX", 1.0m },
            { "AMZN", 1.0m },
            { "GOOGL", 1.0m },
        };

        private double _polyA = 15.998;
        private double _polyB = 1.548;
        private double _polyC = 1.376;
        private readonly int _warmupdays = 30;

        // Class Fields

        private DateTime _lastRebalanceTime = DateTime.MinValue;
        private readonly TimeSpan _rebalanceInterval = TimeSpan.FromDays(30);
        private readonly List<SecurityObject> _consideredSecuritiesList = new List<SecurityObject>();
        private List<SecurityObject> _winningSecuritiesList = new List<SecurityObject>();
        private List<SecurityObject> _losingSecuritiesList = new List<SecurityObject>();
        private bool _firstOnDataCall = true;

        // Initializer

        public override void Initialize()
        {
            SetStartDate(2007, 1, 1);
            SetEndDate(DateTime.Now);
            SetCash(10000);

            foreach (var entry in _tickerDictionary)
            {
                var ticker = entry.Key;
                var weightedholding = entry.Value;

                AddSecurity(SecurityType.Equity, ticker, Resolution.Minute);
                Securities[ticker].SetDataNormalizationMode(DataNormalizationMode.TotalReturn);
                Securities[ticker].SetLeverage(1);

                // ACTIVATE THESE
                //ROIC _roic = ROIC(entry.key, Resolution.Daily)
                //EnterpriseValue _enterprisevalue = ENTVAL(ticker, Resolution.Daily)
                //InvestedCapital _investedcapital = INVCAP(ticker, Resolution,Daily)

                // DEACTIVATE THESE
                var roic = 0.0;
                var enterpriseValue = 0.0;
                var investedCapital = 0.0;

                _consideredSecuritiesList.Add(
                    new SecurityObject
                    {
                        Ticker = ticker,
                        HoldingsWeight = weightedholding,
                        Roic = roic,
                        EnterpriseValue = enterpriseValue,
                        InvestedCapital = investedCapital
                    });

                SetWarmup(_warmupdays);
            }
        }

        // Event-Driven Methods

        public void OnData(TradeBars data)
        {
            // Abandon method if we're warming up the algorithm with historical data
            if (IsWarmingUp) return;

            // Abandon method if this is the first time the OnData() method is called (rules out first-of-day data errors)
            if (_firstOnDataCall)
            {
                _firstOnDataCall = false;
                _lastRebalanceTime = this.Time;
                return;
            }

            // Assess interval rules

            var timeSinceLastRebalance = Time.Subtract(_lastRebalanceTime);

            if (timeSinceLastRebalance <= _rebalanceInterval) return;

            _lastRebalanceTime = Time;

            DefaultWinnersAndLosersToEmptyList();

            AssessWinnersAndLosers();

            TakeAction();
        }

        // Rebalance Rules Methods

        public void DefaultWinnersAndLosersToEmptyList()
        {
            _winningSecuritiesList = new List<SecurityObject>();
            _losingSecuritiesList = new List<SecurityObject>();
        }

        public void AssessWinnersAndLosers()
        {
            foreach (var security in _consideredSecuritiesList)
            {
                security.DetermineValue(_polyA, _polyB, _polyC);

                if (security.IsOvervalued == false)
                {
                    _winningSecuritiesList.Add(security);
                }
                else // Overvalued == true
                {
                    _losingSecuritiesList.Add(security);
                }
            }
        }

        public void TakeAction()
        {
            // Sell our losing securities

            foreach (var security in _losingSecuritiesList)
            {
                if (Portfolio[security.Ticker].HoldStock)
                {
                    SetHoldings(security.Ticker, 0.0m);
                }
            }

            // Buy our winning securities

            var totalweightings = CalculateTotalWeightings();

            foreach (var security in _winningSecuritiesList)
            {
                if (Portfolio[security.Ticker].HoldStock)
                {
                    continue;
                }

                var percentageofportfolio = _tickerDictionary[security.Ticker] / totalweightings;
                SetHoldings(security.Ticker, Convert.ToDouble(percentageofportfolio));
            }

        }

        // Helper Methods

        public decimal CalculateTotalWeightings()
        {
            return _winningSecuritiesList.Sum(security => _tickerDictionary[security.Ticker]);
        }
    }
}