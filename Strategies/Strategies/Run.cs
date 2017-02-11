using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.Random;
using QuantConnect.Indicators;

namespace Strategies
{
    public class Run
    {
        public static void Main(string[] args)
        {
            Random rand = new Random();
            var data = new List<double>();

            for (int i = 0; i < 1000; i++)
            {
                data.Add(rand.NextDouble());
            }

            var garch = new GARCH(data.ToArray());
            Console.WriteLine(garch.GetBestParameters(null)["Vol"]);
            Console.ReadKey();
        }
    }
}
