﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenCvSharp;
using OpenCvSharp.CPlusPlus;

namespace OpenCvSharpDemo
{
    class LucasKanadeAlgo
    {
        protected Mat imgScene, imgObj;
        protected Mat imgSceneOrig, imgObjOrig;
        public Mat ImgScene
        {
            get { return imgSceneOrig; }
            set
            {
                imgScene = value;
                imgSceneOrig = value;
                pyramidLevel = 1;
            }
        }

        public Mat ImgObj
        {
            get { return imgObjOrig; }
            set
            {
                imgObj = value;
                imgObjOrig = value;
                pyramidLevel = 1;
            }
        }

		public LucasKanadeData LucasKanadeData { get; set; }

		public LucasKanadeAlgo(LucasKanadeData data = null)
		{
			LucasKanadeData = data ?? new LucasKanadeData();
		}

        protected void Translate(int x0, int y0, out int x, out int y, bool orig = true)
        {
            x = Translate(x0, y0, 0, orig);
            y = Translate(x0, y0, 1, orig);
        }

        protected int Translate(int x0, int y0, int dim, bool orig)
        {
			var mat = HomographyMatrix;
	        var d = (int) (x0 * mat[2, 0] + y0 * mat[2, 1] + mat[2, 2]);
            return (int) (x0 * mat[dim, 0] + y0 * mat[dim, 1] + mat[dim, 2] / (orig ? d : d * pyramidLevel));
        }

	    private double[,] homography = new double[,] {{1, 0, 0}, {0, 1, 0}, {0, 0, 1}};
	    public Double[,] HomographyMatrix
	    {
		    get { return Multiply(homography, LucasKanadeData.HomographyMatrix); }
			set { homography = value; }
	    }

		private double[,] Multiply(double[,] mat1, double[,] mat2)
		{
			if (mat1.GetLength(1) != mat2.GetLength(0))
				return null;
			int r = mat1.GetLength(1);
			int m = mat1.GetLength(0);
			int n = mat2.GetLength(1);
			var res = new double[m, n];
			for (int i = 0; i < m; i++)
			{
				for (int j = 0; j < n; j++)
				{
					for (int k = 0; k < r; k++)
					{
						res[i, j] += mat1[i, k]*mat2[k, j];
					}
				}
			}
			return res;
		}

        protected int pyramidLevel = 1;
        public int PyramidLevel
        {
            set
            {
                pyramidLevel = value;
                var size = imgObjOrig.Size();
                imgObj = imgObjOrig.Resize(new Size(size.Width / pyramidLevel, size.Height / pyramidLevel));
                size = imgSceneOrig.Size();
                imgScene = imgSceneOrig.Resize(new Size(size.Width / pyramidLevel, size.Height / pyramidLevel));
            }
        }

        private int pixelSize = 1;
        protected Mat Warped
        {
            get
            {
                pixelSize = 1;
                Mat res = new Mat(new Size(imgObj.Size().Width, imgObj.Size().Height), MatType.CV_8U);
                int x, y;
                for (int y0 = 0; y0 < imgObj.Height; y0++)
                {
                    for (int x0 = 0; x0 < imgObj.Width; x0++)
                    {
                        int value = 0;
                        for (int y1 = y0; y1 < y0 + pixelSize; y1++)
                        {
                            for (int x1 = x0; x1 < x0 + pixelSize; x1++)
                            {
                                Translate(x1, y1, out x, out y, false);
                                value += imgScene.Get<byte>(y, x);
                            }
                        }
                            
                        res.Set<byte>(y0, x0, (byte) (value / (pixelSize * pixelSize)));
                    }
                }
                return res;
            }
        }

        Mat gradientsX, gradientsY;
        public Mat GradientsX { get { return gradientsX; } }
        public Mat GradientsY { get { return gradientsY; } }

