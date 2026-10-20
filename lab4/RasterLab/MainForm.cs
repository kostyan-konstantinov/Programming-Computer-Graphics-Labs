using System.Globalization;
using System.Text;

namespace RasterLab;

public sealed class MainForm : Form
{
    private readonly VirtualGrid grid = new(512);
    private readonly GridPainter painter;

    private readonly ComboBox algorithm = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = 240
    };

    private readonly NumericUpDown x1 = Number(-8);
    private readonly NumericUpDown y1 = Number(-4);
    private readonly NumericUpDown x2 = Number(12);
    private readonly NumericUpDown y2 = Number(7);
    private readonly NumericUpDown radius = Number(8, 0, 250);
    private readonly NumericUpDown secondRadius = Number(5, 0, 250);
    private readonly NumericUpDown startAngle = Number(0, 0, 360);
    private readonly NumericUpDown endAngle = Number(360, 0, 360);
    private readonly NumericUpDown zoom = Number(20, 2, 40);

    private readonly Label xLabel = TextLabel("x1");
    private readonly Label yLabel = TextLabel("y1");
    private readonly Label radiusLabel = TextLabel("r / a");

    private readonly Label rasterStatus = new()
    {
        Dock = DockStyle.Bottom,
        Height = 30,
        Padding = new Padding(6)
    };

    private readonly DataGridView pixelsTable = Table();
    private readonly List<(int X, int Y, double Intensity)> pixels = new();

    private bool dirty = true;
    private int shown;

    private readonly List<BenchmarkResult> results = new();
    private readonly BenchmarkChart chart;
    private readonly DataGridView benchmarkTable = Table();

    private readonly ComboBox experiment = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = 235
    };

    private readonly Button runBenchmark = new()
    {
        Text = "Запустить",
        AutoSize = true
    };

    private readonly Button stopBenchmark = new()
    {
        Text = "Стоп",
        AutoSize = true,
        Enabled = false
    };

    private readonly Button exportBenchmark = new()
    {
        Text = "Сохранить CSV",
        AutoSize = true
    };

    private readonly Label benchmarkStatus = new()
    {
        Dock = DockStyle.Bottom,
        Height = 30,
        Padding = new Padding(6)
    };

    private Benchmark? benchmark;

    public MainForm()
    {
        Text = "ЛР 4 - Растровые алгоритмы";
        Width = 1180;
        Height = 800;
        MinimumSize = new Size(1000, 700);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9);

        painter = new GridPainter(grid);
        chart = new BenchmarkChart(results);

        var tabs = new TabControl { Dock = DockStyle.Fill };
        var rasterTab = new TabPage("Построение");
        var benchmarkTab = new TabPage("Бенчмарк");

        tabs.TabPages.Add(rasterTab);
        tabs.TabPages.Add(benchmarkTab);
        Controls.Add(tabs);

        BuildRasterTab(rasterTab);
        BuildBenchmarkTab(benchmarkTab);

        Shown += (_, _) => DrawAll();
        FormClosing += (_, _) => StopBenchmark();
    }

    private void BuildRasterTab(TabPage tab)
    {
        algorithm.Items.AddRange([
            "Пошаговый",
            "ЦДА",
            "Брезенхем - отрезок",
            "Брезенхем - окружность",
            "Ву",
            "Кастла-Питвея - отрезок",
            "Средняя точка - эллипс"
        ]);

        var firstRow = Row();
        firstRow.Controls.Add(TextLabel("Алгоритм"));
        firstRow.Controls.Add(algorithm);
        firstRow.Controls.Add(TextLabel("Масштаб"));
        firstRow.Controls.Add(zoom);
        AddButton(firstRow, "По размеру", Fit);

        var secondRow = Row();
        AddNumber(secondRow, xLabel, x1);
        AddNumber(secondRow, yLabel, y1);
        AddNumber(secondRow, TextLabel("x2"), x2);
        AddNumber(secondRow, TextLabel("y2"), y2);
        AddNumber(secondRow, radiusLabel, radius);
        AddNumber(secondRow, TextLabel("b"), secondRadius);

        var thirdRow = Row();
        AddButton(thirdRow, "Построить", DrawAll);
        AddButton(thirdRow, "В начало", ResetSteps);
        AddButton(thirdRow, "Шаг", Step);

        var fourthRow = Row();
        AddNumber(fourthRow, TextLabel("Начальный угол, °"), startAngle);
        AddNumber(fourthRow, TextLabel("Конечный угол, °"), endAngle);

        var inputPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 164,
            RowCount = 4,
            ColumnCount = 1
        };

        for (int i = 0; i < 4; i++)
            inputPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 41));

        inputPanel.Controls.Add(firstRow, 0, 0);
        inputPanel.Controls.Add(secondRow, 0, 1);
        inputPanel.Controls.Add(thirdRow, 0, 2);
        inputPanel.Controls.Add(fourthRow, 0, 3);

        pixelsTable.Columns.Add("step", "Запись");
        pixelsTable.Columns.Add("x", "x");
        pixelsTable.Columns.Add("y", "y");
        pixelsTable.Columns.Add("alpha", "α");

        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };

        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 235));
        body.Controls.Add(painter, 0, 0);
        body.Controls.Add(pixelsTable, 1, 0);

        tab.Controls.Add(body);
        tab.Controls.Add(rasterStatus);
        tab.Controls.Add(inputPanel);

        algorithm.SelectedIndexChanged += (_, _) =>
        {
            bool circle = algorithm.SelectedIndex == 3;
            bool ellipse = algorithm.SelectedIndex == 6;

            xLabel.Text = circle || ellipse ? "cx" : "x1";
            yLabel.Text = circle || ellipse ? "cy" : "y1";
            radiusLabel.Text = ellipse ? "a" : "r";

            x2.Enabled = y2.Enabled = !circle && !ellipse;
            radius.Enabled = circle || ellipse;
            secondRadius.Enabled = ellipse;
            startAngle.Enabled = endAngle.Enabled = circle || ellipse;

            dirty = true;
        };

        foreach (var number in new[]
        {
            x1, y1, x2, y2, radius, secondRadius, startAngle, endAngle
        })
        {
            number.ValueChanged += (_, _) => dirty = true;
        }

        zoom.ValueChanged += (_, _) =>
        {
            if (painter.CellSize != (int)zoom.Value)
                painter.CellSize = (int)zoom.Value;
        };

        painter.ZoomChanged += (_, _) => zoom.Value = painter.CellSize;
        algorithm.SelectedIndex = 2;
    }

    private void Prepare()
    {
        if (!dirty) return;

        pixels.Clear();
        var target = new PixelCollector(pixels);

        int ax = (int)x1.Value;
        int ay = (int)y1.Value;
        int bx = (int)x2.Value;
        int by = (int)y2.Value;

        switch (algorithm.SelectedIndex)
        {
            case 0:
                RasterEngine.StepLine(ax, ay, bx, by, target);
                break;

            case 1:
                RasterEngine.DDALine(ax, ay, bx, by, target);
                break;

            case 2:
                RasterEngine.BresenhamLine(ax, ay, bx, by, target);
                break;

            case 3:
                RasterEngine.BresenhamCircleArc(
                    ax, ay,
                    (int)radius.Value,
                    (int)startAngle.Value,
                    (int)endAngle.Value,
                    target);
                break;

            case 4:
                RasterEngine.WuLine(ax, ay, bx, by, target);
                break;

            case 5:
                RasterEngine.CastlePitwayLine(ax, ay, bx, by, target);
                break;

            case 6:
                RasterEngine.MidpointEllipseArc(
                    ax, ay,
                    (int)radius.Value,
                    (int)secondRadius.Value,
                    (int)startAngle.Value,
                    (int)endAngle.Value,
                    target);
                break;
        }

        grid.Clear();
        shown = 0;
        pixelsTable.Rows.Clear();

        foreach (var pixel in pixels)
        {
            pixelsTable.Rows.Add(
                pixelsTable.Rows.Count + 1,
                pixel.X,
                pixel.Y,
                pixel.Intensity.ToString("0.###"));
        }

        dirty = false;
    }

    private void DrawAll()
    {
        Prepare();

        while (shown < pixels.Count)
            ApplyPixel();

        painter.Invalidate();
        rasterStatus.Text = $"Построено: {pixels.Count} записей пикселей";
    }

    private void ResetSteps()
    {
        Prepare();
        grid.Clear();
        shown = 0;

        pixelsTable.ClearSelection();
        painter.Invalidate();
        rasterStatus.Text = $"Записей: 0 / {pixels.Count}";
    }

    private void Step()
    {
        Prepare();

        if (shown >= pixels.Count)
        {
            rasterStatus.Text = "Построение завершено; кнопка «В начало» начинает заново";
            return;
        }

        var pixel = pixels[shown];
        ApplyPixel();

        pixelsTable.ClearSelection();
        pixelsTable.Rows[shown - 1].Selected = true;
        pixelsTable.FirstDisplayedScrollingRowIndex = shown - 1;

        painter.Invalidate();
        rasterStatus.Text =
            $"Запись {shown} / {pixels.Count}: x = {pixel.X}, y = {pixel.Y}, α = {pixel.Intensity:0.###}";
    }

    private void ApplyPixel()
    {
        var pixel = pixels[shown++];
        grid.SetPixel(pixel.X, pixel.Y, pixel.Intensity);
    }

    private void Fit()
    {
        Prepare();
        if (pixels.Count == 0) return;

        int minX = pixels[0].X;
        int maxX = pixels[0].X;
        int minY = pixels[0].Y;
        int maxY = pixels[0].Y;

        foreach (var pixel in pixels)
        {
            minX = Math.Min(minX, pixel.X);
            maxX = Math.Max(maxX, pixel.X);
            minY = Math.Min(minY, pixel.Y);
            maxY = Math.Max(maxY, pixel.Y);
        }

        painter.Fit(minX, minY, maxX, maxY);
    }

    private void BuildBenchmarkTab(TabPage tab)
    {
        var toolbar = Row();
        toolbar.Dock = DockStyle.Top;
        toolbar.Height = 42;

        toolbar.Controls.Add(runBenchmark);
        toolbar.Controls.Add(stopBenchmark);
        toolbar.Controls.Add(exportBenchmark);

        experiment.Items.AddRange([
            "Количество отрезков",
            "Длина отрезков",
            "Количество окружностей",
            "Радиус окружностей"
        ]);

        toolbar.Controls.Add(experiment);
        experiment.SelectedIndex = 0;

        foreach (var title in new[]
        {
            "Опыт", "Алгоритм", "Количество", "Длина / радиус", "Время, мс"
        })
        {
            benchmarkTable.Columns.Add(title, title);
        }

        benchmarkTable.Height = 230;
        benchmarkTable.Dock = DockStyle.Bottom;

        tab.Controls.Add(chart);
        tab.Controls.Add(benchmarkTable);
        tab.Controls.Add(benchmarkStatus);
        tab.Controls.Add(toolbar);

        experiment.SelectedIndexChanged += (_, _) =>
        {
            chart.Experiment = experiment.Text;
            chart.Invalidate();
        };

        runBenchmark.Click += async (_, _) => await RunBenchmark();
        stopBenchmark.Click += (_, _) => StopBenchmark();
        exportBenchmark.Click += (_, _) => SaveCsv();
    }

    private async Task RunBenchmark()
    {
        runBenchmark.Enabled = false;
        stopBenchmark.Enabled = true;
        exportBenchmark.Enabled = false;

        results.Clear();
        benchmarkTable.Rows.Clear();
        chart.Invalidate();
        benchmarkStatus.Text = "Бенчмарк выполняется";

        var job = new Benchmark();
        benchmark = job;

        try
        {
            var measured = await Task.Run(job.Run);
            if (IsDisposed || Disposing) return;

            results.AddRange(measured);

            foreach (var result in results)
            {
                benchmarkTable.Rows.Add(
                    result.Experiment,
                    result.Algorithm,
                    result.Count,
                    result.Length,
                    result.Milliseconds.ToString("0.000"));
            }

            chart.Invalidate();

            benchmarkStatus.Text = job.StopRequested
                ? $"Остановлено; завершённых измерений: {results.Count}"
                : "Бенчмарк завершён; результаты можно сохранить в CSV";
        }
        catch (Exception error)
        {
            if (!IsDisposed)
                benchmarkStatus.Text = "Ошибка бенчмарка: " + error.Message;
        }
        finally
        {
            benchmark = null;

            if (!IsDisposed && !Disposing)
            {
                runBenchmark.Enabled = true;
                stopBenchmark.Enabled = false;
                exportBenchmark.Enabled = true;
            }
        }
    }

    private void StopBenchmark()
    {
        if (benchmark == null) return;

        benchmark.StopRequested = true;
        stopBenchmark.Enabled = false;
        benchmarkStatus.Text = "Остановка бенчмарка";
    }

    private void SaveCsv()
    {
        if (results.Count == 0)
        {
            benchmarkStatus.Text = "Сначала запустите бенчмарк";
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Filter = "CSV|*.csv",
            FileName = "benchmark.csv"
        };

        if (dialog.ShowDialog() != DialogResult.OK) return;

        var text = new StringBuilder(
            "experiment;algorithm;count;max_length_or_radius;milliseconds\r\n");

        foreach (var row in results)
        {
            text.AppendLine(
                $"{row.Experiment};{row.Algorithm};{row.Count};{row.Length};"
                + row.Milliseconds.ToString("0.000", CultureInfo.InvariantCulture));
        }

        try
        {
            File.WriteAllText(dialog.FileName, text.ToString(), new UTF8Encoding(true));
            benchmarkStatus.Text = "CSV сохранён";
        }
        catch (Exception error)
        {
            benchmarkStatus.Text = "Ошибка сохранения: " + error.Message;
        }
    }

    private static NumericUpDown Number(int value, int minimum = -250, int maximum = 250)
    {
        return new NumericUpDown
        {
            Minimum = minimum,
            Maximum = maximum,
            Value = value,
            Width = 72
        };
    }

    private static Label TextLabel(string text)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            Margin = new Padding(5, 8, 3, 0)
        };
    }

    private static FlowLayoutPanel Row()
    {
        return new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(4),
            WrapContents = false
        };
    }

    private static DataGridView Table()
    {
        return new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.White,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false
        };
    }

    private static void AddNumber(FlowLayoutPanel row, Label label, NumericUpDown number)
    {
        row.Controls.Add(label);
        row.Controls.Add(number);
    }

    private static void AddButton(FlowLayoutPanel row, string text, Action action)
    {
        var button = new Button { Text = text, AutoSize = true };
        button.Click += (_, _) => action();
        row.Controls.Add(button);
    }

    private sealed class PixelCollector : IRasterTarget
    {
        private readonly List<(int X, int Y, double Intensity)> pixels;

        public PixelCollector(List<(int X, int Y, double Intensity)> pixels)
        {
            this.pixels = pixels;
        }

        public void SetPixel(int x, int y, double intensity)
        {
            pixels.Add((x, y, intensity));
        }
    }
}