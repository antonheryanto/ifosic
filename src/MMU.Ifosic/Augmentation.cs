using MMU.Ifosic.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMU.Ifosic;

public class Augmentation
{
	// parallel fiber layout
	// time based switch, one iteration, multiple iteration
	// time to fiber switch info
	// boundary is distance to fiber switch
	public static void Create(FrequencyShiftDistance fdd, string srcFile, string? refFile = null, int numberOfFiber = 3, int? endIndex = null, int startIndex = 0)
    {
        var parallelTimeIndex = new List<int>();
        for (int i = 0; i < fdd.Traces.Count;)
        {
            for (int j = 1; j < numberOfFiber + 1 && i < fdd.Traces.Count; j++, i++)
                parallelTimeIndex.Add(j);
        }
        var boundaryStart = 2;
        var boundaries = new List<double>();
        for (int i = boundaryStart; i < boundaryStart + numberOfFiber + 1; i++)
        {
            boundaries.Add(fdd.Boundaries[i]);
        }
        endIndex ??= fdd.BoundaryIndexes.Count - 1;
        var parallelTraces = new List<double[]>();
        for (int timeIndex = 0; timeIndex < parallelTimeIndex.Count; timeIndex++)
        {
            parallelTraces.Add(GenerateData(fdd, timeIndex, boundaryStart + parallelTimeIndex[timeIndex], endIndex.Value, startIndex));
        }

        fdd.ToZip($"{srcFile}_Time_Parallel.zip", refFile, parallelTraces, parallelTimeIndex, boundaries);
    }



    private static void GenerateSerial(FrequencyShiftDistance fdd, string srcFile, string? refFile, int numberOfFiber, List<int> parallelTimeIndex, List<double> boundaries)
    {
        var traces = new List<List<double[]>>();
        var timeIndexes = new List<List<int>>();
        for (int i = 0; i < numberOfFiber; i++)
        {
            traces.Add(new());
            timeIndexes.Add(new());
        }
        for (int i = 0; i < fdd.Traces.Count; i++)
        {
            traces[parallelTimeIndex[i] - 1].Add(fdd.Traces[i]);
            timeIndexes[parallelTimeIndex[i] - 1].Add(parallelTimeIndex[i]);
        }
        var serialTraces = new List<double[]>();
        var serialTimeIndex = new List<int>();
        for (int i = 0; i < traces.Count; i++)
        {
            serialTraces.AddRange(traces[i]);
            serialTimeIndex.AddRange(timeIndexes[i]);
        }

        // generate parallel set using serial set 1, fiber 4, 5
        // uses boundary as anchor, put fiber one to the selected fiber, append the reminder 

        fdd.ToZip($"{srcFile}_Time_Serial.zip", refFile, serialTraces, serialTimeIndex, boundaries);
    }

    static double[] GenerateData(FrequencyShiftDistance fdd, int timeIndex, int fiberIndex, int endIndex, int startIndex = 0)
	{
		var trace = new List<double>();
		for (int i = 0; i < (fdd.BoundaryIndexes[fiberIndex - 1] - fdd.BoundaryIndexes[startIndex]); i++) // diff target with start
			trace.Add(fdd.Traces[timeIndex][i]);
		for (int i = 0; i < fdd.BoundaryIndexes[startIndex]; i++) // add up to start
			trace.Add(fdd.Traces[timeIndex][i]);
		for (int i = fdd.BoundaryIndexes[fiberIndex - 1]; i < fdd.BoundaryIndexes[fiberIndex]; i++)
			trace.Add(fdd.Traces[timeIndex][i]);
		for (int i = fdd.BoundaryIndexes[endIndex]; i < fdd.Traces[timeIndex].Length; i++) // add from endIndex to end
			trace.Add(fdd.Traces[timeIndex][i]);
		for (int i = fdd.Traces[timeIndex].Length - (fdd.BoundaryIndexes[endIndex] - fdd.BoundaryIndexes[fiberIndex]); i < fdd.Traces[timeIndex].Length; i++)
			trace.Add(fdd.Traces[timeIndex][i]);
		return trace.ToArray();
	}
}
