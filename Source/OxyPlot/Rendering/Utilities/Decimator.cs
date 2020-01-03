// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Decimator.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Provides functionality to decimate lines.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot
{
    using System;
    using System.Linq;
    using System.Collections.Generic;

    /// <summary>
    /// Provides functionality to decimate lines.
    /// </summary>
    public class Decimator
    {
        /// <summary>
        /// Strategies to select which points are taken.
        /// Linear: <see cref="StepSpaceLinear"/>
        /// MinMaxSpikeDetection: <see cref="StepSpaceMinMaxSpikeDetection"/>
        /// </summary>
        public enum CountDecimateStrategy
        {
            None,
            Linear,
            MinMaxSpikeDetection
        }

        /// <summary>
        /// Decimates lines by reducing all points that have the same integer x value to a maximum of 4 points (first, min, max, last).
        /// </summary>
        /// <param name="input">The input points.</param>
        /// <param name="output">The decimated points.</param>
        public static void Decimate(List<ScreenPoint> input, List<ScreenPoint> output)
        {
            if (input == null || input.Count == 0)
            {
                return;
            }

            var point = input[0];
            var currentX = Math.Round(point.X);
            var currentMinY = Math.Round(point.Y);
            var currentMaxY = currentMinY;
            var currentFirstY = currentMinY;
            var currentLastY = currentMinY;
            for (var i = 1; i < input.Count; ++i)
            {
                point = input[i];
                var newX = Math.Round(point.X);
                var newY = Math.Round(point.Y);
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (newX != currentX)
                {
                    AddVerticalPoints(output, currentX, currentFirstY, currentLastY, currentMinY, currentMaxY);
                    currentFirstY = currentLastY = currentMinY = currentMaxY = newY;
                    currentX = newX;
                    continue;
                }

                if (newY < currentMinY)
                {
                    currentMinY = newY;
                }

                if (newY > currentMaxY)
                {
                    currentMaxY = newY;
                }

                currentLastY = newY;
            }

            // Keep from adding an extra point for last
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            currentLastY = currentFirstY == currentMinY ? currentMaxY : currentMinY;
            AddVerticalPoints(output, currentX, currentFirstY, currentLastY, currentMinY, currentMaxY);
        }

        /// <summary>
        /// Decimates lines by only taking each step-th point. The first and last point are always taken.
        /// </summary>
        /// <param name="input">The input points.</param>
        /// <param name="output">The decimated points.</param>
        /// <param name="step">The index delta between each taken point.</param>
        public static void StepwiseDecimate(List<ScreenPoint> input, List<ScreenPoint> output, uint step)
        {
            if (input == null || input.Count == 0)
                return;

            output.Add(input[0]);
            int i = 1 + (int)step;
            while (i < input.Count - 2)
            {
                output.Add(input[i]);
                i += (int)step + 1;
            }

            if (output.Last() != input.Last())
                output.Add(input.Last());
        }

        /// <summary>
        /// Decimates lines by only taking a specified number of points.
        /// </summary>
        /// <param name="input">The input points.</param>
        /// <param name="output">The decimated points.</param>
        /// <param name="count">Number of points to take.</param>
        /// <param name="logarithmic">Set this to true if the points in input are on a logarithmic axes.</param>
        /// <param name="strategy">There are different strategies for selecting whish points are taken. See <see cref="CountDecimateStrategy"/></param>
        public static void CountDecimate(List<ScreenPoint> input, List<ScreenPoint> output, int count, CountDecimateStrategy strategy = CountDecimateStrategy.Linear, bool logarithmic = false)
        {
            if (input == null || input.Count == 0 || count <= 0)
                return;

            if (input.Count <= count)
            {
                output.AddRange(input);
                return;
            }

            if (logarithmic)
            {
                LogSpace(input, output, count, true, strategy);
            }
            else
            {
                LinSpace(input, output, count, true, strategy);
            }
        }

        /// <summary>
        /// Decimates lines by only taking a specified number of eqauly linear speced points.
        /// </summary>
        /// <param name="input">The input points.</param>
        /// <param name="output">The decimated points.</param>
        /// <param name="count">Number of points to take.</param>
        /// <param name="endpoint">If True, stop is the last sample. Otherwise, it is not included. Default is True.</param>
        /// <param name="strategy">There are different strategies for selecting whish points are taken. See <see cref="CountDecimateStrategy"/></param>
        private static void LinSpace(List<ScreenPoint> input, List<ScreenPoint> output, int count, bool endpoint = true, CountDecimateStrategy strategy = CountDecimateStrategy.Linear)
        {
            if (count <= 0)
            {
                output.Clear();
                return;
            }

            if (endpoint)
            {
                if (count == 1)
                {
                    output.Clear();
                    output.Add(input[0]);
                    return;
                }

                double step = (double)input.Count / ((double)count - 1.0d);
                StepSpace(input, output, step, strategy);
                if (output.Last() != input.Last())
                    output.Add(input.Last());
            }
            else
            {
                double step = (double)input.Count / (double)count;
                StepSpace(input, output, step, strategy);
            }
        }

        /// <summary>
        /// Decimates lines by only taking a specified number of logarithmic speced points.
        /// </summary>
        /// <param name="input">The input points.</param>
        /// <param name="output">The decimated points.</param>
        /// <param name="count">Maximum number of points to take. Because of the logarithmic spacing usually less are taken.</param>
        /// <param name="endpoint">If True, stop is the last sample. Otherwise, it is not included. Default is True.</param>
        /// <param name="strategy">De</param>
        private static void LogSpace(List<ScreenPoint> input, List<ScreenPoint> output, int count, bool endpoint = true, CountDecimateStrategy strategy = CountDecimateStrategy.Linear)
        {
            switch (strategy)
            {
                default:
                    LogSpaceLinear(input, output, count, endpoint);
                    break;
                case CountDecimateStrategy.MinMaxSpikeDetection:
                    LogSpaceMinMaxSpikeDetection(input, output, count, endpoint);
                    break;
            }
        }

        private static void LogSpaceLinear(List<ScreenPoint> input, List<ScreenPoint> output, int count, bool endpoint = true)
        {
            if (count <= 0)
            {
                output.Clear();
                return;
            }

            if (endpoint && count == 1)
            {
                output.Clear();
                output.Add(input[0]);
                return;
            }

            IEnumerable<double> indexes = ArrayBuilder.LogSpace((double)0, Math.Log10((double)(input.Count - 1)), count - 1, endpoint, 10.0d);
            double lastIndex = -1;
            int i, lastI = -1;
            output.Add(input[0]);
            foreach (double index in indexes)
            {
                if (index != lastIndex)
                {
                    i = (int)Math.Round(index);
                    if (i != lastI && i < input.Count)
                    {
                        output.Add(input[i]);
                        lastIndex = index;
                        lastI = i;
                    }
                }
            }
            if (output.Last() != input.Last())
                output.Add(input.Last());
        }

        private static void LogSpaceMinMaxSpikeDetection(List<ScreenPoint> input, List<ScreenPoint> output, int count, bool endpoint = true)
        {
            if (count <= 0)
            {
                output.Clear();
                return;
            }

            if (endpoint && count == 1)
            {
                output.Clear();
                output.Add(input[0]);
                return;
            }

            IEnumerable<double> indexes = ArrayBuilder.LogSpace(0, Math.Log10((input.Count - 1)), count - 1, endpoint, 10.0d);

            double lastIndex = -1;
            int iOutside, iInside, lastI = -1;
            double deltaOutside, deltaInside;

            output.Add(input[0]);
            foreach (double indexOutside in indexes)
            {
                if (indexOutside != lastIndex)
                {
                    iOutside = iInside = (int)Math.Round(indexOutside);
                    if (lastI > 0)
                    {
                        if (iOutside >= input.Count)
                            iOutside = iInside = input.Count - 1;

                        deltaOutside = Math.Abs(input[lastI].Y - input[iOutside].Y);
                        for (int j = lastI + 1; j < iOutside; j++)
                        {
                            deltaInside = Math.Abs(input[lastI].Y - input[j].Y);
                            if (deltaInside > deltaOutside)
                            {
                                deltaOutside = deltaInside;
                                iInside = j;
                            }
                        }
                    }

                    if (iInside != lastI && iInside < input.Count)
                    {
                        output.Add(input[iInside]);
                        lastIndex = indexOutside;
                        lastI = iOutside;
                    }
                }
            }

            if (output.Last() != input.Last())
                output.Add(input.Last());
        }

        /// <summary>
        /// Decimates lines by only taking each step-th point. The first and last point are always taken.
        /// </summary>
        /// <param name="input">The input points.</param>
        /// <param name="output">The decimated points.</param>
        /// <param name="step">The index delta between each taken point.</param>
        /// <param name="strategy">There are different strategies for selecting whish points are taken. See <see cref="CountDecimateStrategy"/></param>
        private static void StepSpace(List<ScreenPoint> input, List<ScreenPoint> output, double step, CountDecimateStrategy strategy = CountDecimateStrategy.Linear)
        {
            switch (strategy)
            {
                default:
                    StepSpaceLinear(input, output, step);
                    break;
                case CountDecimateStrategy.MinMaxSpikeDetection:
                    StepSpaceMinMaxSpikeDetection(input, output, step);
                    break;
            }
        }

        /// <summary>
        /// Decimates lines by only taking each step-th point. The first and last point are always taken.
        /// </summary>
        /// <param name="input">The input points.</param>
        /// <param name="output">The decimated points.</param>
        /// <param name="step">The index delta between each taken point.</param>
        private static void StepSpaceLinear(List<ScreenPoint> input, List<ScreenPoint> output, double step)
        {
            double index = 0;
            int newIndex = -1;
            int lastIndex = -1;
            while (newIndex < input.Count - 1)
            {
                newIndex = (int)Math.Round(index);
                if (newIndex != lastIndex && newIndex < input.Count)
                    output.Add(input[newIndex]);
                index += step;
                lastIndex = newIndex;
            }
        }

        /// <summary>
        /// Decimates lines by only taking each step-th point. But checks if there are spikes between the step-th and the step-th +1 point.
        /// This is done by calculating the Abs(y-Delta) of the step-th and the step-th +1 points. Then each point between the step-th and the step-th +1 point
        /// is checked if it has a higher Abs(y-Delta), if so this point is than taken.
        /// The first and last point are always taken.
        /// </summary>
        /// <param name="input">The input points.</param>
        /// <param name="output">The decimated points.</param>
        /// <param name="step">The index delta between each taken point.</param>
        private static void StepSpaceMinMaxSpikeDetection(List<ScreenPoint> input, List<ScreenPoint> output, double step)
        {
            double index = 0;
            int newIndexOutside, newIndexInside;
            int lastIndex = -1;
            double deltaOutside, deltaInside;


            newIndexOutside = newIndexInside = (int)Math.Round(index);
            if (newIndexInside != lastIndex && newIndexInside < input.Count)
                output.Add(input[newIndexInside]);
            index += step;
            lastIndex = newIndexOutside;

            while (newIndexOutside < input.Count - 1)
            {
                newIndexOutside = newIndexInside = (int)Math.Round(index);

                if (newIndexOutside >= input.Count)
                    newIndexOutside = newIndexInside = input.Count - 1;

                deltaOutside = Math.Abs(input[lastIndex].Y - input[newIndexInside].Y);
                for (int j = lastIndex + 1; j < newIndexInside; j++)
                {
                    deltaInside = Math.Abs(input[lastIndex].Y - input[j].Y);
                    if (deltaInside > deltaOutside)
                    {
                        newIndexInside = j;
                        deltaOutside = deltaInside;
                    }
                }

                if (newIndexInside != lastIndex && newIndexInside < input.Count)
                    output.Add(input[newIndexInside]);
                index += step;
                lastIndex = newIndexOutside;
            }
        }

        /// <summary>
        /// Adds vertical points to the <paramref name="result" /> list.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <param name="x">The x coordinate.</param>
        /// <param name="firstY">The first y.</param>
        /// <param name="lastY">The last y.</param>
        /// <param name="minY">The minimum y.</param>
        /// <param name="maxY">The maximum y.</param>
        private static void AddVerticalPoints(
            // ReSharper disable SuggestBaseTypeForParameter
            List<ScreenPoint> result,
            // ReSharper restore SuggestBaseTypeForParameter
            double x,
            double firstY,
            double lastY,
            double minY,
            double maxY)
        {
            result.Add(new ScreenPoint(x, firstY));
            // ReSharper disable CompareOfFloatsByEqualityOperator
            if (firstY == minY)
            {
                if (minY != maxY)
                {
                    result.Add(new ScreenPoint(x, maxY));
                }

                if (maxY != lastY)
                {
                    result.Add(new ScreenPoint(x, lastY));
                }

                return;
            }

            if (firstY == maxY)
            {
                if (maxY != minY)
                {
                    result.Add(new ScreenPoint(x, minY));
                }

                if (minY != lastY)
                {
                    result.Add(new ScreenPoint(x, lastY));
                }

                return;
            }

            if (lastY == minY)
            {
                if (minY != maxY)
                {
                    result.Add(new ScreenPoint(x, maxY));
                }
            }
            else if (lastY == maxY)
            {
                if (maxY != minY)
                {
                    result.Add(new ScreenPoint(x, minY));
                }
            }
            else
            {
                result.Add(new ScreenPoint(x, minY));
                result.Add(new ScreenPoint(x, maxY));
            }
            // ReSharper restore CompareOfFloatsByEqualityOperator
            result.Add(new ScreenPoint(x, lastY));
        }
    }
}
