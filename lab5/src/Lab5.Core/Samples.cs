using System.Globalization;
using System.Text;

namespace Lab5.Core;

public sealed class PolygonSample
{
    public string Name { get; set; } = "Многоугольник";
    public Box Window { get; set; } = new(-40, -30, 40, 30);
    public List<Vec[]> Rings { get; set; } = [];
    public override string ToString() => Name;
    public static PolygonSample Load(string path)
    {
        string[] lines = File.ReadAllLines(path);
        string name = lines.FirstOrDefault(l => l.TrimStart().StartsWith('#'))?.Trim().TrimStart('#').Trim()
            ?? Path.GetFileNameWithoutExtension(path);
        string[] words = lines.Select(l => l.Split('#')[0])
            .SelectMany(l => l.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToArray();
        int index = 0;
        string Next() => index < words.Length ? words[index++] : throw new FormatException("Не хватает координат или границ окна.");
        int ReadInt(int max)
        {
            if (!int.TryParse(Next(), out int n) || n < 0 || n > max) throw new FormatException("Неверное число контуров или вершин.");
            return n;
        }
        double ReadNumber()
        {
            if (!double.TryParse(Next(), NumberStyles.Float, CultureInfo.InvariantCulture, out double d) ||
                !double.IsFinite(d) || Math.Abs(d) > 1e6) throw new FormatException("Координаты: конечные числа от −10⁶ до 10⁶.");
            return d;
        }
        var sample = new PolygonSample { Name = name };
        int count = ReadInt(666), total = 0;
        for (int i = 0; i < count; i++)
        {
            int n = ReadInt(2000); total += n;
            if (n < 3 || total > 2000) throw new FormatException("В контуре нужно хотя бы 3 вершины; всего не больше 2000.");
            var ring = new Vec[n];
            for (int j = 0; j < n; j++) ring[j] = new(ReadNumber(), ReadNumber());
            sample.Rings.Add(ring);
        }
        sample.Window = new(ReadNumber(), ReadNumber(), ReadNumber(), ReadNumber());
        if (sample.Window.Width <= Geo.Eps || sample.Window.Height <= Geo.Eps) throw new FormatException("Размер окна должен быть положительным.");
        if (index != words.Length) throw new FormatException("Лишние данные после границ окна.");
        return sample;
    }
    public void Save(string path)
    {
        var text = new StringBuilder();
        text.AppendLine("# " + Name.Replace('\r', ' ').Replace('\n', ' '));
        text.AppendLine(Rings.Count.ToString(CultureInfo.InvariantCulture));
        foreach (var ring in Rings)
        {
            text.AppendLine(ring.Length.ToString(CultureInfo.InvariantCulture));
            foreach (var p in ring) text.AppendLine(FormattableString.Invariant($"{p.X:R} {p.Y:R}"));
        }
        text.AppendLine(FormattableString.Invariant($"{Window.Left:R} {Window.Bottom:R} {Window.Right:R} {Window.Top:R}"));
        File.WriteAllText(path, text.ToString());
    }
}

public static class Samples
{
    public const int MaxLines = 500_000;
    public static readonly Box World = new(-1000, -1000, 1000, 1000);
    public static Segment[] Generate(int count, string distribution, int seed = 21)
    {
        if (count < 1 || count > MaxLines) throw new ArgumentOutOfRangeException(nameof(count), "Количество: от 1 до 500 000.");
        if (distribution != "dense" && distribution != "short") throw new ArgumentException("Выберите плотные или короткие отрезки.");
        var random = new Random(seed);
        var data = new Segment[count];
        for (int i = 0; i < count; i++)
        {
            double x = random.NextDouble() * 1980 - 990, y = random.NextDouble() * 1980 - 990;
            double length = distribution == "dense" ? 80 : 12;
            double dx = (random.NextDouble() - .5) * length, dy = (random.NextDouble() - .5) * length;
            data[i] = new(new(x, y), new(Math.Clamp(x + dx, -999, 999), Math.Clamp(y + dy, -999, 999)));
        }
        return data;
    }
    public static (Segment[] Lines, Box Window) LoadLines(string path)
    {
        var words = File.ReadLines(path).Select(l => l.Split('#')[0])
            .SelectMany(l => l.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToArray();
        if (words.Length == 0 || !int.TryParse(words[0], out int n) || n < 0 || n > MaxLines || words.Length != 1L + n * 4L + 4)
            throw new FormatException("Формат: n, затем n строк x1 y1 x2 y2, затем xmin ymin xmax ymax.");
        int index = 1;
        double Read()
        {
            double d = double.Parse(words[index++], CultureInfo.InvariantCulture);
            if (!double.IsFinite(d) || Math.Abs(d) > 1e6) throw new FormatException("Координаты: конечные числа от −10⁶ до 10⁶.");
            return d;
        }
        var lines = new Segment[n];
        for (int i = 0; i < n; i++) lines[i] = new(new(Read(), Read()), new(Read(), Read()));
        var window = new Box(Read(), Read(), Read(), Read());
        if (window.Width <= Geo.Eps || window.Height <= Geo.Eps) throw new FormatException("Размер окна должен быть положительным.");
        return (lines, window);
    }
    public static Box Bounds(Segment[] lines)
    {
        if (lines.Length == 0) return new(-100, -100, 100, 100);
        return new(lines.Min(s=>s.Bounds.Left)-1, lines.Min(s=>s.Bounds.Bottom)-1,
            lines.Max(s=>s.Bounds.Right)+1, lines.Max(s=>s.Bounds.Top)+1);
    }
}
