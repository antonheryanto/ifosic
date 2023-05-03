using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System.Security.Cryptography.Xml;

namespace MMU.Ifosic.Models;

public class NumberToGrid
{
	private readonly double _step = 0;
	private readonly double[] _values = Array.Empty<double>();
    public NumberToGrid(double min = 0, double max = 9, int length = 10)
    {
        _step = (max - min) / (length - 1.0);
		_values = new double[length];
		for (int i = 0; i < length; i++) { 
			_values[i] = min + _step * i;
		}
    }

	public Tensor<float> ToTensor(double[][] values)
	{
		Tensor<float> item = new DenseTensor<float>(new int[] { values.Length, 1, values[0].Length, _values.Length });
		for (int i = 0; i < values.Length; i++)
		{ 
			for (int j = 0; j < values[i].Length; j++) {
				var v = values[i][j];
				var id = GetClass(v);
				item[i, 0, j, id] = 1;
			}
		}
        return item;
    }

    public double[][] GetItem(double[] values)
    {
        var item = new double[values.Length][];
		for (int i = 0; i < item.Length; i++)
			item[i] = GetItem(values[i]);
        return item;
    }

    public double[] GetItem(double v)
	{
		var item = new double[_values.Length];
		item[GetClass(v)] = 1;
		return item;
	}

	public int GetClass(double v)
	{
		if (v < _values[0])
			return 0;
		if (v > _values[^1])
			return _values.Length - 1;

		for (int i = 0; i < _values.Length; i++)
		{
			if (_values[i] >= v)
				return i;
		}

		return default;
	}
}

public class Characterisation
{
	public static readonly DateTime UnixTime = new (1970, 1, 1);

	public List<double[]> Averages { get; set; } = new();
	public List<double[]> AveragePoints { get; set; } = new();
	public List<double[]> Candidates { get; set; } = new();
	public List<double[]> CrossPlotPoints { get; set; } = new();
	public List<double[]> GuidedPoints { get; set; } = new();
	public List<double[]> References { get; set; } = new();
	public List<double[]> ReferencePoints { get; set; } = new();
	public List<double[]> RegressionPoints { get; set; } = new();
    public double Slope { get; set; }
    public double Intercept { get; set; }

    public Characterisation(FrequencyShiftDistance? fdd = null, int fiberId = 0, string measurement = "Pressure", int borderGap = 0)
	{
		if (fdd is null || fdd.Traces.Count == 0)
			return;
		var times = new double[fdd.Traces.Count];
		for (int j = 0; j < fdd.Traces.Count; j++)
		{
			if (fdd.TimeBoundaries?.Count > 0 && fdd.TimeBoundaries[j] != fiberId)
				continue;
			times[j] = fdd.MeasurementStart[j]?.Subtract(UnixTime).TotalMilliseconds ?? 0;
		}
		if (fdd.BoundaryIndexes.Count == 0)
			return;
		var usesCategory = fdd.Categories.Where(w => w > 0).Count() > fdd.Categories.Count * 0.05;
		for (int i = fdd.BoundaryIndexes[fiberId - 1]; i < fdd.BoundaryIndexes[fiberId]; i++)
		{
			// aggregate only good signal
			if (usesCategory && fdd.Categories[i] != 1)
				continue;

			// get each time freq at that location
			for (int j = 0; j < fdd.Traces.Count; j++)
			{
				if (times[j] == 0)
					continue;
				Candidates.Add(new double[] { times[j], fdd.Traces[j][i] });
			}
		}

		var groups = Candidates.GroupBy(x => x[0]).ToList();
		var averages = new Dictionary<double, double>();
		foreach (var group in groups)
		{
			var sum = 0d;
			var n = 0;
			foreach (var v in group)
			{
				sum += v[1];
				n++;
			}
			averages[group.Key] = sum / n;
		}

		Averages = averages.Select(d => new double[] { d.Key, d.Value }).ToList();
		var refValues = new Dictionary<double, int>();
		if (!fdd.References.TryGetValue(measurement, out var references))
			return;

		var refMax = double.MinValue;
		var refDate = references[0].Date;
		var timeDiff = refDate >= fdd.MeasurementStart[0] ? new TimeSpan()
				: (fdd.MeasurementStart[0] - refDate) ?? new TimeSpan();
		List<double> referenceValues = new();
		foreach (var (d, v) in references)
		{
			refMax = Math.Max(refMax, v);
			References.Add(new double[] { d.Add(timeDiff).Subtract(UnixTime).TotalMilliseconds, v });
			if (referenceValues.Count == 0)
			{
				referenceValues.Add(v);
				continue;
			}
			if (referenceValues[^1] != v)
				referenceValues.Add(v);

			if (!refValues.ContainsKey(v))
				refValues.Add(v, 0);
			refValues[v]++;
		}

		if (refValues.Count == 0)
			return;

		var averageValues = averages.Values.ToArray();
		var averageTimes = times.Where(w => w != 0).ToArray();
		var (averagePoints, timeGroups) = Signal.GetAveragePoint(averageValues, averageTimes, borderGap);

		for (int i = 0; i < timeGroups.Count; i++)
		{
			var v = i < referenceValues.Count ? referenceValues[i] : -1000;
			for (int j = timeGroups[i].Start + borderGap; j < timeGroups[i].Stop - borderGap; j++)
			{
				if (v != -1000)
					CrossPlotPoints.Add(new double[] { v, averageValues[j] });
				GuidedPoints.Add(new double[] { averageTimes[j], averagePoints[i][1] });
			}
		}

		AveragePoints = averagePoints;
		var averageIndex = 0;
		for (int i = 0; i < AveragePoints.Count; i++)
		{
			var second = AveragePoints[i][0];
			var aDate = UnixTime.AddMilliseconds(second);
			if (refDate > aDate)
				continue;
			averageIndex = i;
			break;
		}

		var refArray = refValues.Keys.ToList();
		var refMaxId = refArray.IndexOf(refMax);
		var refStart = refMaxId < refArray.Count / 2 ? refMaxId : 0;
		var avgPoints = new List<double>();
		var refPoints = new List<double>();

		for (int j = refStart; j < refArray.Count; j++)
		{
			var avgIdx = j + averageIndex;
			if (avgIdx > AveragePoints.Count - 1 || AveragePoints[avgIdx][1] < 0)
				continue;
			refPoints.Add(refArray[j]);
			avgPoints.Add(AveragePoints[avgIdx][1]);
			ReferencePoints.Add(new double[] { refPoints[^1], avgPoints[^1] });
		}
		if (refPoints.Count < 2)
		{
			for (int i = refPoints.Count; i < 3; i++)
			{
				refPoints.Insert(0, 0);
				avgPoints.Insert(0, 0);
			}
		}
		//var p = MathNet.Numerics.Fit.Line(refArray, refPoints);
		(Intercept, Slope) = MathNet.Numerics.LinearRegression.SimpleRegression.Fit(refPoints.ToArray(), avgPoints.ToArray());
		for (int i = 0; i < refPoints.Count; i++)
		{
			RegressionPoints.Add(new double[] { refPoints[i], refPoints[i] * Slope + Intercept });
		}
	}
}

