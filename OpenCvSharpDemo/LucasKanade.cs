using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;
using OpenCvSharp.CPlusPlus;

namespace OpenCvSharpDemo
{
    class LucasKanade
    {
        public int[] LucasKanadeStep(int[] t, int scale)
        {
            // Transform the image
            //Translation(img2, img2t, t);
            // Compute the gradients and summed error by comparing img1 and img2t
            var A = new double[2, 2];
            var b = new double[2];
            int diff = 1;
            errorImg = new Mat(new Size(img2.Width - 2, img2.Height - 2), MatType.CV_8SC1);
            gradientsX = new Mat(new Size(img2.Width - 2, img2.Height - 2), MatType.CV_8SC1);
            gradientsY = new Mat(new Size(img2.Width - 2, img2.Height - 2), MatType.CV_8SC1);
            for (int y = diff; y < img2.Height - diff; y++)
            { // ignore borders
                for (int x = diff; x < img2.Width - diff; x++)
                {
                    // If both have full alphas, then compute and accumulate the error
                    var e = img2.At<byte>(y, x) - img1.At<byte>(y + t[1], x + t[0]);
                    errorImg.Set<byte>(y - diff, x - diff, (byte) (127 - Math.Abs(e)));
                    // Accumulate the matrix entries
                    double gx, gy;
                    if (!useOriginGradients)
                    {
                        gx = 0.5 * (img1.At<byte>(y + t[1], x + diff + t[0]) - img1.At<byte>(y + t[1], x - diff + t[0]));
                        gy = 0.5 * (img1.At<byte>(y + diff + t[1], x + t[0]) - img1.At<byte>(y - diff + t[1], x + t[0]));
                    }
                    else
                    {
                        gx = 0.5 * (img2.At<byte>(y, x + diff) - img2.At<byte>(y, x - diff));
                        gy = 0.5 * (img2.At<byte>(y + diff, x) - img2.At<byte>(y - diff, x));
                    }
                    gradientsX.Set<byte>(y - diff, x - diff, (byte)(gx / 2 + 63));
                    gradientsY.Set<byte>(y - diff, x - diff, (byte)(gy / 2 + 63));

                    A[0, 0] += gx * gx; A[0, 1] += gx * gy;
                    A[1, 0] += gx * gy; A[1, 1] += gy * gy;
                    b[0] += e * gx; b[1] += e * gy;
                }
            }
            double det = 1.0 / (A[0, 0] * A[1, 1] - A[1, 0] * A[0, 1]);
            var t0 = (int)(det * scale * (A[1, 1] * b[0] - A[1, 0] * b[1]));
            var t1 = (int)(det * scale * (A[0, 0] * b[1] - A[0, 1] * b[0]));
            return new int[] { t[0] + t0, t[1] + t1 };
        }

        bool useOriginGradients = false;
        public bool UseOriginGradients { set { this.useOriginGradients = value; } }

        private Mat img1, img2;
        public Mat ImgScene {
            set { img1 = value; }
        }
        public Mat ImgObj {
            set { img2 = value; }
        }

        private Mat errorImg;
        public Mat ErrorImg { get { return errorImg; } }

        private Mat gradientsX;
        public Mat GradientsX { get { return gradientsX; } }

        private Mat gradientsY;
        public Mat GradientsY { get { return gradientsY;  } }

        private double scale = 10;
        public double Scale
        {
            get { return scale; }
            set
            {
                scale = value;
                lastVector = null;
            }
        }

        public long ImgsDiff(int[] t)
        {
            long res = 0;
            for (int y = 0; y < img2.Height; y++)
            {
                for (int x = 0; x < img2.Width; x++)
                {
                    var e = img2.At<byte>(y + t[1], x + t[0]) - img1.At<byte>(y, x);
                    res += e * e;// / 10000;
                }
            }
            return res;
        }

        private int[] lastVector;

        public int[] LucasKanadeStepNorm(int[] t)
        {
            var vector = LucasKanadeStep(t, 256);
            var diff = new int[] {vector[0] - t[0], vector[1] - t[1]};
            if (diff[0] == 0 && diff[1] == 0)
                return null;
            var norm = (int)Math.Sqrt(diff[0] * diff[0] + diff[1] * diff[1]);
            if (scale > 1)
            {
                diff[0] = (int)scale * diff[0] / norm;
                diff[1] = (int)scale * diff[1] / norm;
            }
            else
            {
                if (Math.Abs(diff[0]) > Math.Abs(diff[1]))
                {
                    diff[0] = diff[0] > 0 ? 1 : -1;
                    diff[1] = 0;
                }
                else if (diff[1] > 0)
                {
                    diff[0] = 0;
                    diff[1] = diff[1] > 0 ? 1 : -1;
                }
                else
                {
                    return null;
                }
            }
            if (lastVector != null && lastVector[0] * diff[0] + lastVector[1] * diff[1] < 0)
                return null;
            lastVector = diff;
            return new int[] {t[0] + diff[0], t[1] + diff[1]};
        }
    }
}
