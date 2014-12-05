using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenCvSharp;
using OpenCvSharp.CPlusPlus;

namespace OpenCvSharpDemo
{
	/// <summary>
	/// Based on SIFT keypoints detector algorithm method to stitch scenes to objects
	/// </summary>
	public class KeyPointStitcher : IStitcher
	{
		public KeyPointStitcher(bool showMatchings = false)
		{
			ShowMatchings = showMatchings;
		}

		public bool ShowMatchings { get; set; }

		public double[,] Stitch(Mat imgScene, Mat imgObject)
		{
			return Stitch(imgScene, imgObject, null);
		}

		private int GetOctave(KeyPoint kpt)
		{

			// keep octave below 256 (255 is 1111 1111)
			int octave = kpt.Octave & 255;
			// if octave is >= 128, ...????
			octave = octave < 128 ? octave : (-128 | octave);
			// 1/2^absval(octave)
			float scale = octave >= 0 ? 1.0f / (1 << octave) : (float)(1 << -octave);
			// multiply the point's radius by the calculated scale
			float scl = kpt.Size * 0.5f * scale;

			//Console.WriteLine(scl);

			// the constant sclFactor is 3 and has the following comment:
			// determines the size of a single descriptor orientation histogram
			//float histWidth = scl;
			// descWidth is the number of histograms on one side of the descriptor
			// the long float is sqrt(2)
			//int radius = (int)(histWidth * 1.4142135623730951f * (descWidth + 1) * 0.5f);

			return kpt.Octave & 0xFF00;
		}

		public double[,] Stitch(Mat imgScene, Mat imgObject, double[,] homographyStart)
		{
			var algo = new SIFT();
			KeyPoint[] keypointsScene, keypointsObject;
			var descriptorsScene = new MatOfFloat();
			var descriptorsObj = new MatOfFloat();
			
			algo.Run(imgScene, null, out keypointsScene, descriptorsScene);
			algo.Run(imgObject, null, out keypointsObject, descriptorsObj);

			keypointsScene = keypointsScene.Where((key) => key.Size > 10).ToArray();
			keypointsObject = keypointsObject.Where((key) => key.Size > 10).ToArray();

			descriptorsScene = SiftPcaDescriptor.GetDescriptors(imgScene, keypointsScene);
			descriptorsObj = SiftPcaDescriptor.GetDescriptors(imgObject, keypointsObject);
			

			Console.WriteLine(descriptorsScene.Height);
			Console.WriteLine(descriptorsScene.Width);

			var matcher = new BFMatcher();
			var matches = matcher.Match(descriptorsObj, descriptorsScene);

			if (homographyStart != null)
			{
				int j = 0;
				var nmatches = new DMatch[matches.Count()];
				foreach (var match in matches)
				{
					var p = keypointsObject[match.QueryIdx].Pt;
					var point0 = PerspectiveTransform(p, homographyStart);
					var p1 = keypointsScene[match.TrainIdx].Pt;
					nmatches[j++] = new DMatch(match.QueryIdx, match.TrainIdx, match.Distance * (300 + (float)point0.DistanceTo(p1)));
				}
				matches = nmatches;
			}
			else
			{
				int j = 0;
				var nmatches = new DMatch[matches.Count()];
				foreach (var match in matches)
				{
					var p = keypointsObject[match.QueryIdx];
					var p1 = keypointsScene[match.TrainIdx];
					nmatches[j++] = new DMatch(match.QueryIdx, match.TrainIdx, match.Distance * (1 + Math.Abs(p1.Size - 0.9f * p.Size)));
				}
				matches = nmatches;
			}

			

			matches = matches.Where((m) => (GetOctave(keypointsObject[m.QueryIdx]) > 1)).ToArray();

			var sortedMatches = matches.OrderBy((m) => m.Distance);
			matches = sortedMatches.Where((m, id) => id < 20).ToArray();

			float[][] vv1 = new float[20][];
			float[][] vv2 = new float[20][];
			var k = 0;

            /*foreach (var m in matches)
            {
				var v1 = SiftPcaDescriptor.GetValues(imgObject, keypointsObject[m.QueryIdx]);
                var m1 = SiftPcaDescriptor.win;
	            vv1[k] = v1;
                var v2 = SiftPcaDescriptor.GetValues(imgScene, keypointsScene[m.TrainIdx]);
                var m2 = SiftPcaDescriptor.win;
	            vv2[k++] = v2;

				DrawPoints(imgObject, new KeyPoint[] { keypointsObject[m.QueryIdx] }, 1, 0);
				DrawPoints(imgScene, new KeyPoint[] { keypointsScene[m.TrainIdx] }, 1, 0);
                
				Console.WriteLine("=================");
				for (int i = 0; i < 36; i++)
				{
					Console.WriteLine(v1[i] + " " + v2[i]);
				}

					using (new Window("1", m1))
					using (new Window("2", m2))
					{
						DrawMatchings(imgScene, imgObject, homographyStart, keypointsObject, keypointsScene,
									  new DMatch[] { m });
					}
				 
            }

			System.IO.StreamWriter file = new System.IO.StreamWriter("out.txt");
			
			for (int i = 0; i < 20; i++)
			{
				for (int j = 0; j < 20; j++)
				{
					var v1 = vv1[i];
					var v2 = vv2[j];
					var diff = 0f;
					for (int z = 0; z < 36; z++)
					{
						diff += Math.Abs(vv1[i][z] - vv2[j][z]);
					}
					file.Write("{0,10:0.0}  ", diff / 1e6);
				}
				file.WriteLine();
			}

			file.Close();*/


			int n = matches.Count();

			var pt1 = new CvPoint2D32f[n];
			var pt2 = new CvPoint2D32f[n];
			for (int i = 0; i < n; i++)
			{
				pt1[i] = keypointsObject[matches[i].QueryIdx].Pt;
				pt2[i] = keypointsScene[matches[i].TrainIdx].Pt;
			}

			var pt1Mat = new CvMat(matches.Count(), 2, MatrixType.F32C1, pt1);
			var pt2Mat = new CvMat(matches.Count(), 2, MatrixType.F32C1, pt2);

			var h = new CvMat(3, 3, MatrixType.F64C1);

			Cv.FindHomography(pt1Mat, pt2Mat, h);

			var result = new double[,] { { h[0], h[1], h[2] }, { h[3], h[4], h[5] }, { h[6], h[7], h[8] } };
			
			if (ShowMatchings)
			{
				/*foreach (var match in matches)
				{
					DrawPoints(imgObject, new KeyPoint[] { keypointsObject[match.QueryIdx] }, 1, 0);
					DrawPoints(imgScene, new KeyPoint[] { keypointsScene[match.TrainIdx] }, 1, 0);
				}*/
				DrawMatchings(imgScene, imgObject, result, keypointsObject, keypointsScene, matches);
			}

			return result;
		}

