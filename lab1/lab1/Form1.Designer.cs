namespace lab1
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
            groupBox1 = new GroupBox();
            label3 = new Label();
            label2 = new Label();
            label1 = new Label();
            numV = new NumericUpDown();
            numS = new NumericUpDown();
            numH = new NumericUpDown();
            trackV = new TrackBar();
            trackS = new TrackBar();
            trackH = new TrackBar();
            groupBox2 = new GroupBox();
            label4 = new Label();
            label5 = new Label();
            label6 = new Label();
            numZ = new NumericUpDown();
            numY = new NumericUpDown();
            numX = new NumericUpDown();
            trackZ = new TrackBar();
            trackY = new TrackBar();
            trackX = new TrackBar();
            groupBox3 = new GroupBox();
            label7 = new Label();
            label8 = new Label();
            label9 = new Label();
            numB = new NumericUpDown();
            numA = new NumericUpDown();
            numL = new NumericUpDown();
            trackB = new TrackBar();
            trackA = new TrackBar();
            trackL = new TrackBar();
            picColorPreview = new PictureBox();
            btnChooseColor = new Button();
            lblWarning = new Label();
            rbD65 = new RadioButton();
            rbD50 = new RadioButton();
            rbE = new RadioButton();
            lblRGB = new Label();
            panelH = new Panel();
            panelS = new Panel();
            panelV = new Panel();
            panelL = new Panel();
            panelX = new Panel();
            panelY = new Panel();
            panelZ = new Panel();
            panelA = new Panel();
            panelB = new Panel();
            groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numV).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numS).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numH).BeginInit();
            ((System.ComponentModel.ISupportInitialize)trackV).BeginInit();
            ((System.ComponentModel.ISupportInitialize)trackS).BeginInit();
            ((System.ComponentModel.ISupportInitialize)trackH).BeginInit();
            groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numZ).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numY).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numX).BeginInit();
            ((System.ComponentModel.ISupportInitialize)trackZ).BeginInit();
            ((System.ComponentModel.ISupportInitialize)trackY).BeginInit();
            ((System.ComponentModel.ISupportInitialize)trackX).BeginInit();
            groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numB).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numA).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numL).BeginInit();
            ((System.ComponentModel.ISupportInitialize)trackB).BeginInit();
            ((System.ComponentModel.ISupportInitialize)trackA).BeginInit();
            ((System.ComponentModel.ISupportInitialize)trackL).BeginInit();
            ((System.ComponentModel.ISupportInitialize)picColorPreview).BeginInit();
            SuspendLayout();
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(panelV);
            groupBox1.Controls.Add(panelS);
            groupBox1.Controls.Add(panelH);
            groupBox1.Controls.Add(label3);
            groupBox1.Controls.Add(label2);
            groupBox1.Controls.Add(label1);
            groupBox1.Controls.Add(numV);
            groupBox1.Controls.Add(numS);
            groupBox1.Controls.Add(numH);
            groupBox1.Controls.Add(trackV);
            groupBox1.Controls.Add(trackS);
            groupBox1.Controls.Add(trackH);
            groupBox1.Location = new Point(12, 12);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(596, 202);
            groupBox1.TabIndex = 0;
            groupBox1.TabStop = false;
            groupBox1.Text = "HSV";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(20, 157);
            label3.Name = "label3";
            label3.Size = new Size(18, 20);
            label3.TabIndex = 8;
            label3.Text = "V";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(20, 95);
            label2.Name = "label2";
            label2.Size = new Size(17, 20);
            label2.TabIndex = 7;
            label2.Text = "S";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(20, 33);
            label1.Name = "label1";
            label1.Size = new Size(20, 20);
            label1.TabIndex = 6;
            label1.Text = "H";
            // 
            // numV
            // 
            numV.Location = new Point(435, 150);
            numV.Name = "numV";
            numV.Size = new Size(150, 27);
            numV.TabIndex = 5;
            numV.ValueChanged += OnHSVChanged;
            // 
            // numS
            // 
            numS.Location = new Point(435, 88);
            numS.Name = "numS";
            numS.Size = new Size(150, 27);
            numS.TabIndex = 4;
            numS.ValueChanged += OnHSVChanged;
            // 
            // numH
            // 
            numH.Location = new Point(435, 26);
            numH.Maximum = new decimal(new int[] { 360, 0, 0, 0 });
            numH.Name = "numH";
            numH.Size = new Size(150, 27);
            numH.TabIndex = 3;
            numH.ValueChanged += OnHSVChanged;
            // 
            // trackV
            // 
            trackV.Location = new Point(49, 150);
            trackV.Maximum = 100;
            trackV.Name = "trackV";
            trackV.Size = new Size(380, 56);
            trackV.TabIndex = 2;
            trackV.ValueChanged += OnHSVChanged;
            // 
            // trackS
            // 
            trackS.Location = new Point(49, 88);
            trackS.Maximum = 100;
            trackS.Name = "trackS";
            trackS.Size = new Size(380, 56);
            trackS.TabIndex = 1;
            trackS.ValueChanged += OnHSVChanged;
            // 
            // trackH
            // 
            trackH.Location = new Point(49, 26);
            trackH.Maximum = 360;
            trackH.Name = "trackH";
            trackH.Size = new Size(380, 56);
            trackH.TabIndex = 0;
            trackH.ValueChanged += OnHSVChanged;
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(panelX);
            groupBox2.Controls.Add(panelY);
            groupBox2.Controls.Add(panelZ);
            groupBox2.Controls.Add(label4);
            groupBox2.Controls.Add(label5);
            groupBox2.Controls.Add(label6);
            groupBox2.Controls.Add(numZ);
            groupBox2.Controls.Add(numY);
            groupBox2.Controls.Add(numX);
            groupBox2.Controls.Add(trackZ);
            groupBox2.Controls.Add(trackY);
            groupBox2.Controls.Add(trackX);
            groupBox2.Location = new Point(12, 224);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(596, 202);
            groupBox2.TabIndex = 9;
            groupBox2.TabStop = false;
            groupBox2.Text = "XYZ";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(20, 157);
            label4.Name = "label4";
            label4.Size = new Size(18, 20);
            label4.TabIndex = 8;
            label4.Text = "Z";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(20, 95);
            label5.Name = "label5";
            label5.Size = new Size(17, 20);
            label5.TabIndex = 7;
            label5.Text = "Y";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(20, 33);
            label6.Name = "label6";
            label6.Size = new Size(18, 20);
            label6.TabIndex = 6;
            label6.Text = "X";
            // 
            // numZ
            // 
            numZ.DecimalPlaces = 2;
            numZ.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
            numZ.Location = new Point(435, 150);
            numZ.Maximum = new decimal(new int[] { 10890, 0, 0, 131072 });
            numZ.Name = "numZ";
            numZ.Size = new Size(150, 27);
            numZ.TabIndex = 5;
            numZ.ValueChanged += OnXYZChanged;
            // 
            // numY
            // 
            numY.DecimalPlaces = 2;
            numY.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
            numY.Location = new Point(435, 88);
            numY.Name = "numY";
            numY.Size = new Size(150, 27);
            numY.TabIndex = 4;
            numY.ValueChanged += OnXYZChanged;
            // 
            // numX
            // 
            numX.DecimalPlaces = 2;
            numX.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
            numX.Location = new Point(435, 26);
            numX.Maximum = new decimal(new int[] { 9505, 0, 0, 131072 });
            numX.Name = "numX";
            numX.Size = new Size(150, 27);
            numX.TabIndex = 3;
            numX.ValueChanged += OnXYZChanged;
            // 
            // trackZ
            // 
            trackZ.Location = new Point(49, 150);
            trackZ.Maximum = 108;
            trackZ.Name = "trackZ";
            trackZ.Size = new Size(380, 56);
            trackZ.TabIndex = 2;
            trackZ.ValueChanged += OnXYZChanged;
            // 
            // trackY
            // 
            trackY.Location = new Point(49, 88);
            trackY.Maximum = 100;
            trackY.Name = "trackY";
            trackY.Size = new Size(380, 56);
            trackY.TabIndex = 1;
            trackY.ValueChanged += OnXYZChanged;
            // 
            // trackX
            // 
            trackX.Location = new Point(49, 26);
            trackX.Maximum = 95;
            trackX.Name = "trackX";
            trackX.Size = new Size(380, 56);
            trackX.TabIndex = 0;
            trackX.ValueChanged += OnXYZChanged;
            // 
            // groupBox3
            // 
            groupBox3.Controls.Add(panelA);
            groupBox3.Controls.Add(panelB);
            groupBox3.Controls.Add(panelL);
            groupBox3.Controls.Add(label7);
            groupBox3.Controls.Add(label8);
            groupBox3.Controls.Add(label9);
            groupBox3.Controls.Add(numB);
            groupBox3.Controls.Add(numA);
            groupBox3.Controls.Add(numL);
            groupBox3.Controls.Add(trackB);
            groupBox3.Controls.Add(trackA);
            groupBox3.Controls.Add(trackL);
            groupBox3.Location = new Point(12, 432);
            groupBox3.Name = "groupBox3";
            groupBox3.Size = new Size(596, 202);
            groupBox3.TabIndex = 9;
            groupBox3.TabStop = false;
            groupBox3.Text = "LAB";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(20, 157);
            label7.Name = "label7";
            label7.Size = new Size(18, 20);
            label7.TabIndex = 8;
            label7.Text = "b";
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(20, 95);
            label8.Name = "label8";
            label8.Size = new Size(17, 20);
            label8.TabIndex = 7;
            label8.Text = "a";
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new Point(20, 33);
            label9.Name = "label9";
            label9.Size = new Size(16, 20);
            label9.TabIndex = 6;
            label9.Text = "L";
            // 
            // numB
            // 
            numB.DecimalPlaces = 2;
            numB.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
            numB.Location = new Point(435, 150);
            numB.Maximum = new decimal(new int[] { 127, 0, 0, 0 });
            numB.Minimum = new decimal(new int[] { 128, 0, 0, int.MinValue });
            numB.Name = "numB";
            numB.Size = new Size(150, 27);
            numB.TabIndex = 5;
            numB.ValueChanged += OnLABChanged;
            // 
            // numA
            // 
            numA.DecimalPlaces = 2;
            numA.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
            numA.Location = new Point(435, 88);
            numA.Maximum = new decimal(new int[] { 127, 0, 0, 0 });
            numA.Minimum = new decimal(new int[] { 128, 0, 0, int.MinValue });
            numA.Name = "numA";
            numA.Size = new Size(150, 27);
            numA.TabIndex = 4;
            numA.ValueChanged += OnLABChanged;
            // 
            // numL
            // 
            numL.DecimalPlaces = 2;
            numL.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
            numL.Location = new Point(435, 26);
            numL.Name = "numL";
            numL.Size = new Size(150, 27);
            numL.TabIndex = 3;
            numL.ValueChanged += OnLABChanged;
            // 
            // trackB
            // 
            trackB.Location = new Point(49, 157);
            trackB.Maximum = 127;
            trackB.Minimum = -128;
            trackB.Name = "trackB";
            trackB.Size = new Size(380, 56);
            trackB.TabIndex = 2;
            trackB.ValueChanged += OnLABChanged;
            // 
            // trackA
            // 
            trackA.Location = new Point(49, 88);
            trackA.Maximum = 127;
            trackA.Minimum = -128;
            trackA.Name = "trackA";
            trackA.Size = new Size(380, 56);
            trackA.TabIndex = 1;
            trackA.ValueChanged += OnLABChanged;
            // 
            // trackL
            // 
            trackL.Location = new Point(49, 26);
            trackL.Maximum = 100;
            trackL.Name = "trackL";
            trackL.Size = new Size(380, 56);
            trackL.TabIndex = 0;
            trackL.ValueChanged += OnLABChanged;
            // 
            // picColorPreview
            // 
            picColorPreview.BackColor = Color.Silver;
            picColorPreview.Location = new Point(614, 22);
            picColorPreview.Name = "picColorPreview";
            picColorPreview.Size = new Size(405, 192);
            picColorPreview.TabIndex = 10;
            picColorPreview.TabStop = false;
            // 
            // btnChooseColor
            // 
            btnChooseColor.Location = new Point(762, 224);
            btnChooseColor.Name = "btnChooseColor";
            btnChooseColor.Size = new Size(116, 53);
            btnChooseColor.TabIndex = 11;
            btnChooseColor.Text = "Выбрать из палитры";
            btnChooseColor.UseVisualStyleBackColor = true;
            btnChooseColor.Click += btnChooseColor_Click;
            // 
            // lblWarning
            // 
            lblWarning.AutoSize = true;
            lblWarning.ForeColor = Color.Red;
            lblWarning.Location = new Point(614, 613);
            lblWarning.Name = "lblWarning";
            lblWarning.Size = new Size(299, 20);
            lblWarning.TabIndex = 12;
            lblWarning.Text = "Выход за предел sRGB! Стратегия clipping";
            // 
            // rbD65
            // 
            rbD65.AutoSize = true;
            rbD65.Checked = true;
            rbD65.Location = new Point(621, 462);
            rbD65.Name = "rbD65";
            rbD65.Size = new Size(57, 24);
            rbD65.TabIndex = 13;
            rbD65.TabStop = true;
            rbD65.Text = "D65";
            rbD65.UseVisualStyleBackColor = true;
            rbD65.CheckedChanged += rbWhitePoint_CheckedChanged;
            // 
            // rbD50
            // 
            rbD50.AutoSize = true;
            rbD50.Location = new Point(621, 497);
            rbD50.Name = "rbD50";
            rbD50.Size = new Size(57, 24);
            rbD50.TabIndex = 14;
            rbD50.Text = "D50";
            rbD50.UseVisualStyleBackColor = true;
            rbD50.CheckedChanged += rbWhitePoint_CheckedChanged;
            // 
            // rbE
            // 
            rbE.AutoSize = true;
            rbE.Location = new Point(621, 532);
            rbE.Name = "rbE";
            rbE.Size = new Size(38, 24);
            rbE.TabIndex = 15;
            rbE.Text = "E";
            rbE.UseVisualStyleBackColor = true;
            rbE.CheckedChanged += rbWhitePoint_CheckedChanged;
            // 
            // lblRGB
            // 
            lblRGB.AutoSize = true;
            lblRGB.Location = new Point(762, 280);
            lblRGB.Name = "lblRGB";
            lblRGB.Size = new Size(0, 20);
            lblRGB.TabIndex = 16;
            // 
            // panelH
            // 
            panelH.BackgroundImageLayout = ImageLayout.Stretch;
            panelH.Location = new Point(66, 61);
            panelH.Name = "panelH";
            panelH.Size = new Size(347, 8);
            panelH.TabIndex = 9;
            // 
            // panelS
            // 
            panelS.BackgroundImageLayout = ImageLayout.Stretch;
            panelS.Location = new Point(66, 123);
            panelS.Name = "panelS";
            panelS.Size = new Size(347, 8);
            panelS.TabIndex = 10;
            // 
            // panelV
            // 
            panelV.BackgroundImageLayout = ImageLayout.Stretch;
            panelV.Location = new Point(66, 185);
            panelV.Name = "panelV";
            panelV.Size = new Size(347, 8);
            panelV.TabIndex = 17;
            // 
            // panelL
            // 
            panelL.BackgroundImageLayout = ImageLayout.Stretch;
            panelL.Location = new Point(66, 61);
            panelL.Name = "panelL";
            panelL.Size = new Size(347, 8);
            panelL.TabIndex = 17;
            // 
            // panelX
            // 
            panelX.BackgroundImageLayout = ImageLayout.Stretch;
            panelX.Location = new Point(66, 61);
            panelX.Name = "panelX";
            panelX.Size = new Size(347, 8);
            panelX.TabIndex = 10;
            // 
            // panelY
            // 
            panelY.BackgroundImageLayout = ImageLayout.Stretch;
            panelY.Location = new Point(66, 122);
            panelY.Name = "panelY";
            panelY.Size = new Size(347, 8);
            panelY.TabIndex = 10;
            // 
            // panelZ
            // 
            panelZ.BackgroundImageLayout = ImageLayout.Stretch;
            panelZ.Location = new Point(66, 188);
            panelZ.Name = "panelZ";
            panelZ.Size = new Size(347, 8);
            panelZ.TabIndex = 10;
            // 
            // panelA
            // 
            panelA.BackgroundImageLayout = ImageLayout.Stretch;
            panelA.Location = new Point(66, 123);
            panelA.Name = "panelA";
            panelA.Size = new Size(347, 8);
            panelA.TabIndex = 17;
            // 
            // panelB
            // 
            panelB.BackgroundImageLayout = ImageLayout.Stretch;
            panelB.Location = new Point(66, 192);
            panelB.Name = "panelB";
            panelB.Size = new Size(347, 8);
            panelB.TabIndex = 10;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1031, 642);
            Controls.Add(lblRGB);
            Controls.Add(rbE);
            Controls.Add(rbD50);
            Controls.Add(rbD65);
            Controls.Add(lblWarning);
            Controls.Add(btnChooseColor);
            Controls.Add(picColorPreview);
            Controls.Add(groupBox3);
            Controls.Add(groupBox2);
            Controls.Add(groupBox1);
            Name = "Form1";
            Text = "ColorConverter";
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numV).EndInit();
            ((System.ComponentModel.ISupportInitialize)numS).EndInit();
            ((System.ComponentModel.ISupportInitialize)numH).EndInit();
            ((System.ComponentModel.ISupportInitialize)trackV).EndInit();
            ((System.ComponentModel.ISupportInitialize)trackS).EndInit();
            ((System.ComponentModel.ISupportInitialize)trackH).EndInit();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numZ).EndInit();
            ((System.ComponentModel.ISupportInitialize)numY).EndInit();
            ((System.ComponentModel.ISupportInitialize)numX).EndInit();
            ((System.ComponentModel.ISupportInitialize)trackZ).EndInit();
            ((System.ComponentModel.ISupportInitialize)trackY).EndInit();
            ((System.ComponentModel.ISupportInitialize)trackX).EndInit();
            groupBox3.ResumeLayout(false);
            groupBox3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numB).EndInit();
            ((System.ComponentModel.ISupportInitialize)numA).EndInit();
            ((System.ComponentModel.ISupportInitialize)numL).EndInit();
            ((System.ComponentModel.ISupportInitialize)trackB).EndInit();
            ((System.ComponentModel.ISupportInitialize)trackA).EndInit();
            ((System.ComponentModel.ISupportInitialize)trackL).EndInit();
            ((System.ComponentModel.ISupportInitialize)picColorPreview).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private GroupBox groupBox1;
        private TrackBar trackV;
        private TrackBar trackS;
        private TrackBar trackH;
        private NumericUpDown numV;
        private NumericUpDown numS;
        private NumericUpDown numH;
        private Label label3;
        private Label label2;
        private Label label1;
        private GroupBox groupBox2;
        private Label label4;
        private Label label5;
        private Label label6;
        private NumericUpDown numZ;
        private NumericUpDown numY;
        private NumericUpDown numX;
        private TrackBar trackZ;
        private TrackBar trackY;
        private TrackBar trackX;
        private GroupBox groupBox3;
        private Label label7;
        private Label label8;
        private Label label9;
        private NumericUpDown numB;
        private NumericUpDown numA;
        private NumericUpDown numL;
        private TrackBar trackB;
        private TrackBar trackA;
        private TrackBar trackL;
        private PictureBox picColorPreview;
        private Button btnChooseColor;
        private Label lblWarning;
        private RadioButton rbD65;
        private RadioButton rbD50;
        private RadioButton rbE;
        private Label lblRGB;
        private Panel panelV;
        private Panel panelS;
        private Panel panelH;
        private Panel panelX;
        private Panel panelY;
        private Panel panelZ;
        private Panel panelL;
        private Panel panelA;
        private Panel panelB;
    }
}
