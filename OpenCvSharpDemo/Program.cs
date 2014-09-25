using System;
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
            //var lc = new LucasKanadeAlgo();
            var lc = new LucasKanadeAffine();
            lc.UseOriginGradients = false;

            //Mat scene = new Mat("../../../scene.png", LoadMode.GrayScale);
            //Mat obj = new Mat("../../../obj.png", LoadMode.GrayScale);
            Mat scene = new Mat("../../../toksovo_scene.png", LoadMode.GrayScale);
            Mat obj = new Mat("../../../toksovo_obj.png", LoadMode.GrayScale);

            //MakeEqualBright(scene, obj);

            /*CvPoint2D32f[] srcPnt = new CvPoint2D32f[3];
            CvPoint2D32f[] dstPnt = new CvPoint2D32f[3];
                srcPnt[0] = new CvPoint2D32f(200.0f, 200.0f);
                srcPnt[1] = new CvPoint2D32f(250.0f, 200.0f);
                srcPnt[2] = new CvPoint2D32f(200.0f, 100.0f);
                dstPnt[0] = new CvPoint2D32f(300.0f, 100.0f);
                dstPnt[1] = new CvPoint2D32f(300.0f, 50.0f);
                dstPnt[2] = new CvPoint2D32f(200.0f, 100.0f);
                using (CvMat mapMatrix = Cv.GetAffineTransform(srcPnt, dstPnt))
                {
                    var img = scene.ToIplImage();

                    img.WarpAffine(mapMatrix, new Size());
                }*/

            lc.ImgScene = scene;
            lc.ImgObj = obj;

            var t = new double[] { 240, 20, -0.2, 0.15 };//-1, 1};
            //var d = lc.LucasKanadeStep(t, 100);


            byte color = 0;
            
            double[] p;
            
