// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ArrayBuilder.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Provides functionality to build arrays.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Provides functionality to build arrays.
    /// </summary>
    public static class ArrayBuilder
    {
        /// <summary>
        /// Creates a vector.
        /// </summary>
        /// <param name="x0">The first value.</param>
        /// <param name="x1">The last value.</param>
        /// <param name="n">The number of steps.</param>
        /// <returns>A vector.</returns>
        public static double[] CreateVector(double x0, double x1, int n)
        {
            var result = new double[n];
            for (int i = 0; i < n; i++)
            {
                result[i] = Math.Round(x0 + ((x1 - x0) * i / (n - 1)), 8);
            }

            return result;
        }

        /// <summary>
        /// Creates a vector.
        /// </summary>
        /// <param name="x0">The first value.</param>
        /// <param name="x1">The last value.</param>
        /// <param name="dx">The step size.</param>
        /// <returns>A vector.</returns>
        public static double[] CreateVector(double x0, double x1, double dx)
        {
            var n = (int)Math.Round((x1 - x0) / dx);
            var result = new double[n + 1];
            for (int i = 0; i <= n; i++)
            {
                result[i] = Math.Round(x0 + (i * dx), 8);
            }

            return result;
        }

        /// <summary>
        /// Evaluates the specified function.
        /// </summary>
        /// <param name="f">The function.</param>
        /// <param name="x">The x values.</param>
        /// <param name="y">The y values.</param>
        /// <returns>Array of evaluations. The value of f(x_i,y_j) will be placed at index [i, j].</returns>
        public static double[,] Evaluate(Func<double, double, double> f, double[] x, double[] y)
        {
            int m = x.Length;
            int n = y.Length;
            var result = new double[m, n];
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    result[i, j] = f(x[i], y[j]);
                }
            }

            return result;
        }

        /// <summary>
        /// Fills the array with the specified value.
        /// </summary>
        /// <param name="array">The array to fill.</param>
        /// <param name="value">The value.</param>
        public static void Fill(this double[] array, double value)
        {
            for (var i = 0; i < array.Length; i++)
            {
                array[i] = value;
            }
        }

        /// <summary>
        /// Fills the two-dimensional array with the specified value.
        /// </summary>
        /// <param name="array">The two-dimensional array.</param>
        /// <param name="value">The value.</param>
        public static void Fill2D(this double[,] array, double value)
        {
            for (var i = 0; i < array.GetLength(0); i++)
            {
                for (var j = 0; j < array.GetLength(1); j++)
                {
                    array[i, j] = value;
                }
            }
        }

        /// <summary>
        /// Returns evenly spaced values within a given interval.
        /// </summary>
        /// <param name="start">Start of interval.</param>
        /// <param name="count">The number of sequential values to generate.</param>
        /// <returns>IEnumerable of evenly spaced values.</returns>
        public static IEnumerable<double> Arange(double start, int count)
        {
            return Enumerable.Range((int)start, count).Select(v => (double)v);
        }

        /// <summary>
        /// Raises the elements of an array by a specified power.
        /// </summary>
        /// <param name="exponents">An array of double-precision floating-point numbers to be raised to a power.</param>
        /// <param name="baseValue">A double-precision floating-point number that specifies a power.</param>
        /// <returns>IEnumerable of raised values.</returns>
        public static IEnumerable<double> Power(IEnumerable<double> exponents, double baseValue = 10.0d)
        {
            return exponents.Select(v => Math.Pow(baseValue, v));
        }

        /// <summary>
        /// Returns an array of evenly spaced numbers over a specified interval.
        /// </summary>
        /// <param name="start">The starting value of the sequence.</param>
        /// <param name="stop">The end value of the sequence, unless endpoint is set to False. In that case, the sequence consists of all but the last of num + 1 evenly spaced samples, so that stop is excluded. Note that the step size changes when endpoint is False.</param>
        /// <param name="num">Number of samples to generate. Must be non-negative.</param>
        /// <param name="endpoint">If True, stop is the last sample. Otherwise, it is not included. Default is True.</param>
        /// <returns>IEnumerable of evenly spaced numbers.</returns>
        public static IEnumerable<double> LinSpace(double start, double stop, int num, bool endpoint = true)
        {
            var result = new List<double>();
            if (num <= 0)
            {
                return result;
            }

            if (endpoint)
            {
                if (num == 1)
                {
                    return new List<double>() { start };
                }

                var step = (stop - start) / ((double)num - 1.0d);
                result = Arange(0, num).Select(v => (v * step) + start).ToList();
            }
            else
            {
                var step = (stop - start) / (double)num;
                result = Arange(0, num).Select(v => (v * step) + start).ToList();
            }

            return result;
        }

        /// <summary>
        /// Returns an array of logarithmic spaced numbers over a specified interval.
        /// </summary>
        /// <param name="start">The starting value of the sequence.</param>
        /// <param name="stop">The end value of the sequence, unless endpoint is set to False. In that case, the sequence consists of all but the last of num + 1 evenly spaced samples, so that stop is excluded. Note that the step size changes when endpoint is False.</param>
        /// <param name="num">Number of samples to generate. Must be non-negative.</param>
        /// <param name="endpoint">If True, stop is the last sample. Otherwise, it is not included. Default is True.</param>
        /// <param name="numericBase">The base of the logarithmic spacing. Default is 10.</param>
        /// <returns>IEnumerable of logarithmic spaced numbers.</returns>
        public static IEnumerable<double> LogSpace(double start, double stop, int num, bool endpoint = true, double numericBase = 10.0d)
        {
            var y = LinSpace(start, stop, num: num, endpoint: endpoint);
            return Power(y, numericBase);
        }

        public static IEnumerable<double> GeomSpace(double start, double stop, int num, bool endpoint = true, double numericBase = 10.0d)
        {            
            if((start == 0) || (stop == 0))
                return new double[0];

            double log_start = Math.Log10(start);
            double log_stop = Math.Log10(stop);
            return LogSpace(log_start, log_stop, num, endpoint, numericBase);
        }
    }
}
