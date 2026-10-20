namespace RasterLab;

public static class RasterEngine
{
    private static int Round(double value)
    {
        return (int)Math.Round(value, MidpointRounding.AwayFromZero);
    }

    public static void StepLine(int x1, int y1, int x2, int y2, IRasterTarget target)
    {
        int dx = x2 - x1;
        int dy = y2 - y1;

        if (dx == 0 && dy == 0)
        {
            target.SetPixel(x1, y1, 1);
            return;
        }

        if (Math.Abs(dx) >= Math.Abs(dy))
        {
            double k = (double)dy / dx;
            int step = Math.Sign(dx);

            for (int x = x1; ; x += step)
            {
                target.SetPixel(x, Round(y1 + k * (x - x1)), 1);
                if (x == x2) break;
            }
        }
        else
        {
            double k = (double)dx / dy;
            int step = Math.Sign(dy);

            for (int y = y1; ; y += step)
            {
                target.SetPixel(Round(x1 + k * (y - y1)), y, 1);
                if (y == y2) break;
            }
        }
    }

    public static void DDALine(int x1, int y1, int x2, int y2, IRasterTarget target)
    {
        int dx = x2 - x1;
        int dy = y2 - y1;
        int steps = Math.Max(Math.Abs(dx), Math.Abs(dy));

        if (steps == 0)
        {
            target.SetPixel(x1, y1, 1);
            return;
        }

        double x = x1;
        double y = y1;
        double stepX = (double)dx / steps;
        double stepY = (double)dy / steps;

        for (int i = 0; i < steps; i++)
        {
            target.SetPixel(Round(x), Round(y), 1);
            x += stepX;
            y += stepY;
        }

        target.SetPixel(x2, y2, 1);
    }

    public static void BresenhamLine(int x1, int y1, int x2, int y2, IRasterTarget target)
    {
        int dx = Math.Abs(x2 - x1);
        int dy = Math.Abs(y2 - y1);
        int sx = Math.Sign(x2 - x1);
        int sy = Math.Sign(y2 - y1);
        int error = dx - dy;

        while (true)
        {
            target.SetPixel(x1, y1, 1);
            if (x1 == x2 && y1 == y2) break;

            int doubledError = 2 * error;

            if (doubledError > -dy)
            {
                error -= dy;
                x1 += sx;
            }

            if (doubledError < dx)
            {
                error += dx;
                y1 += sy;
            }
        }
    }

    public static void BresenhamCircle(int cx, int cy, int radius, IRasterTarget target)
    {
        if (radius < 0)
            throw new ArgumentOutOfRangeException(nameof(radius));

        int x = 0;
        int y = radius;
        int error = 3 - 2 * radius;

        while (x <= y)
        {
            CirclePixels(cx, cy, x, y, target);

            if (error < 0)
            {
                error += 4 * x + 6;
            }
            else
            {
                error += 4 * (x - y) + 10;
                y--;
            }

            x++;
        }
    }

    private static void CirclePixels(int cx, int cy, int x, int y, IRasterTarget target)
    {
        target.SetPixel(cx + x, cy + y, 1);
        target.SetPixel(cx - x, cy + y, 1);
        target.SetPixel(cx + x, cy - y, 1);
        target.SetPixel(cx - x, cy - y, 1);
        target.SetPixel(cx + y, cy + x, 1);
        target.SetPixel(cx - y, cy + x, 1);
        target.SetPixel(cx + y, cy - x, 1);
        target.SetPixel(cx - y, cy - x, 1);
    }

    public static void WuLine(int x1, int y1, int x2, int y2, IRasterTarget target)
    {
        if (x1 == x2 && y1 == y2)
        {
            target.SetPixel(x1, y1, 1);
            return;
        }

        bool steep = Math.Abs(y2 - y1) > Math.Abs(x2 - x1);

        if (steep)
        {
            (x1, y1) = (y1, x1);
            (x2, y2) = (y2, x2);
        }

        if (x1 > x2)
        {
            (x1, x2) = (x2, x1);
            (y1, y2) = (y2, y1);
        }

        double gradient = (double)(y2 - y1) / (x2 - x1);

        WuPixel(x1, y1, 0.5, steep, target);
        WuPixel(x2, y2, 0.5, steep, target);

        double y = y1 + gradient;

        for (int x = x1 + 1; x < x2; x++)
        {
            int lower = (int)Math.Floor(y);
            double fraction = y - lower;

            WuPixel(x, lower, 1 - fraction, steep, target);
            WuPixel(x, lower + 1, fraction, steep, target);

            y += gradient;
        }
    }

    private static void WuPixel(int x, int y, double intensity, bool steep, IRasterTarget target)
    {
        if (intensity <= 0) return;

        if (steep)
            target.SetPixel(y, x, intensity);
        else
            target.SetPixel(x, y, intensity);
    }

