
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
            picPreview.SizeMode = PictureBoxSizeMode.Zoom;
        }

        private void btnChooseFolder_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                txtFolderPath.Text = folderBrowserDialog.SelectedPath;
            }
        }

        private async void btnScan_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFolderPath.Text))
            {
                MessageBox.Show("Сначала выберите папку.");
                return;
            }

            if (!Directory.Exists(txtFolderPath.Text))
            {
                MessageBox.Show("Папка не существует.");
                return;
            }

            string[] files = Directory.GetFiles(txtFolderPath.Text);

            if (files.Length == 0)
            {
                MessageBox.Show("В выбранной папке нет файлов.");
                return;
            }

            dgvImages.Rows.Clear();

            lblInfo.Text = "";
            lblExtraInfo.Text = "";

            if (picPreview.Image != null)
            {
                Image oldImage = picPreview.Image;
                picPreview.Image = null;
                oldImage.Dispose();
            }

            progressBar.Minimum = 0;
            progressBar.Maximum = files.Length;
            progressBar.Value = 0;

            lblProgress.Text = "0 / " + files.Length;
            lblTime.Text = "Время: 0 сек";

            btnChooseFolder.Enabled = false;
            btnScan.Enabled = false;
            btnStop.Enabled = true;

            cancellationTokenSource = new CancellationTokenSource();

            CancellationToken token = cancellationTokenSource.Token;

            int processed = 0;

            Stopwatch stopwatch = Stopwatch.StartNew();

            try
            {
                ParallelOptions options = new ParallelOptions
                {
                    CancellationToken = token,
                    MaxDegreeOfParallelism = Environment.ProcessorCount
                };

                await Parallel.ForEachAsync(
                    files,
                    options,
                    (file, ct) =>
                    {
                        ct.ThrowIfCancellationRequested();

                        ImageInfo info = ImageScanner.ReadFile(file);

                        Invoke(() =>
                        {
                            int rowIndex = dgvImages.Rows.Add(
                                info.FileName,
                                info.Format,
                                info.Width,
                                info.Height,
                                info.Dpi,
                                info.ColorDepth,
                                info.Compression,
                                info.Status
                            );

                            dgvImages.Rows[rowIndex].Tag = info;

                            processed++;

                            progressBar.Value = processed;

                            lblProgress.Text =
                                processed + " / " + files.Length;
                        });

                        return ValueTask.CompletedTask;
                    }
                );
            }
            catch (OperationCanceledException)
            {
                lblProgress.Text =
                    "Остановлено: " + processed + " / " + files.Length;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
            finally
            {
                stopwatch.Stop();

                lblTime.Text =
                    "Время: " +
                    stopwatch.Elapsed.TotalSeconds.ToString("F2") +
                    " сек";

                btnChooseFolder.Enabled = true;
                btnScan.Enabled = true;
                btnStop.Enabled = false;

                cancellationTokenSource?.Dispose();
                cancellationTokenSource = null;
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            cancellationTokenSource?.Cancel();

            btnStop.Enabled = false;
        }

        private void dgvImages_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvImages.SelectedRows.Count == 0)
                return;

            DataGridViewRow row = dgvImages.SelectedRows[0];

            string fileName =
                row.Cells[0].Value?.ToString() ?? "";

            if (string.IsNullOrWhiteSpace(fileName))
                return;

            string path = Path.Combine(txtFolderPath.Text, fileName);

            if (!File.Exists(path))
                return;

            lblInfo.Text =
                "Имя: " + row.Cells[0].Value + Environment.NewLine +
                "Формат: " + row.Cells[1].Value + Environment.NewLine +
                "Размер: " +
                row.Cells[2].Value + " x " +
                row.Cells[3].Value + Environment.NewLine +
                "DPI: " + row.Cells[4].Value + Environment.NewLine +
                "Глубина цвета: " + row.Cells[5].Value + Environment.NewLine +
                "Сжатие: " + row.Cells[6].Value + Environment.NewLine +
                "Статус: " + row.Cells[7].Value;

            ImageInfo? info = row.Tag as ImageInfo;

            lblExtraInfo.Text = info?.ExtraInfo ?? "";

            if (picPreview.Image != null)
            {
                Image oldImage = picPreview.Image;

                picPreview.Image = null;

                oldImage.Dispose();
            }

            try
            {
                using Image tempImage = Image.FromFile(path);

                picPreview.Image = new Bitmap(tempImage);
            }
            catch
            {
                picPreview.Image = null;

                if (!string.IsNullOrWhiteSpace(lblExtraInfo.Text))
                {
                    lblExtraInfo.Text += Environment.NewLine;
                }

                lblExtraInfo.Text +=
                    "Предпросмотр недоступен \nдля данного изображения.";
            }
        }
    }
}