#if true
            int diff = lc.ImgsDiff(t);
            Console.WriteLine(diff);
            Affine.DrawImageOver(scene, obj, lc.PointsConvertation);

            foreach (int scale in new int[] { 100, 30, 5, 1 })//200, 110, 70, 35, 20, 10, 7, 5, 3, 2, 1})
            {
                
                lc.Scale = scale;
                Console.WriteLine("scale = " + scale);
                for (int i = 0; i < 10; i++)
                //while ((p = lc.LucasKanadeStep(t)) != null)
                    {
                        p = lc.LucasKanadeStepAutoScale(t, scale);
                        Console.WriteLine(lc.ImgsDiff(p));
                        var diff0 = lc.ImgsDiff(p);
                        if (Eq(p, t))// || diff0 - diff > -1000)
                            break;
                        diff = diff0;
                        t = p;


                        /*scene.Set<byte>(t[1], t[0], color);
                        scene.Set<byte>(t[1] - 1, t[0], color);
                        scene.Set<byte>(t[1] + 1, t[0], color);
                        scene.Set<byte>(t[1], t[0] - 1, color);
                        scene.Set<byte>(t[1], t[0] + 1, color);
                        scene.Set<byte>(t[1] - 2, t[0], color);
                        scene.Set<byte>(t[1] + 2, t[0], color);
                        scene.Set<byte>(t[1], t[0] - 2, color);
                        scene.Set<byte>(t[1], t[0] + 2, color);
                        scene.Set<byte>(t[1] - 3, t[0], color);
                        scene.Set<byte>(t[1] + 3, t[0], color);
                        scene.Set<byte>(t[1], t[0] - 3, color);
                        scene.Set<byte>(t[1], t[0] + 3, color);*/
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

#else
            var oldDiff = lc.ImgsDiff(t);
            for (int scale = 128; scale > 0; )
            {
                p = lc.LucasKanadeStep(t, scale);
                var diff = lc.ImgsDiff(p);
                if (oldDiff > diff)
                {
                    Console.WriteLine(oldDiff + "->" + diff);
                    Console.WriteLine("({0}, {1}) -> ({2}, {3})",  t[0], t[1], p[0], p[1]);
                    t = p;
                    oldDiff = diff;
                    scene.Set<byte>(t[1], t[0], color);
                    scene.Set<byte>(t[1] - 1, t[0], color);
                    scene.Set<byte>(t[1] + 1, t[0], color);
                    scene.Set<byte>(t[1], t[0] - 1, color);
                    scene.Set<byte>(t[1], t[0] + 1, color);
                    scene.Set<byte>(t[1] - 2, t[0], color);
                    scene.Set<byte>(t[1] + 2, t[0], color);
                    scene.Set<byte>(t[1], t[0] - 2, color);
                    scene.Set<byte>(t[1], t[0] + 2, color);
                    scene.Set<byte>(t[1] - 3, t[0], color);
                    scene.Set<byte>(t[1] + 3, t[0], color);
                    scene.Set<byte>(t[1], t[0] - 3, color);
                    scene.Set<byte>(t[1], t[0] + 3, color);

                    using (new Window("src image", scene))
                    using (new Window("obj image", obj))
                    using (new Window("gr x", lc.GradientsX))
                    using (new Window("gr y", lc.GradientsY))
                    //using (new Window("error", lc.ErrorImg))
                    {
                        Cv2.WaitKey();
                    }
                }
                else
                {
                    scale /= 2;
                    Console.WriteLine("Scale = " + scale);
                    using (new Window("src image", scene))
                    using (new Window("obj image", obj))
                    using (new Window("gr x", lc.GradientsX))
                    using (new Window("gr y", lc.GradientsY))
                    //using (new Window("error", lc.ErrorImg))
                    {
                        Cv2.WaitKey();
                    }
                }
                //Console.WriteLine(oldDiff + " " + diff);
                //Console.WriteLine(p[0] + " " + p[1]);
                //Console.WriteLine(t[0] + " " + t[1]);
                //Console.WriteLine("------------");

             

               

            }
#endif
            color = 255;
            /*scene.Set<byte>(t[1], t[0], color);
            scene.Set<byte>(t[1] - 1, t[0], color);
            scene.Set<byte>(t[1] + 1, t[0], color);
            scene.Set<byte>(t[1], t[0] - 1, color);
            scene.Set<byte>(t[1], t[0] + 1, color);
            scene.Set<byte>(t[1] - 2, t[0], color);
            scene.Set<byte>(t[1] + 2, t[0], color);
            scene.Set<byte>(t[1], t[0] - 2, color);
            scene.Set<byte>(t[1], t[0] + 2, color);
            scene.Set<byte>(t[1] - 3, t[0], color);
            scene.Set<byte>(t[1] + 3, t[0], color);
            scene.Set<byte>(t[1], t[0] - 3, color);
            scene.Set<byte>(t[1], t[0] + 3, color);*/

            using (new Window("src image", scene))
            using (new Window("dst image", obj))

            {
                Affine.DrawImageOver(scene, obj, lc.PointsConvertation);
                Cv2.WaitKey();
            }

            /*Mat src = new Mat("2.png", LoadMode.GrayScale);

            Console.WriteLine(src.Width + " " + src.Height);
            for (int j = 0; j < src.Height; j++) 
            {
                for (int i = 0; i < src.Width; i++)
                {
                    Console.Write(src.At<byte>(j, i) > 200 ? "1" : " ");
                }
                Console.WriteLine();
            }

            Mat dst = new Mat();

            Cv2.Canny(src, dst, 50, 200);
            using (new Window("src image", src))
            using (new Window("dst image", dst))
            {
                Cv2.WaitKey();
            }*/

            /*IplImage src = Cv.LoadImage("lenna.png", LoadMode.GrayScale);
            IplImage dst = Cv.CreateImage(new CvSize(src.Width, src.Height), BitDepth.U8, 1);
            Cv.Canny(src, dst, 50, 200);
            Cv.NamedWindow("src image");
            Cv.ShowImage("src image", src);
            Cv.NamedWindow("dst image");
            Cv.ShowImage("dst image", dst);
            Cv.WaitKey();
            Cv.DestroyAllWindows();
            Cv.ReleaseImage(src);
            Cv.ReleaseImage(dst);*/

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
