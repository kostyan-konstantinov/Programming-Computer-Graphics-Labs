using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Lab5.Core;

public sealed record TimingStats(double Median, double P10, double P90);
public sealed record AlgorithmResult(string Algorithm, TimingStats Time, long Multiply, long Divide, int Visible);
public sealed record BenchmarkRow(string Type, int Count, int Depth, int Nodes, TimingStats Build,
    TimingStats Clip, TimingStats Scan, double Candidates);
public sealed record DensityRow(string Type, int Count, TimingStats Clip, double Candidates);
public sealed record BenchmarkSample(string Experiment, string Type, int Count, int Depth, string Metric,
    int Round, int Iterations, double Milliseconds);
public sealed class BenchmarkResult
{
    public List<BenchmarkRow> Rows { get; } = [];
    public List<DensityRow> Density { get; } = [];
    public List<AlgorithmResult> Algorithms { get; set; } = [];
    public List<BenchmarkSample> Samples { get; } = [];
    public bool HasResults => Rows.Count + Density.Count + Algorithms.Count + Samples.Count > 0;
    public string SourceDirectory { get; set; } = "";
}

public static class Benchmark
{
    public const int Repeats = 15;
    static readonly CultureInfo Russian = CultureInfo.GetCultureInfo("ru-RU");
    static readonly Encoding CsvEncoding = new UTF8Encoding(true);

    public static TimingStats Summarize(IEnumerable<double> source)
    {
        var values = source.Order().ToArray();
        if (values.Length == 0 || values.Any(v => !double.IsFinite(v) || v < 0))
            throw new ArgumentException("Нужны конечные неотрицательные замеры.");
        double Percentile(double fraction)
        {
            double position = (values.Length - 1) * fraction;
            int index = (int)position;
            return values[index] + (values[Math.Min(index + 1, values.Length - 1)] - values[index]) * (position - index);
        }
        return new(Percentile(.5), Percentile(.1), Percentile(.9));
    }

