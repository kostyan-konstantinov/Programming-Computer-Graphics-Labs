namespace Lab5.Core;

public readonly record struct Vec(double X, double Y)
{
    public static Vec operator +(Vec a, Vec b) => new(a.X + b.X, a.Y + b.Y);
    public static Vec operator -(Vec a, Vec b) => new(a.X - b.X, a.Y - b.Y);
    public static Vec operator *(Vec a, double t) => new(a.X * t, a.Y * t);
    public double Length => Math.Sqrt(X * X + Y * Y);
}

public readonly record struct Segment(Vec A, Vec B)
{
    public Box Bounds => new(Math.Min(A.X, B.X), Math.Min(A.Y, B.Y),
        Math.Max(A.X, B.X), Math.Max(A.Y, B.Y));
}

public readonly record struct Box(double Left, double Bottom, double Right, double Top)
{
    public double Width => Right - Left;
    public double Height => Top - Bottom;
    public Vec Center => new((Left + Right) / 2, (Bottom + Top) / 2);
    public bool Contains(Vec p) => p.X >= Left - Geo.Eps && p.X <= Right + Geo.Eps &&
        p.Y >= Bottom - Geo.Eps && p.Y <= Top + Geo.Eps;
    public bool Contains(Box b) => b.Left >= Left - Geo.Eps && b.Right <= Right + Geo.Eps &&
        b.Bottom >= Bottom - Geo.Eps && b.Top <= Top + Geo.Eps;
    public bool Intersects(Box b) => !(b.Right < Left - Geo.Eps || b.Left > Right + Geo.Eps ||
        b.Top < Bottom - Geo.Eps || b.Bottom > Top + Geo.Eps);
    public Vec[] Ring => [new(Left, Bottom), new(Right, Bottom), new(Right, Top), new(Left, Top)];
    public Box Move(Vec d) => new(Left + d.X, Bottom + d.Y, Right + d.X, Top + d.Y);
}

public static class Geo
{
    public const double Eps = 1e-7;
    public static bool Near(double a, double b) => Math.Abs(a - b) <= Eps;
    public static bool Near(Vec a, Vec b) => Math.Abs(a.X - b.X) <= Eps && Math.Abs(a.Y - b.Y) <= Eps;
    public static double Cross(Vec a, Vec b) => a.X * b.Y - a.Y * b.X;
    public static double Dot(Vec a, Vec b) => a.X * b.X + a.Y * b.Y;
    public static double Area(IReadOnlyList<Vec> ring)
    {
        if(ring.Count<3) return 0;
        double sum = 0;
        Vec origin=ring[0];
        for (int i = 0; i < ring.Count; i++) sum += Cross(ring[i]-origin, ring[(i + 1) % ring.Count]-origin);
        return sum / 2;
    }
    public static bool Inside(Vec p, IReadOnlyList<Vec[]> rings)
    {
        bool inside = false;
        foreach (var ring in rings)
            for (int i = 0, j = ring.Length - 1; i < ring.Length; j = i++)
            {
                var a = ring[j]; var b = ring[i];
                if (Math.Abs(b.Y-a.Y)<=Eps) continue;
                if ((a.Y > p.Y+Eps) == (b.Y > p.Y+Eps)) continue;
                double x = a.X + (p.Y - a.Y) * (b.X - a.X) / (b.Y - a.Y);
                if (x > p.X+Eps) inside = !inside;
            }
        return inside;
    }
}

public sealed class Counts
{
    public long Multiply, Divide;
}