		private static void DrawPoints(
			Mat img, ///< [in/out] Image for drawing
			IEnumerable<KeyPoint> keypoints, ///< [in]     Key points founded on image
			int pointsToDraw, ///< [in]     How many points to draw
			float minSize ///< [in]     Minimum key point size
			)
		{
			//Dictionary<int, int> sh = new Dictionary<int, int>();
			var drawedPoints = 0;
			foreach (var kp in keypoints)
			{
				//sh[(int) kp.Size]++;
				if (kp.Size > minSize)
				{
					Point p = new Point(kp.Pt.X, kp.Pt.Y);


					img.Circle(p, (int) kp.Size * 5, new CvScalar(0, 255, 0));
					float angle = (float) ((kp.Angle*Math.PI)/180.0f);
					Point p2 = new Point((int) (p.X + kp.Size*Math.Cos(angle) * 5), (int) (p.Y + kp.Size*Math.Sin(angle) * 5));
					img.Line(p, p2, new CvScalar(0, 255, 0));
					++drawedPoints;
					if (drawedPoints > pointsToDraw)
					{
						break;
					}
				}
			}
		}

		private static void DrawMatchings(Mat imgScene, Mat imgObject, double[,] h, KeyPoint[] keypointsObject,
		                                  KeyPoint[] keypointsScene, DMatch[] matches)
		{

			/*var imgScene_ = imgScene.Clone();
			var imgObj_ = imgObject.Clone();
			DrawPoints(imgScene_, keypointsScene, null, 200, 10);
			DrawPoints(imgObj_, keypointsObject, null, 60, 10);
			using (new Window("1", imgScene_))
			using (new Window("2", imgObj_))
				Cv.WaitKey();
			return;*/

			//DrawPoints(imgScene, keypointsScene, 1000, 10);
			//DrawPoints(imgObject, keypointsObject, 1000, 10);
			


			var points = new CvPoint2D32f[4];
			points[0] = new Point2f(0, 0);
			points[1] = new Point2f(imgObject.Width, 0);
			points[2] = new Point2f(imgObject.Width, imgObject.Height);
			points[3] = new Point2f(0, imgObject.Height);

			for (int i = 0; i < points.Count(); i++)
			{
				points[i] = PerspectiveTransform(points[i], h);
			}

			var view = new Mat();
			Cv2.DrawMatches(imgObject, keypointsObject, imgScene, keypointsScene, matches, view, null, null, null, DrawMatchesFlags.NotDrawSinglePoints);

			for (int i = 0; i < points.Count(); i++)
			{
				points[i].X += imgObject.Width;
			}

			for (int i = 0; i < points.Count(); i++)
			{
				view.Line((int)(points[i].X), (int)points[i].Y,
						  (int)points[(i + 1) % points.Count()].X, (int)points[(i + 1) % points.Count()].Y,
						  new CvScalar(0, 255, 0), 2);
			}

			using (new Window("SIFT matching", WindowMode.AutoSize, view))
			{
				Cv2.WaitKey();
			}
		}

		

		private static Point2f PerspectiveTransform(Point2f point, double[,] h)
		{
			double x = point.X;
			double y = point.Y;
			double Z = 1.0 / (h[2, 0] * x + h[2, 1] * y + h[2, 2]);
			double X = (h[0, 0] * x + h[0, 1] * y + h[0, 2]) * Z;
			double Y = (h[1, 0] * x + h[1, 1] * y + h[1, 2]) * Z;
			return new Point2f(Cv.Round(X), Cv.Round(Y));
		}
	}
}
