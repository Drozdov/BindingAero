using OpenCvSharp.CPlusPlus;

namespace OpenCvSharpDemo
{
	/// <summary>
	/// Common interface for photo stitchers
	/// </summary>
	public interface IStitcher
	{
		/// <summary>
		/// Finds homography for given scene and object images.
		/// </summary>
		/// <param name="scene"></param>
		/// <param name="obj"></param>
		/// <returns>Homography matrix</returns>
		double[,] Stitch(Mat scene, Mat obj);

		/// <summary>
		/// Finds homography for given scene and object images with the use of estimated homography matrix 
		/// </summary>
		/// <param name="scene"></param>
		/// <param name="obj"></param>
		/// <param name="homographyStart"></param>
		/// <returns>Homography matrix</returns>
		double[,] Stitch(Mat scene, Mat obj, double[,] homographyStart);
	}
}
