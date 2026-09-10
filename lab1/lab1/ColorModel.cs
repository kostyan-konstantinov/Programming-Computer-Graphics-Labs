using System;

namespace lab1
{
    public static class ColorModel
    {
        //public static bool OutOfGamut = false;

        public static double CurrentWhiteX;
        public static double CurrentWhiteY;
        public static double CurrentWhiteZ;

        private static double[,] M_RGB_to_XYZ = new double[3, 3];
        private static double[,] M_XYZ_to_RGB = new double[3, 3];

        private const double xr = 0.64, yr = 0.33;
        private const double xg = 0.30, yg = 0.60;
        private const double xb = 0.15, yb = 0.06;

        public static void SetWhitePoint(string name)
        {
            double xw = 0.31272, yw = 0.32903;
            if (name == "D50") { xw = 0.34567; yw = 0.35850; }
            else if (name == "E") { xw = 1.0 / 3.0; yw = 1.0 / 3.0; }

            CurrentWhiteY = 100.0;
            CurrentWhiteX = 100.0 * (xw / yw);
            CurrentWhiteZ = 100.0 * ((1.0 - xw - yw) / yw);

            // матр. цветности
            double[,] P = {
                { xr / yr, xg / yg, xb / yb },
                { 1.0,     1.0,     1.0     },
                { (1.0 - xr - yr) / yr, (1.0 - xg - yg) / yg, (1.0 - xb - yb) / yb }
            };

            
            double detP = P[0, 0] * (P[1, 1] * P[2, 2] - P[1, 2] * P[2, 1]) -
                          P[0, 1] * (P[1, 0] * P[2, 2] - P[1, 2] * P[2, 0]) +
                          P[0, 2] * (P[1, 0] * P[2, 1] - P[1, 1] * P[2, 0]);

            double[,] P_inv = {
                { (P[1, 1] * P[2, 2] - P[1, 2] * P[2, 1]) / detP, (P[0, 2] * P[2, 1] - P[0, 1] * P[2, 2]) / detP, (P[0, 1] * P[1, 2] - P[0, 2] * P[1, 1]) / detP },
                { (P[1, 2] * P[2, 0] - P[1, 0] * P[2, 2]) / detP, (P[0, 0] * P[2, 2] - P[0, 2] * P[2, 0]) / detP, (P[0, 2] * P[1, 0] - P[0, 0] * P[1, 2]) / detP },
                { (P[1, 0] * P[2, 1] - P[1, 1] * P[2, 0]) / detP, (P[0, 1] * P[2, 0] - P[0, 0] * P[2, 1]) / detP, (P[0, 0] * P[1, 1] - P[0, 1] * P[1, 0]) / detP }
            };

            // координаты нашего белого цвета в xyz
            double W_X = xw / yw;
            double W_Y = 1.0;
            double W_Z = (1.0 - xw - yw) / yw;

            double Sr = P_inv[0, 0] * W_X + P_inv[0, 1] * W_Y + P_inv[0, 2] * W_Z;
            double Sg = P_inv[1, 0] * W_X + P_inv[1, 1] * W_Y + P_inv[1, 2] * W_Z;
            double Sb = P_inv[2, 0] * W_X + P_inv[2, 1] * W_Y + P_inv[2, 2] * W_Z;

            M_RGB_to_XYZ[0, 0] = P[0, 0] * Sr; 
            M_RGB_to_XYZ[0, 1] = P[0, 1] * Sg; 
            M_RGB_to_XYZ[0, 2] = P[0, 2] * Sb;
            M_RGB_to_XYZ[1, 0] = P[1, 0] * Sr;
            M_RGB_to_XYZ[1, 1] = P[1, 1] * Sg;
            M_RGB_to_XYZ[1, 2] = P[1, 2] * Sb;
            M_RGB_to_XYZ[2, 0] = P[2, 0] * Sr;
            M_RGB_to_XYZ[2, 1] = P[2, 1] * Sg;
            M_RGB_to_XYZ[2, 2] = P[2, 2] * Sb;

            double detM = M_RGB_to_XYZ[0, 0] * (M_RGB_to_XYZ[1, 1] * M_RGB_to_XYZ[2, 2] - M_RGB_to_XYZ[1, 2] * M_RGB_to_XYZ[2, 1]) -
                          M_RGB_to_XYZ[0, 1] * (M_RGB_to_XYZ[1, 0] * M_RGB_to_XYZ[2, 2] - M_RGB_to_XYZ[1, 2] * M_RGB_to_XYZ[2, 0]) +
                          M_RGB_to_XYZ[0, 2] * (M_RGB_to_XYZ[1, 0] * M_RGB_to_XYZ[2, 1] - M_RGB_to_XYZ[1, 1] * M_RGB_to_XYZ[2, 0]);

            M_XYZ_to_RGB[0, 0] = (M_RGB_to_XYZ[1, 1] * M_RGB_to_XYZ[2, 2] - M_RGB_to_XYZ[1, 2] * M_RGB_to_XYZ[2, 1]) / detM;
            M_XYZ_to_RGB[0, 1] = -(M_RGB_to_XYZ[0, 1] * M_RGB_to_XYZ[2, 2] - M_RGB_to_XYZ[0, 2] * M_RGB_to_XYZ[2, 1]) / detM;
            M_XYZ_to_RGB[0, 2] = (M_RGB_to_XYZ[0, 1] * M_RGB_to_XYZ[1, 2] - M_RGB_to_XYZ[0, 2] * M_RGB_to_XYZ[1, 1]) / detM;

            M_XYZ_to_RGB[1, 0] = -(M_RGB_to_XYZ[1, 0] * M_RGB_to_XYZ[2, 2] - M_RGB_to_XYZ[1, 2] * M_RGB_to_XYZ[2, 0]) / detM;
            M_XYZ_to_RGB[1, 1] = (M_RGB_to_XYZ[0, 0] * M_RGB_to_XYZ[2, 2] - M_RGB_to_XYZ[0, 2] * M_RGB_to_XYZ[2, 0]) / detM;
            M_XYZ_to_RGB[1, 2] = -(M_RGB_to_XYZ[0, 0] * M_RGB_to_XYZ[1, 2] - M_RGB_to_XYZ[0, 2] * M_RGB_to_XYZ[1, 0]) / detM;

            M_XYZ_to_RGB[2, 0] = (M_RGB_to_XYZ[1, 0] * M_RGB_to_XYZ[2, 1] - M_RGB_to_XYZ[1, 1] * M_RGB_to_XYZ[2, 0]) / detM;
            M_XYZ_to_RGB[2, 1] = -(M_RGB_to_XYZ[0, 0] * M_RGB_to_XYZ[2, 1] - M_RGB_to_XYZ[0, 1] * M_RGB_to_XYZ[2, 0]) / detM;
            M_XYZ_to_RGB[2, 2] = (M_RGB_to_XYZ[0, 0] * M_RGB_to_XYZ[1, 1] - M_RGB_to_XYZ[0, 1] * M_RGB_to_XYZ[1, 0]) / detM;

        }

