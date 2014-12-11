using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinFormsApp
{
    public partial class AppForm : Form
    {
        public AppForm()
        {
            InitializeComponent();
        }

        private void showButton_Click(object sender, EventArgs e)
        {
            ShowCurrent();
        }

        private void applyButton_Click(object sender, EventArgs e)
        {
            ApplyCurrent();
            this.Close();
        }

        public event ShowH ShowCurrentH;
        public event ShowH ApplyCurrentH;

        private double[,] H
        {
            get
            {
                return new double[,] { 
                    { (double)this.numericUpDown1.Value, (double)this.numericUpDown2.Value, (double)this.numericUpDown3.Value },
                    { (double)this.numericUpDown4.Value, (double)this.numericUpDown5.Value, (double)this.numericUpDown6.Value },
                    { (double)this.numericUpDown7.Value, (double)this.numericUpDown8.Value, (double)this.numericUpDown9.Value }
                };
            }
        }

        void ShowCurrent()
        {
            if (ShowCurrentH != null)
                ShowCurrentH(H);
        }

        void ApplyCurrent()
        {
            if (ApplyCurrentH != null)
                ApplyCurrentH(H);
        }
    }

    public delegate void ShowH(double[,] h);
}