    static TimingStats Measure(Action action, BenchmarkResult result, BenchmarkSample sample,
        IProgress<string>? progress, CancellationToken cancellation, int operations = 1)
    {
        var warmup = Stopwatch.StartNew();
        int warmed = 0;
        do { cancellation.ThrowIfCancellationRequested(); action(); warmed++; }
        while (warmed < 3 || warmup.Elapsed.TotalMilliseconds < 150);
        int iterations = 1;
        while (true)
        {
            var timer = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++) { cancellation.ThrowIfCancellationRequested(); action(); }
            if (timer.Elapsed.TotalMilliseconds >= 40 || iterations >= 1024) break;
            iterations *= 2;
        }
        var values = new double[Repeats];
        for (int round = 0; round < Repeats; round++)
        {
            cancellation.ThrowIfCancellationRequested();
            progress?.Report($"{sample.Type}, {sample.Count:N0}, {sample.Metric.ToLower(Russian)}, глубина {sample.Depth}: серия {round + 1}/{Repeats}");
            long start = Stopwatch.GetTimestamp();
            for (int i = 0; i < iterations; i++) { cancellation.ThrowIfCancellationRequested(); action(); }
            values[round] = Stopwatch.GetElapsedTime(start).TotalMilliseconds / iterations / operations;
            result.Samples.Add(sample with { Round = round + 1, Iterations = iterations, Milliseconds = values[round] });
        }
        return Summarize(values);
    }

    public static List<AlgorithmResult> Compare(Segment[] data, Box window, BenchmarkResult? result = null,
        IProgress<string>? progress = null, CancellationToken cancellation = default)
    {
        result ??= new();
        string[] names = ["Средняя точка", "Сазерленд–Коэн", "Лианг–Барски"];
        var rows = new List<AlgorithmResult>();
        foreach (var method in Enum.GetValues<LineMethod>())
        {
            int visible = 0;
            void Run()
            {
                int accepted = 0;
                foreach (var segment in data)
                    if (LineClipper.Clip(segment, window, method, out _)) accepted++;
                visible = accepted;
            }
            string name = names[(int)method];
            var timing = Measure(Run, result, new("Алгоритмы", name, data.Length, 0, "Сравнение", 0, 0, 0), progress, cancellation);
            var counts = new Counts();
            int checkedVisible = 0;
            foreach (var segment in data)
                if (LineClipper.Clip(segment, window, method, out _, counts)) checkedVisible++;
            if (visible != checkedVisible) throw new InvalidOperationException("Результат отсечения изменился при подсчёте операций.");
            rows.Add(new(name, timing, counts.Multiply, counts.Divide, visible));
        }
        return rows;
    }

    public static BenchmarkResult Run(int count = Samples.MaxLines, IProgress<string>? progress = null,
        CancellationToken cancellation = default)
    {
        if (count < 1 || count > Samples.MaxLines) throw new ArgumentOutOfRangeException(nameof(count));
        var result = new BenchmarkResult();
        Box[] windows = Enumerable.Range(0, 8).Select(i =>
        {
            double x = Math.Sin(i * .7) * 500, y = Math.Cos(i * .6) * 500;
            return new Box(x - 120, y - 90, x + 120, y + 90);
        }).ToArray();
        foreach (string type in new[] { "dense", "short" })
        {
            cancellation.ThrowIfCancellationRequested();
            string name = type == "dense" ? "Плотные" : "Короткие";
            var data = Samples.Generate(count, type);
            var scan = Measure(() => Query(data, null, windows), result,
                new("Глубина", name, count, 0, "Без дерева", 0, 0, 0), progress, cancellation, windows.Length);
            foreach (int depth in new[] { 2, 4, 6, 8, 10 })
            {
                QuadTree? tree = null;
                var build = Measure(() => tree = new QuadTree(data, Samples.World, depth), result,
                    new("Глубина", name, count, depth, "Построение", 0, 0, 0), progress, cancellation);
                var clip = Measure(() => Query(data, tree, windows), result,
                    new("Глубина", name, count, depth, "Отсечение", 0, 0, 0), progress, cancellation, windows.Length);
                double candidates = Query(data, tree, windows);
                result.Rows.Add(new(name, count, depth, tree!.Nodes, build, clip, scan, candidates));
            }
            foreach (int n in new[] { .1, .2, .4, .7, 1 }.Select(f => Math.Max(1, (int)(count * f))).Distinct())
            {
                var subset = data.Take(n).ToArray();
                var tree = new QuadTree(subset, Samples.World, 6);
                var clip = Measure(() => Query(subset, tree, windows), result,
                    new("Плотность", name, n, 6, "Отсечение", 0, 0, 0), progress, cancellation, windows.Length);
                result.Density.Add(new(name, n, clip, Query(subset, tree, windows)));
            }
            if (type == "dense") result.Algorithms = Compare(data, new(-120, -90, 120, 90), result, progress, cancellation);
        }
        return result;
    }

    static double Query(Segment[] data, QuadTree? tree, Box[] windows)
    {
        double candidates = 0;
        foreach (var window in windows)
            candidates += LineClipper.ClipAll(data, tree, window, LineMethod.LiangBarsky).Candidates;
        return candidates / windows.Length;
    }

    static string Number(double value) => value.ToString("R", Russian);
    static string Stats(TimingStats value) => $"{Number(value.Median)};{Number(value.P10)};{Number(value.P90)}";
    static void Write(string path, string header, IEnumerable<string> rows) =>
        File.WriteAllText(path, string.Join("\r\n", new[] { header }.Concat(rows)) + "\r\n", CsvEncoding);

    public static void Save(BenchmarkResult result, string directory)
    {
        Directory.CreateDirectory(directory);
        Write(Path.Combine(directory, "benchmark.csv"),
            "Тип;Количество;Глубина;Узлы;Построение медиана мс;Построение P10 мс;Построение P90 мс;Отсечение медиана мс;Отсечение P10 мс;Отсечение P90 мс;Без дерева медиана мс;Без дерева P10 мс;Без дерева P90 мс;Кандидаты",
            result.Rows.Select(r => $"{r.Type};{r.Count};{r.Depth};{r.Nodes};{Stats(r.Build)};{Stats(r.Clip)};{Stats(r.Scan)};{Number(r.Candidates)}"));
        Write(Path.Combine(directory, "density.csv"), "Тип;Количество;Отсечение медиана мс;Отсечение P10 мс;Отсечение P90 мс;Кандидаты",
            result.Density.Select(r => $"{r.Type};{r.Count};{Stats(r.Clip)};{Number(r.Candidates)}"));
        Write(Path.Combine(directory, "algorithms.csv"), "Алгоритм;Время медиана мс;Время P10 мс;Время P90 мс;Умножения;Деления;Видимые",
            result.Algorithms.Select(r => $"{r.Algorithm};{Stats(r.Time)};{r.Multiply};{r.Divide};{r.Visible}"));
        Write(Path.Combine(directory, "samples.csv"), "Эксперимент;Тип;Количество;Глубина;Показатель;Серия;Повторов в пакете;Время на операцию мс",
            result.Samples.Select(r => $"{r.Experiment};{r.Type};{r.Count};{r.Depth};{r.Metric};{r.Round};{r.Iterations};{Number(r.Milliseconds)}"));
    }

    public static BenchmarkResult Load(string path)
    {
        string directory = Directory.Exists(path) ? Path.GetFullPath(path) : Path.GetDirectoryName(Path.GetFullPath(path))!;
        string? selected = File.Exists(path) ? Path.GetFullPath(path) : null;
        string? selectedName = null;
        if (selected != null)
        {
            string header = File.ReadLines(selected).FirstOrDefault() ?? "";
            selectedName = header.StartsWith("Эксперимент;") ? "samples.csv" :
                header.StartsWith("Алгоритм;") ? "algorithms.csv" :
                header.StartsWith("Тип;Количество;Глубина;") ? "benchmark.csv" :
                header.StartsWith("Тип;Количество;Отсечение") ? "density.csv" : null;
            if (selectedName == null) throw new FormatException("Неизвестный формат CSV бенчмарка.");
        }
        var result = new BenchmarkResult { SourceDirectory = directory };
        IEnumerable<string[]> Read(string name)
        {
            string file = name == selectedName ? selected! : Path.Combine(directory, name);
            if (!File.Exists(file)) return [];
            var lines = File.ReadAllLines(file);
            if (lines.Length == 0 || !lines[0].Contains(';'))
                throw new FormatException("Нужны CSV версии 1.5 с русскими заголовками и разделителем «;».");
            return lines.Skip(1).Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => l.Split(';'));
        }
        double Parse(string text)
        {
            double value = double.Parse(text, NumberStyles.Float, Russian);
            if (!double.IsFinite(value) || value < 0) throw new FormatException("В CSV найдено недопустимое число.");
            return value;
        }
        TimingStats ReadStats(string[] p, int index)
        {
            var value = new TimingStats(Parse(p[index]), Parse(p[index + 1]), Parse(p[index + 2]));
            if (value.P10 > value.Median || value.Median > value.P90) throw new FormatException("В CSV нарушен порядок P10 ≤ медиана ≤ P90.");
            return value;
        }
        foreach (var p in Read("benchmark.csv"))
            result.Rows.Add(new(p[0], int.Parse(p[1]), int.Parse(p[2]), int.Parse(p[3]), ReadStats(p, 4), ReadStats(p, 7), ReadStats(p, 10), Parse(p[13])));
        foreach (var p in Read("density.csv"))
            result.Density.Add(new(p[0], int.Parse(p[1]), ReadStats(p, 2), Parse(p[5])));
        foreach (var p in Read("algorithms.csv"))
            result.Algorithms.Add(new(p[0], ReadStats(p, 1), long.Parse(p[4]), long.Parse(p[5]), int.Parse(p[6])));
        foreach (var p in Read("samples.csv"))
            result.Samples.Add(new(p[0], p[1], int.Parse(p[2]), int.Parse(p[3]), p[4], int.Parse(p[5]), int.Parse(p[6]), Parse(p[7])));
        if (!result.HasResults) throw new FormatException("В CSV нет результатов.");
        return result;
    }
}
