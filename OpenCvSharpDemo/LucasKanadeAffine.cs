using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenCvSharp;

namespace OpenCvSharpDemo
{
	internal class LucasKanadeAffine : LucasKanadeAlgo
	{
		public override Func<int, int, double[,]> Jacobian
		{
			get { return (x, y) => new double[,] {{1, 0, x, y, 0, 0}, {0, 1, 0, 0, x, y}}; }
		}

		public override double[,] Matrix
		{
			get { return new double[,] {{1 + p[2], p[3], p[0]}, {p[4], 1 + p[5], p[1]}}; }
		}
	}

	internal class LucasKanadeAffineNoScale : LucasKanadeAffine
	{
		public override Func<int, int, double[,]> Jacobian
		{
			get { return (x, y) => new double[,] {{1, 0, x, y, 0, 0}, {0, 1, 0, 0, x, y}}; }
		}
	}

	internal class LucasKanadeAffineNoTransform : LucasKanadeAffine
	{
		public override Func<int, int, double[,]> Jacobian
		{
			get { return (x, y) => new double[,] {{1, 0}, {0, 1}}; }
		}
	}

	internal class LucaKanadeSimilarity : LucasKanadeAlgo
	{
		public override Func<int, int, double[,]> Jacobian
		{
			get { return (x, y) => new double[,] {{1, 0, x, -y}, {0, 1, y, x}}; }
		}

		public override double[,] Matrix
		{
			get { return new double[,] {{1 + p[2], -p[3], p[0]}, {p[3], 1 + p[2], p[1]}}; }
		}
	}

	/*internal class LucasKanadeScale : LucasKanadeAlgo
	{
		public override Func<int, int, double[,]> Jacobian
		{
			get
			{
				//return (x, y) => new double[,] { { 1, 0, x }, { 0, 1, y } };
				return (x, y) => new double[,] {{1 - p[2]/2, 0, x - p[0]/2}, {0, 1 - p[2]/2, y - p[1]/2}};
				//return (x, y) => new double[,] { { 1 - p[2] / 10, 0, x / 5 - p[0] / 10 }, { 0, 1 - p[2] / 10, y / 5 - p[1] / 10 } };
			}
		}

		public override double[,] Matrix
		{
			get
			{
				//return new double[,] { { 1 + p[2], 0, p[0] }, { 0, 1 + p[2], p[1] } };
				return new double[,] {{1 + p[2], 0, p[0]*(1 - p[2]/2)}, {0, 1 + p[2], p[1]*(1 - p[2]/2)}};
				//return new double[,] { { 1 + p[2] / 5, 0, p[0] * (1 - p[2] / 10) }, { 0, 1 + p[2] / 5, p[1] * (1 - p[2] / 10) } };
			}
		}
	}*/

	internal class LucasKanadeSimilarity : LucasKanadeAlgo
	{
		protected double Teta
		{
			get { return p != null ? p[2] : 0; }
		}

		protected double Sin
		{
			get { return Math.Sin(Teta); }
		}

		protected double Cos
		{
			get { return Math.Cos(Teta); }
		}

		protected double Alpha
		{
			get { return p.Length > 3 ? p[3] : 1; }
		}

		public override int Dimension
		{
			get { return 4; }
		}

		public override Func<int, int, double[,]> Jacobian
		{
			get
			{
				return (x, y) => new double[,]
					{
						{1, 0, -Sin*x*Alpha - Cos*y*Alpha, x*Cos - y*Sin},
						{0, 1, Cos*x*Alpha - Sin*y*Alpha, y*Cos - x*Sin}
					};
			}
		}

		public override double[,] Matrix
		{
			get
			{
				var cos = Cos;
				var sin = Sin;
				return new double[,] { { cos * Alpha, -sin * Alpha, p[0] }, { sin * Alpha, cos * Alpha, p[1] } };
				//return new double[,] { { 1 + p[2], p[3], p[0] }, { p[4], 1 + p[5], p[1] } };
			}
		}
	}

    internal class LucasKanadeEuclidean : LucasKanadeSimilarity
    {
        public override Func<int, int, double[,]> Jacobian
        {
            get { return (x, y) => new double[,] { { 1, 0, -Sin * x * Alpha - Cos * y * Alpha }, { 0, 1, Cos * x * Alpha - Sin * y * Alpha } }; }
        }

        public override int Dimension { get { return 3; } }
    }

    internal class LucasKanadeScaleNoMove : LucasKanadeSimilarity
    {
        public override Func<int, int, double[,]> Jacobian
        {
            get { return (x, y) => new double[,] { { x * Cos - y * Sin }, { y * Cos - x * Sin } }; }
        }

        protected override int[] Indices { get { return new int[] { 3 }; } }
    }

	internal class LucasKanadeTranslate : LucasKanadeEuclidean
	{
		public override Func<int, int, double[,]> Jacobian
		{
			get { return (x, y) => new double[,] { { 1, 0 }, { 0, 1 } }; }
		}

		public override int Dimension
		{
			get { return 2; }
		}
	}

}