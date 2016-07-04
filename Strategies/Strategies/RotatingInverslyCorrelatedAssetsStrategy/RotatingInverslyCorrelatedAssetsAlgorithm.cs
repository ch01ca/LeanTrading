using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Algorithm;

namespace Strategies.RotatingInverslyCorrelatedAssetsStrategy
{
    public class RotatingInverslyCorrelatedAssetsAlgorithm : QCAlgorithm
    {
        //Create variables for analyzing Nifty
        CorrelationPair today = new CorrelationPair();
        List<CorrelationPair> prices = new List<CorrelationPair>();
        int minimumCorrelationHistory = 11;

        public override void Initialize()
        {
            SetStartDate(1998, 1, 1);
            SetEndDate(DateTime.Now);

            //Set the cash for the strategy:
            SetCash(100000);

            //Define the symbol and "type" of our generic data:
            AddData<USDINR>("USDINR");
            AddData<Nifty>("NIFTY");
            //public IEnumerable<BaseData> Get(Symbol symbol, Resolution resolution, DateTime startUtc, DateTime endUtc)
        }

        public void OnData(USDINR data)
        {
            today = new CorrelationPair(data.Time);
            today.Add("USDINR", data.Close);
        }

        public void OnData(Nifty data)
        {
            try
            {
                today.Add("NIFTY", data.Close);
                if (today.Date == data.Time)
                {
                    prices.Add(today);

                    if (prices.Count > minimumCorrelationHistory)
                    {
                        prices.RemoveAt(0);
                    }
                }

                if (prices.Count < 2)
                {
                    return;
                }

                string maxAsset = "";
                double maxGain = -9999;

                foreach (string i in today.Prices.Keys)
                {
                    double last = (from pair in prices select pair.Prices[i]).Last();
                    double first = (from pair in prices select pair.Prices[i]).First();
                    double gain = (last - first) / first;
                    if (gain > maxGain)
                    {
                        maxAsset = i;
                        maxGain = gain;
                    }
                }

                //Strategy
                if (maxAsset != "")
                {
                    CustomSetHoldings(maxAsset, 1, true);
                }
            }
            catch (Exception err)
            {
                Debug("Error: " + err.Message);
            }
        }

        //Plot Nifty
        public override void OnEndOfDay()
        {
            if (!today.Prices.ContainsKey("NIFTY")) return;

            if (today.Prices["NIFTY"].Equals(0.0) && today.Date.DayOfWeek == DayOfWeek.Wednesday)
            {
                Plot("NIFTY", today.Prices["NIFTY"]);
            }
            if (today.Prices["USDINR"].Equals(0.0) && today.Date.DayOfWeek == DayOfWeek.Wednesday)
            {
                Plot("USDINR", today.Prices["USDINR"]);
            }
        }

        public void CustomSetHoldings(string symbol, decimal percentage, bool liquidateExistingHoldings = false)
        {
            decimal cash = Portfolio.Cash;
            decimal currentHoldingQuantity = Portfolio[symbol].Quantity;

            //Range check values:
            if (percentage > 1) percentage = 1;
            if (percentage < -1) percentage = -1;

            //If they triggered a liquidate
            if (liquidateExistingHoldings)
            {
                foreach (var holdingSymbol in Portfolio.Keys)
                {
                    if (holdingSymbol != symbol)
                    {
                        //Go through all existing holdings, market order the inverse quantity
                        Order(holdingSymbol, -Portfolio[holdingSymbol].Quantity);
                    }
                }
            }

            //Now rebalance the symbol requested:
            decimal targetHoldingQuantity = Math.Floor((percentage * Portfolio.TotalPortfolioValue) / Securities[symbol].Price);

            decimal netHoldingQuantity = targetHoldingQuantity - currentHoldingQuantity;
            if (Math.Abs(netHoldingQuantity) > 0)
            {
                Order(symbol, (int)netHoldingQuantity);
            }
        }
    }
}
