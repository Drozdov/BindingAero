using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenCvSharp;

namespace OpenCvSharpDemo
{
	internal class LucasKanadeData
	{
		public virtual Func<int, int, double[,]> Jacobian
		{
			get
			{
				return (x, y) => new double[,] { { 1, 0 }, { 0, 1 } };
			}
		}

		public virtual int[] Indices
		{
			get
			{
				int dim = Dimension;
				int[] res = new int[dim];
				for (int i = 0; i < dim; i++)
					res[i] = i;
				return res;
			}
		}

		public virtual int Dimension { get { return Jacobian(0, 0).GetLength(1); } }

		public double[] P { get; set; }

		public virtual double[,] HomographyMatrix
		{
			get
			{
				return new double[,] { { 1, 0, P[0] }, { 0, 1, P[1] }, { 0, 0, 1 } };
			}
		}

		public LucasKanadeData()
		{
			P = new double[Dimension];
		}
	}

	internal class LucasKanadeAffine : LucasKanadeData
	{
        const int d = 10;
		public override Func<int, int, double[,]> Jacobian
		{
			get { return (x, y) => new double[,] {{1, 0, x / d, y / d, 0, 0}, {0, 1, 0, 0, x / d, y / d}}; }
		}

		public override double[,] HomographyMatrix
		{
			get { return new double[,] {{1 + P[2] / d, P[3] / d, P[0]}, {P[4] / d, 1 + P[5] / d, P[1]}, {0, 0, 1}}; }
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

	internal class LucaKanadeSimilarity : LucasKanadeData
	{
		public override Func<int, int, double[,]> Jacobian
		{
			get { return (x, y) => new double[,] {{1, 0, x, -y}, {0, 1, y, x}}; }
		}

		public override double[,] HomographyMatrix
		{
			get { return new double[,] { { 1 + P[2], -P[3], P[0] }, { P[3], 1 + P[2], P[1] }, { 0, 0, 1 } }; }
		}
	}

	/*internal class LucasKanadeScale : LucasKanadeAlgo
	{
		public override Func<int, int, double[,]> Jacobian
		{
			get
			{
				//return (x, y) => new double[,] { { 1, 0, x }, { 0, 1, y } };
				return (x, y) => new double[,] {{1 - P[2]/2, 0, x - P[0]/2}, {0, 1 - P[2]/2, y - P[1]/2}};
				//return (x, y) => new double[,] { { 1 - P[2] / 10, 0, x / 5 - P[0] / 10 }, { 0, 1 - P[2] / 10, y / 5 - P[1] / 10 } };
			}
		}

		public override double[,] HomographyMatrix
		{
			get
			{
				//return new double[,] { { 1 + P[2], 0, P[0] }, { 0, 1 + P[2], P[1] } };
				return new double[,] {{1 + P[2], 0, P[0]*(1 - P[2]/2)}, {0, 1 + P[2], P[1]*(1 - P[2]/2)}};
				//return new double[,] { { 1 + P[2] / 5, 0, P[0] * (1 - P[2] / 10) }, { 0, 1 + P[2] / 5, P[1] * (1 - P[2] / 10) } };
			}
		}
	}*/

	internal class LucasKanadeSimilarity : LucasKanadeData
	{
		protected double Teta
		{
			get { return (P != null && P.Length > 2) ? P[2] : 0; }
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
			get { return P.Length > 3 ? P[3] : 1; }
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
						{1, 0, (-Sin*x*Alpha - Cos*y*Alpha), (x*Cos - y*Sin)},
						{0, 1, (Cos*x*Alpha - Sin*y*Alpha), (y*Cos - x*Sin)}
					};
			}
		}

		public override double[,] HomographyMatrix
		{
			get
			{
				var cos = Cos;
				var sin = Sin;
				return new double[,] { { cos * Alpha, -sin * Alpha, P[0] }, { sin * Alpha, cos * Alpha, P[1] }, { 0, 0, 1 } };
				//return new double[,] { { 1 + P[2], P[3], P[0] }, { P[4], 1 + P[5], P[1] } };
			}
		}

        public LucasKanadeSimilarity()
        {
            if (P.Length > 3)
                P[3] = 1;
        }
	}

    internal class LucasKanadeEuclidean : LucasKanadeSimilarity
    {
        public override Func<int, int, double[,]> Jacobian
        {
            get { return (x, y) => new double[,] { { 1, 0, -Sin * x * Alpha - Cos * y * Alpha }, { 0, 1, Cos * x * Alpha - Sin * y * Alpha } }; }
        }

		public override int[] Indices { get { return new int[] { 0, 1, 2 }; } }
        //public override int Dimension { get { return 4; } }
    }

    internal class LucasKanadeScaleNoMove : LucasKanadeSimilarity
    {
        public override Func<int, int, double[,]> Jacobian
        {
            get { return (x, y) => new double[,] { { x * Cos - y * Sin }, { y * Cos - x * Sin } }; }
        }

        public override int[] Indices { get { return new int[] { 3 }; } }
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

        public override int[] Indices { get { return new int[] { 0, 1 }; } }
	}

}