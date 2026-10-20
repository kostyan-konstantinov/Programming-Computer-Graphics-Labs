using System.ComponentModel;

namespace RasterLab;

public sealed class BenchmarkChart : Control
{
    private readonly List<BenchmarkResult> results;

    [DefaultValue("Количество отрезков")]
    public string Experiment { get; set; } = "Количество отрезков";

    public BenchmarkChart(List<BenchmarkResult> results)
    {
        this.results = results;
        Dock = DockStyle.Fill;
        DoubleBuffered = true;
        BackColor = Color.White;
        SetStyle(ControlStyles.ResizeRedraw, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var data = new List<BenchmarkResult>();
        foreach (var result in results)
            if (result.Experiment == Experiment) data.Add(result);
        if (data.Count == 0)
        {
            e.Graphics.DrawString("Запустите бенчмарк", Font, Brushes.Black, 20, 20);
            return;
        }
        bool byCount = Experiment.StartsWith("Количество");
        double maxX = 1;
        double maxY = 1;
        var algorithms = new List<string>();
        foreach (var result in data)
        {
            maxX = Math.Max(maxX, byCount ? result.Count : result.Length);
            maxY = Math.Max(maxY, result.Milliseconds);
            if (!algorithms.Contains(result.Algorithm)) algorithms.Add(result.Algorithm);
        }
        maxY *= 1.1;
        var area = new Rectangle(80, 30, Math.Max(1, Width - 110), Math.Max(1, Height - 160));
        Graphics g = e.Graphics;
        using var gridPen = new Pen(Color.LightGray);
        for (int i = 0; i <= 5; i++)
        {
            float x = area.Left + area.Width * i / 5f;
            float y = area.Bottom - area.Height * i / 5f;
            g.DrawLine(gridPen, area.Left, y, area.Right, y);
            g.DrawString((maxY * i / 5).ToString("0.0"), Font, Brushes.Black, 5, y - 7);
            g.DrawString((maxX * i / 5).ToString("0"), Font, Brushes.Black, x - 10, area.Bottom + 5);
        }
        g.DrawLine(Pens.Black, area.Left, area.Top, area.Left, area.Bottom);
        g.DrawLine(Pens.Black, area.Left, area.Bottom, area.Right, area.Bottom);
        g.DrawString("Время, мс", Font, Brushes.Black, 5, 5);
        string xLabel = byCount ? "Количество примитивов"
            : (Experiment == "Радиус окружностей" ? "Максимальный радиус" : "Максимальная длина");
        g.DrawString(xLabel, Font, Brushes.Black, area.Left, area.Bottom + 28);

        Color[] colors = [Color.Blue, Color.DarkGreen, Color.Red, Color.DarkOrange, Color.Purple];
        int index = 0;
        foreach (string algorithm in algorithms)
        {
            Color color = colors[index % colors.Length];
            using var pen = new Pen(color, 2);
            using var brush = new SolidBrush(color);
            PointF previous = new();
            bool hasPrevious = false;
            foreach (var row in data)
            {
                if (row.Algorithm != algorithm) continue;
                var point = new PointF(
                    area.Left + (float)((byCount ? row.Count : row.Length) / maxX * area.Width),
                    area.Bottom - (float)(row.Milliseconds / maxY * area.Height));
                if (hasPrevious) g.DrawLine(pen, previous, point);
                g.FillRectangle(brush, point.X - 2, point.Y - 2, 4, 4);
                previous = point;
                hasPrevious = true;
            }
            int legendX = area.Left + index % 3 * Math.Max(100, area.Width / 3);
            int legendY = area.Bottom + 55 + index / 3 * 22;
            g.DrawString(algorithm, Font, brush, legendX, legendY);
            index++;
        }
    }
}
