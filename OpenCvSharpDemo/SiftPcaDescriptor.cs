using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using OpenCvSharp;
using OpenCvSharp.CPlusPlus;

namespace OpenCvSharpDemo
{
	static class SiftPcaDescriptor
	{
		public const int AllFeaturesCount = 3042;
		public const int FilteredFeaturesCount = 36;
		public const int PatchSize = 41;

		public static readonly float[,] Eigenvectors = ReadEigenVectors();

		private static SIFT sift = new SIFT();

		static float[,] ReadEigenVectors()
		{
			float[,] res = new float[AllFeaturesCount, FilteredFeaturesCount];
			using (StreamReader sr = new StreamReader("eigenvectors.txt"))
			{
				for (int i = 0; i < AllFeaturesCount; i++)
				{
					var line = sr.ReadLine();
					var elements = line.Split(null).Where((arg) => arg.Length > 0).ToArray();
					for (int j = 0; j < FilteredFeaturesCount; j++)
					{
						res[i, j] = float.Parse(elements[j], CultureInfo.InvariantCulture);
					}
				}
			}
			return res;
		}

		private const int noctaveLayers = 3;
		private const int firstOctave = -1;

		public static float[] GetValues(Mat image, KeyPoint key)
		{

			var pyrs = sift.BuildGaussianPyramid(image, 7);
			var octave = key.Octave & 255;
			var layer = (key.Octave >> 8) & 255;
			octave = octave < 128 ? octave : (-128 | octave);
			var scale = octave >= 0 ? 1f / (1 << octave) : 1 << -octave;
			//var size = key.Size * scale;

			var x0 = key.Pt.X * scale;
			var y0 = key.Pt.Y * scale;

			var angle = 360f - key.Angle;
			if (Math.Abs(angle - 360f) < 1e-6)
				angle = 0f;

			//size /= 2;

			var cos = Math.Cos(angle*Math.PI/180);
			var sin = Math.Sin(angle*Math.PI/180);

			Console.WriteLine((octave - firstOctave) * (noctaveLayers + 3) + layer);

			var mat = pyrs[(octave - firstOctave) * (noctaveLayers + 3) + layer];

			int gsize = AllFeaturesCount; // = (PatchSize - 2) * (PatchSize - 2) * 2;

			var v = new float[gsize];
			int count = 0;

			for (int y = 1; y < PatchSize - 1; y++)
			{
				for (int x = 1; x < PatchSize - 1; x++)
				{
					var xp = x0 + x * cos - y * sin;
					var yp = y0 + x * sin + y * cos;

					if (0 <= xp || xp >= mat.Width || 0 <= yp || yp >= mat.Height)
					{
						v[count++] = 0;
						v[count++] = 0;
					}
					else
					{

						var x1 = mat.At<byte>(y, x + 1);
						var x2 = mat.At<byte>(y, x - 1);

						var y1 = mat.At<byte>(y + 1, x);
						var y2 = mat.At<byte>(y - 1, x);

						// would normally divide by 2, but we normalize later
						float gx = x1 - x2;
						float gy = y1 - y2;

						v[count] = gx;
						v[count + 1] = gy;

						count += 2;
					}
				}

			}

			float sum = v.Sum();
			for (int i = 0; i < v.Length; i++)
			{
				v[i] /= sum;
			}
				
			var result = new float[FilteredFeaturesCount];
			for (int i = 0; i < FilteredFeaturesCount; i++)
			{
				for (int j = 0; j < AllFeaturesCount; j++)
				{
					result[i] += Eigenvectors[j, i]*v[i];
				}

			}
			var view = new Mat();
			Cv2.DrawKeypoints(image, new KeyPoint[] { key }, view);
			var view2 = new Mat();

			Console.WriteLine(x0 + " " + y0);

			Cv2.DrawKeypoints(image, new KeyPoint[] { new KeyPoint(new CvPoint2D32f(x0, y0), 1),  }, view2);
			using (new Window("obj image", view))
			using (new Window("obj image2", view2))
			{
				Cv2.WaitKey();
			}

			return result;
		}
	}
}