        public static double[] RGBtoXYZ(double rVal, double gVal, double bVal)
        {
            double r = rVal / 255.0; double g = gVal / 255.0; double b = bVal / 255.0;

            r = (r > 0.04045) ? Math.Pow((r + 0.055) / 1.055, 2.4) : (r / 12.92);
            g = (g > 0.04045) ? Math.Pow((g + 0.055) / 1.055, 2.4) : (g / 12.92);
            b = (b > 0.04045) ? Math.Pow((b + 0.055) / 1.055, 2.4) : (b / 12.92);

            double x = r * M_RGB_to_XYZ[0, 0] + g * M_RGB_to_XYZ[0, 1] + b * M_RGB_to_XYZ[0, 2];
            double y = r * M_RGB_to_XYZ[1, 0] + g * M_RGB_to_XYZ[1, 1] + b * M_RGB_to_XYZ[1, 2];
            double z = r * M_RGB_to_XYZ[2, 0] + g * M_RGB_to_XYZ[2, 1] + b * M_RGB_to_XYZ[2, 2];

            return new double[] { x * 100.0, y * 100.0, z * 100.0 };
        }

        public static double[] XYZtoRGB(double xVal, double yVal, double zVal)
        {
            double x = xVal / 100.0; double y = yVal / 100.0; double z = zVal / 100.0;

            double r = x * M_XYZ_to_RGB[0, 0] + y * M_XYZ_to_RGB[0, 1] + z * M_XYZ_to_RGB[0, 2];
            double g = x * M_XYZ_to_RGB[1, 0] + y * M_XYZ_to_RGB[1, 1] + z * M_XYZ_to_RGB[1, 2];
            double b = x * M_XYZ_to_RGB[2, 0] + y * M_XYZ_to_RGB[2, 1] + z * M_XYZ_to_RGB[2, 2];

            r = (r > 0.0031308) ? (1.055 * Math.Pow(r, 1.0 / 2.4) - 0.055) : (12.92 * r);
            g = (g > 0.0031308) ? (1.055 * Math.Pow(g, 1.0 / 2.4) - 0.055) : (12.92 * g);
            b = (b > 0.0031308) ? (1.055 * Math.Pow(b, 1.0 / 2.4) - 0.055) : (12.92 * b);

            return new double[] { r * 255.0, g * 255.0, b * 255.0 };
        }

