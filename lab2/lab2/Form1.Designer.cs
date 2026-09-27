namespace lab2
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            txtFolderPath = new TextBox();
            btnChooseFolder = new Button();
            btnScan = new Button();
            btnStop = new Button();
            progressBar = new ProgressBar();
            lblProgress = new Label();
            dgvImages = new DataGridView();
            colFileName = new DataGridViewTextBoxColumn();
            colFormat = new DataGridViewTextBoxColumn();
            colWidth = new DataGridViewTextBoxColumn();
            colHeight = new DataGridViewTextBoxColumn();
            colDpi = new DataGridViewTextBoxColumn();
            colColorDepth = new DataGridViewTextBoxColumn();
            colCompression = new DataGridViewTextBoxColumn();
            colStatus = new DataGridViewTextBoxColumn();
            picPreview = new PictureBox();
            lblInfo = new Label();
            lblTime = new Label();
            folderBrowserDialog = new FolderBrowserDialog();
            lblExtraInfo = new Label();
            ((System.ComponentModel.ISupportInitialize)dgvImages).BeginInit();
            ((System.ComponentModel.ISupportInitialize)picPreview).BeginInit();
            SuspendLayout();
            // 
            // txtFolderPath
            // 
            txtFolderPath.Location = new Point(6, 10);
            txtFolderPath.Name = "txtFolderPath";
            txtFolderPath.Size = new Size(362, 27);
            txtFolderPath.TabIndex = 0;
            // 
            // btnChooseFolder
            // 
            btnChooseFolder.Location = new Point(374, 8);
            btnChooseFolder.Name = "btnChooseFolder";
            btnChooseFolder.Size = new Size(117, 29);
            btnChooseFolder.TabIndex = 1;
            btnChooseFolder.Text = "Выбор папки";
            btnChooseFolder.UseVisualStyleBackColor = true;
            btnChooseFolder.Click += btnChooseFolder_Click;
            // 
            // btnScan
            // 
            btnScan.Location = new Point(374, 43);
            btnScan.Name = "btnScan";
            btnScan.Size = new Size(117, 29);
            btnScan.TabIndex = 2;
            btnScan.Text = "Сканировать";
            btnScan.UseVisualStyleBackColor = true;
            btnScan.Click += btnScan_Click;
            // 
            // btnStop
            // 
            btnStop.Location = new Point(374, 78);
            btnStop.Name = "btnStop";
            btnStop.Size = new Size(117, 29);
            btnStop.TabIndex = 3;
            btnStop.Text = "Остановить";
            btnStop.UseVisualStyleBackColor = true;
            btnStop.Click += btnStop_Click;
            // 
            // progressBar
            // 
            progressBar.Location = new Point(6, 43);
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(362, 29);
            progressBar.TabIndex = 4;
            // 
            // lblProgress
            // 
            lblProgress.AutoSize = true;
            lblProgress.Location = new Point(6, 82);
            lblProgress.Name = "lblProgress";
            lblProgress.Size = new Size(39, 20);
            lblProgress.TabIndex = 5;
            lblProgress.Text = "0 / 0";
            // 
            // dgvImages
            // 
            dgvImages.AllowUserToAddRows = false;
            dgvImages.BackgroundColor = SystemColors.ControlLight;
            dgvImages.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvImages.Columns.AddRange(new DataGridViewColumn[] { colFileName, colFormat, colWidth, colHeight, colDpi, colColorDepth, colCompression, colStatus });
            dgvImages.Location = new Point(6, 149);
            dgvImages.MultiSelect = false;
            dgvImages.Name = "dgvImages";
            dgvImages.ReadOnly = true;
            dgvImages.RowHeadersWidth = 51;
            dgvImages.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvImages.Size = new Size(854, 244);
            dgvImages.TabIndex = 6;
            dgvImages.SelectionChanged += dgvImages_SelectionChanged;
            // 
            // colFileName
            // 
            colFileName.HeaderText = "Имя файла";
            colFileName.MinimumWidth = 6;
            colFileName.Name = "colFileName";
            colFileName.ReadOnly = true;
            colFileName.Width = 125;
            // 
            // colFormat
            // 
            colFormat.HeaderText = "Формат";
            colFormat.MinimumWidth = 6;
            colFormat.Name = "colFormat";
            colFormat.ReadOnly = true;
            colFormat.Width = 99;
            // 
            // colWidth
            // 
            colWidth.HeaderText = "Ширина";
            colWidth.MinimumWidth = 6;
            colWidth.Name = "colWidth";
            colWidth.ReadOnly = true;
            colWidth.Width = 125;
            // 
            // colHeight
            // 
            colHeight.HeaderText = "Высота";
            colHeight.MinimumWidth = 6;
            colHeight.Name = "colHeight";
            colHeight.ReadOnly = true;
            colHeight.Width = 101;
            // 
            // colDpi
            // 
            colDpi.HeaderText = "DPI";
            colDpi.MinimumWidth = 6;
            colDpi.Name = "colDpi";
            colDpi.ReadOnly = true;
            colDpi.Width = 125;
            // 
            // colColorDepth
            // 
            colColorDepth.HeaderText = "Глубина цвета";
            colColorDepth.MinimumWidth = 6;
            colColorDepth.Name = "colColorDepth";
            colColorDepth.ReadOnly = true;
            colColorDepth.Width = 125;
            // 
            // colCompression
            // 
            colCompression.HeaderText = "Сжатие";
            colCompression.MinimumWidth = 6;
            colCompression.Name = "colCompression";
            colCompression.ReadOnly = true;
            colCompression.Width = 125;
            // 
            // colStatus
            // 
            colStatus.HeaderText = "Статус";
            colStatus.MinimumWidth = 6;
            colStatus.Name = "colStatus";
            colStatus.ReadOnly = true;
            colStatus.Width = 101;
            // 
            // picPreview
            // 
            picPreview.Location = new Point(863, 149);
            picPreview.Name = "picPreview";
            picPreview.Size = new Size(242, 244);
            picPreview.SizeMode = PictureBoxSizeMode.Zoom;
            picPreview.TabIndex = 7;
            picPreview.TabStop = false;
            // 
            // lblInfo
            // 
            lblInfo.AutoSize = true;
            lblInfo.Location = new Point(497, 8);
            lblInfo.Name = "lblInfo";
            lblInfo.Size = new Size(0, 20);
            lblInfo.TabIndex = 8;
            // 
            // lblTime
            // 
            lblTime.AutoSize = true;
            lblTime.Location = new Point(6, 396);
            lblTime.Name = "lblTime";
            lblTime.Size = new Size(95, 20);
            lblTime.TabIndex = 9;
            lblTime.Text = "Время: 0 сек";
            // 
            // lblExtraInfo
            // 
            lblExtraInfo.AutoSize = true;
            lblExtraInfo.Location = new Point(759, 8);
            lblExtraInfo.Name = "lblExtraInfo";
            lblExtraInfo.Size = new Size(0, 20);
            lblExtraInfo.TabIndex = 10;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1117, 425);
            Controls.Add(lblExtraInfo);
            Controls.Add(lblTime);
            Controls.Add(lblInfo);
            Controls.Add(picPreview);
            Controls.Add(dgvImages);
            Controls.Add(lblProgress);
            Controls.Add(progressBar);
            Controls.Add(btnStop);
            Controls.Add(btnScan);
            Controls.Add(btnChooseFolder);
            Controls.Add(txtFolderPath);
            Name = "Form1";
            Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)dgvImages).EndInit();
            ((System.ComponentModel.ISupportInitialize)picPreview).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox txtFolderPath;
        private Button btnChooseFolder;
        private Button btnScan;
        private Button btnStop;
        private ProgressBar progressBar;
        private Label lblProgress;
        private DataGridView dgvImages;
        private PictureBox picPreview;
        private Label lblInfo;
        private Label lblTime;
        private FolderBrowserDialog folderBrowserDialog;
        private DataGridViewTextBoxColumn colFileName;
        private DataGridViewTextBoxColumn colFormat;
        private DataGridViewTextBoxColumn colWidth;
        private DataGridViewTextBoxColumn colHeight;
        private DataGridViewTextBoxColumn colDpi;
        private DataGridViewTextBoxColumn colColorDepth;
        private DataGridViewTextBoxColumn colCompression;
        private DataGridViewTextBoxColumn colStatus;
        private Label lblExtraInfo;
    }
}