    public static void CastlePitwayLine(int x1, int y1, int x2, int y2, IRasterTarget target)
    {
        int dx = Math.Abs(x2 - x1);
        int dy = Math.Abs(y2 - y1);
        int sx = Math.Sign(x2 - x1);
        int sy = Math.Sign(y2 - y1);

        bool steep = dy > dx;
        int major = Math.Max(dx, dy);
        int minor = Math.Min(dx, dy);

        target.SetPixel(x1, y1, 1);
        if (major == 0) return;

        if (minor == 0 || major == minor)
        {
            for (int i = 0; i < major; i++)
            {
                x1 += sx;
                y1 += sy;
                target.SetPixel(x1, y1, 1);
            }

            return;
        }

        int a = major - minor;
        int b = minor;
        string straight = "s";
        string diagonal = "d";

        while (a != b)
        {
            if (a > b)
            {
                a -= b;
                diagonal = straight + Reverse(diagonal);
            }
            else
            {
                b -= a;
                straight = diagonal + Reverse(straight);
            }
        }

        string pattern = diagonal + Reverse(straight);

        for (int repeat = 0; repeat < a; repeat++)
        {
            foreach (char step in pattern)
            {
                if (steep)
                {
                    y1 += sy;
                    if (step == 'd') x1 += sx;
                }
                else
                {
                    x1 += sx;
                    if (step == 'd') y1 += sy;
                }

                target.SetPixel(x1, y1, 1);
            }
        }
    }

    private static string Reverse(string value)
    {
        char[] chars = value.ToCharArray();
        Array.Reverse(chars);
        return new string(chars);
    }

    public static void MidpointEllipse(int cx, int cy, int a, int b, IRasterTarget target)
    {
        if (a < 0 || b < 0)
            throw new ArgumentOutOfRangeException();

        if (a == 0 || b == 0)
        {
            BresenhamLine(cx - a, cy - b, cx + a, cy + b, target);
            return;
        }

        long a2 = (long)a * a;
        long b2 = (long)b * b;

        int x = 0;
        int y = b;

        long dx = 0;
        long dy = 2 * a2 * y;
        long error = 4 * b2 - 4 * a2 * b + a2;

        while (dx < dy)
        {
            EllipsePixels(cx, cy, x, y, target);

            x++;
            dx += 2 * b2;

            if (error < 0)
            {
                error += 4 * dx + 4 * b2;
            }
            else
            {
                y--;
                dy -= 2 * a2;
                error += 4 * dx - 4 * dy + 4 * b2;
            }
        }

        error = b2 * (2L * x + 1) * (2L * x + 1)
            + 4 * a2 * (y - 1L) * (y - 1L)
            - 4 * a2 * b2;

        while (y >= 0)
        {
            EllipsePixels(cx, cy, x, y, target);

            y--;
            dy -= 2 * a2;

            if (error > 0)
            {
                error += 4 * a2 - 4 * dy;
            }
            else
            {
                x++;
                dx += 2 * b2;
                error += 4 * dx - 4 * dy + 4 * a2;
            }
        }

        while (x < a)
        {
            EllipsePixels(cx, cy, x, 0, target);
            x++;
        }

        EllipsePixels(cx, cy, a, 0, target);
    }

    private static void EllipsePixels(int cx, int cy, int x, int y, IRasterTarget target)
    {
        target.SetPixel(cx + x, cy + y, 1);
        target.SetPixel(cx - x, cy + y, 1);
        target.SetPixel(cx + x, cy - y, 1);
        target.SetPixel(cx - x, cy - y, 1);
    }

    public static void BresenhamCircleArc(
        int cx, int cy, int radius,
        int startAngle, int endAngle,
        IRasterTarget target)
    {
        if (Math.Abs(endAngle - startAngle) < 360)
            target = new ArcRasterTarget(cx, cy, startAngle, endAngle, target);

        BresenhamCircle(cx, cy, radius, target);
    }

    public static void MidpointEllipseArc(
        int cx, int cy, int a, int b,
        int startAngle, int endAngle,
        IRasterTarget target)
    {
        if (Math.Abs(endAngle - startAngle) < 360)
            target = new ArcRasterTarget(cx, cy, startAngle, endAngle, target);

        MidpointEllipse(cx, cy, a, b, target);
    }

    private sealed class ArcRasterTarget : IRasterTarget
    {
        private readonly int cx;
        private readonly int cy;
        private readonly int startAngle;
        private readonly int endAngle;
        private readonly IRasterTarget target;

        public ArcRasterTarget(
            int cx, int cy,
            int startAngle, int endAngle,
            IRasterTarget target)
        {
            this.cx = cx;
            this.cy = cy;
            this.startAngle = ((startAngle % 360) + 360) % 360;
            this.endAngle = ((endAngle % 360) + 360) % 360;
            this.target = target;
        }

        public void SetPixel(int x, int y, double intensity)
        {
            if (x == cx && y == cy)
            {
                target.SetPixel(x, y, intensity);
                return;
            }

            double angle = Math.Atan2(y - cy, x - cx) * 180 / Math.PI;

            if (angle < 0)
                angle += 360;

            const double epsilon = 0.000000001;
            bool inside;

            if (startAngle <= endAngle)
            {
                inside = angle >= startAngle - epsilon
                    && angle <= endAngle + epsilon;
            }
            else
            {
                inside = angle >= startAngle - epsilon
                    || angle <= endAngle + epsilon;
            }

            if (inside)
                target.SetPixel(x, y, intensity);
        }
    }
}