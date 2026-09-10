using System;
using System.Drawing;
using System.Windows.Forms;

namespace lab1
{
    public partial class Form1 : Form
    {
        private bool isUpdating = false;

        public Form1()
        {
            InitializeComponent();
            this.DoubleBuffered = true;

            numH.Minimum = 0; numH.Maximum = 360; trackH.Minimum = 0; trackH.Maximum = 360;
            numS.Minimum = 0; numS.Maximum = 100; trackS.Minimum = 0; trackS.Maximum = 100;
            numV.Minimum = 0; numV.Maximum = 100; trackV.Minimum = 0; trackV.Maximum = 100;

            numL.Minimum = 0; numL.Maximum = 100; trackL.Minimum = 0; trackL.Maximum = 10000;
            numA.Minimum = -128; numA.Maximum = 127; trackA.Minimum = -12800; trackA.Maximum = 12700;
            numB.Minimum = -128; numB.Maximum = 127; trackB.Minimum = -12800; trackB.Maximum = 12700;

            // вызов методов для того чтобы включить двойную буферизацию для градиентных панелей
            // без нее при обновлении интерфейса они моргают белым
            EnableDoubleBuffering(panelH);
            EnableDoubleBuffering(panelS);
            EnableDoubleBuffering(panelV);
            EnableDoubleBuffering(panelX);
            EnableDoubleBuffering(panelY);
            EnableDoubleBuffering(panelZ);
            EnableDoubleBuffering(panelL);
            EnableDoubleBuffering(panelA);
            EnableDoubleBuffering(panelB);

            ColorModel.SetWhitePoint("D65");
            UpdateInterface(0, 160, 180);
        }

        private void EnableDoubleBuffering(Panel panel)
        {
            System.Reflection.PropertyInfo? property = typeof(Panel).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            property?.SetValue(panel, true, null);
        }

        private void OnHSVChanged(object sender, EventArgs e)
        {
            if (isUpdating) return;

            if (sender is TrackBar) { numH.Value = trackH.Value; numS.Value = trackS.Value; numV.Value = trackV.Value; }
            else { trackH.Value = (int)numH.Value; trackS.Value = (int)numS.Value; trackV.Value = (int)numV.Value; }

            double[] rgb = ColorModel.HSVtoRGB((double)numH.Value, (double)numS.Value, (double)numV.Value);
            double[] xyz = ColorModel.RGBtoXYZ(rgb[0], rgb[1], rgb[2]);
            double[] lab = ColorModel.XYZtoLAB(xyz[0], xyz[1], xyz[2]);

            UpdateInterfaceFromValues(new double[0], xyz, lab, rgb);
        }

        private void OnXYZChanged(object sender, EventArgs e)
        {
            if (isUpdating) return;

            if (sender is TrackBar)
            {
                numX.Value = (decimal)(trackX.Value / 100.0);
                numY.Value = (decimal)(trackY.Value / 100.0);
                numZ.Value = (decimal)(trackZ.Value / 100.0);
            }
            else
            {
                trackX.Value = (int)Math.Clamp(Math.Round((double)numX.Value * 100.0), trackX.Minimum, trackX.Maximum);
                trackY.Value = (int)Math.Clamp(Math.Round((double)numY.Value * 100.0), trackY.Minimum, trackY.Maximum);
                trackZ.Value = (int)Math.Clamp(Math.Round((double)numZ.Value * 100.0), trackZ.Minimum, trackZ.Maximum);
            }

            double[] rgb = ColorModel.XYZtoRGB((double)numX.Value, (double)numY.Value, (double)numZ.Value);
            double[] hsv = ColorModel.RGBtoHSV(rgb[0], rgb[1], rgb[2]);
            double[] lab = ColorModel.XYZtoLAB((double)numX.Value, (double)numY.Value, (double)numZ.Value);

            UpdateInterfaceFromValues(hsv, new double[0], lab, rgb);
        }

