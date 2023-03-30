using MathNet.Numerics.LinearAlgebra;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using MathNet.Numerics.LinearAlgebra;
using System;
using MathNet.Numerics.IntegralTransforms;
using MathNet.Numerics;
using MMU.Ifosic.Models;

namespace MMU.Ifosic.Models;

public class Signal
{
	private static readonly Microsoft.ML.OnnxRuntime.SessionOptions options = new();

	public static int[] Inference(IList<double[]> traces, string modelFile)
	{
		using var session = new InferenceSession(modelFile, options);
		var inputMeta = session.InputMetadata.First();
		var dim = inputMeta.Value.Dimensions;
		dim[0] = traces.Count;
		Tensor<float> inputs = new DenseTensor<float>(dim);
		var len = Math.Min(1280, traces[0].Length);
		for (int i = 0; i < inputs.Dimensions[0]; i++)
		{
			for (int k = 0; k < len; k++)
			{
				inputs[i, 0, k] = i < 2 ? 0 : (float)traces[i-2][k];
				inputs[i, 1, k] = i < 1 ? 0 : (float)traces[i-1][k];
				inputs[i, 2, k] = (float)traces[i][k];
				inputs[i, 3, k] = i > len - 1 ? 0 : (float)traces[i+1][k];
				inputs[i, 4, k] = i > len - 2 ? 0 : (float)traces[i+2][k];
			}
		}

		var data = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor(inputMeta.Key, inputs) };
		using var results = session.Run(data);
		var o = results.First().AsTensor<float>();
		var rx = new int[traces.Count];
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
		}
		return rx;
	}

	public static List<double[]> GetAveragePoint(double[] averages, double[]? x = null)
	{
		var conv = Signal.Convolution(averages);
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
				timeGroups[timeGroups.Count - 1].Y = i;
				groupId++;
				timeGroups.Add(new(i, 0, groupId));
				start = false;
			}
		}

		var averagePoint = new List<double[]>();
		foreach (var group in timeGroups)
		{
			var sum = 0d;
			for (int i = group.X; i < group.Y; i++)
			{
				sum += averages[i];
			}
			var pX = (group.X + (group.Y - group.X) / 2);			
			averagePoint.Add(new[] { x is null ? pX : x[pX], sum / (group.Y - group.X) });
		}
		return averagePoint;
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

	public static double[] Convolution(double[] input)
	{
		var kernel = GaussianKernel((input.Length /2), 5);
		var inputC = new Complex32[kernel.Length];
		for (int i = 0; i < input.Length; i++)
		{
			inputC[i] = (float)input[i];
		}
		Vector<Complex32> kernelV = Vector<Complex32>.Build.Dense(kernel);
		Fourier.Forward(inputC, FourierOptions.Matlab);
		Vector<Complex32> inputV = Vector<Complex32>.Build.Dense(inputC);
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

