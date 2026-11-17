using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Lab5.Core;
using Microsoft.Win32;

namespace Lab5.App;

public partial class BenchmarkWindow : Window
{
    public BenchmarkResult Results { get; private set; } = new();
    readonly string reportDirectory;
    CancellationTokenSource? cancellation;

    public BenchmarkWindow(string directory, int count, BenchmarkResult? previous = null)
    {
        InitializeComponent();
        reportDirectory = directory;
        Count.Text = count.ToString();
        if (previous?.HasResults == true) { Results = previous; ShowResults(); }
        else { State.Text = "Нажмите «Запустить» для новых измерений или откройте сохранённые CSV."; SaveButton.IsEnabled = false; }
        Closed += (_, _) => cancellation?.Cancel();
    }
    async void Run_Click(object sender, RoutedEventArgs e)
    {
        if (cancellation != null) { cancellation.Cancel(); return; }
        if (!int.TryParse(Count.Text, out int count) || count < 1 || count > Samples.MaxLines)
        { State.Text = "Количество: от 1 до 500 000."; return; }
        cancellation = new CancellationTokenSource();
        var token = cancellation.Token;
        OpenButton.IsEnabled = SaveButton.IsEnabled = Count.IsEnabled = false;
        RunButton.Content = "Остановить";
        State.Text = "Прогрев и измерения…";
        var progress = new Progress<string>(text => State.Text = text);
        try
        {
            Results = await Task.Run(() => Benchmark.Run(count, progress, token));
            ShowResults();
        }
        catch (OperationCanceledException) { State.Text = "Измерение остановлено. Предыдущие результаты сохранены."; }
        catch (Exception ex) { State.Text = ex.Message; }
        finally
        {
            cancellation.Dispose(); cancellation = null;
            RunButton.Content = "Запустить";
            OpenButton.IsEnabled = Count.IsEnabled = true;
            SaveButton.IsEnabled = Results.HasResults;
        }
    }
    void ShowResults()
    {
        ResultTabs.SelectedIndex = Results.Rows.Count > 0 ? 0 : Results.Density.Count > 0 ? 2 : Results.Algorithms.Count > 0 ? 4 : 5;
        Table.ItemsSource = Results.Rows;
        Algorithms.ItemsSource = Results.Algorithms;
        SamplesTable.ItemsSource = Results.Samples;
        Source.Text = Results.SourceDirectory.Length > 0
            ? "Сохранённые CSV: " + Results.SourceDirectory
            : "Результаты запуска на этом компьютере в текущем сеансе.";
        Plot(DepthPlot, Results.Rows.Select(r => (r.Type, (double)r.Depth, r.Clip)), "Глубина дерева");
        Plot(BuildPlot, Results.Rows.Select(r => (r.Type, (double)r.Depth, r.Build)), "Глубина дерева");
        Plot(DensityPlot, Results.Density.Select(r => (r.Type, (double)r.Count, r.Clip)), "Количество отрезков в мире 2000 × 2000");
        if (Results.Rows.Count > 0) Count.Text = Results.Rows[0].Count.ToString();
        State.Text = "Измерения завершены. 15 серий, время в миллисекундах.";
        SaveButton.IsEnabled = Results.HasResults;
    }
    void Open_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Title = "Открыть результаты бенчмарка", Filter = "CSV (*.csv)|*.csv", FileName = "benchmark.csv" };
        if (Directory.Exists(reportDirectory)) dialog.InitialDirectory = reportDirectory;
        if (dialog.ShowDialog(this) != true) return;
        try { var loaded = Benchmark.Load(dialog.FileName); Results = loaded; ShowResults(); }
        catch (Exception ex) { State.Text = ex.Message; }
    }
    void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!Results.HasResults) { State.Text = "Сначала выполните замеры."; return; }
        var dialog = new OpenFolderDialog { Title = "Папка для CSV" };
        if (dialog.ShowDialog(this) != true) return;
        try { Benchmark.Save(Results, dialog.FolderName); State.Text = "Результаты сохранены."; }
        catch (Exception ex) { State.Text = ex.Message; }
    }
    static void Plot(Canvas canvas, IEnumerable<(string Type, double X, TimingStats Y)> source, string xTitle)
    {
        canvas.Children.Clear(); var data = source.ToArray();
        if (data.Length == 0) return;
        double maxX = Math.Max(1, data.Max(p => p.X)), maxY = Math.Max(.001, data.Max(p => p.Y.P90) * 1.1);
        void Text(string text, double x, double y, Brush? brush = null)
        {
            var label = new TextBlock { Text = text, FontSize = 12, Foreground = brush ?? Brushes.Black };
            Canvas.SetLeft(label, x); Canvas.SetTop(label, y); canvas.Children.Add(label);
        }
        void Line(double x1, double y1, double x2, double y2, Brush brush)
            => canvas.Children.Add(new Line { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, Stroke = brush });
        for (int i = 0; i <= 5; i++)
        {
            double x = 75 + i * 140, y = 370 - i * 64;
            Line(75, y, 775, y, Brushes.LightGray);
            Text((maxY * i / 5).ToString("0.###"), 5, y - 8);
            Text((maxX * i / 5).ToString("0"), x - 12, 380);
        }
        int k = 0;
        foreach (var group in data.GroupBy(p => p.Type))
        {
            Brush brush = k++ == 0 ? Brushes.RoyalBlue : Brushes.OrangeRed;
            var line = new Polyline { Stroke = brush, StrokeThickness = 2 };
            foreach (var p in group.OrderBy(p => p.X))
            {
                double x = 75 + p.X / maxX * 700, y = 370 - p.Y.Median / maxY * 320;
                double low = 370 - p.Y.P10 / maxY * 320, high = 370 - p.Y.P90 / maxY * 320;
                Line(x, low, x, high, brush); Line(x - 4, low, x + 4, low, brush); Line(x - 4, high, x + 4, high, brush);
                line.Points.Add(new Point(x, y));
                var point = new Ellipse { Width = 6, Height = 6, Fill = brush };
                Canvas.SetLeft(point, x - 3); Canvas.SetTop(point, y - 3); canvas.Children.Add(point);
            }
            canvas.Children.Add(line); Text(group.Key, 75 + (k - 1) * 160, 435, brush);
        }
        Text("Время, мс", 5, 20); Text(xTitle, 180, 407);
    }
}
