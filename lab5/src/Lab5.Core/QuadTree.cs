namespace Lab5.Core;

public sealed class QuadTree
{
    sealed class Node(Box bounds, int depth)
    {
        public Box Bounds = bounds;
        public int Depth = depth;
        public readonly List<int> Items = [];
        public Node[]? Children;
    }
    readonly Node root;
    readonly Segment[] data;
    readonly Box[] bounds;
    readonly int maxDepth;
    public int Nodes { get; private set; } = 1;

    public QuadTree(Segment[] segments, Box world, int depth)
    {
        data = segments; maxDepth = Math.Clamp(depth, 1, 14);
        bounds = data.Select(s => s.Bounds).ToArray();
        root = new(world, 0);
        for (int i = 0; i < data.Length; i++) Insert(root, i);
    }
    void Insert(Node n, int id)
    {
        var b = bounds[id];
        if (n.Depth < maxDepth && n.Bounds.Contains(b))
        {
            var p = n.Bounds.Center;
            int x = b.Right < p.X - Geo.Eps ? 0 : b.Left > p.X + Geo.Eps ? 1 : -1;
            int y = b.Top < p.Y - Geo.Eps ? 0 : b.Bottom > p.Y + Geo.Eps ? 1 : -1;
            if (x >= 0 && y >= 0)
            {
                if (n.Children == null)
                {
                    var w = n.Bounds;
                    n.Children = [new(new(w.Left,w.Bottom,p.X,p.Y),n.Depth+1),
                        new(new(p.X,w.Bottom,w.Right,p.Y),n.Depth+1),
                        new(new(w.Left,p.Y,p.X,w.Top),n.Depth+1),
                        new(new(p.X,p.Y,w.Right,w.Top),n.Depth+1)];
                    Nodes += 4;
                }
                Insert(n.Children[x + 2 * y], id); return;
            }
        }
        n.Items.Add(id);
    }
    public void Query(Box window, List<int> candidates)
    {
        candidates.Clear();
        Visit(root);
        void Visit(Node n)
        {
            if (!window.Intersects(n.Bounds)) return;
            foreach (int id in n.Items)
            {
                var s = data[id];
                if ((LineClipper.Code(s.A, window) & LineClipper.Code(s.B, window)) == 0 &&
                    window.Intersects(bounds[id])) candidates.Add(id);
            }
            if (n.Children != null) foreach (var child in n.Children) Visit(child);
        }
    }
}
