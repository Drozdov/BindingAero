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

		public double[,] Stitch(Mat imgScene, Mat imgObject, double[,] homographyStart)
		{
			var algo = new SIFT();
			
			KeyPoint[] keypointsScene, keypointsObject;
			var descriptorsScene = new MatOfFloat();
			var descriptorsObj = new MatOfFloat();

			algo.Run(imgScene, null, out keypointsScene, descriptorsScene);
			algo.Run(imgObject, null, out keypointsObject, descriptorsObj);

			Console.WriteLine(descriptorsScene.Width);
			Console.WriteLine(descriptorsScene.Height);
			Console.WriteLine(descriptorsObj.Width);
			Console.WriteLine(descriptorsObj.Height);
			Console.WriteLine(keypointsScene.Count());
			Console.WriteLine(keypointsObject.Count());
			
			var matcher = new FlannBasedMatcher();//new BFMatcher());
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

			

			//matches = matches.Where((m) => (keypointsObject[m.QueryIdx].Octave > 0)).ToArray();

			var sortedMatches = matches.OrderBy((m) => m.Distance);
			matches = sortedMatches.Where((m, id) => id < 20).ToArray();

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
				DrawMatchings(imgScene, imgObject, result, keypointsObject, keypointsScene, matches);
			}

			return result;
		}

		private static void DrawMatchings(Mat imgScene, Mat imgObject, double[,] h, KeyPoint[] keypointsObject,
		                                  KeyPoint[] keypointsScene, DMatch[] matches)
		{
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
			Cv2.DrawMatches(imgObject, keypointsObject, imgScene, keypointsScene, matches, view);

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
