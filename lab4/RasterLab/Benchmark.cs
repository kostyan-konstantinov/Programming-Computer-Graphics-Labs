using System.Diagnostics;

namespace RasterLab;

public sealed class Benchmark
{
    public volatile bool StopRequested;

    private readonly VirtualGrid grid = new(300);
    private readonly List<BenchmarkResult> results = new();

    private readonly string[] names =
    [
        "Пошаговый", "ЦДА", "Брезенхем", "Ву", "Кастла-Питвея"
    ];

    public List<BenchmarkResult> Run()
    {
        int[] counts = [1000, 10000, 25000, 50000, 100000];
        int[] lengths = [16, 32, 64, 128, 256];
        int[] circleCounts = [1000, 5000, 10000, 25000, 50000];

        foreach (int count in counts)
        {
            TestLines("Количество отрезков", count, 128);
            if (StopRequested) return results;
        }

        foreach (int length in lengths)
        {
            TestLines("Длина отрезков", 100000, length);
            if (StopRequested) return results;
        }

        foreach (int count in circleCounts)
        {
            TestCircles("Количество окружностей", count, 64);
            if (StopRequested) return results;
        }

        foreach (int radius in lengths)
        {
            TestCircles("Радиус окружностей", 50000, radius);
            if (StopRequested) return results;
        }

        return results;
    }

    private void TestLines(string experiment, int count, int length)
    {
        var random = new Random(42);
        var endpoints = new (int X, int Y)[count];

        for (int i = 0; i < count; i++)
        {
            if (i % 1024 == 0 && StopRequested) return;

            int major = random.Next(1, length + 1);
            int minor = random.Next(0, major + 1);

            endpoints[i] = (i % 8) switch
            {
                0 => (major, minor),
                1 => (minor, major),
                2 => (-minor, major),
                3 => (-major, minor),
                4 => (-major, -minor),
                5 => (-minor, -major),
                6 => (minor, -major),
                _ => (major, -minor)
            };
        }

        for (int algorithm = 0; algorithm < names.Length; algorithm++)
        {
            double time = MeasureLines(algorithm, endpoints);
            if (StopRequested) return;

            results.Add(new BenchmarkResult(
                experiment, names[algorithm], count, length, time));
        }
    }

    private double MeasureLines(int algorithm, (int X, int Y)[] endpoints)
    {
        DrawLines(algorithm, endpoints);
        double[] times = new double[3];

        for (int i = 0; i < times.Length; i++)
        {
            if (StopRequested) return 0;

            grid.Clear();
            var timer = Stopwatch.StartNew();

            DrawLines(algorithm, endpoints);

            timer.Stop();
            if (StopRequested) return 0;

            times[i] = timer.Elapsed.TotalMilliseconds;
        }

        Array.Sort(times);
        return times[1];
    }

    private void DrawLines(int algorithm, (int X, int Y)[] endpoints)
    {
        for (int i = 0; i < endpoints.Length; i++)
        {
            if (i % 1024 == 0 && StopRequested) return;

            int x = endpoints[i].X;
            int y = endpoints[i].Y;

            switch (algorithm)
            {
                case 0:
                    RasterEngine.StepLine(0, 0, x, y, grid);
                    break;
                case 1:
                    RasterEngine.DDALine(0, 0, x, y, grid);
                    break;
                case 2:
                    RasterEngine.BresenhamLine(0, 0, x, y, grid);
                    break;
                case 3:
                    RasterEngine.WuLine(0, 0, x, y, grid);
                    break;
                case 4:
                    RasterEngine.CastlePitwayLine(0, 0, x, y, grid);
                    break;
            }
        }
    }

    private void TestCircles(string experiment, int count, int maxRadius)
    {
        var random = new Random(42);
        int[] radii = new int[count];

        for (int i = 0; i < count; i++)
        {
            if (i % 1024 == 0 && StopRequested) return;
            radii[i] = random.Next(1, maxRadius + 1);
        }

        double time = MeasureCircles(radii);
        if (StopRequested) return;

        results.Add(new BenchmarkResult(
            experiment, "Брезенхем - окружность", count, maxRadius, time));
    }

    private double MeasureCircles(int[] radii)
    {
        DrawCircles(radii);
        double[] times = new double[3];

        for (int i = 0; i < times.Length; i++)
        {
            if (StopRequested) return 0;

            grid.Clear();
            var timer = Stopwatch.StartNew();

            DrawCircles(radii);

            timer.Stop();
            if (StopRequested) return 0;

            times[i] = timer.Elapsed.TotalMilliseconds;
        }

        Array.Sort(times);
        return times[1];
    }

    private void DrawCircles(int[] radii)
    {
        for (int i = 0; i < radii.Length; i++)
        {
            if (i % 1024 == 0 && StopRequested) return;
            RasterEngine.BresenhamCircle(0, 0, radii[i], grid);
        }
    }
}