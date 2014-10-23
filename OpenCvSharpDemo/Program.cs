﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;
using OpenCvSharp.CPlusPlus;

// use NuGet
namespace OpenCvSharpDemo
{
    class Program
    {
        static bool Eq(double[] arr1, double[] arr2)
        {
            for (int i = 0; i < arr1.Length; i++)
            {
                if (arr1[i] != arr2[i])
                    return false;
            }
            return true;
        }

        public static void Main(string[] arg)
        {
	        
            LucasKanadeAlgo lc = new LucasKanadeTranslate();
            lc.UseOriginGradients = false;

            //Mat scene = new Mat("../../../scene.png", LoadMode.GrayScale);
            //Mat obj = new Mat("../../../obj.png", LoadMode.GrayScale);
            Mat scene = new Mat("../../../toksovo_scene.png", LoadMode.GrayScale);
            Mat obj = new Mat("../../../toksovo_obj.png", LoadMode.GrayScale);
            //obj = obj.Resize(new Size(obj.Size().Width * 10 / 35, obj.Size().Height * 10 / 35));

            //Mat scene = new Mat("../../../abrau_scene.png");
            //Mat obj = new Mat("../../../abrau_obj.png");

	        //scene = scene.ExtractChannel(1);
	        //obj = obj.ExtractChannel(1);

	        MakeEqualBright(scene, obj);
            lc.ImgScene = scene;
            lc.ImgObj = obj;

	       /* var h = new double[,] {{1, 0, 200}, {0, 1, 60}, {0, 0, 1}};
			Affine.DrawImageOver(scene, obj, h);
			h = new KeyPointStitcher(true).Stitch(scene, obj);

			Affine.DrawImageOver(scene, obj, h);
			
			return;*/

            var t = new double[] { 180, 50, 0.2, 1, 0, 0};//-0.2, 0.15 };//-1, 1};
            //var d = lc.LucasKanadeStep(t, 100);
            
            double[] p;
            
            int diff = lc.ImgsDiff(t);
            Console.WriteLine(diff);
            Affine.DrawImageOver(scene, obj, lc.PointsConvertation);

            var lccc = new LucasKanadeScaleNoMove();
            lccc.ImgScene = scene;
            lccc.ImgObj = obj;

            foreach (int pyramid in new int[] { 64, 32, 16, 8, 4, 2, 1 })
            {
				if (pyramid == 64)
				{
					lc = new LucasKanadeEuclidean();
					lc.ImgScene = scene;
					lc.ImgObj = obj;
				}
				if (pyramid == 4)
				{
					lc = new LucasKanadeSimilarity();
					lc.ImgScene = scene;
					lc.ImgObj = obj;
				}
                Console.WriteLine("Pyramid = " + pyramid);
				lc.PyramidLevel = pyramid;
                lccc.PyramidLevel = pyramid;
                foreach (int scale in new int[] { 128, 64, 32, 16, 8, 4, 2, 1 })//200, 110, 70, 35, 20, 10, 7, 5, 3, 2, 1})
                {
                    foreach (var lcc in new LucasKanadeAlgo[] { lc/*, lccc*/ })
                    {


                        //lc.Scale = scale;
                        Console.WriteLine("scale = " + scale);
                        diff = int.MaxValue;
                        for (int i = 0; /* i < 10 */; i++)
                        //while ((p = lc.LucasKanadeStep(t)) != null)
                        {
                            lcc.Scale = scale;
                            p = lcc.LucasKanadeStepAutoScale(t, scale);
                            //p = lc.LucasKanadeStep(t);
                            Console.WriteLine(lcc.ImgsDiff(p));
                            var diff0 = lcc.ImgsDiff(p);
                            if (p == null || Eq(p, t))// || diff0 < diff)
                                break;

                            diff = diff0;
                            t = p;


                            using (new Window("src image", scene))
                            using (new Window("obj image", obj))
                            using (new Window("gr x", lcc.GradientsX))
                            using (new Window("gr y", lcc.GradientsY))
                            //using (new Window("error", lc.ErrorImg))
                            {
                                Affine.DrawImageOver(scene, obj, lcc.PointsConvertation);
                                //Cv2.WaitKey();
                            }
                        }
                    }
                }
            }



            using (new Window("src image", scene))
            using (new Window("dst image", obj))

            {
                Affine.DrawImageOver(scene, obj, lc.PointsConvertation);
                Cv2.WaitKey();
            }

        }

        static void MakeEqualBright(Mat img1, Mat img2)
        {
            int sum1 = 0, sum2 = 0;
            for (int i = 0; i < img1.Width; i++)
            {
                for (int j = 0; j < img1.Height; j++)
                {
                    sum1 += img1.Get<byte>(j, i);
                }
            }
            for (int i = 0; i < img2.Width; i++)
            {
                for (int j = 0; j < img2.Height; j++)
                {
                    sum2 += img2.Get<byte>(j, i);
                }
            }
            double average1 = 1.0 * sum1 / (img1.Width * img1.Height);
            double average2 = 1.0 * sum2 / (img2.Width * img2.Height);
            if (average2 > average1)
            {
                for (int i = 0; i < img2.Width; i++)
                {
                    for (int j = 0; j < img2.Height; j++)
                    {
                        img2.Set<byte>(j, i, (byte)(Math.Min(255, img2.Get<byte>(j, i) * average1 / average2)));
                    }
                }
            }
            else
            {
                for (int i = 0; i < img1.Width; i++)
                {
                    for (int j = 0; j < img1.Height; j++)
                    {
                        img1.Set<byte>(j, i, (byte)(Math.Min(255, img1.Get<byte>(j, i) * average2 / average1)));
                    }
                }
            }
        }

    }
}
