using System.Diagnostics;

namespace Lab5.Core;

public enum LineMethod { Midpoint, CohenSutherland, LiangBarsky }

public sealed record LineResult(Segment[] Segments, int Candidates, double Milliseconds);

public static class LineClipper
{
    public static int Code(Vec p, Box w)
    {
        int c = 0;
        if (p.X < w.Left - Geo.Eps) c |= 1;
        if (p.X > w.Right + Geo.Eps) c |= 2;
        if (p.Y < w.Bottom - Geo.Eps) c |= 4;
        if (p.Y > w.Top + Geo.Eps) c |= 8;
        return c;
    }

    public static bool Clip(Segment s, Box w, LineMethod method, out Segment result, Counts? c = null) =>
        method switch
        {
            LineMethod.Midpoint => Midpoint(s, w, out result, c),
            LineMethod.CohenSutherland => Cohen(s, w, out result, c),
            _ => Liang(s, w, out result, c)
        };

    public static LineResult ClipAll(Segment[] data, QuadTree? tree, Box window, LineMethod method)
    {
        var timer = Stopwatch.StartNew();
        var candidates = new List<int>();
        if (tree != null) tree.Query(window, candidates);
        else for (int i = 0; i < data.Length; i++)
            if ((Code(data[i].A, window) & Code(data[i].B, window)) == 0 && window.Intersects(data[i].Bounds))
                candidates.Add(i);
        var visible = new List<Segment>(candidates.Count);
        foreach (int id in candidates)
            if (Clip(data[id], window, method, out var result)) visible.Add(result);
        return new(visible.ToArray(), candidates.Count, timer.Elapsed.TotalMilliseconds);
    }

    public static bool Midpoint(Segment s, Box w, out Segment result, Counts? stats = null)
    {
        result = default;
        int initialA=Code(s.A,w), initialB=Code(s.B,w);
        if((initialA|initialB)==0 || (initialA&initialB)!=0)
        {
            bool accepted=(initialA|initialB)==0;
            if(accepted) result=s;
            return accepted;
        }
        double dx = Math.Abs(s.B.X - s.A.X), dy = Math.Abs(s.B.Y - s.A.Y);
        double length = Math.Max(dx, dy);
        double ex = dx <= Geo.Eps || length <= Geo.Eps ? Geo.Eps : Geo.Eps * dx / length;
        double ey = dy <= Geo.Eps || length <= Geo.Eps ? Geo.Eps : Geo.Eps * dy / length;
        if (stats != null && length > Geo.Eps) { stats.Multiply += (dx > Geo.Eps ? 1 : 0) + (dy > Geo.Eps ? 1 : 0); stats.Divide += (dx > Geo.Eps ? 1 : 0) + (dy > Geo.Eps ? 1 : 0); }
        int MidCode(Vec p)
        {
            int code = 0;
            if (p.X < w.Left - ex) code |= 1;
            if (p.X > w.Right + ex) code |= 2;
            if (p.Y < w.Bottom - ey) code |= 4;
            if (p.Y > w.Top + ey) code |= 8;
            return code;
        }
        bool Search(Vec a, Vec b, int depth, out Segment found)
        {
            found = default;
            int ca = MidCode(a), cb = MidCode(b);
            if ((ca | cb) == 0) { found = new(a, b); return true; }
            if ((ca & cb) != 0) return false;
            var m = new Vec((a.X + b.X) * .5, (a.Y + b.Y) * .5);
            if (stats != null) stats.Multiply += 2;
            if (depth >= 64 || (Math.Abs(a.X - b.X) <= Geo.Eps && Math.Abs(a.Y - b.Y) <= Geo.Eps))
            {
                if (w.Contains(m)) { found = new(m, m); return true; }
                return false;
            }
            bool left = Search(a, m, depth + 1, out var l);
            bool right = Search(m, b, depth + 1, out var r);
            found = left && right ? new(l.A, r.B) : left ? l : r;
            return left || right;
        }
        return Search(s.A, s.B, 0, out result);
    }

    public static bool Cohen(Segment s, Box w, out Segment result, Counts? c = null)
    {
        Vec a = s.A, b = s.B;
        result = default;
        for (int i = 0; i < 16; i++)
        {
            int ca = Code(a, w), cb = Code(b, w);
            if ((ca | cb) == 0) { result = new(a, b); return true; }
            if ((ca & cb) != 0) return false;
            int code = ca != 0 ? ca : cb;
            double dx = b.X - a.X, dy = b.Y - a.Y;
            Vec p;
            if ((code & 12) != 0)
            {
                if (Math.Abs(dy) <= Geo.Eps) return false;
                double y = (code & 8) != 0 ? w.Top : w.Bottom;
                p = new(a.X + (y - a.Y) * dx / dy, y);
            }
            else
            {
                if (Math.Abs(dx) <= Geo.Eps) return false;
                double x = (code & 2) != 0 ? w.Right : w.Left;
                p = new(x, a.Y + (x - a.X) * dy / dx);
            }
            if (c != null) { c.Multiply++; c.Divide++; }
            if (ca != 0) a = p; else b = p;
        }
        return false;
    }

    public static bool Liang(Segment s, Box w, out Segment result, Counts? c = null)
    {
        result = default;
        double enter = 0, exit = 1;
        double dx = s.B.X - s.A.X, dy = s.B.Y - s.A.Y;
        double tolerance = Geo.Eps / Math.Max(1, Math.Max(Math.Abs(dx), Math.Abs(dy)));
        if (c != null) c.Divide++;
        bool Edge(double p, double q)
        {
            if (Math.Abs(p) <= Geo.Eps) return q >= -Geo.Eps;
            double t = q / p;
            if (c != null) c.Divide++;
            if (p < 0) enter = Math.Max(enter, t); else exit = Math.Min(exit, t);
            return enter <= exit + tolerance;
        }
        if (!Edge(-dx, s.A.X - w.Left) || !Edge(dx, w.Right - s.A.X) ||
            !Edge(-dy, s.A.Y - w.Bottom) || !Edge(dy, w.Top - s.A.Y)) return false;
        if (enter > exit)
        {
            enter = exit = (enter + exit) * .5;
            if (c != null) c.Multiply++;
        }
        result = new(new(s.A.X + enter * dx, s.A.Y + enter * dy),
            new(s.A.X + exit * dx, s.A.Y + exit * dy));
        if (c != null) c.Multiply += 4;
        return true;
    }
}
