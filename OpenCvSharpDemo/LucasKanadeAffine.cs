using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenCvSharp;

namespace OpenCvSharpDemo
{
    class LucasKanadeAffine : LucasKanadeAlgo
    {
        public override Func<int, int, double[,]> Jacobian
        {
            get
            {
                return (x, y) => new double[,] { {1, 0, x, y, 0, 0}, {0, 1, 0, 0, x, y}};
            }
        }

        public override double[,] Matrix
        {
            get
            {
                return new double[,] { { 1 + p[2], p[3], p[0] }, { p[4], 1 + p[5], p[1] } };
            }
        }

    }

    class LucaKanadeSimilarity : LucasKanadeAlgo
    {
       public override Func<int, int, double[,]> Jacobian
        {
            get
            {
                return (x, y) => new double[,] { { 1, 0, x, -y }, { 0, 1, y, x } };
            }
        }

        public override double[,] Matrix
        {
            get
            {
                return new double[,] { { 1 + p[2], -p[3], p[0] }, { p[3], 1 + p[2], p[1] } };
            }
        }

    }

    class LucasKanadeScale : LucasKanadeAlgo
    {
        public override Func<int, int, double[,]> Jacobian
        {
            get
            {
                //return (x, y) => new double[,] { { 1, 0, x }, { 0, 1, y } };
                return (x, y) => new double[,] { { 1 - p[2] / 2, 0, x - p[0] / 2}, { 0, 1 - p[2] / 2, y - p[1] / 2 } };
            }
        }

        public override double[,] Matrix
        {
            get
            {
                //return new double[,] { { 1 + p[2], 0, p[0] }, { 0, 1 + p[2], p[1] } };
                return new double[,] { { 1 + p[2], 0, p[0] * (1 - p[2] / 2) }, { 0, 1 + p[2], p[1] * (1 - p[2] / 2) } };
            }
        }

    }

    class LucasKanadeEuclidean : LucasKanadeAlgo
    {
        private double Teta { get { return p != null ? p[2] : 0; } }
        private double Sin { get { return Math.Sin(Teta); } }
        private double Cos { get { return Math.Cos(Teta); } }

        public override Func<int, int, double[,]> Jacobian
        {
            get
            {
                return (x, y) => new double[,] { { 1, 0, Sin * x - Cos * y }, { 0, 1, Cos * x - Sin * y } };
            }
        }

        public override double[,] Matrix
        {
            get
            {
                var cos = Cos;
                var sin = Sin;
                return new double[,] { { cos, -sin, p[0] }, { sin, cos, p[1] } };
                //return new double[,] { { 1 + p[2], p[3], p[0] }, { p[4], 1 + p[5], p[1] } };
            }
        }

    }
}