        private void OnLABChanged(object sender, EventArgs e)
        {
            if (isUpdating) return;

            if (sender is TrackBar)
            {
                numL.Value = (decimal)(trackL.Value / 100.0);
                numA.Value = (decimal)(trackA.Value / 100.0);
                numB.Value = (decimal)(trackB.Value / 100.0);
            }
            else
            {
                trackL.Value = (int)Math.Clamp(Math.Round((double)numL.Value * 100.0), trackL.Minimum, trackL.Maximum);
                trackA.Value = (int)Math.Clamp(Math.Round((double)numA.Value * 100.0), trackA.Minimum, trackA.Maximum);
                trackB.Value = (int)Math.Clamp(Math.Round((double)numB.Value * 100.0), trackB.Minimum, trackB.Maximum);
            }

            double[] xyz = ColorModel.LABtoXYZ((double)numL.Value, (double)numA.Value, (double)numB.Value);
            double[] rgb = ColorModel.XYZtoRGB(xyz[0], xyz[1], xyz[2]);
            double[] hsv = ColorModel.RGBtoHSV(rgb[0], rgb[1], rgb[2]);

            UpdateInterfaceFromValues(hsv, xyz, new double[0], rgb);
        }

        // прошу обратить внимание, что при выборе иной точки цвет берется с экрана!!
        // вы увидите обновление xyz и lab как следствие 
        private void rbWhitePoint_CheckedChanged(object sender, EventArgs e)
        {
            if (isUpdating || !(sender is RadioButton rb) || !rb.Checked) return;

            ColorModel.SetWhitePoint(rb.Text);
            Color c = picColorPreview.BackColor;
            UpdateInterface(c.R, c.G, c.B);
        }

        private void btnChooseColor_Click(object sender, EventArgs e)
        {
            using (ColorDialog cd = new ColorDialog())
            {
                if (cd.ShowDialog() == DialogResult.OK) UpdateInterface(cd.Color.R, cd.Color.G, cd.Color.B);
            }
        }

        private void UpdateInterface(double r, double g, double b)
        {
            //ColorModel.OutOfGamut = false;

            double[] hsv = ColorModel.RGBtoHSV(r, g, b);
            double[] xyz = ColorModel.RGBtoXYZ(r, g, b);
            double[] lab = ColorModel.XYZtoLAB(xyz[0], xyz[1], xyz[2]);

            UpdateInterfaceFromValues(hsv, xyz, lab, new double[] { r, g, b });
        }

        private void UpdateInterfaceFromValues(double[] hsv, double[] xyz, double[] lab, double[] rgb)
        {
            isUpdating = true;

            double mX = ColorModel.CurrentWhiteX;
            double mY = ColorModel.CurrentWhiteY;
            double mZ = ColorModel.CurrentWhiteZ;

      
            trackX.Maximum = (int)Math.Round(mX * 100.0); numX.Maximum = (decimal)(trackX.Maximum / 100.0);
            trackY.Maximum = (int)Math.Round(mY * 100.0); numY.Maximum = (decimal)(trackY.Maximum / 100.0);
            trackZ.Maximum = (int)Math.Round(mZ * 100.0); numZ.Maximum = (decimal)(trackZ.Maximum / 100.0);


            if (hsv.Length != 0)
            {
                numH.Value = (decimal)Math.Clamp(hsv[0], 0, 360); trackH.Value = (int)Math.Round(numH.Value);
                numS.Value = (decimal)Math.Clamp(hsv[1], 0, 100); trackS.Value = (int)Math.Round(numS.Value);
                numV.Value = (decimal)Math.Clamp(hsv[2], 0, 100); trackV.Value = (int)Math.Round(numV.Value);
            }

            if (xyz.Length != 0)
            {
                numX.Value = (decimal)Math.Clamp(xyz[0], 0, (double)numX.Maximum);
                numY.Value = (decimal)Math.Clamp(xyz[1], 0, (double)numY.Maximum);
                numZ.Value = (decimal)Math.Clamp(xyz[2], 0, (double)numZ.Maximum);

                trackX.Value = (int)Math.Clamp(Math.Round(xyz[0] * 100.0), 0, trackX.Maximum);
                trackY.Value = (int)Math.Clamp(Math.Round(xyz[1] * 100.0), 0, trackY.Maximum);
                trackZ.Value = (int)Math.Clamp(Math.Round(xyz[2] * 100.0), 0, trackZ.Maximum);
            }

            if (lab.Length != 0)
            {
                numL.Value = (decimal)Math.Clamp(lab[0], 0, 100);
                numA.Value = (decimal)Math.Clamp(lab[1], -128, 127);
                numB.Value = (decimal)Math.Clamp(lab[2], -128, 127);

                trackL.Value = (int)Math.Clamp(Math.Round(lab[0] * 100.0), trackL.Minimum, trackL.Maximum);
                trackA.Value = (int)Math.Clamp(Math.Round(lab[1] * 100.0), trackA.Minimum, trackA.Maximum);
                trackB.Value = (int)Math.Clamp(Math.Round(lab[2] * 100.0), trackB.Minimum, trackB.Maximum);
            }

            bool isOut = (rgb[0] < -0.5 || rgb[0] > 255.5 ||
                  rgb[1] < -0.5 || rgb[1] > 255.5 ||
                  rgb[2] < -0.5 || rgb[2] > 255.5);

            lblWarning.Visible = isOut;

            int displayR = (int)Math.Clamp(Math.Round(rgb[0]), 0.0, 255.0);
            int displayG = (int)Math.Clamp(Math.Round(rgb[1]), 0.0, 255.0);
            int displayB = (int)Math.Clamp(Math.Round(rgb[2]), 0.0, 255.0);

            picColorPreview.BackColor = Color.FromArgb(displayR, displayG, displayB);
            UpdateTrackBarBackgrounds();

            lblRGB.Text = $"R={Math.Round(rgb[0])}  G={Math.Round(rgb[1])}  B={Math.Round(rgb[2])}";
            isUpdating = false;
        }


