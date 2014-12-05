//#define useRecalc

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

#if (useRecalc)
		private const string fileToRead = "eigenvectors2.txt"; 
#else
		private const string fileToRead = "eigenvectors.txt"; 
#endif

		public const int AllFeaturesCount = 3042;
		public const int FilteredFeaturesCount = 36;
		public const int PatchSize = 41;

		public static readonly float[,] Eigenvectors = ReadEigenVectors();
        public static float[] Averages;

		private static SIFT sift = new SIFT();

		static float[,] ReadEigenVectors()
		{
            Averages = new float[AllFeaturesCount];
			float[,] res = new float[AllFeaturesCount, FilteredFeaturesCount];
            using (var sr = new StreamReader(fileToRead))
            {
#if (!useRecalc)
				for (int i = 0; i < AllFeaturesCount; i++)
                {
                    Averages[i] = float.Parse(sr.ReadLine().Trim(), CultureInfo.InvariantCulture);
                }
#endif
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
        private const float sigma = 1.6f;
        private const int patchMag = 20;
        private const float log2 = 0.69314718056f;
        private const int scalesPerOctave = 3;
        private static Mat[] pyrs;
        private static Mat image;

        public static Mat win;

		public static float[] GetFullValues(Mat image, KeyPoint key)
		{
			if (image != SiftPcaDescriptor.image)
			{
				SiftPcaDescriptor.image = image;
				pyrs = sift.BuildGaussianPyramid(image, 7);
			}
			//var octave = key.Octave & 255;
			//var layer = (key.Octave >> 8) & 255;
			//octave = octave < 128 ? octave : (-128 | octave);

			var gscale = key.Size / 2;
			float tmp = (float)(Math.Log(gscale / sigma) / log2 + 1.0);
			var octave = (int)tmp;
			var fscale = (tmp - octave) * (float)scalesPerOctave;
			var _scale = (int)Math.Round(fscale);
			if (_scale == 0 && octave > 1)
			{
				_scale = scalesPerOctave;
				octave--;
				fscale += scalesPerOctave;
			}
			if (octave < 1)
			{
				_scale = scalesPerOctave;
				octave = 1;
				fscale -= scalesPerOctave;
			}

			var x0 = key.Pt.X;
			var y0 = key.Pt.Y;

			var sx = x0 / Math.Pow(2.0, octave - 1);
			var sy = y0 / Math.Pow(2.0, octave - 1);

			double angle = 360f - key.Angle;
			if (Math.Abs(angle - 360f) < 1e-6)
				angle = 0f;
			angle = angle * Math.PI / 180.0f;
			//angle += Math.PI/2;

			int patchsize;
			int iradius;
			float sine, cosine;
			float sizeratio;

			var scale = (float)(sigma * Math.Pow(2.0, fscale / scalesPerOctave));

			patchsize = (int)(patchMag * scale);

			// make odd
			patchsize /= 2;
			patchsize = patchsize * 2 + 1;

			if (patchsize < PatchSize)
				patchsize = PatchSize;

			sizeratio = (float)patchsize / (float)PatchSize;

			win = new Mat(new Size(patchsize, patchsize), MatType.CV_8UC1);

			sine = (float)Math.Sin(angle);
			cosine = (float)Math.Cos(angle);

			iradius = patchsize / 2;

			//var _octave = key.Octave & 255;
			//var layer = (key.Octave >> 8) & 255;
			//_octave = _octave < 128 ? _octave : (-128 | _octave);

			var mat = pyrs[(octave - 1) * (noctaveLayers + 3) + _scale];


			/* Examine all points from the gradient image that could lie within the
			   index square.
			*/

			//fprintf(stderr, "Scale %f  %d\n", scale, patchsize);

			//fprintf(stderr, "Key Patch of orientation %f\n", key->ori);
			for (int y = -iradius; y <= iradius; y++)
				for (int x = -iradius; x <= iradius; x++)
				{

					// calculate sample window coordinates (rotated along keypoint)
					float cpos = (float)((cosine * x + sine * (float)y) + sx);
					float rpos = (float)((-sine * x + cosine * (float)y) + sy);

					win.Set(y + iradius, x + iradius, (byte)mat.GetPixelBI(cpos, rpos));

					//fprintf(stderr, "  (%d, %d) -> (%f, %f)\n", j, i, cpos, rpos);
				}

			var result = new float[AllFeaturesCount];
			int count = 0;

			var grx = new Mat(new Size(PatchSize - 2, PatchSize - 2), MatType.CV_8SC1);
			var gry = new Mat(new Size(PatchSize - 2, PatchSize - 2), MatType.CV_8SC1);


			for (int y = 1; y < PatchSize - 1; y++)
			{
				for (int x = 1; x < PatchSize - 1; x++)
				{
					float x1 = win.GetPixelBI((float)(x + 1) * sizeratio, (float)y * sizeratio);
					float x2 = win.GetPixelBI((float)(x - 1) * sizeratio, (float)y * sizeratio);

					float y1 = win.GetPixelBI((float)x * sizeratio, (float)(y + 1) * sizeratio);
					float y2 = win.GetPixelBI((float)x * sizeratio, (float)(y - 1) * sizeratio);

					// would need to divide by 2 (span 2 pixels), but we normalize anyway
					// so it's not necessary
					float gx = x1 - x2;
					float gy = y1 - y2;

					grx.Set(y - 1, x - 1, (sbyte)gx);
					gry.Set(y - 1, x - 1, (sbyte)gy);

					result[count] = gx;
					result[count + 1] = gy;

					count += 2;
				}
				//fprintf(stderr, "\n");
			}

			NormVec(result);
			return result;
		}

		public static float[] GetValues(Mat image, KeyPoint key)
		{

			var result = GetFullValues(image, key);
			var vector = new float[FilteredFeaturesCount];
			for (var i = 0; i < FilteredFeaturesCount; i++)
			{
				vector[i] = 0;
				for (var j = 0; j < AllFeaturesCount; j++)
				{
					vector[i] += Eigenvectors[j, i]*result[j];
				}
			}
			//return vector;

            var view = new Mat();
			Cv2.DrawKeypoints(image, new KeyPoint[] { key }, view);
			var view2 = new Mat();

			
			/*Cv2.DrawKeypoints(mat, new KeyPoint[] { new KeyPoint(new CvPoint2D32f(sx, sy), 1),  }, view2);
			using (new Window("obj image", WindowMode.NormalGui, view))
            using (new Window("obj image2", view2))
            using (new Window("win", win))
			using (new Window("grx", grx))
			using (new Window("gry", gry))
            {
                //foreach (var mat_ in pyrs)
                //{
                //    using (new Window("pyr", mat_))
                        Cv2.WaitKey();
                //}
			}*/
			return vector;
		}

		public static MatOfFloat GetDescriptors(Mat mat, KeyPoint[] keypoints)
		{
			var result = new MatOfFloat(keypoints.Count(), FilteredFeaturesCount);
			for (int i = 0; i < keypoints.Count(); i++)
			{
				GetValues(mat, keypoints[i]);
				result.SetArray(i, 0, GetValues(mat, keypoints[i]));
			}
			return result;
		}

		private static void NormVec(float[] v)
		{
			var sum = v.Sum(f => Math.Abs(f)) / v.Count();

			for (int i = 0; i < v.Count(); i++)
			{
				v[i] = 100*v[i]/(sum*256);// -Averages[i];
			}
		}

		private const int KeysPerMat = 500;
		public static void GenerateEigenVectors(IEnumerable<Mat> images)
		{
			PCA pca = new PCA();
			var inp = new Mat(new Size(AllFeaturesCount, KeysPerMat * images.Count()), MatType.CV_32F);
			int row = 0;
			foreach (var mat in images)
			{
				Console.WriteLine("New mat");
				var keys = sift.Run(mat, null);
				keys = keys.OrderBy((k) => k.Size).ToArray();
				var size = keys[Math.Min(KeysPerMat, keys.Count())].Size;
				keys = keys.Where((key, id) => id < KeysPerMat).ToArray();
				foreach (var key in keys)
				{
					var vals = GetFullValues(mat, key);
					inp.SetArray(row++, 0, vals);
				}
			}
			Console.WriteLine("Start pca");
			var result = pca.Compute(inp, new Mat(), PCAFlag.DataAsRow);
			Console.WriteLine("Finish pca");
			var eigenVecs = result.Eigenvectors;
			var file = new System.IO.StreamWriter("eigenvectors2.txt");
			for (int i = 0; i < AllFeaturesCount; i++)
			{
				for (int j = 0; j < FilteredFeaturesCount; j++)
				{
					file.Write("{0,10} ", eigenVecs.Get<float>(j, i));
				}
				file.WriteLine();
			}
			file.Close();
		}
	}

    internal static class MatExtensions
    {
        public static float GetPixelBI(this Mat mat, float col, float row)
        {
            int irow, icol;
            float rfrac, cfrac;
            float row1 = 0, row2 = 0;

            irow = (int)row;
            icol = (int)col;

            if (irow < 0 || irow >= mat.Height
                || icol < 0 || icol >= mat.Width)
            {
                //Console.WriteLine(icol + " " + irow);
                return 0;

            }
            if (row > mat.Height - 1)
                row = mat.Height - 1;

            if (col > mat.Width - 1)
                col = mat.Width - 1;

            rfrac = 1f - (row - (float)irow);
            cfrac = 1f - (col - (float)icol);


            if (cfrac < 1)
            {
                row1 = cfrac * mat.Get<byte>(irow, icol) + (1f - cfrac) * mat.Get<byte>(irow, icol + 1);
            }
            else
            {
                row1 = mat.Get<byte>(irow, icol);
            }

            if (rfrac < 1)
            {
                if (cfrac < 1)
                {
                    row2 = cfrac * mat.Get<byte>(irow + 1, icol) + (1f - cfrac) * mat.Get<byte>(irow + 1, icol + 1);
                }
                else
                {
                    row2 = mat.Get<byte>(irow + 1, icol);
                }
            }

            return (rfrac * row1 + (1f - rfrac) * row2);
        }
    }
}
