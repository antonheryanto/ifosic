using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace MMU.Ifosic.Web.Models;

public class Signal
{
	private static readonly Microsoft.ML.OnnxRuntime.SessionOptions options = new();

	public static double[] Inference(IList<double[]> traces, string modelFile)
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
		var rx = new double[traces.Count];
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
}