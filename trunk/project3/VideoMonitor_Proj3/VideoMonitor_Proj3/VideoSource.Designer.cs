namespace VideoMonitor_Proj3
{
    partial class VideoSource
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.startStream = new System.Windows.Forms.Button();
            this.endStream = new System.Windows.Forms.Button();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.streamGroupBox1 = new System.Windows.Forms.GroupBox();
            this.optsrcWebcam = new System.Windows.Forms.RadioButton();
            this.optsrcWMV = new System.Windows.Forms.RadioButton();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.streamGroupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // timer1
            // 
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // startStream
            // 
            this.startStream.Location = new System.Drawing.Point(13, 3);
            this.startStream.Name = "startStream";
            this.startStream.Size = new System.Drawing.Size(108, 29);
            this.startStream.TabIndex = 0;
            this.startStream.Text = "Start Stream";
            this.startStream.UseVisualStyleBackColor = true;
            this.startStream.Click += new System.EventHandler(this.startStream_Click);
            // 
            // endStream
            // 
            this.endStream.Location = new System.Drawing.Point(13, 38);
            this.endStream.Name = "endStream";
            this.endStream.Size = new System.Drawing.Size(108, 29);
            this.endStream.TabIndex = 1;
            this.endStream.Text = "End Stream";
            this.endStream.UseVisualStyleBackColor = true;
            this.endStream.Click += new System.EventHandler(this.endStream_Click);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pictureBox1.Location = new System.Drawing.Point(0, 0);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(473, 361);
            this.pictureBox1.TabIndex = 2;
            this.pictureBox1.TabStop = false;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.streamGroupBox1);
            this.splitContainer1.Panel1.Controls.Add(this.startStream);
            this.splitContainer1.Panel1.Controls.Add(this.endStream);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.pictureBox1);
            this.splitContainer1.Size = new System.Drawing.Size(715, 361);
            this.splitContainer1.SplitterDistance = 238;
            this.splitContainer1.TabIndex = 3;
            // 
            // streamGroupBox1
            // 
            this.streamGroupBox1.Controls.Add(this.optsrcWebcam);
            this.streamGroupBox1.Controls.Add(this.optsrcWMV);
            this.streamGroupBox1.Location = new System.Drawing.Point(14, 73);
            this.streamGroupBox1.Name = "streamGroupBox1";
            this.streamGroupBox1.Size = new System.Drawing.Size(107, 69);
            this.streamGroupBox1.TabIndex = 7;
            this.streamGroupBox1.TabStop = false;
            this.streamGroupBox1.Text = "Stream Source";
            // 
            // optsrcWebcam
            // 
            this.optsrcWebcam.AutoSize = true;
            this.optsrcWebcam.Checked = true;
            this.optsrcWebcam.Location = new System.Drawing.Point(6, 19);
            this.optsrcWebcam.Name = "optsrcWebcam";
            this.optsrcWebcam.Size = new System.Drawing.Size(68, 17);
            this.optsrcWebcam.TabIndex = 4;
            this.optsrcWebcam.TabStop = true;
            this.optsrcWebcam.Text = "Webcam";
            this.optsrcWebcam.UseVisualStyleBackColor = true;
            // 
            // optsrcWMV
            // 
            this.optsrcWMV.AutoSize = true;
            this.optsrcWMV.Location = new System.Drawing.Point(6, 43);
            this.optsrcWMV.Name = "optsrcWMV";
            this.optsrcWMV.Size = new System.Drawing.Size(71, 17);
            this.optsrcWMV.TabIndex = 5;
            this.optsrcWMV.Text = "WMV File";
            this.optsrcWMV.UseVisualStyleBackColor = true;
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            this.openFileDialog1.Filter = "AVI Files|*.avi";
            // 
            // VideoSource
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainer1);
            this.Name = "VideoSource";
            this.Size = new System.Drawing.Size(715, 361);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            this.streamGroupBox1.ResumeLayout(false);
            this.streamGroupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Button startStream;
        private System.Windows.Forms.Button endStream;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.RadioButton optsrcWMV;
        private System.Windows.Forms.RadioButton optsrcWebcam;
        private System.Windows.Forms.GroupBox streamGroupBox1;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
    }
}
