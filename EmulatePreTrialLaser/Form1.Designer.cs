
namespace EmulatePreTrialLaser
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.btnGenerate = new System.Windows.Forms.Button();
            this.btnAbort = new System.Windows.Forms.Button();
            this.tbFolder = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.button1 = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.nmTarget = new System.Windows.Forms.NumericUpDown();
            this.label4 = new System.Windows.Forms.Label();
            this.nmInterval = new System.Windows.Forms.NumericUpDown();
            this.label5 = new System.Windows.Forms.Label();
            this.nmIndex = new System.Windows.Forms.NumericUpDown();
            this.tbArticle = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.nmTarget)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmInterval)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmIndex)).BeginInit();
            this.SuspendLayout();
            // 
            // btnGenerate
            // 
            this.btnGenerate.Location = new System.Drawing.Point(356, 102);
            this.btnGenerate.Name = "btnGenerate";
            this.btnGenerate.Size = new System.Drawing.Size(245, 59);
            this.btnGenerate.TabIndex = 0;
            this.btnGenerate.Text = "Generate";
            this.btnGenerate.UseVisualStyleBackColor = true;
            this.btnGenerate.Click += new System.EventHandler(this.btnGenerate_Click);
            // 
            // btnAbort
            // 
            this.btnAbort.Enabled = false;
            this.btnAbort.Location = new System.Drawing.Point(631, 102);
            this.btnAbort.Name = "btnAbort";
            this.btnAbort.Size = new System.Drawing.Size(199, 59);
            this.btnAbort.TabIndex = 3;
            this.btnAbort.Text = "Abort";
            this.btnAbort.UseVisualStyleBackColor = true;
            this.btnAbort.Click += new System.EventHandler(this.btnAbort_Click);
            // 
            // tbFolder
            // 
            this.tbFolder.Location = new System.Drawing.Point(131, 24);
            this.tbFolder.Name = "tbFolder";
            this.tbFolder.ReadOnly = true;
            this.tbFolder.Size = new System.Drawing.Size(666, 20);
            this.tbFolder.TabIndex = 4;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(37, 27);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(88, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Datalog Location";
            // 
            // timer1
            // 
            this.timer1.Interval = 1000;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(804, 24);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(52, 23);
            this.button1.TabIndex = 5;
            this.button1.Text = "select";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(311, 65);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(86, 13);
            this.label3.TabIndex = 2;
            this.label3.Text = "Qty To Generate";
            // 
            // nmTarget
            // 
            this.nmTarget.Location = new System.Drawing.Point(403, 63);
            this.nmTarget.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nmTarget.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nmTarget.Name = "nmTarget";
            this.nmTarget.Size = new System.Drawing.Size(120, 20);
            this.nmTarget.TabIndex = 6;
            this.nmTarget.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(550, 65);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(68, 13);
            this.label4.TabIndex = 2;
            this.label4.Text = "Time Interval";
            // 
            // nmInterval
            // 
            this.nmInterval.Location = new System.Drawing.Point(642, 63);
            this.nmInterval.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nmInterval.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nmInterval.Name = "nmInterval";
            this.nmInterval.Size = new System.Drawing.Size(120, 20);
            this.nmInterval.TabIndex = 6;
            this.nmInterval.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(41, 67);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(33, 13);
            this.label5.TabIndex = 2;
            this.label5.Text = "Index";
            // 
            // nmIndex
            // 
            this.nmIndex.Location = new System.Drawing.Point(131, 65);
            this.nmIndex.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nmIndex.Name = "nmIndex";
            this.nmIndex.Size = new System.Drawing.Size(120, 20);
            this.nmIndex.TabIndex = 6;
            // 
            // tbArticle
            // 
            this.tbArticle.Location = new System.Drawing.Point(131, 118);
            this.tbArticle.Name = "tbArticle";
            this.tbArticle.Size = new System.Drawing.Size(190, 20);
            this.tbArticle.TabIndex = 7;
            this.tbArticle.Text = "7070249200";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(41, 125);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(35, 13);
            this.label1.TabIndex = 8;
            this.label1.Text = "article";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(868, 173);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.tbArticle);
            this.Controls.Add(this.nmInterval);
            this.Controls.Add(this.nmIndex);
            this.Controls.Add(this.nmTarget);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.tbFolder);
            this.Controls.Add(this.btnAbort);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.btnGenerate);
            this.Name = "Form1";
            this.Text = "Emulate Laser";
            ((System.ComponentModel.ISupportInitialize)(this.nmTarget)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmInterval)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmIndex)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnGenerate;
        private System.Windows.Forms.Button btnAbort;
        private System.Windows.Forms.TextBox tbFolder;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown nmTarget;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.NumericUpDown nmInterval;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.NumericUpDown nmIndex;
        private System.Windows.Forms.TextBox tbArticle;
        private System.Windows.Forms.Label label1;
    }
}

