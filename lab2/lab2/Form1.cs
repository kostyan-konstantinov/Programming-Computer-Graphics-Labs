using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using lab2.Models;
using lab2.Services;

namespace lab2
{
    public partial class Form1 : Form
    {
        private CancellationTokenSource? cancellationTokenSource;

        public Form1()
        {
            InitializeComponent();

            btnStop.Enabled = false;

            lblBrokenCount.Text = "Повреждено: 0";
            lblUnknownCount.Text = "Неизвестных: 0";
        }

        // Выбор папки
        private void btnChooseFolder_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                txtFolderPath.Text = folderBrowserDialog.SelectedPath;
            }
        }

        // Сканирование
        private async void btnScan_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFolderPath.Text))
            {
                MessageBox.Show("Сначала выберите папку.");
                return;
            }

            if (!Directory.Exists(txtFolderPath.Text))
            {
                MessageBox.Show("Такой папки не существует.");
                return;
            }

            dgvImages.Rows.Clear();

            progressBar.Value = 0;

            lblProgress.Text = "0 / 0";
            lblTime.Text = "Время: 0 сек";

            lblBrokenCount.Text = "Повреждено: 0";
            lblUnknownCount.Text = "Неизвестных: 0";

            string[] files;

            try
            {
                // Берём файлы из выбранной папки
                // и всех вложенных подпапок
                files = Directory.GetFiles(
                    txtFolderPath.Text,
                    "*.*",
                    SearchOption.AllDirectories
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка при обходе папок: " + ex.Message
                );

                return;
            }

            if (files.Length == 0)
            {
                MessageBox.Show("Файлы не найдены.");
                return;
            }

            progressBar.Minimum = 0;
            progressBar.Maximum = files.Length;

            lblProgress.Text =
                "0 / " + files.Length;

            btnChooseFolder.Enabled = false;
            btnScan.Enabled = false;
            btnStop.Enabled = true;

            cancellationTokenSource =
                new CancellationTokenSource();

            Stopwatch stopwatch =
                Stopwatch.StartNew();

            int processed = 0;
            int brokenCount = 0;
            int unknownCount = 0;

            try
            {
                ParallelOptions options =
                    new ParallelOptions
                    {
                        CancellationToken =
                            cancellationTokenSource.Token,

                        MaxDegreeOfParallelism =
                            Environment.ProcessorCount
                    };

                await Parallel.ForEachAsync(
                    files,
                    options,
                    (file, ct) =>
                    {
                        ct.ThrowIfCancellationRequested();

                        // Читаем файл
                        ImageInfo info =
                            ImageScanner.ReadFile(file);

                        // Неизвестный формат
                        if (
                            info.Format == "Unknown" ||
                            info.Format == "Неизвестный"
                        )
                        {
                            Interlocked.Increment(
                                ref unknownCount
                            );
                        }

                        // Формат известен,
                        // но файл имеет ошибку
                        else if (
                            !string.IsNullOrWhiteSpace(
                                info.Status
                            ) &&
                            info.Status != "OK" &&
                            !info.Status.Contains(
                                "расшир",
                                StringComparison
                                    .OrdinalIgnoreCase
                            )
                        )
                        {
                            Interlocked.Increment(
                                ref brokenCount
                            );
                        }

                        int current =
                            Interlocked.Increment(
                                ref processed
                            );

                        // Обновляем интерфейс
                        Invoke(() =>
                        {
                            int rowIndex =
                                dgvImages.Rows.Add(
                                    info.FileName,
                                    info.Format,
                                    info.Width,
                                    info.Height,
                                    info.Dpi,
                                    info.ColorDepth,
                                    info.Compression,
                                    info.Status
                                );

                            // Сохраняем полный путь.
                            // Это важно теперь,
                            // когда есть подпапки.
                            dgvImages.Rows[
                                rowIndex
                            ].Tag = file;

                            progressBar.Value =
                                Math.Min(
                                    current,
                                    progressBar.Maximum
                                );

                            lblProgress.Text =
                                current +
                                " / " +
                                files.Length;

                            lblBrokenCount.Text =
                                "Повреждено: " +
                                brokenCount;

                            lblUnknownCount.Text =
                                "Неизвестных: " +
                                unknownCount;
                        });

                        return ValueTask.CompletedTask;
                    }
                );
            }
            catch (OperationCanceledException)
            {
                lblProgress.Text =
                    "Остановлено: " +
                    processed +
                    " / " +
                    files.Length;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка: " + ex.Message
                );
            }
            finally
            {
                stopwatch.Stop();

                lblTime.Text =
                    "Время: " +
                    stopwatch.Elapsed
                        .TotalSeconds
                        .ToString("F2") +
                    " сек";

                btnChooseFolder.Enabled = true;
                btnScan.Enabled = true;
                btnStop.Enabled = false;

                cancellationTokenSource?.Dispose();
                cancellationTokenSource = null;
            }
        }

        // Остановка сканирования
        private void btnStop_Click(
            object sender,
            EventArgs e
        )
        {
            cancellationTokenSource?.Cancel();

            btnStop.Enabled = false;
        }

        // Выбор строки в таблице
        private void dgvImages_SelectionChanged(
            object sender,
            EventArgs e
        )
        {
            if (dgvImages.SelectedRows.Count == 0)
                return;

            DataGridViewRow row =
                dgvImages.SelectedRows[0];

            // Теперь здесь хранится полный путь,
            // поэтому файлы из подпапок тоже откроются
            string? path =
                row.Tag as string;

            if (
                string.IsNullOrWhiteSpace(path) ||
                !File.Exists(path)
            )
            {
                return;
            }

            lblInfo.Text =
                "Имя: " +
                row.Cells[0].Value +
                Environment.NewLine +

                "Формат: " +
                row.Cells[1].Value +
                Environment.NewLine +

                "Размер: " +
                row.Cells[2].Value +
                " x " +
                row.Cells[3].Value +
                Environment.NewLine +

                "DPI: " +
                row.Cells[4].Value +
                Environment.NewLine +

                "Глубина цвета: " +
                row.Cells[5].Value +
                Environment.NewLine +

                "Сжатие: " +
                row.Cells[6].Value +
                Environment.NewLine +

                "Статус: " +
                row.Cells[7].Value;

            // Удаляем старое изображение
            if (picPreview.Image != null)
            {
                Image oldImage =
                    picPreview.Image;

                picPreview.Image = null;

                oldImage.Dispose();
            }

            // Предпросмотр
            try
            {
                using Image temp =
                    Image.FromFile(path);

                picPreview.Image =
                    new Bitmap(temp);

                lblExtraInfo.Text = "";
            }
            catch
            {
                picPreview.Image = null;

                lblExtraInfo.Text =
                    "Предпросмотр недоступен";
            }
        }
    }
}