        protected int[,,] Gradient(Mat mat)
        {
            gradientsX = new Mat(new Size(mat.Width, mat.Height), MatType.CV_8SC1);
            gradientsY = new Mat(new Size(mat.Width, mat.Height), MatType.CV_8SC1);
            var res = new int[mat.Height, mat.Width, 2];
            int diff = 1;
            for (int y = 0; y < mat.Height; y++)
            {
                for (int x = 0; x < mat.Width; x++)
                {
                    if (x < diff)
                        res[y, x, 0] = (mat.At<byte>(y, x + diff) - mat.At<byte>(y, x)) / diff;
                    else if (x > mat.Width - diff)
                        res[y, x, 0] = (mat.At<byte>(y, x) - mat.At<byte>(y, x - diff)) / diff;
                    else
                        res[y, x, 0] = (mat.At<byte>(y, x + diff) - mat.At<byte>(y, x - diff)) / (2 * diff);
                    if (y < diff)
                        res[y, x, 1] = (mat.At<byte>(y + diff, x) - mat.At<byte>(y, x)) / diff;
                    else if (y > mat.Height - diff)
                        res[y, x, 1] = (mat.At<byte>(y, x) - mat.At<byte>(y - diff, x)) / diff;
                    else
                        res[y, x, 1] = (mat.At<byte>(y + diff, x) - mat.At<byte>(y - diff, x)) / (2 * diff);

                    gradientsX.Set<byte>(y, x, (byte)(res[y, x, 0] / 2 + 63));
                    gradientsY.Set<byte>(y, x, (byte)(res[y, x, 1] / 2 + 63));
                }
            }
            return res;
        }

        double[] GetDp()
        {
            var warped = Warped;
            var gradient = Gradient(warped);
			var dim = LucasKanadeData.Indices.Length;
            var hessian = new Mat(new Size(dim, dim), MatType.CV_64F, 0);
            var hes = new int[dim, dim];
            var b = new int[dim];
            for (int y = 1; y < imgObj.Height - 1; y++)
            {
                for (int x = 1; x < imgObj.Width - 1; x++)
                {
					var jacobian = LucasKanadeData.Jacobian(x, y);
                    var p0 = new int[dim];
                    for (int i = 0; i < dim; i++)
                    {
                        p0[i] = (int) (gradient[y, x, 0] * jacobian[0, i] + gradient[y, x, 1] * jacobian[1, i]);
                    }
                    for (int i = 0; i < dim; i++)
                    {
                        for (int j = 0; j < dim; j++)
                        {
                            hessian.Set<double>(i, j, hessian.At<double>(i, j) + p0[i] * p0[j]);
                            hes[i, j] += p0[i] * p0[j];
                        }
                        b[i] += (imgObj.At<byte>(y, x) - warped.At<byte>(y, x)) * p0[i];
                    }
                }
            }
            try
            {
                var hessianInv = hessian.Inv();
				double[] res = new double[LucasKanadeData.Dimension];
                for (int i = 0; i < dim; i++)
                {
                    for (int j = 0; j < dim; j++)
                    {
						res[LucasKanadeData.Indices[i]] += hessianInv.Get<double>(i, j) * b[j];
                    }
                }
                return res;
            } catch
            {
                return null;
            }
            //Console.WriteLine(res[2] + " " + res[3]);
        }

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

        public double[] LucasKanadeStep(double[] t)
        {
            return LucasKanadeStep(t, (int) Scale);
        }

        public double[] LucasKanadeStep(double[] t, int scale)
        {
            if (t == null)
				t = new double[LucasKanadeData.Dimension];
			if (t.Length < LucasKanadeData.Dimension)
            {
				var t1 = new double[LucasKanadeData.Dimension];
                for (int i = 0; i < t.Length; i++)
                {
                    t1[i] = t[i];
                }
                t = t1;
            }
			LucasKanadeData.P = t;//.Select(x => (double) x).ToArray();
            var res0 = GetDp();
            var res = new double[res0.Length];
            for (int i = 0; i < res0.Length; i++)
            {
                res[i] = t[i] + (res0[i] * scale);
            }
            return res;
        }

