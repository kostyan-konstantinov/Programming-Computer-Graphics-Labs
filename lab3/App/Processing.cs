using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lab3Variant9
{
    public enum Method { Otsu, Isodata, Sauvola, Points, Lines, Sobel, Canny }

    public sealed class Settings
    {
        public Method Method = Method.Otsu;
        public BorderMode Border = BorderMode.Reflect;
        public bool Parallel = true;
        public int Radius = 15;
        public double K = 0.2;
        public double Sigma = 1.2;
        public double Threshold = 100;
        public double Low = 35;
        public double High = 90;
        public int LineDirection;
    }

    public sealed class ProcessingResult
    {
        public PixelBuffer Image;
        public PixelBuffer DirectionMap;
        public float[] Angles;
        public float[] OffsetX, OffsetY;
        public double Threshold = double.NaN;
        public int Iterations;
        public double[] OtsuVariance;
    }

    public sealed class ProcessingContext
    {
        public CancellationToken Cancellation;
        public bool Parallel;
        private readonly IProgress<int> progress;
        private int lastProgress = -1;
        private readonly object sync = new object();

        public ProcessingContext(bool parallel, CancellationToken cancellation, IProgress<int> progress = null)
        { Parallel = parallel; Cancellation = cancellation; this.progress = progress; }

        public void Report(int value)
        {
            lock (sync)
            {
                if (value > lastProgress)
                { lastProgress = value; if (progress != null) progress.Report(value); }
            }
        }

        public void Rows(int height, int start, int end, Action<int> action)
        {
            int done = 0;
            Action<int> row = y =>
            {
                Cancellation.ThrowIfCancellationRequested();
                action(y);
                int count = Interlocked.Increment(ref done);
                Report(start + count * (end - start) / height);
            };
            if (Parallel)
                System.Threading.Tasks.Parallel.For(0, height,
                    new ParallelOptions { CancellationToken = Cancellation }, row);
            else for (int y = 0; y < height; y++) row(y);
        }
    }

    public static class Processing
    {
        public static long[] Histogram(byte[] gray)
        {
            long[] histogram = new long[256];
            for (int i = 0; i < gray.Length; i++) histogram[gray[i]]++;
            return histogram;
        }

        public static int Otsu(long[] histogram, out double[] variance)
        {
            long total = 0, count0 = 0;
            double sum = 0, sum0 = 0;
            for (int i = 0; i < 256; i++) { total += histogram[i]; sum += i * (double)histogram[i]; }
            variance = new double[256];
            double best = -1;
            int threshold = 0;
            for (int t = 0; t < 256; t++)
            {
                count0 += histogram[t]; sum0 += t * (double)histogram[t];
                long count1 = total - count0;
                if (count0 == 0 || count1 == 0) continue;
                double delta = sum0 / count0 - (sum - sum0) / count1;
                variance[t] = (count0 / (double)total) * (count1 / (double)total) * delta * delta;
                // при нескольких равных максимумах выбирается первый порог
                if (variance[t] > best) { best = variance[t]; threshold = t; }
            }
            if (best < 0)
                for (int i = 0; i < 256; i++) if (histogram[i] > 0) { threshold = i; break; }
            return threshold;
        }

        public static double Isodata(long[] histogram, out int iterations)
        {
            int min = 0, max = 255;
            while (min < 255 && histogram[min] == 0) min++;
            while (max > 0 && histogram[max] == 0) max--;
            double threshold = (min + max) / 2.0;
            iterations = 0;
            for (int step = 0; step < 256; step++)
            {
                long n0 = 0, n1 = 0;
                double s0 = 0, s1 = 0;
                for (int i = 0; i < 256; i++)
                    if (i <= threshold) { n0 += histogram[i]; s0 += i * (double)histogram[i]; }
                    else { n1 += histogram[i]; s1 += i * (double)histogram[i]; }
                iterations++;
                if (n0 == 0 || n1 == 0) break;
                double next = (s0 / n0 + s1 / n1) / 2;
                if ((int)Math.Floor(next) == (int)Math.Floor(threshold)) { threshold = next; break; }
                threshold = next;
            }
            return threshold;
        }

        private static byte[] Binarize(byte[] gray, int width, int height, double threshold, ProcessingContext context)
        {
            byte[] output = new byte[gray.Length];
            context.Rows(height, 15, 95, y =>
            {
                for (int x = 0; x < width; x++)
                { int i = y * width + x; output[i] = gray[i] > threshold ? (byte)255 : (byte)0; }
            });
            return output;
        }

        // SAT строится для дополненного буфера потому окно у края сохраняет размер
        public sealed class IntegralImage
        {
            private readonly long[] sum, squares;
            private readonly int stride;
            public IntegralImage(byte[] gray, int width, int height, int radius, BorderMode border, ProcessingContext context)
            {
                int paddedWidth = checked(width + 2 * radius), paddedHeight = checked(height + 2 * radius);
                stride = paddedWidth + 1;
                sum = new long[checked(stride * (paddedHeight + 1))];
                squares = new long[sum.Length];
                for (int y = 0; y < paddedHeight; y++)
                {
                    context.Cancellation.ThrowIfCancellationRequested();
                    long rowSum = 0, rowSquares = 0;
                    int sy = PixelBuffer.Index(y - radius, height, border);
                    for (int x = 0; x < paddedWidth; x++)
                    {
                        int value = gray[sy * width + PixelBuffer.Index(x - radius, width, border)];
                        rowSum += value; rowSquares += value * value;
                        int p = (y + 1) * stride + x + 1;
                        sum[p] = sum[p - stride] + rowSum;
                        squares[p] = squares[p - stride] + rowSquares;
                    }
                    context.Report(5 + (y + 1) * 35 / paddedHeight);
                }
            }

            private long Rectangle(long[] table, int x, int y, int size)
            {
                int a = y * stride + x, b = a + size;
                int c = (y + size) * stride + x, d = c + size;
                return table[d] - table[b] - table[c] + table[a];
            }

            public void Statistics(int x, int y, int size, out double mean, out double std)
            {
                double n = size * size;
                mean = Rectangle(sum, x, y, size) / n;
                std = Math.Sqrt(Math.Max(0, Rectangle(squares, x, y, size) / n - mean * mean));
            }
        }

        private static byte[] Sauvola(byte[] gray, int width, int height, Settings settings, ProcessingContext context)
        {
            IntegralImage sat = new IntegralImage(gray, width, height, settings.Radius, settings.Border, context);
            int size = 2 * settings.Radius + 1;
            byte[] output = new byte[gray.Length];
            context.Rows(height, 40, 95, y =>
            {
                for (int x = 0; x < width; x++)
                {
                    double mean, std;
                    sat.Statistics(x, y, size, out mean, out std);
                    double threshold = mean * (1 + settings.K * (std / 128 - 1));
                    int i = y * width + x;
                    output[i] = gray[i] > threshold ? (byte)255 : (byte)0;
                }
            });
            return output;
        }

        public static float[] Gaussian(float[] input, int width, int height, double sigma, BorderMode border,
            ProcessingContext context, int start = 5, int end = 35)
        {
            int radius = (int)Math.Ceiling(3 * sigma);
            double[] kernel = new double[2 * radius + 1];
            double sum = 0;
            for (int k = -radius; k <= radius; k++)
            { kernel[k + radius] = Math.Exp(-k * k / (2 * sigma * sigma)); sum += kernel[k + radius]; }
            for (int k = 0; k < kernel.Length; k++) kernel[k] /= sum;
            float[] temp = new float[input.Length], output = new float[input.Length];
            int middle = (start + end) / 2;
            context.Rows(height, start, middle, y =>
            {
                for (int x = 0; x < width; x++)
                {
                    double value = 0;
                    for (int k = -radius; k <= radius; k++)
                        value += kernel[k + radius] * PixelBuffer.Sample(input, width, height, x + k, y, border);
                    temp[y * width + x] = (float)value;
                }
            });
            context.Rows(height, middle, end, y =>
            {
                for (int x = 0; x < width; x++)
                {
                    double value = 0;
                    for (int k = -radius; k <= radius; k++)
                        value += kernel[k + radius] * PixelBuffer.Sample(temp, width, height, x, y + k, border);
                    output[y * width + x] = (float)value;
                }
            });
            return output;
        }

        private static void Gradient(float[] input, int width, int height, BorderMode border, ProcessingContext context,
            int start, int end, out float[] magnitude, out float[] angle)
        {
            float[] m = new float[input.Length], a = new float[input.Length];
            context.Rows(height, start, end, y =>
            {
                for (int x = 0; x < width; x++)
                {
                    float tl = PixelBuffer.Sample(input, width, height, x - 1, y - 1, border);
                    float tc = PixelBuffer.Sample(input, width, height, x, y - 1, border);
                    float tr = PixelBuffer.Sample(input, width, height, x + 1, y - 1, border);
                    float ml = PixelBuffer.Sample(input, width, height, x - 1, y, border);
                    float mr = PixelBuffer.Sample(input, width, height, x + 1, y, border);
                    float bl = PixelBuffer.Sample(input, width, height, x - 1, y + 1, border);
                    float bc = PixelBuffer.Sample(input, width, height, x, y + 1, border);
                    float br = PixelBuffer.Sample(input, width, height, x + 1, y + 1, border);
                    double gx = -tl + tr - 2 * ml + 2 * mr - bl + br;
                    double gy = -tl - 2 * tc - tr + bl + 2 * bc + br;
                    int i = y * width + x;
                    m[i] = (float)Math.Sqrt(gx * gx + gy * gy);
                    a[i] = (float)Math.Atan2(gy, gx);
                }
            });
            magnitude = m; angle = a;
        }

        
        public static PixelBuffer Directions(float[] magnitude, float[] angle, int width, int height)
        {
            byte[] rgb = new byte[checked(width * height * 3)];
            double max = 0;
            for (int i = 0; i < magnitude.Length; i++) max = Math.Max(max, magnitude[i]);
            if (max == 0) return new PixelBuffer(width, height, rgb);
            for (int i = 0; i < magnitude.Length; i++)
            {
                double h = angle[i] * 180 / Math.PI;
                if (h < 0) h += 360;
                double v = magnitude[i] / max, c = v;
                double x = c * (1 - Math.Abs((h / 60) % 2 - 1));
                double r = 0, g = 0, b = 0;
                if (h < 60) { r = c; g = x; } else if (h < 120) { r = x; g = c; }
                else if (h < 180) { g = c; b = x; } else if (h < 240) { g = x; b = c; }
                else if (h < 300) { r = x; b = c; } else { r = c; b = x; }
                rgb[3 * i] = (byte)Math.Round(r * 255); rgb[3 * i + 1] = (byte)Math.Round(g * 255);
                rgb[3 * i + 2] = (byte)Math.Round(b * 255);
            }
            return new PixelBuffer(width, height, rgb);
        }

        private static ProcessingResult Edges(byte[] gray, int width, int height, Settings settings, ProcessingContext context)
        {
            float[] input = new float[gray.Length];
            for (int i = 0; i < gray.Length; i++) input[i] = gray[i];
            if (settings.Method == Method.Canny)
                input = Gaussian(input, width, height, settings.Sigma, settings.Border, context);
            float[] magnitude, angle;
            Gradient(input, width, height, settings.Border, context, 35, 55, out magnitude, out angle);
            byte[] output = new byte[gray.Length];
            ProcessingResult result = new ProcessingResult { Angles = angle };
            if (settings.Method == Method.Sobel)
            {
                context.Rows(height, 55, 90, y =>
                {
                    for (int x = 0; x < width; x++)
                    { int i = y * width + x; output[i] = magnitude[i] >= settings.Threshold && magnitude[i] > 0 ? (byte)255 : (byte)0; }
                });

            }
            else
            {
                float[] nms = new float[gray.Length];
                float[] offsetX = new float[gray.Length], offsetY = new float[gray.Length];
                context.Rows(height, 55, 75, y =>
                {
                    for (int x = 0; x < width; x++)
                    {
                        int i = y * width + x;
                        double value = magnitude[i];
                        if (value <= 0) continue;
                        double dx = Math.Cos(angle[i]), dy = Math.Sin(angle[i]);
                        double before = PixelBuffer.Bilinear(magnitude, width, height, x - dx, y - dy, settings.Border);
                        double after = PixelBuffer.Bilinear(magnitude, width, height, x + dx, y + dy, settings.Border);
                        if (value < before || value < after) continue;
                  
                        if (value == before && value == after) continue;
                        if (value == after && value > before) continue;
                        nms[i] = (float)value;
                        double denominator = before - 2 * value + after;
                        double offset = Math.Abs(denominator) > 1e-9 ? 0.5 * (before - after) / denominator : 0;
                        offset = Math.Max(-0.5, Math.Min(0.5, offset));
                        offsetX[i] = (float)(offset * dx); offsetY[i] = (float)(offset * dy);
                    }
                });
                
                int[] queue = new int[gray.Length];
                int head = 0, tail = 0;
                for (int i = 0; i < nms.Length; i++)
                {
                    if ((i & 65535) == 0) context.Cancellation.ThrowIfCancellationRequested();
                    if (nms[i] >= settings.High && nms[i] > 0) { output[i] = 255; queue[tail++] = i; }
                }
                while (head < tail)
                {
                    if ((head & 4095) == 0) { context.Cancellation.ThrowIfCancellationRequested(); context.Report(75 + head * 20 / gray.Length); }
                    int i = queue[head++], x = i % width, y = i / width;
                    for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                        int j = ny * width + nx;
                        if (output[j] == 0 && nms[j] >= settings.Low && nms[j] > 0)
                        { output[j] = 255; queue[tail++] = j; }
                    }
                }
                result.OffsetX = offsetX; result.OffsetY = offsetY;

            }
            context.Cancellation.ThrowIfCancellationRequested();
            result.DirectionMap = Directions(magnitude, angle, width, height);
            result.Image = PixelBuffer.FromGray(width, height, output);
            return result;
        }

        private static byte[] Detect(byte[] gray, int width, int height, Settings settings, ProcessingContext context)
        {
            float[] input = new float[gray.Length];
            for (int i = 0; i < gray.Length; i++) input[i] = gray[i];
            int[][] masks = settings.Method == Method.Points ?
                new int[][] { new int[] { -1, -1, -1, -1, 8, -1, -1, -1, -1 } } :
                new int[][] {
                    new int[] { -1,-1,-1,2,2,2,-1,-1,-1 },
                    new int[] { -1,2,-1,-1,2,-1,-1,2,-1 },
                    new int[] { 2,-1,-1,-1,2,-1,-1,-1,2 },
                    new int[] { -1,-1,2,-1,2,-1,2,-1,-1 } };
            byte[] output = new byte[gray.Length];
            context.Rows(height, 5, 95, y =>
            {
                for (int x = 0; x < width; x++)
                {
                    double response = 0;
                    for (int m = 0; m < masks.Length; m++)
                    {
                        if (settings.Method == Method.Lines && settings.LineDirection > 0 && m != settings.LineDirection - 1) continue;
                        double value = 0;
                        for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
                            value += masks[m][(dy + 1) * 3 + dx + 1] * PixelBuffer.Sample(input, width, height, x + dx, y + dy, settings.Border);
                        response = Math.Max(response, Math.Abs(value));
                    }
                    output[y * width + x] = response >= settings.Threshold && response > 0 ? (byte)255 : (byte)0;
                }
            });
            return output;
        }

        public static ProcessingResult Run(PixelBuffer source, Settings settings, ProcessingContext context)
        {
            if (settings.Radius < 1 || settings.Radius > 100 || settings.Sigma <= 0 || settings.Sigma > 5 ||
                settings.K < 0 || settings.K > 1 || settings.Threshold < 0 || settings.Low <= 0 || settings.High <= 0 || settings.Low > settings.High)
                throw new ArgumentException("Недопустимые параметры обработки.");
            context.Cancellation.ThrowIfCancellationRequested();
            byte[] gray = source.Gray();
            context.Report(5);
            ProcessingResult result = new ProcessingResult();
            byte[] output;
            if (settings.Method == Method.Otsu || settings.Method == Method.Isodata)
            {
                long[] histogram = Histogram(gray);
                if (settings.Method == Method.Otsu)
                {
                    double[] variance;
                    result.Threshold = Otsu(histogram, out variance); result.OtsuVariance = variance;
                }
                else
                {
                    int iterations;
                    result.Threshold = Isodata(histogram, out iterations); result.Iterations = iterations;
                }
                output = Binarize(gray, source.Width, source.Height, result.Threshold, context);
                result.Image = PixelBuffer.FromGray(source.Width, source.Height, output);
            }
            else if (settings.Method == Method.Sauvola)
            {
                output = Sauvola(gray, source.Width, source.Height, settings, context);
                result.Image = PixelBuffer.FromGray(source.Width, source.Height, output);
            }
            else if (settings.Method == Method.Sobel || settings.Method == Method.Canny)
                result = Edges(gray, source.Width, source.Height, settings, context);
            else
            {
                output = Detect(gray, source.Width, source.Height, settings, context);
                result.Image = PixelBuffer.FromGray(source.Width, source.Height, output);
            }
            context.Cancellation.ThrowIfCancellationRequested();
            context.Report(100);
            return result;
        }
    }
}
