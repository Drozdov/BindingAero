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
        public static float[] Averages;

		private static SIFT sift = new SIFT();

		static float[,] ReadEigenVectors()
		{
            Averages = new float[AllFeaturesCount];
			float[,] res = new float[AllFeaturesCount, FilteredFeaturesCount];
            using (StreamReader sr = new StreamReader("eigenvectors.txt"))
            {
                for (int i = 0; i < AllFeaturesCount; i++)
                {
                    Averages[i] = float.Parse(sr.ReadLine().Trim(), CultureInfo.InvariantCulture);
                }
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
		public static float[] GetValues(Mat image, KeyPoint key)
		{
            if (image != SiftPcaDescriptor.image)
            {
                SiftPcaDescriptor.image = image; 
                pyrs = sift.BuildGaussianPyramid(image, 7);
            }
			//var octave = key.Octave & 255;
			//var layer = (key.Octave >> 8) & 255;
			//octave = octave < 128 ? octave : (-128 | octave);

            var gscale = key.Size;
            float tmp = (float)(Math.Log(gscale / sigma) / log2 + 1.0);
            var octave = (int)tmp;
            var fscale = (tmp - octave) * (float)scalesPerOctave;
            var _scale = (int)Math.Round(fscale);
            if (_scale == 0 && octave > 0)
            {
                _scale = scalesPerOctave;
                octave--;
                fscale += scalesPerOctave;
            }

            // TODO: scale /= ??

            var x0 = key.Pt.X;
			var y0 = key.Pt.Y;

            var sx = x0 / Math.Pow(2.0, octave - 1);
            var sy = y0 / Math.Pow(2.0, octave - 1);

            /*var angle = 360f - key.Angle;
			if (Math.Abs(angle - 360f) < 1e-6)
				angle = 0f;*/
            var angle = key.Angle;

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

            win = new Mat(new Size(patchsize, patchsize), MatType.CV_8S);
            
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

                    win.Set<byte>(y + iradius, x + iradius, mat.GetPixelBI(cpos, rpos));

                    //fprintf(stderr, "  (%d, %d) -> (%f, %f)\n", j, i, cpos, rpos);
                }

            var result = new float[AllFeaturesCount];
            int count = 0;

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


                    result[count] = gx;
                    result[count + 1] = gy;

                    count += 2;
                }
                //fprintf(stderr, "\n");
            }
            #region comment
            //size /= 2;

            /*var cos = Math.Cos(angle*Math.PI/180);
            var sin = Math.Sin(angle*Math.PI/180);

            //Console.WriteLine((octave - firstOctave) + " " + ((octave - firstOctave) * (noctaveLayers + 3) + layer));

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

            }*/
            #endregion

            var view = new Mat();
			Cv2.DrawKeypoints(image, new KeyPoint[] { key }, view);
			var view2 = new Mat();

			
			/*Cv2.DrawKeypoints(mat, new KeyPoint[] { new KeyPoint(new CvPoint2D32f(sx, sy), 1),  }, view2);
			using (new Window("obj image", WindowMode.NormalGui, view))
            using (new Window("obj image2", view2))
            using (new Window("win", WindowMode.NormalGui, win))
            {
                //foreach (var mat_ in pyrs)
                //{
                //    using (new Window("pyr", mat_))
                        Cv2.WaitKey();
                //}
			}*/

            return null;// result;
		}
	}

    internal static class MatExtensions
    {
        public static byte GetPixelBI(this Mat mat, float col, float row)
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
                row1 = cfrac * mat.Get<sbyte>(irow, icol) + (1f - cfrac) * mat.Get<sbyte>(irow, icol + 1);
            }
            else
            {
                row1 = mat.Get<sbyte>(irow, icol);
            }

            if (rfrac < 1)
            {
                if (cfrac < 1)
                {
                    row2 = cfrac * mat.Get<sbyte>(irow + 1, icol) + (1f - cfrac) * mat.Get<sbyte>(irow + 1, icol + 1);
                }
                else
                {
                    row2 = mat.Get<sbyte>(irow + 1, icol);
                }
            }

            return (byte)((sbyte)(rfrac * row1 + (1.0 - rfrac) * row2));
        }
    }
}