public class Signal
{
	private static readonly Microsoft.ML.OnnxRuntime.SessionOptions options = new();

	public static (int[], double[][]) Inference(Tensor<float> inputs, string modelFile)
	{
		using var session = new InferenceSession(modelFile, options);
		var inputMeta = session.InputMetadata.First();
		var dim = inputMeta.Value.Dimensions;
		//dim[0] = traces.Count;

		var data = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor(inputMeta.Key, inputs) };
		using var results = session.Run(data);
		var o = results.First().AsTensor<float>();
		var rx = new int[inputs.Dimensions[0]];
		var pb = new double[o.Dimensions[0]][];
		for (int i = 0; i < rx.Length; i++)
		{
			var max = float.MinValue;
			for (int j = 0; j < 4; j++)
			{
				if (o[i, j] > max)
				{
					max = o[i, j];
					rx[i] = j;
				}
			}

            var sum = 0d;
			var exp = new float[o.Dimensions[1]];
            for (int j = 0; j < exp.Length; j++)
            {
				exp[j] = MathF.Exp(o[i, j] - max);
                sum += exp[j];
            }
			pb[i] = exp.Select(v => v / sum).ToArray();
        }
		return (rx, pb);
	}

	public static float[] Softmax(float[] values)
	{
        var maxVal = values.Max();
		var o = new double[values.Length];
		var sum = 0d;
		for (int i = 0; i < o.Length; i++)
		{
			o[i] = Math.Exp(values[i] - maxVal);
            sum += o[i];
        }
		return o.Select(v => (float)( v / sum)).ToArray();
    }

	public static double[][] Resize(IList<double[]> traces, int width = 512)
	{
		if (traces.Count == 0)
			return Array.Empty<double[]>();
		var (transpose, min, max) = Transpose(traces);
		var scaller = max - min;
		using var img = new SixLabors.ImageSharp.Image<L16>(transpose[0].Length, transpose.Length);
		for (int y = 0; y < transpose.Length; y++)
		{
			for (int x = 0; x < transpose[y].Length; x++)
			{
				var r = transpose[y][x];
				var f = (r - min) / scaller;
				var v = (ushort)(f * ushort.MaxValue);
				img[x, y] = new L16(v);
			}
		}
		img.Mutate(x => x.Resize(width, img.Height));
        var inputs = new double[img.Height][];
        img.ProcessPixelRows(accessor => { 
			for (int y = 0; y < accessor.Height; y++)
			{
                var pixelRow = accessor.GetRowSpan(y);
                inputs[y] = new double[accessor.Width];
				for (int x = 0; x < inputs[y].Length; x++)
				{
                    ref var pixel = ref pixelRow[x];
                    var f = 1.0 * pixel.PackedValue / ushort.MaxValue;
                    var v = (f * scaller) + min;
					inputs[y][x] = v;
				}
			}
        });
		return inputs;
	}

	// min max scaller 0 1, v - min / (max - min)
	public static (double[][] Values, double Min, double Max) Transpose(IList<double[]> traces)
	{
        var transpose = new double[traces[0].Length][];
		var min = double.MaxValue;
		var max = double.MinValue;
        for (int i = 0; i < transpose.Length; i++)
        {
            transpose[i] = new double[traces.Count];
            for (int j = 0; j < transpose[i].Length; j++)
            {
                transpose[i][j] = traces[j][i];
				min = Math.Min(min, transpose[i][j]);
				max = Math.Max(max, transpose[i][j]);
            }
        }
		return (transpose, min, max);
    }

	public static Tensor<float> Slider(IList<double[]> traces, int[] dim)
	{
        Tensor<float> inputs = new DenseTensor<float>(dim);
        var len = Math.Min(1280, traces[0].Length);

        for (int i = 0; i < inputs.Dimensions[0]; i++)
        {
            for (int k = 0; k < len; k++)
            {
                inputs[i, 0, k] = i < 2 ? 0 : (float)traces[i - 2][k];
                inputs[i, 1, k] = i < 1 ? 0 : (float)traces[i - 1][k];
                inputs[i, 2, k] = (float)traces[i][k];
                inputs[i, 3, k] = i > len - 1 ? 0 : (float)traces[i + 1][k];
                inputs[i, 4, k] = i > len - 2 ? 0 : (float)traces[i + 2][k];
            }
        }
		return inputs;
    }

	public static (IList<int>, IList<double>) GetBoundary(FrequencyShiftDistance fdd, string modelPath, int nFiber = 6, int width=512)
	{
        var ix = Resize(fdd.Traces, width);
        var ng = new NumberToGrid(-20, 30, 64);
        var inputs = ng.ToTensor(ix);
		var (pred, probabilities) = Inference(inputs, modelPath);
        var distances = new double[fdd.Distance.Count - 1];
		var idx = 2;// transitional probablities
        for (int i = 2; i < ix.Length - 2; i++)
        {
            for (int j = i - 2; j < i + 2; j++)
            {
                distances[i] += Distance.Euclidean(ix[j], ix[i]) / 4;
            }
			distances[i] *= probabilities[i][idx];
        }

		// find the first and last 
		// list candidates
		int start = 0;
		int stop = pred.Length - 1;
		for (int i = 0, j = pred.Length - 1; i < pred.Length - 1; i++, j--)
		{
			// search forward
			if (start == 0 && pred[i] != 0 && pred[i+1] == 1)
			{
				start = i+1;
			}

            // search backward
            if (stop == pred.Length -1 && pred[j] != 0 && pred[j - 1] == 1)
            {
				stop = j-1;
            }
        }

		List<(int Index, double Value)> candidates = new();
		var range = 5;
		for (int i = start + range; i < stop-range; i++)
		{
			if (pred[i-1] == 2 && pred[i] == 1)
				candidates.Add((i, distances[i]));
		}

        //candidates.Add((731, distances[731]));
        //candidates.RemoveAt(3);
        //candidates.RemoveAt(2);
        //candidates.RemoveAt(1);

        // if candidate more than possible fiber remove
        // if candidate less than possible fiber add
        // recursively check when candidate less 
		
        if (candidates.Count > 0 && candidates.Count < nFiber - 1)
		{
			if (candidates.Count == 1)
			{
				//candidates.Insert(0, (start, distances[start]));
				candidates.Add((stop, distances[stop]));
			}
			int p = 0;
		FindMore:
            int maxP = candidates.Count - 1;
			// candidates.Insert(0, (start, distances[start]));
			// candidates.Add((stop, distances[stop]));
            // find the boundary between found candidates			
            while (candidates.Count < nFiber + 1)
			{
				var distance = double.MinValue;
				var index = 0;
				for (int i = candidates[p].Index; i < candidates[p+1].Index; i++)
				{
					if (distance < distances[i] && (i > candidates[p].Index + range && i < candidates[p+1].Index - range))
					{
                        distance = distances[i];
                        index = i;
					}
				}
				if (index > 0)
					candidates.Add((index, distance));
				p++;
				if (p == maxP)
					break;
			}

			candidates.Sort((p1, p2) => p1.Index.CompareTo(p2.Index));
			if (candidates.Count > 0 && candidates.Count < (nFiber + (candidates[^1].Index == stop ? 1 : 0)))
			{
				if (candidates[^1].Index == stop)
					p--;
				goto FindMore;
			}
			if (candidates[0].Index == start)
				candidates.RemoveAt(0);
			if (candidates[^1].Index == stop)
				candidates.RemoveAt(candidates.Count - 1);
		}

		if (candidates.Count > nFiber - 1 + (candidates[^1].Index == stop ? 1 : 0))
        {
            candidates.Sort((p1, p2) => p1.Value.CompareTo(p2.Value));
            candidates.RemoveRange(0, candidates.Count - (nFiber - 1));
        }

        candidates.Sort((p1, p2) => p1.Index.CompareTo(p2.Index));
        candidates.Insert(0, (start, distances[start]));
		if (candidates[^1].Index != stop)
			candidates.Add((stop, distances[stop]));

        fdd.Categories = new(pred);
		if (fdd.Boundaries.Count != nFiber || fdd.BoundaryIndexes.Count != nFiber)
		{
			fdd.BoundaryIndexes = candidates.Select(s => s.Index).ToList();
			fdd.Boundaries.Clear();
			foreach (var index in fdd.BoundaryIndexes)
			{
				fdd.Boundaries.Add(fdd.Distance[index]);
			}
		}

		return (candidates.Select(s => s.Index).ToList(), distances);
	}

	public static (List<double[]> Averages, List<Group> Groups) GetAveragePoint(double[] averages, double[]? x = null, int borderGap = 10)
	{
		var conv = Convolution(averages);
		var convDist = new double[conv.Length];
		var timeGroups = new List<Group>();
		var treshold = 0.001;
		var groupId = 0;
		timeGroups.Add(new(0, 0, groupId));
		bool start = false;
		for (int i = 0; i < conv.Length - 1; i++)
		{
			// get distance 
			convDist[i] = MathNet.Numerics.Distance.Euclidean(
					new double[] { i, conv[i] }, new double[] { i + 1, conv[i + 1] });
			if (i == 0)
				continue;
			// find hill valey pattern and get center of it
			var delta = convDist[i] - convDist[i - 1];
			if (!start && delta > treshold)
			{
				start = true;
			}

			if (start && Math.Abs(delta) > treshold && delta < 0)
			{
				timeGroups[^1].Stop = i;
				groupId++;
				timeGroups.Add(new(i, 0, groupId));
				start = false;
			}
		}

		if (timeGroups[^1].Stop == 0)
			timeGroups.RemoveAt(timeGroups.Count - 1);

		var averagePoint = new List<double[]>();
		foreach (var group in timeGroups)
		{
			var sum = 0d;
			for (int i = group.Start + borderGap; i < group.Stop - borderGap; i++)
			{
				sum += averages[i];
			}
			var pX = (group.Start + (group.Stop - group.Start) / 2);			
			averagePoint.Add(new[] { x is null ? pX : x[pX], sum / (group.Stop - group.Start - borderGap * 2) });
		}
		return (averagePoint, timeGroups);
	}


	public static Complex32[] GaussianKernel(int width, double sigma)
	{
		Complex32[] kernel = new Complex32[width + 1 + width];
		for (int i = -width; i <= width; i++)
		{
			kernel[width + i] = (float)(Math.Exp(-(i * i) / (2 * sigma * sigma)) / (Math.PI * 2 * sigma * sigma));
		}
		Fourier.Forward(kernel, FourierOptions.Matlab);
		return kernel;
	}

	public static double[] Convolution(double[] input, double sigma = 5)
	{
		var kernel = GaussianKernel((input.Length /2), sigma);
		var inputC = new Complex32[kernel.Length];
		for (int i = 0; i < input.Length; i++)
		{
			inputC[i] = (float)input[i];
		}
		var kernelV = Vector<Complex32>.Build.Dense(kernel);
		Fourier.Forward(inputC, FourierOptions.Matlab);
		var inputV = Vector<Complex32>.Build.Dense(inputC);
		var pwm = kernelV.PointwiseMultiply(inputV);
		Fourier.Inverse(pwm.AsArray(), FourierOptions.Matlab);
		var cx = pwm;//.Conjugate();
		//flipud
		var o = new double[pwm.Count - 1];
		for (int i = 0, x = cx.Count - 1; x > 0; i++, x--)
		{
			var b = Trig.Atan(cx[x].ToComplex());
			var sign = b.Real < 0 ? -1 : 1;
			o[i] = b.Magnitude * sign * 180 / Math.PI;
		}

		var v = new double[o.Length];
		for (int i = o.Length / 2, k = o.Length - 1, j = 0; i > 0; i--, j++, k--)
		{
			v[j] = o[i] / 3;
			v[j + o.Length / 2] = o[k] / 3;
		}

		return v;
	}
}

