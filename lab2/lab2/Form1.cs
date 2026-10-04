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

        private class RowData
        {
            public string Path { get; set; } = "";
            public ImageInfo Info { get; set; } = new ImageInfo();
        }

        public Form1()
        {
            InitializeComponent();

            btnStop.Enabled = false;
            btnExtraInfo.Enabled = false;

            lblBrokenCount.Text = "Повреждено: 0";
            lblUnknownCount.Text = "Неизвестных: 0";
            lblExtraInfo.Text = "";
        }

        private void btnChooseFolder_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                txtFolderPath.Text =
                    folderBrowserDialog.SelectedPath;
            }
        }

        private async void btnScan_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFolderPath.Text))
            {
                MessageBox.Show(
                    "Сначала выберите папку."
                );

                return;
            }

            if (!Directory.Exists(txtFolderPath.Text))
            {
                MessageBox.Show(
                    "Папка не существует."
                );

                return;
            }

            dgvImages.Rows.Clear();

            lblInfo.Text = "";
            lblExtraInfo.Text = "";

            lblBrokenCount.Text =
                "Повреждено: 0";

            lblUnknownCount.Text =
                "Неизвестных: 0";

            btnExtraInfo.Enabled = false;

            string[] files;

            try
            {
                files = Directory.GetFiles(
                    txtFolderPath.Text,
                    "*.*",
                    SearchOption.AllDirectories
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка обхода папок: " +
                    ex.Message
                );

                return;
            }

            if (files.Length == 0)
            {
                MessageBox.Show(
                    "Файлы не найдены."
                );

                return;
            }

            progressBar.Minimum = 0;
            progressBar.Maximum = files.Length;
            progressBar.Value = 0;

            lblProgress.Text =
                "0 / " + files.Length;

            lblTime.Text =
                "Время: 0 сек";

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

                        //Thread.Sleep(50);

                        ImageInfo info =
                            ImageScanner.ReadFile(file);

                        if (
                            info.Format == "Unknown" ||
                            info.Format == "Неизвестный"
                        )
                        {
                            Interlocked.Increment(
                                ref unknownCount
                            );
                        }
                        else if (
                            !string.IsNullOrWhiteSpace(
                                info.Status
                            ) &&
                            info.Status != "OK" &&
                            !info.Status.Contains(
                                "расшир",
                                StringComparison.OrdinalIgnoreCase
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

                            dgvImages.Rows[rowIndex].Tag =
                                new RowData
                                {
                                    Path = file,
                                    Info = info
                                };

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
                    "Ошибка: " +
                    ex.Message
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

        private void btnStop_Click(
            object sender,
            EventArgs e
        )
        {
            cancellationTokenSource?.Cancel();

            btnStop.Enabled = false;
        }

        private void dgvImages_SelectionChanged(
            object sender,
            EventArgs e
        )
        {
            if (dgvImages.SelectedRows.Count == 0)
            {
                btnExtraInfo.Enabled = false;
                lblExtraInfo.Text = "";

                return;
            }

            DataGridViewRow row =
                dgvImages.SelectedRows[0];

            RowData? data =
                row.Tag as RowData;

            if (data == null)
            {
                btnExtraInfo.Enabled = false;
                lblExtraInfo.Text = "";

                return;
            }

            string path =
                data.Path;

            ImageInfo info =
                data.Info;

            lblInfo.Text =
                "Имя: " +
                info.FileName +
                Environment.NewLine +

                "Формат: " +
                info.Format +
                Environment.NewLine +

                "Размер: " +
                info.Width +
                " x " +
                info.Height +
                Environment.NewLine +

                "DPI: " +
                info.Dpi +
                Environment.NewLine +

                "Глубина цвета: " +
                info.ColorDepth +
                Environment.NewLine +

                "Сжатие: " +
                info.Compression +
                Environment.NewLine +

                "Статус: " +
                info.Status;

            if (string.IsNullOrWhiteSpace(info.ExtraInfo))
            {
                lblExtraInfo.Text =
                    "Дополнительной информации нет";

                btnExtraInfo.Enabled = false;
            }
            else
            {
                lblExtraInfo.Text =
                    "Есть дополнительная информация";

                btnExtraInfo.Enabled = true;
            }

            if (picPreview.Image != null)
            {
                Image oldImage =
                    picPreview.Image;

                picPreview.Image = null;

                oldImage.Dispose();
            }

            if (!File.Exists(path))
                return;

            try
            {
                using Image temp =
                    Image.FromFile(path);

                picPreview.Image =
                    new Bitmap(temp);
            }
            catch
            {
                picPreview.Image = null;
            }
        }

        private void btnExtraInfo_Click(
            object sender,
            EventArgs e
        )
        {
            if (dgvImages.SelectedRows.Count == 0)
                return;

            DataGridViewRow row =
                dgvImages.SelectedRows[0];

            RowData? data =
                row.Tag as RowData;

            if (data == null)
                return;

            if (string.IsNullOrWhiteSpace(
                data.Info.ExtraInfo
            ))
            {
                return;
            }

            Form extraForm =
                new Form();

            extraForm.Text =
                "Дополнительная информация - " +
                data.Info.FileName;

            extraForm.Width = 750;
            extraForm.Height = 650;

            extraForm.StartPosition =
                FormStartPosition.CenterParent;

            RichTextBox textBox =
                new RichTextBox();

            textBox.Dock =
                DockStyle.Fill;

            textBox.ReadOnly = true;

            textBox.Font =
                new Font(
                    "Consolas",
                    10
                );

            textBox.WordWrap = false;

            textBox.ScrollBars =
                RichTextBoxScrollBars.Both;

            textBox.Text =
                data.Info.ExtraInfo;

            extraForm.Controls.Add(textBox);

            extraForm.ShowDialog(this);
        }
    }
}