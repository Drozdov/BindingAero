using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenCvSharp;
using OpenCvSharp.CPlusPlus;

namespace OpenCvSharpDemo
{
    class Affine
    {
        static void Main2(String[] args)
        {
            new Affine();
        }

        public Affine()
        {
            // cvGetAffineTransform + cvWarpAffine
            using (IplImage srcImg = new IplImage("../../../scene.png", LoadMode.AnyDepth | LoadMode.AnyColor))
            using (IplImage dstImg = srcImg.Clone())
            {
                CvPoint2D32f[] srcPnt = new CvPoint2D32f[3];
                CvPoint2D32f[] dstPnt = new CvPoint2D32f[3];
                srcPnt[0] = new CvPoint2D32f(0, 0);
                srcPnt[1] = new CvPoint2D32f(srcImg.Width - 1, 0);
                srcPnt[2] = new CvPoint2D32f(0, srcImg.Height - 1);
                dstPnt[0] = new CvPoint2D32f(0, 0);
                dstPnt[2] = new CvPoint2D32f(dstImg.Width - 1, 0);
                dstPnt[1] = new CvPoint2D32f(0, dstImg.Height - 1);
                using (CvMat mapMatrix = Cv.GetAffineTransform(srcPnt, dstPnt))
                {
                    Cv.WarpAffine(srcImg, dstImg, mapMatrix, Interpolation.Linear | Interpolation.FillOutliers);
                    using (new CvWindow("src", srcImg))
                    using (new CvWindow("dst", dstImg))
                    {
                        Cv.WaitKey(0);
                    }
                    
                }
                
                
            }
        }

        public static void DrawImageOver(Mat scene, Mat template, CvPoint2D32f[][] pointsConvertation)
        {
            var srcPnt = pointsConvertation[0];
            var dstPnt = pointsConvertation[1];
            using (IplImage srcImg = template.ToIplImage())
            using (IplImage dstImg = scene.ToIplImage())
            {
                using (CvMat mapMatrix = Cv.GetAffineTransform(srcPnt, dstPnt))
                {
                    Cv.WarpAffine(srcImg, dstImg, mapMatrix, Interpolation.Linear);// | Interpolation.FillOutliers);
                    using (new CvWindow("src", srcImg))
                    using (new CvWindow("dst", dstImg))
                    {
                        Cv.WaitKey(0);
                    }

                }
            }
        }
    }
}