        public static double[] RGBtoHSV(double rVal, double gVal, double bVal)
        {
            double r = Math.Clamp(rVal, 0.0, 255.0) / 255.0;
            double g = Math.Clamp(gVal, 0.0, 255.0) / 255.0;
            double b = Math.Clamp(bVal, 0.0, 255.0) / 255.0;

            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double delta = max - min;

            double h = 0, s = 0, v = max;

            if (max != 0) s = delta / max;

            if (s != 0 && delta != 0)
            {
                if (r == max) h = (g - b) / delta;
                else if (g == max) h = 2.0 + (b - r) / delta;
                else h = 4.0 + (r - g) / delta;

                h *= 60.0;
                if (h < 0) h += 360.0;
            }

            return new double[] { h, s * 100.0, v * 100.0 };
        }

        public static double[] HSVtoRGB(double h, double sVal, double vVal)
        {
            double s = sVal / 100.0; double v = vVal / 100.0;
            double r = 0, g = 0, b = 0;
            if (s == 0) { r = g = b = v; }
            else
            {
                double sectorPos = h / 60.0; 
                int sectorNumber = (int)Math.Floor(sectorPos); 
                double fractionalSector = sectorPos - sectorNumber;
                double p = v * (1.0 - s); 
                double q = v * (1.0 - (s * fractionalSector)); 
                double t = v * (1.0 - (s * (1.0 - fractionalSector)));
                switch (sectorNumber % 6)
                {
                    case 0: r = v; g = t; b = p; break;
                    case 1: r = q; g = v; b = p; break;
                    case 2: r = p; g = v; b = t; break;
                    case 3: r = p; g = q; b = v; break;
                    case 4: r = t; g = p; b = v; break;
                    case 5: r = v; g = p; b = q; break;
                }
            }
            //OutOfGamut = false;
            return new double[] { r * 255.0, g * 255.0, b * 255.0 };
        }

        private static double F_xyz_to_lab(double t) => (t > 0.008856451) ? Math.Pow(t, 1.0 / 3.0) : (7.787037 * t) + (16.0 / 116.0);
        private static double F_lab_to_xyz(double t) => (t > 0.2068966) ? Math.Pow(t, 3.0) : (t - 16.0 / 116.0) / 7.787037;

        public static double[] XYZtoLAB(double x, double y, double z)
        {
            double fx = F_xyz_to_lab(x / CurrentWhiteX);
            double fy = F_xyz_to_lab(y / CurrentWhiteY);
            double fz = F_xyz_to_lab(z / CurrentWhiteZ);
            return new double[] { (116.0 * fy) - 16.0, 500.0 * (fx - fy), 200.0 * (fy - fz) };
        }

        public static double[] LABtoXYZ(double l, double a, double b)
        {
            double fy = (l + 16.0) / 116.0;
            double fx = fy + (a / 500.0);
            double fz = fy - (b / 200.0);
            return new double[] { CurrentWhiteX * F_lab_to_xyz(fx), CurrentWhiteY * F_lab_to_xyz(fy), CurrentWhiteZ * F_lab_to_xyz(fz) };
        }
    }
}
