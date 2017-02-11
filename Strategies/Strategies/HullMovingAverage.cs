using System;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect.Indicators
{
    /// 
    /// Produces a Hull Moving Average as explained at http://www.alanhull.com/hull-moving-average/
    /// and derived from the instructions for the Excel VBA code at http://finance4traders.blogspot.com/2009/06/how-to-calculate-hull-moving-average.html
    /// 
    public class HullMovingAverage : WindowIndicator<IndicatorDataPoint>
    {
        private readonly LinearWeightedMovingAverage _longWma;
        private readonly LinearWeightedMovingAverage _shortWma;
        private readonly RollingWindow<IndicatorDataPoint> _smooth;
        private readonly LinearWeightedMovingAverage _result;
        // The length of the smoothed window
        // square root of period rounded to the nearest whole number 

        /// 
        /// A Hull Moving Average 
        /// 
        /// string - a name for the indicator
        /// int - the number of periods over which to calculate the HMA - the length of the longWMA
        public HullMovingAverage(string name, int period)
            : base(name, period)
        {
            // Creates the long LWMA for the number of periods specified in the constuctor
            _longWma = new LinearWeightedMovingAverage("Long", period);

            // Creates the short LWMA for half the period rounded to the nearest whole number
            _shortWma = new LinearWeightedMovingAverage("Short", System.Convert.ToInt32(Math.Round((double)period / 2)));

            // Creates the smoother data set to which the resulting wma is applied
            _smooth = new RollingWindow<IndicatorDataPoint>(period);

            // number of historical periods to look at in the resulting WMA
            int k = System.Convert.ToInt32((Math.Round(Math.Sqrt(period))));

            // Creates the LWMA for the output. This step probably could have been skipped
            _result = new LinearWeightedMovingAverage("Result", k);

        }

        /// 
        /// A Hull Moving Average with the default name
        /// 
        /// int - the number of periods over which to calculate the HMA - the length of the longWMA
        public HullMovingAverage(int period)
            : this("HMA" + period, period)
        {
        }

        public override bool IsReady => _smooth.IsReady;

        /// 
        /// Computes the next value for this indicator from the given state.
        /// 
        /// The window of data held in this indicator
        /// The input value to this indicator on this time step /// 
        /// 
        /// The Hull moving average is a series of nested weighted moving averages. 
        /// Using the LWMA custom function for calculating weighted moving averages, 
        /// the Hull moving average can be calculated following the steps. 
        ///
        ///1.Calculate the n periodweighted moving average of a series "=WMA(price for n periods)"
        ///2.Calculate the n/2 period weighted moving average of a series"=WMA(price for n/2 periods)". Round n/2 to the nearest whole number
        ///3.Create a time series with 2*WMA from Step 2 - WMA from Step 1
        ///4.The HMA is the WMA of the series in Step 3. "=WMA(Step 3 outputs fo k period)"
        /// 
        protected override decimal ComputeNextValue(IReadOnlyWindow<IndicatorDataPoint> window, IndicatorDataPoint input)
        {
            _longWma.Update(input);
            _shortWma.Update(input);
            //if (!_longWma.IsReady)
            // _smooth.Add(new IndicatorDataPoint(input.Time, 2 * input.Value - input.Value));
            //else
            _smooth.Add(new IndicatorDataPoint(input.Time, 2 * _shortWma.Current.Value - _longWma.Current.Value));
            _result.Update(new IndicatorDataPoint(input.Time, _smooth[0].Value));
            return _result.Current.Value;
        }
    }
}