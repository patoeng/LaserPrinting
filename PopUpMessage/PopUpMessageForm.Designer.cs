
namespace PopUpMessage
{
    partial class PopUpMessageForm
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
            this.tmrRandomizer = new System.Windows.Forms.Timer(this.components);
            this.label1 = new System.Windows.Forms.Label();
            this.tmrBlink = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // tmrRandomizer
            // 
            this.tmrRandomizer.Enabled = true;
            this.tmrRandomizer.Interval = 5000;
            this.tmrRandomizer.Tick += new System.EventHandler(this.tmrRandomizer_Tick);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 30F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.Yellow;
            this.label1.Location = new System.Drawing.Point(179, 67);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(443, 46);
            this.label1.TabIndex = 0;
            this.label1.Text = "Wrong Rating Drawing !";
            // 
            // tmrBlink
            // 
            this.tmrBlink.Enabled = true;
            this.tmrBlink.Interval = 250;
            this.tmrBlink.Tick += new System.EventHandler(this.tmrBlink_Tick);
            // 
            // PopUpMessageForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Tomato;
            this.ClientSize = new System.Drawing.Size(816, 191);
            this.ControlBox = false;
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "PopUpMessageForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "Wrong Drawing ";
            this.TopMost = true;
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Timer tmrRandomizer;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Timer tmrBlink;
    }
}

