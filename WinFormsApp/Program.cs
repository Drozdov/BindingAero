using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinFormsApp
{
    static class Program
    {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var app = new AppForm();
            app.ShowCurrentH += app_ShowCurrentH;
            app.ApplyCurrentH += app_ShowCurrentH;
            Application.Run(app);
        }

        static void app_ShowCurrentH(double[,] h)
        {
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    Console.Write(h[i, j] + " ");
                }
                Console.WriteLine();
            }
        }
    }
}
