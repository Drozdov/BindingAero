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

		public struct Test
		{
			private Mat scene, obj;
			private double[,] homography;

			public Mat Scene { get { return scene; } }
			public Mat Object { get { return obj; } }
			public double[,] Homography { get { return homography; } }

			public Test(Mat scene, Mat obj, double[,] h)
			{
				this.scene = scene;
				this.obj = obj;
				this.homography = h;
			}
		}

		static Test test0 = new Test(
			new Mat("../../../abrau_scenefull.png", LoadMode.GrayScale),
			Resize(new Mat("../../../abrau_obj.jpg", LoadMode.GrayScale), 6),
			new double[,] { { 0.32, 0.64, 600 }, { -0.64, 0.32, 750 }, { 0, 0, 1 } }
			);

		static Test test1 = new Test(
			new Mat("../../../abrau_scenefull.png", LoadMode.GrayScale),
			Resize(new Mat("../../../abrau_obj_.jpg", LoadMode.GrayScale), 6), 
			new double[,] {{0.32, 0.64, 250}, {-0.64, 0.32, 550}, {0, 0, 1}}
			);

	    private static Test test2 = new Test(
		    new Mat("../../../abrau_scene2full.png", LoadMode.GrayScale),
		    Resize(new Mat("../../../abrau_obj2.jpg", LoadMode.GrayScale), 6),
		    new double[,] {{0.6, 0.5, -100}, {-0.6, 0.5, 600}, {0, 0, 1}}
		    );

	    private static Test test3 = new Test(
		    new Mat("../../../abrau_scene3full.png", LoadMode.GrayScale),
		    Resize(new Mat("../../../abrau_obj3.jpg", LoadMode.GrayScale), 3),
		    new double[,] {{0, 1, 800}, {-1, 0, 600}, {0, 0, 1}}
		    );



	    private static Mat Resize(Mat orig, int times)
	    {
		    return orig.Resize(new Size(orig.Width/times, orig.Height/times));
	    }

	    public static void Main(string[] arg)
        {
	        
            LucasKanadeAlgo lc = new LucasKanadeTranslate();
            lc.UseOriginGradients = false;

            //Mat scene = new Mat("../../../scene.png", LoadMode.GrayScale);
            //Mat obj = new Mat("../../../obj.png", LoadMode.GrayScale);
            
	        //scene = scene.Resize(new Size(scene.Width, scene.Height));
            //obj = obj.Resize(new Size(obj.Size().Width * 10 / 35, obj.Size().Height * 10 / 35));

            //Mat scene = new Mat("../../../abrau_scene.png");
            //Mat obj = new Mat("../../../abrau_obj.png");

	        //scene = scene.ExtractChannel(1);
	        //obj = obj.ExtractChannel(1);

	        Test test = test3;
	        Mat scene = test.Scene;
	        Mat obj = test.Object;
	        var h = test.Homography;

	        MakeEqualBright(scene, obj);
            lc.ImgScene = scene;
            lc.ImgObj = obj;

	        Affine.DrawImageOver(scene, obj, h);
			h = new KeyPointStitcher(true).Stitch(scene, obj, h);

			Affine.DrawImageOver(scene, obj, h);
			
			return;
			
            var t = new double[] { 260, 80, 0, 0.5, 0, 0};//-0.2, 0.15 };//-1, 1};
            //var d = lc.LucasKanadeStep(t, 100);
            
            double[] p;
            
            int diff = lc.ImgsDiff(t);
            Console.WriteLine(diff);
            Affine.DrawImageOver(scene, obj, lc.PointsConvertation);
            foreach (int pyramid in new int[] { 64, 32, 16, 8, 4, 2, 1 })
            {
				/*if (pyramid == 64)
				{
					lc = new LucasKanadeEuclidean();
					lc.ImgScene = scene;
					lc.ImgObj = obj;
				}*/
				if (pyramid == 4)
				{
					lc = new LucasKanadeSimilarity();
					lc.ImgScene = scene;
					lc.ImgObj = obj;
				}
                Console.WriteLine("Pyramid = " + pyramid);
				lc.PyramidLevel = pyramid;
                foreach (int scale in new int[] { 128, 64, 32, 16, 8, 4, 2, 1 })//200, 110, 70, 35, 20, 10, 7, 5, 3, 2, 1})
                {
                    
                    //lc.Scale = scale;
                    Console.WriteLine("scale = " + scale);
                    diff = int.MaxValue;
                    for (int i = 0; /* i < 10 */; i++)
                    //while ((p = lc.LucasKanadeStep(t)) != null)
                    {
                        lc.Scale = scale;
                        p = lc.LucasKanadeStepAutoScale(t, scale);
                        //p = lc.LucasKanadeStep(t);
                        Console.WriteLine(lc.ImgsDiff(p));
                        var diff0 = lc.ImgsDiff(p);
                        if (p == null || Eq(p, t))// || diff0 < diff)
                            break;

                        diff = diff0;
                        t = p;


                        using (new Window("src image", scene))
                        using (new Window("obj image", obj))
                        using (new Window("gr x", lc.GradientsX))
                        using (new Window("gr y", lc.GradientsY))
                        //using (new Window("error", lc.ErrorImg))
                        {
                            Affine.DrawImageOver(scene, obj, lc.PointsConvertation);
                            //Cv2.WaitKey();
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