        public double[] LucasKanadeStepAutoScale(double[] t, int step)
        {
            if (t == null)
				t = new double[LucasKanadeData.Dimension];
			if (t.Length < LucasKanadeData.Dimension)
            {
				var t1 = new double[LucasKanadeData.Dimension];
                for (int i = 0; i < t.Length; i++)
                {
                    t1[i] = t[i];
                }
                t = t1;
            }
			LucasKanadeData.P = t;//.Select(x => (double) x).ToArray();
            var res0 = GetDp();
            double[] result = null;
            int min = int.MaxValue;
            for (int j = 0; j < 10; j++)
            {
				var res = new double[LucasKanadeData.P.Length];
                for (int i = 0; i < res.Length; i++)
                {
                    res[i] = t[i] + (i < res0.Length ? (res0[i] * step * j) : 0);
                }
                int min0 = ImgsDiff(res);
                if (min0 < min)
                {
                    min = min0;
                    result = res;
                }
            }
            return result;
        }

        private int[] lastVector;
        public int[] LucasKanadeStepNorm(double[] t)
        {
            var vector = LucasKanadeStep(t, 1024);
            var diff = new int[] { (int)(vector[0] - t[0]), (int)(vector[1] - t[1]) };
            if (diff[0] == 0 && diff[1] == 0)
                return null;
            var norm = (int)Math.Sqrt(diff[0] * diff[0] + diff[1] * diff[1]);
            if (Scale > 1)
            {
                diff[0] = (int)Scale * diff[0] / norm;
                diff[1] = (int)Scale * diff[1] / norm;
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
            return new int[] { (int)t[0] + diff[0], (int)t[1] + diff[1] };
        }

        // TODO: not used yet
        bool useOriginGradients = false;
        public bool UseOriginGradients { set { this.useOriginGradients = value; } }

        public int ImgsDiff(double[] p) {
            int res = 0;
            try
            {
				LucasKanadeData.P = p;
	            int w = imgObjOrig.Size().Width, h = imgObjOrig.Size().Height;
	            //if (fail(0, 0) || fail(w, 0) || fail(0, h) || fail(w, h))
		        //    return int.MaxValue;
	            
                int x, y;
                for (int y0 = 0; y0 < imgObj.Height; y0++)
                {
                    for (int x0 = 0; x0 < imgObj.Width; x0++)
                    {
                        Translate(x0, y0, out x, out y, false);
                        int d;
                        if (x < 0 || x >= imgScene.Width || y < 0 || y >= imgScene.Height)
                            d = 127;
                        else
                            d = imgObj.Get<byte>(y0, x0) - imgScene.Get<byte>(y, x);
							//res += Math.Abs(imgObj.Get<byte>(y0, x0) - imgScene.Get<byte>(y, x));
                        res += d * d;
                    }
                }
            }
            catch (Exception e)
            { 
                return int.MaxValue;
            }
            return res;
        }

	    private bool fail(int x0, int y0)
	    {
		    int x, y;
		    Translate(x0, y0, out x, out y);
		    return x < 0 || x >= imgSceneOrig.Width || y < 0 || y >= imgSceneOrig.Height;
	    }

	    public virtual CvPoint2D32f[][] PointsConvertation
        {
            get
            {
                CvPoint2D32f[] srcPnt = new CvPoint2D32f[3];
                CvPoint2D32f[] dstPnt = new CvPoint2D32f[3];
                int[] x = new int[] { 0, imgObjOrig.Width - 1, 0 };
                int[] y = new int[] { 0, 0, imgObjOrig.Height - 1 };
                for (int i = 0; i < 3; i++)
                {
                    srcPnt[i] = new CvPoint2D32f(x[i], y[i]);
                    int x1, y1;
                    Translate(x[i], y[i], out x1, out y1);
                    dstPnt[i] = new CvPoint2D32f(x1, y1);
                }
                return new CvPoint2D32f[][] { srcPnt, dstPnt };
            }
        }
    }
}
