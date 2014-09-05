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
        static bool Eq(int[] arr1, int[] arr2)
        {
            for (int i = 0; i < arr1.Length; i++)
            {
                if (arr1[i] != arr2[i])
                    return false;
            }
            return true;
        }

        static void Main(string[] args)
        {
            var lc = new LucasKanade();

            Mat scene = new Mat("../../../scene_lake.png", LoadMode.GrayScale);
            Mat obj = new Mat("../../../object_lake.png", LoadMode.GrayScale);

            lc.ImgScene = scene;
            lc.ImgObj = obj;

            Console.WriteLine(scene.Width);
            int[] t = new int[] { 150, 150 };
            /*int[] d = lc.LucasKanadeStep(scene, obj, t);

            while (!Eq(d, t))
            {
                Console.WriteLine(d[0] + " " + d[1]);
                t = d;
                d = lc.LucasKanadeStep(scene, obj, t);
            }
            Console.WriteLine(d[0] + " " + d[1]);*/

            byte color = 0;

            int[] p;
            for (double scale = 16; scale > 0; scale -= 5)
            {
                lc.Scale = scale;
                Console.WriteLine("scale = " + scale);
                while ((p = lc.LucasKanadeStepNorm(t)) != null)
                {
                    t = p;
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
                    using (new Window("error", lc.ErrorImg))
                    {
                        Cv2.WaitKey();
                    }
                }
            }

            /*var oldDiff = lc.ImgsDiff(scene, obj, t);
            for (int scale = 128; scale > 0; )
            {
                var p = lc.LucasKanadeStep(scene, obj, t, scale);
                var diff = lc.ImgsDiff(scene, obj, p);
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
                    using (new Window("error", lc.ErrorImg))
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
                    using (new Window("error", lc.ErrorImg))
                    {
                        Cv2.WaitKey();
                    }
                }
                //Console.WriteLine(oldDiff + " " + diff);
                //Console.WriteLine(p[0] + " " + p[1]);
                //Console.WriteLine(t[0] + " " + t[1]);
                //Console.WriteLine("------------");

             

               

            }*/

            
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
            using (new Window("dst image", obj))
            using (new Window("error image x", lc.GradientsX))
            using (new Window("error image y", lc.GradientsY))
            {
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
    }
}