        private void UpdateTrackBarBackgrounds()
        {
            double curH = (double)numH.Value;
            double curS = (double)numS.Value;
            double curV = (double)numV.Value;

            double curX = (double)numX.Value;
            double curY = (double)numY.Value;
            double curZ = (double)numZ.Value;

            double curL = (double)numL.Value;
            double curA = (double)numA.Value;
            double curB = (double)numB.Value;

            Bitmap bmp;
            double[] rgb;
            int r, g, b;


            panelH.BackgroundImage?.Dispose();
            bmp = new Bitmap(360, 1);
            for (int i = 0; i < 360; i++)
            {
                rgb = ColorModel.HSVtoRGB(i, curS, curV);
                r = (int)Math.Clamp(Math.Round(rgb[0]), 0, 255);
                g = (int)Math.Clamp(Math.Round(rgb[1]), 0, 255);
                b = (int)Math.Clamp(Math.Round(rgb[2]), 0, 255);
                bmp.SetPixel(i, 0, Color.FromArgb(r, g, b));
            }
            panelH.BackgroundImage = bmp;


            panelS.BackgroundImage?.Dispose();
            bmp = new Bitmap(100, 1);
            for (int i = 0; i < 100; i++)
            {
                rgb = ColorModel.HSVtoRGB(curH, i, curV);
                r = (int)Math.Clamp(Math.Round(rgb[0]), 0, 255);
                g = (int)Math.Clamp(Math.Round(rgb[1]), 0, 255);
                b = (int)Math.Clamp(Math.Round(rgb[2]), 0, 255);
                bmp.SetPixel(i, 0, Color.FromArgb(r, g, b));
            }
            panelS.BackgroundImage = bmp;

            panelV.BackgroundImage?.Dispose();
            bmp = new Bitmap(100, 1);
            for (int i = 0; i < 100; i++)
            {
                rgb = ColorModel.HSVtoRGB(curH, curS, i);
                r = (int)Math.Clamp(Math.Round(rgb[0]), 0, 255);
                g = (int)Math.Clamp(Math.Round(rgb[1]), 0, 255);
                b = (int)Math.Clamp(Math.Round(rgb[2]), 0, 255);
                bmp.SetPixel(i, 0, Color.FromArgb(r, g, b));
            }
            panelV.BackgroundImage = bmp;

            panelX.BackgroundImage?.Dispose();
            bmp = new Bitmap(100, 1);
            double maxX = (double)numX.Maximum;
            for (int i = 0; i < 100; i++)
            {
                double currentStepX = (i / 100.0) * maxX;
                rgb = ColorModel.XYZtoRGB(currentStepX, curY, curZ);
                r = (int)Math.Clamp(Math.Round(rgb[0]), 0, 255);
                g = (int)Math.Clamp(Math.Round(rgb[1]), 0, 255);
                b = (int)Math.Clamp(Math.Round(rgb[2]), 0, 255);
                bmp.SetPixel(i, 0, Color.FromArgb(r, g, b));
            }
            panelX.BackgroundImage = bmp;

            panelY.BackgroundImage?.Dispose();
            bmp = new Bitmap(100, 1);
            double maxY = (double)numY.Maximum;
            for (int i = 0; i < 100; i++)
            {
                double currentStepY = (i / 100.0) * maxY;
                rgb = ColorModel.XYZtoRGB(curX, currentStepY, curZ);
                r = (int)Math.Clamp(Math.Round(rgb[0]), 0, 255);
                g = (int)Math.Clamp(Math.Round(rgb[1]), 0, 255);
                b = (int)Math.Clamp(Math.Round(rgb[2]), 0, 255);
                bmp.SetPixel(i, 0, Color.FromArgb(r, g, b));
            }
            panelY.BackgroundImage = bmp;

            panelZ.BackgroundImage?.Dispose();
            bmp = new Bitmap(100, 1);
            double maxZ = (double)numZ.Maximum;
            for (int i = 0; i < 100; i++)
            {
                double currentStepZ = (i / 100.0) * maxZ;
                rgb = ColorModel.XYZtoRGB(curX, curY, currentStepZ);
                r = (int)Math.Clamp(Math.Round(rgb[0]), 0, 255);
                g = (int)Math.Clamp(Math.Round(rgb[1]), 0, 255);
                b = (int)Math.Clamp(Math.Round(rgb[2]), 0, 255);
                bmp.SetPixel(i, 0, Color.FromArgb(r, g, b));
            }
            panelZ.BackgroundImage = bmp;

            panelL.BackgroundImage?.Dispose();
            bmp = new Bitmap(100, 1);
            for (int i = 0; i < 100; i++)
            {
                double[] xyz = ColorModel.LABtoXYZ(i, curA, curB);
                rgb = ColorModel.XYZtoRGB(xyz[0], xyz[1], xyz[2]);
                r = (int)Math.Clamp(Math.Round(rgb[0]), 0, 255);
                g = (int)Math.Clamp(Math.Round(rgb[1]), 0, 255);
                b = (int)Math.Clamp(Math.Round(rgb[2]), 0, 255);
                bmp.SetPixel(i, 0, Color.FromArgb(r, g, b));
            }
            panelL.BackgroundImage = bmp;

            panelA.BackgroundImage?.Dispose();
            bmp = new Bitmap(255, 1);
            for (int i = 0; i < 255; i++)
            {
                double[] xyz = ColorModel.LABtoXYZ(curL, i - 128, curB);
                rgb = ColorModel.XYZtoRGB(xyz[0], xyz[1], xyz[2]);
                r = (int)Math.Clamp(Math.Round(rgb[0]), 0, 255);
                g = (int)Math.Clamp(Math.Round(rgb[1]), 0, 255);
                b = (int)Math.Clamp(Math.Round(rgb[2]), 0, 255);
                bmp.SetPixel(i, 0, Color.FromArgb(r, g, b));
            }
            panelA.BackgroundImage = bmp;

            panelB.BackgroundImage?.Dispose();
            bmp = new Bitmap(255, 1);
            for (int i = 0; i < 255; i++)
            {
                double[] xyz = ColorModel.LABtoXYZ(curL, curA, i - 128);
                rgb = ColorModel.XYZtoRGB(xyz[0], xyz[1], xyz[2]);
                r = (int)Math.Clamp(Math.Round(rgb[0]), 0, 255);
                g = (int)Math.Clamp(Math.Round(rgb[1]), 0, 255);
                b = (int)Math.Clamp(Math.Round(rgb[2]), 0, 255);
                bmp.SetPixel(i, 0, Color.FromArgb(r, g, b));
            }
            panelB.BackgroundImage = bmp;
        }

    }
}
