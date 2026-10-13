using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Lab3Variant9
{
    public sealed class Graph : Control
    {
        public double[] Values;
        public double Marker = double.NaN;
        public double Maximum;
        public string Caption = "";
        public string EmptyText = "Сначала обработайте изображение.";
        public bool Histogram = true;
        private readonly ToolTip tooltip = new ToolTip();
        private Rectangle plot;
        private int hovered = -1;
        private bool binaryHistogram;
        public Graph() { DoubleBuffered = true; BackColor = Color.White; Dock = DockStyle.Fill; MinimumSize = new Size(230, 130); }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.DrawString(Caption, Font, Brushes.Black, 8, 5);
            if (Values == null)
            {
                plot = Rectangle.Empty;
                g.DrawString(EmptyText, Font, Brushes.Gray, new RectangleF(12, 38, Math.Max(10, Width - 24), Math.Max(10, Height - 45)));
                return;
            }
            double maximum = Maximum;
            if (maximum <= 0) for (int i = 0; i < Values.Length; i++) maximum = Math.Max(maximum, Values[i]);
            if (maximum <= 0) maximum = 1;
            int left = Math.Max(48, (int)Math.Ceiling(g.MeasureString(AxisValue(maximum), Font).Width) + 12);
            int top = 35, w = Math.Max(10, Width - left - 18), h = Math.Max(10, Height - top - 40);
            plot = new Rectangle(left, top, w, h);
            bool binary = Histogram && Values.Length == 256;
            if (binary) for (int i = 1; i < 255; i++) if (Values[i] != 0) { binary = false; break; }
            binaryHistogram = binary;
            for (int tick = 0; tick <= 2; tick++)
            {
                float y = top + h - tick * h / 2f;
                g.DrawLine(Pens.Gainsboro, left, y, left + w, y);
                string label = AxisValue(maximum * tick / 2);
                g.DrawString(label, Font, Brushes.DimGray, left - g.MeasureString(label, Font).Width - 5, y - Font.Height / 2f);
            }
            using (Pen pen = new Pen(Color.SteelBlue, Math.Max(1f, w / 256f)))
                for (int i = 0; i < Values.Length; i++)
                {
                    float x = left + i * w / 255f;
                    float height = (float)(Values[i] / maximum * h);
                    if (binary && Values[i] > 0)
                        g.FillRectangle(Brushes.SteelBlue, i == 0 ? left : left + w - 12, top + h - height, 12, height);
                    else if (!binary) g.DrawLine(pen, x, top + h, x, top + h - height);
                }
            g.DrawLine(Pens.Gray, left - 2, top, left - 2, top + h);
            g.DrawLine(Pens.Gray, left, top + h, left + w, top + h);
            if (!double.IsNaN(Marker) && Marker >= 0 && Marker <= 255)
            {
                float x = left + (float)(Marker * w / 255);
                g.DrawLine(Pens.Firebrick, x, top, x, top + h);
                string label = "T = " + Marker.ToString("0.##", CultureInfo.CurrentCulture);
                g.DrawString(label, Font, Brushes.Firebrick, Math.Min(x + 3, Width - g.MeasureString(label, Font).Width - 5), top + 2);
            }
            foreach (int level in new int[] { 0, 64, 128, 192, 255 })
            {
                string label = level.ToString();
                float x = left + level * w / 255f;
                g.DrawString(label, Font, Brushes.Black, Math.Max(left - 3, Math.Min(x - g.MeasureString(label, Font).Width / 2, Width - 28)), top + h + 4);
            }
            string axis = Histogram ? "Яркость: 0 — чёрный, 255 — белый" : "Порог яркости";
            if (binary)
            {
                double total = Values[0] + Values[255];
                axis = "Чёрных: " + (total == 0 ? 0 : Values[0] * 100 / total).ToString("0.#") + "%; белых: " +
                    (total == 0 ? 0 : Values[255] * 100 / total).ToString("0.#") + "%";
            }
            g.DrawString(axis, Font, Brushes.DimGray, left, Height - Font.Height - 3);
        }
        private string AxisValue(double value) { return value.ToString(Histogram ? "N0" : "0.##", CultureInfo.GetCultureInfo("ru-RU")); }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (Values == null || !plot.Contains(e.Location)) { tooltip.SetToolTip(this, ""); hovered = -1; return; }
            int level = Math.Max(0, Math.Min(255, (int)Math.Round((e.X - plot.Left) * 255.0 / plot.Width)));
            if (binaryHistogram && e.X < plot.Left + 12) level = 0;
            else if (binaryHistogram && e.X >= plot.Right - 12) level = 255;
            if (level == hovered) return;
            hovered = level;
            double total = 0; if (Histogram) foreach (double value in Values) total += value;
            tooltip.SetToolTip(this, Histogram ? "Яркость " + level + ": " + AxisValue(Values[level]) + " пикселей (" +
                (total == 0 ? 0 : Values[level] * 100 / total).ToString("0.##") + "%)" : "Порог " + level + ": дисперсия " + AxisValue(Values[level]));
        }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); tooltip.SetToolTip(this, ""); hovered = -1; }
        protected override void Dispose(bool disposing) { if (disposing) tooltip.Dispose(); base.Dispose(disposing); }
    }

    public sealed class Parameter : FlowLayoutPanel
    {
        public NumericUpDown Number;
        private readonly TrackBar slider;
        private bool syncing;
        private readonly decimal factor;
        public Parameter(string caption, decimal minimum, decimal maximum, decimal value, int decimals, decimal? sliderMaximum = null)
        {
            AutoSize = true; AutoSizeMode = AutoSizeMode.GrowAndShrink; Width = 280;
            FlowDirection = FlowDirection.LeftToRight; WrapContents = true; Margin = new Padding(0, 3, 0, 3);
            factor = decimals == 0 ? 1 : decimals == 1 ? 10 : 100;
            decimal trackMaximum = Math.Max(minimum, Math.Min(maximum, sliderMaximum ?? maximum));
            Label captionLabel = new Label { Text = caption, Width = 265, AutoSize = true, MaximumSize = new Size(265, 0), Margin = new Padding(0, 4, 0, 0) };
            Controls.Add(captionLabel);
            SetFlowBreak(captionLabel, true);
            slider = new TrackBar { Minimum = (int)(minimum * factor), Maximum = (int)(trackMaximum * factor),
                Value = (int)(Math.Min(value, trackMaximum) * factor), TickStyle = TickStyle.None, Width = 185, Height = 30, AutoSize = false, Margin = new Padding(0) };
            Number = new NumericUpDown { Minimum = minimum, Maximum = maximum, Value = value, DecimalPlaces = decimals,
                Increment = 1 / factor, Width = 76, Margin = new Padding(0), ThousandsSeparator = false };
            slider.ValueChanged += (s, e) => { if (!syncing) { syncing = true; Number.Value = slider.Value / factor; syncing = false; } };
            Number.ValueChanged += (s, e) => { if (!syncing) { syncing = true; slider.Value = Math.Min(slider.Maximum, (int)(Number.Value * factor)); syncing = false; } };
            Controls.Add(slider); Controls.Add(Number);
        }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public double Value { get { return (double)Number.Value; } }
    }

    public sealed class MainForm : Form
    {
        private readonly ComboBox methods = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 265 };
        private readonly ComboBox border = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 265 };
        private readonly ComboBox lines = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 265 };
        private readonly CheckBox parallel = new CheckBox { Text = "Несколько потоков", Checked = true, AutoSize = true };
        private readonly Parameter radius = new Parameter("Радиус окна (размер 2r + 1)", 1, 100, 15, 0);
        private readonly Parameter k = new Parameter("Коэффициент k Сауволы", 0, 1, 0.2m, 2);
        private readonly Parameter sigma = new Parameter("Сигма Гаусса", 0.3m, 5, 1.2m, 1);
        private readonly Parameter threshold = new Parameter("Порог отклика", 0, 2040, 100, 0);
        private readonly Parameter low = new Parameter("Нижний порог Кэнни", 1, 1443, 35, 0, 300);
        private readonly Parameter high = new Parameter("Верхний порог Кэнни", 1, 1443, 90, 0, 300);
        private readonly Button open = new Button { Text = "Открыть изображение", AutoSize = true };
        private readonly Button samples = new Button { Text = "Тестовые изображения", AutoSize = true };
        private readonly Button run = new Button { Text = "Обработать", Width = 130, Height = 32 };
        private readonly Button cancel = new Button { Text = "Стоп", Enabled = false, Width = 90, Height = 32 };
        private readonly Button save = new Button { Text = "Сохранить PNG", AutoSize = true, Enabled = false };
        private readonly PictureBox before = Picture(), after = Picture(), direction = Picture();
        private readonly Graph histBefore = new Graph { Caption = "До: количество пикселей по яркости", EmptyText = "Откройте изображение." };
        private readonly Graph histAfter = new Graph { Caption = "После: количество пикселей по яркости" };
        private readonly Graph variance = new Graph { Caption = "Оцу: межклассовая дисперсия; максимум определяет порог", Histogram = false, MinimumSize = new Size(230, 100) };
        private readonly Label status = new Label { AutoSize = false, Dock = DockStyle.Fill, Text = "Откройте изображение или выберите пример.", Padding = new Padding(6) };
        private readonly Label info = new Label { Width = 265, AutoSize = true, MaximumSize = new Size(265, 0) };
        private readonly ProgressBar progress = new ProgressBar { Dock = DockStyle.Fill, Maximum = 100 };
        private readonly FlowLayoutPanel settingsPanel = new FlowLayoutPanel();
        private readonly TabControl resultTabs = new TabControl { Dock = DockStyle.Fill };
        private readonly TabPage imageTab = new TabPage("Результат"), directionTab = new TabPage("Углы градиента");
        private readonly TableLayoutPanel visual = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3 };
        private readonly Label borderLabel = new Label { Text = "Обработка краёв", AutoSize = true, Margin = new Padding(0, 8, 0, 3) };
        private readonly Label scaleNote = new Label { Text = "Независимые шкалы. X — яркость; Y — число пикселей. Значения — при наведении.", Dock = DockStyle.Fill, AutoSize = false };
        private PixelBuffer source;
        private ProcessingResult result;
        private readonly ToolTip contourTip = new ToolTip();
        private int hoveredPixel = -1;
        private string sourceName = "";
        private CancellationTokenSource cancellation;
        private bool busy, closing, syncingCanny;

        public MainForm()
        {
            Text = "ЛР 3 — Обработка изображений — Вариант 9";
            Width = 1280; Height = 880; MinimumSize = new Size(1000, 760);
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = new Font("Segoe UI", 9);
            TableLayoutPanel root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3 };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320)); root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
            Controls.Add(root);
            settingsPanel.Dock = DockStyle.Fill; settingsPanel.AutoScroll = true;
            settingsPanel.FlowDirection = FlowDirection.TopDown; settingsPanel.WrapContents = false; settingsPanel.Padding = new Padding(10);
            settingsPanel.Controls.Add(open); settingsPanel.Controls.Add(samples);
            AddLabel("Метод обработки");
            methods.Items.AddRange(new object[] { "Глобальный — Оцу", "Глобальный — ISODATA", "Адаптивный — Саувола", "Обнаружение точек", "Обнаружение линий", "Перепады — Собель", "Перепады — Кэнни" });
            methods.SelectedIndex = 0; settingsPanel.Controls.Add(methods);
            info.Margin = new Padding(0, 8, 0, 6); settingsPanel.Controls.Add(info);
            settingsPanel.Controls.Add(borderLabel); border.Items.AddRange(new object[] { "Clamp — повтор края", "Reflect — зеркальное отражение" });
            border.SelectedIndex = 1; settingsPanel.Controls.Add(border); settingsPanel.Controls.Add(parallel);
            settingsPanel.Controls.Add(radius); settingsPanel.Controls.Add(k); settingsPanel.Controls.Add(sigma);
            settingsPanel.Controls.Add(threshold); settingsPanel.Controls.Add(low); settingsPanel.Controls.Add(high);
            lines.Items.AddRange(new object[] { "Линии — все направления", "Горизонтальные", "Вертикальные", "Диагональ ↘", "Диагональ ↗" });
            lines.SelectedIndex = 0; settingsPanel.Controls.Add(lines);
            settingsPanel.Controls.Add(run); settingsPanel.Controls.Add(save);
            AddLabel("Каждая обработка использует исходное изображение.");
            root.Controls.Add(settingsPanel, 0, 0);
            visual.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); visual.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            visual.RowStyles.Add(new RowStyle(SizeType.Percent, 64)); visual.RowStyles.Add(new RowStyle(SizeType.Percent, 22));
            visual.RowStyles.Add(new RowStyle(SizeType.Percent, 14));
            GroupBox original = new GroupBox { Text = "Исходное изображение", Dock = DockStyle.Fill }; original.Controls.Add(before);
            visual.Controls.Add(original, 0, 0);
            imageTab.Controls.Add(after);
            TableLayoutPanel directionLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
            directionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            directionLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); directionLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            directionLayout.Controls.Add(direction, 0, 0);
            Label legend = new Label { Text = "Угол нормали к контуру: 0° → красный, 90° ↓ жёлто-зелёный, 180° ← голубой, 270° ↑ фиолетовый. Чёрный — нулевой градиент; яркость цвета — сила градиента.", Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(5) };
            directionLayout.Controls.Add(legend, 0, 1);
            directionLayout.SizeChanged += (s, e) => { legend.MaximumSize = new Size(Math.Max(10, directionLayout.ClientSize.Width - 6), 0); };
            directionTab.Controls.Add(directionLayout); resultTabs.TabPages.Add(imageTab);
            visual.Controls.Add(resultTabs, 1, 0); visual.Controls.Add(histBefore, 0, 1); visual.Controls.Add(histAfter, 1, 1);
            visual.Controls.Add(variance, 0, 2); visual.SetColumnSpan(variance, 2); root.Controls.Add(visual, 1, 0);
            root.Controls.Add(cancel, 0, 1);
            TableLayoutPanel footer = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
            footer.RowStyles.Add(new RowStyle(SizeType.Percent, 55)); footer.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
            footer.Controls.Add(status, 0, 0); footer.Controls.Add(scaleNote, 0, 1); root.Controls.Add(footer, 1, 1);
            root.Controls.Add(progress, 0, 2); root.SetColumnSpan(progress, 2);
            methods.SelectedIndexChanged += (s, e) =>
            {
                if (busy) return;
                ClearResult(); UpdateParameters();
                if (source != null) status.Text = "Выбран метод «" + methods.Text + "». Нажмите «Обработать».";
            };
            foreach (Parameter parameter in new Parameter[] { radius, k, sigma, threshold, low, high })
                parameter.Number.ValueChanged += (s, e) => ParametersChanged();
            low.Number.ValueChanged += (s, e) => MatchCannyThresholds(true);
            high.Number.ValueChanged += (s, e) => MatchCannyThresholds(false);
            border.SelectedIndexChanged += (s, e) => ParametersChanged();
            lines.SelectedIndexChanged += (s, e) => ParametersChanged();
            parallel.CheckedChanged += (s, e) => ParametersChanged();
            open.Click += async (s, e) => await OpenFile(false);
            samples.Click += async (s, e) => await OpenFile(true);
            run.Click += async (s, e) => await ProcessImage();
            cancel.Click += (s, e) => { if (cancellation != null) cancellation.Cancel(); cancel.Enabled = false; status.Text = "Остановка…"; };
            save.Click += async (s, e) => await SaveImage();
            after.MouseMove += ShowContourCoordinates;
            after.MouseLeave += (s, e) => ClearContourTip();
            FormClosing += (s, e) => { if (busy) { e.Cancel = true; closing = true; if (cancellation != null) cancellation.Cancel(); } };
            FormClosed += (s, e) => { SetPicture(before, null); SetPicture(after, null); SetPicture(direction, null); directionTab.Dispose(); contourTip.Dispose(); };
            UpdateParameters(); UpdateResultViews(); UpdateActions();
        }

        private void AddLabel(string text) { settingsPanel.Controls.Add(new Label { Text = text, Width = 265, AutoSize = true, Margin = new Padding(0, 8, 0, 3) }); }
        private static PictureBox Picture() { return new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.FromArgb(235, 235, 235) }; }
        private static void SetPicture(PictureBox box, Image image) { Image old = box.Image; box.Image = image; if (old != null) old.Dispose(); }
        private static double[] ToDouble(long[] counts) { double[] values = new double[counts.Length]; for (int i = 0; i < values.Length; i++) values[i] = counts[i]; return values; }

        private void UpdateParameters()
        {
            Method method = (Method)methods.SelectedIndex;
            bool adaptive = method == Method.Sauvola;
            bool edges = method == Method.Sobel || method == Method.Canny;
            settingsPanel.SuspendLayout();
            radius.Visible = k.Visible = adaptive;
            sigma.Visible = low.Visible = high.Visible = method == Method.Canny;
            threshold.Visible = method == Method.Points || method == Method.Lines || method == Method.Sobel;
            lines.Visible = method == Method.Lines;
            border.Visible = borderLabel.Visible = method != Method.Otsu && method != Method.Isodata;
            string[] help = {
                "Один порог для всего изображения. Оцу автоматически выбирает его по максимуму дисперсии.",
                "Один порог для всего изображения. ISODATA уточняет его по средним яркостям двух классов.",
                "Местный порог по среднему и разбросу яркости в окне. Полезен при неравномерном освещении.",
                "Выделяет резкие отличия пикселя от соседей. Найденные точки показаны белым.",
                "Выделяет линии выбранного направления. Найденные линии показаны белым.",
                "Выделяет перепады яркости. Порог отсекает слабые отклики; карта углов показывает направление градиента.",
                "Сглаживает шум и выделяет тонкие связанные контуры. Наведите мышь на контур, чтобы увидеть уточнённые координаты." };
            info.Text = help[methods.SelectedIndex] + (edges ? " Контуры — белые." : adaptive || method == Method.Otsu || method == Method.Isodata ? " Тёмные детали — чёрные." : "");
            foreach (Control control in new Control[] { radius, k, sigma, threshold, low, high, lines, border, parallel })
                control.Enabled = source != null && !busy;
            settingsPanel.ResumeLayout(true);
        }

        private void ParametersChanged()
        {
            if (!busy && source != null)
                status.Text = result == null ? "Нажмите «Обработать», чтобы применить параметры." : "Параметры изменены. Показан предыдущий результат; нажмите «Обработать».";
        }

        private void MatchCannyThresholds(bool lowerChanged)
        {
            if (syncingCanny || low.Value <= high.Value) return;
            syncingCanny = true;
            if (lowerChanged) high.Number.Value = low.Number.Value; else low.Number.Value = high.Number.Value;
            syncingCanny = false;
        }

        private void UpdateActions()
        {
            run.Enabled = !busy && source != null;
            save.Enabled = !busy && result != null;
            resultTabs.Enabled = !busy;
        }

        private void UpdateResultViews()
        {
            bool hasAngles = result != null && result.DirectionMap != null;
            if (hasAngles && !resultTabs.TabPages.Contains(directionTab)) resultTabs.TabPages.Add(directionTab);
            if (!hasAngles && resultTabs.TabPages.Contains(directionTab))
            { resultTabs.SelectedTab = imageTab; resultTabs.TabPages.Remove(directionTab); }
            bool hasVariance = result != null && result.OtsuVariance != null;
            visual.SuspendLayout();
            variance.Visible = hasVariance;
            visual.RowStyles[0].Height = hasVariance ? 64 : 74;
            visual.RowStyles[1].Height = hasVariance ? 22 : 26;
            visual.RowStyles[2].Height = hasVariance ? 14 : 0;
            visual.ResumeLayout(true);
        }

        private void ClearResult()
        {
            result = null; progress.Value = 0; ClearContourTip();
            SetPicture(after, null); SetPicture(direction, null);
            histAfter.Values = null; histBefore.Maximum = histAfter.Maximum = 0;
            histBefore.Marker = double.NaN; variance.Values = null; variance.Marker = double.NaN;
            imageTab.Text = "Результат";
            histBefore.Invalidate(); histAfter.Invalidate(); variance.Invalidate();
            UpdateResultViews(); UpdateActions();
        }

        private Settings CaptureSettings()
        {
            Method method = (Method)methods.SelectedIndex;
            return new Settings { Method = method, Border = (BorderMode)border.SelectedIndex,
                Parallel = parallel.Checked, Radius = (int)radius.Value, K = k.Value, Sigma = sigma.Value,
                Threshold = threshold.Value,
                Low = method == Method.Canny ? low.Value : 35, High = method == Method.Canny ? high.Value : 90, LineDirection = lines.SelectedIndex };
        }

        private void SetBusy(bool value, bool canCancel = false)
        {
            busy = value; settingsPanel.Enabled = !value; cancel.Enabled = value && canCancel;
            UpdateActions();
            if (value) { cancellation = new CancellationTokenSource(); progress.Value = 0; }
            else { if (cancellation != null) { cancellation.Dispose(); cancellation = null; } UpdateParameters(); if (closing) Close(); }
        }

        private async Task OpenFile(bool test)
        {
            if (busy) return;
            using (OpenFileDialog dialog = new OpenFileDialog { Filter = "Изображения|*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff;*.gif|Все файлы|*.*", Title = "Выберите исходное изображение" })
            {
                if (test) dialog.InitialDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestImages", "Исходники");
                if (dialog.ShowDialog() != DialogResult.OK) return;
                SetBusy(true, true); status.Text = "Загрузка изображения…";
                Bitmap display = null;
                try
                {
                    PixelBuffer loaded = await Task.Run(() => PixelBuffer.Load(dialog.FileName));
                    cancellation.Token.ThrowIfCancellationRequested();
                    display = await Task.Run(() => loaded.ToBitmap());
                    double[] histogram = await Task.Run(() => ToDouble(Processing.Histogram(loaded.Gray())));
                    cancellation.Token.ThrowIfCancellationRequested();
                    DisplaySource(loaded, Path.GetFileName(dialog.FileName), display, histogram); display = null;
                    progress.Value = 100;
                }
                catch (OperationCanceledException) { status.Text = "Загрузка отменена."; }
                catch (Exception error) { MessageBox.Show(error.Message, "Ошибка загрузки", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                finally { if (display != null) display.Dispose(); SetBusy(false); }
            }
        }

        private void DisplaySource(PixelBuffer loaded, string name, Bitmap display, double[] histogram)
        {
            ClearResult(); source = loaded; sourceName = name;
            SetPicture(before, display); histBefore.Values = histogram; histBefore.Invalidate();
            status.Text = sourceName + " — " + source.Width + "×" + source.Height + ". Нажмите «Обработать».";
            UpdateParameters(); UpdateActions();
        }

        private async Task ProcessImage()
        {
            if (source == null || busy) return;
            Settings settings = CaptureSettings();
            if (settings.Method == Method.Canny && settings.Low > settings.High) { MessageBox.Show("Нижний порог Кэнни должен быть не выше верхнего."); return; }
            SetBusy(true, true); status.Text = "Обработка…";
            CancellationTokenSource operation = cancellation;
            Progress<int> reporter = new Progress<int>(value => { if (busy && !IsDisposed && ReferenceEquals(cancellation, operation)) progress.Value = Math.Max(progress.Value, Math.Max(0, Math.Min(100, value))); });
            Bitmap output = null, angleMap = null;
            try
            {
                Stopwatch watch = Stopwatch.StartNew();
                ProcessingResult computed = await Task.Run(() => Processing.Run(source, settings,
                    new ProcessingContext(settings.Parallel, operation.Token, reporter)));
                watch.Stop();
                output = await Task.Run(() => computed.Image.ToBitmap());
                angleMap = computed.DirectionMap == null ? null : await Task.Run(() => computed.DirectionMap.ToBitmap());
                double[] histogram = await Task.Run(() => ToDouble(Processing.Histogram(computed.Image.Gray())));
                operation.Token.ThrowIfCancellationRequested();
                result = computed; ClearContourTip(); long elapsed = watch.ElapsedMilliseconds;
                SetPicture(after, output); output = null; SetPicture(direction, angleMap); angleMap = null;
                histAfter.Values = histogram;
                histBefore.Maximum = histAfter.Maximum = 0; histBefore.Marker = result.Threshold;
                variance.Values = result.OtsuVariance; variance.Marker = result.Threshold;
                imageTab.Text = "Результат";
                UpdateResultViews();
                histBefore.Invalidate(); histAfter.Invalidate(); variance.Invalidate(); progress.Value = 100;
                status.Text = elapsed + " мс; " + methods.Text + (double.IsNaN(result.Threshold) ? "" : "; T = " + result.Threshold.ToString("0.##")) +
                    (result.Iterations == 0 ? "" : "; итераций: " + result.Iterations);
                resultTabs.SelectedIndex = 0;
            }
            catch (OperationCanceledException) { status.Text = result == null ? "Обработка отменена." : "Обработка отменена. Предыдущий результат сохранён."; }
            catch (Exception error) { MessageBox.Show(error.Message, "Ошибка обработки", MessageBoxButtons.OK, MessageBoxIcon.Error); status.Text = "Обработка не выполнена."; }
            finally { if (output != null) output.Dispose(); if (angleMap != null) angleMap.Dispose(); SetBusy(false); }
        }

        private async Task SaveImage()
        {
            if (busy || result == null) return;
            using (SaveFileDialog dialog = new SaveFileDialog { Filter = "PNG|*.png", FileName = "result.png" })
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                PixelBuffer buffer = resultTabs.SelectedTab == directionTab ? result.DirectionMap : result.Image;
                if (buffer == null) { MessageBox.Show("У выбранного метода нет карты направлений."); return; }
                SetBusy(true);
                try { await Task.Run(() => { using (Bitmap b = buffer.ToBitmap()) b.Save(dialog.FileName, ImageFormat.Png); }); status.Text = "Изображение сохранено."; }
                catch (Exception error) { MessageBox.Show(error.Message, "Ошибка сохранения"); }
                finally { SetBusy(false); }
            }
        }

        private void ClearContourTip()
        {
            hoveredPixel = -1;
            contourTip.SetToolTip(after, "");
        }

        private void ShowContourCoordinates(object sender, MouseEventArgs e)
        {
            if (busy || result == null || result.OffsetX == null || after.Image == null)
            { ClearContourTip(); return; }
            int width = result.Image.Width, height = result.Image.Height;
            double scale = Math.Min(after.ClientSize.Width / (double)width, after.ClientSize.Height / (double)height);
            if (scale <= 0) { ClearContourTip(); return; }
            double left = (after.ClientSize.Width - width * scale) / 2;
            double top = (after.ClientSize.Height - height * scale) / 2;
            double px = (e.X - left) / scale, py = (e.Y - top) / scale;
            if (px < 0 || py < 0 || px >= width || py >= height)
            { ClearContourTip(); return; }
            int x = (int)px, y = (int)py;
            if (result.Image.Pixels[y, x, 0] == 0) { ClearContourTip(); return; }
            int i = y * width + x;
            if (i == hoveredPixel) return;
            hoveredPixel = i;
            double angle = result.Angles[i] * 180 / Math.PI;
            if (angle < 0) angle += 360;
            contourTip.SetToolTip(after, "Пиксель: (" + x + "; " + y + ")" +
                "\nУточнённая граница: (" + (x + result.OffsetX[i]).ToString("0.000") + "; " +
                (y + result.OffsetY[i]).ToString("0.000") + ")\nУгол градиента: " + angle.ToString("0.0") + "°");
        }
    }
}
