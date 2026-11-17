namespace Lab5.Core;

public sealed record PolygonResult(List<Vec[]> Rings, int Intersections, int Pieces)
{
    public int Components => Rings.Count(r => Geo.Area(r) > Geo.Eps * Geo.Eps);
    public int Holes => Rings.Count - Components;
    public double Area => Math.Abs(Rings.Sum(r => Geo.Area(r)));
}

public static class PolygonClipper
{
    sealed class Edge(Vec a, Vec b)
    {
        public Vec A = a, B = b;
        public List<double> Cuts = [0, 1];
    }
    sealed class Vertex(Vec point)
    {
        public Vec Point = point;
        public List<int> Outgoing = [];
    }
    sealed class Arc(int from, int to)
    {
        public int From = from, To = to;
        public bool Used;
    }

    public static PolygonResult Clip(IReadOnlyList<Vec[]> subject, IReadOnlyList<Vec[]> clip)
    {
        var edges = new List<Edge>();
        foreach (var ring in subject.Concat(clip))
            for (int i = 0; i < ring.Length; i++)
                if (!Geo.Near(ring[i], ring[(i + 1) % ring.Length]))
                    edges.Add(new(ring[i], ring[(i + 1) % ring.Length]));
        int intersections = 0;
        for (int i = 0; i < edges.Count; i++)
            for (int j = i + 1; j < edges.Count; j++)
                if (Split(edges[i], edges[j])) intersections++;

        var vertices = new List<Vertex>();
        var buckets = new Dictionary<(long, long), List<int>>();
        var arcs = new List<Arc>();
        var unique = new HashSet<(int, int)>();
        int VertexId(Vec p)
        {
            long x = (long)Math.Floor(p.X / Geo.Eps), y = (long)Math.Floor(p.Y / Geo.Eps);
            for (int dx = -1; dx <= 1; dx++) for (int dy = -1; dy <= 1; dy++)
                if (buckets.TryGetValue((x + dx, y + dy), out var ids))
                    foreach (int id in ids) if (Geo.Near(vertices[id].Point, p)) return id;
            int index = vertices.Count; vertices.Add(new(p));
            if (!buckets.TryGetValue((x,y), out var bucket)) buckets[(x,y)] = bucket = [];
            bucket.Add(index); return index;
        }
        bool Filled(Vec p) => Geo.Inside(p, subject) && Geo.Inside(p, clip);

        foreach (var edge in edges)
        {
            edge.Cuts.Sort();
            var direction = edge.B - edge.A;
            double length = direction.Length;
            for (int i = 1; i < edge.Cuts.Count; i++)
            {
                double t0 = edge.Cuts[i - 1], t1 = edge.Cuts[i];
                if ((t1 - t0) * length <= Geo.Eps) continue;
                Vec a = edge.A + direction * t0, b = edge.A + direction * t1;
                Vec mid = (a + b) * .5;
                double offset = Math.Min((b - a).Length * .1, Geo.Eps * 4);
                foreach (var other in edges)
                {
                    var d = other.B - other.A;
                    double u = Math.Clamp(Geo.Dot(mid - other.A, d) / Geo.Dot(d, d), 0, 1);
                    double distance = (mid - (other.A + d * u)).Length;
                    if (distance > Geo.Eps) offset = Math.Min(offset, distance * .25);
                }
                Vec normal = new(-direction.Y / length * offset, direction.X / length * offset);
                bool left = Filled(mid + normal), right = Filled(mid - normal);
                if (left == right) continue;
                int from = VertexId(left ? a : b), to = VertexId(left ? b : a);
                if (from == to || !unique.Add((from, to))) continue;
                vertices[from].Outgoing.Add(arcs.Count); arcs.Add(new(from, to));
            }
        }

        var rings = new List<Vec[]>();
        for (int start = 0; start < arcs.Count; start++)
        {
            if (arcs[start].Used) continue;
            var ring = new List<Vec>();
            int current = start, firstVertex = arcs[start].From;
            bool closed = false;
            for (int step = 0; step <= arcs.Count; step++)
            {
                var arc = arcs[current];
                if (arc.Used) break;
                arc.Used = true; ring.Add(vertices[arc.From].Point);
                if (arc.To == firstVertex) { closed = true; break; }
                Vec reverse = vertices[arc.From].Point - vertices[arc.To].Point;
                double angle = Math.Atan2(reverse.Y, reverse.X), best = double.PositiveInfinity;
                int next = -1;
                foreach (int candidate in vertices[arc.To].Outgoing)
                {
                    if (arcs[candidate].Used) continue;
                    Vec d = vertices[arcs[candidate].To].Point - vertices[arc.To].Point;
                    double turn = (angle - Math.Atan2(d.Y, d.X) + Math.PI * 2) % (Math.PI * 2);
                    if (turn < best) { best = turn; next = candidate; }
                }
                if (next < 0) break;
                current = next;
            }
            if (!closed) throw new InvalidOperationException("Не замкнулся контур. Проверьте масштаб и расстояния между вершинами (ε=10⁻⁷).");
            Simplify(ring);
            if (ring.Count >= 3 && Math.Abs(Geo.Area(ring)) > Geo.Eps * Geo.Eps) rings.Add(ring.ToArray());
        }
        return new(rings, intersections, arcs.Count);
    }

    static bool Split(Edge a, Edge b)
    {
        Vec r = a.B - a.A, s = b.B - b.A, q = b.A - a.A;
        double lr = r.Length, ls = s.Length, denominator = Geo.Cross(r, s);
        if (Math.Abs(denominator) > Geo.Eps * Math.Min(lr, ls))
        {
            double t = Geo.Cross(q, s) / denominator, u = Geo.Cross(q, r) / denominator;
            if (t < -Geo.Eps / lr || t > 1 + Geo.Eps / lr || u < -Geo.Eps / ls || u > 1 + Geo.Eps / ls) return false;
            a.Cuts.Add(Math.Clamp(t, 0, 1)); b.Cuts.Add(Math.Clamp(u, 0, 1));
            return t > Geo.Eps / lr && t < 1 - Geo.Eps / lr || u > Geo.Eps / ls && u < 1 - Geo.Eps / ls;
        }
        if (Math.Abs(Geo.Cross(q, r)) > Geo.Eps * lr) return false;
        bool added = false;
        Add(a, b.A); Add(a, b.B); Add(b, a.A); Add(b, a.B);
        return added;
        void Add(Edge e, Vec point)
        {
            var d = e.B - e.A;
            double t = Geo.Dot(point - e.A, d) / Geo.Dot(d, d);
            double tolerance=Geo.Eps/d.Length;
            if (t >= -tolerance && t <= 1+tolerance) { e.Cuts.Add(Math.Clamp(t,0,1)); added = true; }
        }
    }
    static void Simplify(List<Vec> ring)
    {
        bool changed = true;
        while (changed && ring.Count > 3)
        {
            changed = false;
            for (int i = 0; i < ring.Count; i++)
            {
                Vec a = ring[(i + ring.Count - 1) % ring.Count], b = ring[i], c = ring[(i + 1) % ring.Count];
                if (Math.Abs(Geo.Cross(b - a, c - b)) <= Geo.Eps * Math.Max((c - a).Length, Geo.Eps) && Geo.Dot(b - a, c - b) >= -Geo.Eps*Geo.Eps)
                { ring.RemoveAt(i); changed = true; break; }
            }
        }
    }
}
