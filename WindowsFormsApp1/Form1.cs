using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            string s = textBox1.Text;
            string len = (s.Length + 2).ToString();
            if (len.Length > 1)
                len = len.Substring(1);
            var kk= LuhnDotNet.Luhn.ComputeLuhnCheckDigit(s +len);
            string ocr = s + len + kk;
            textBox2.Text = ocr;
        }
    }
}
