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
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // timer1
            // 
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // startStream
            // 
            this.startStream.Location = new System.Drawing.Point(3, 118);
            this.startStream.Name = "startStream";
            this.startStream.Size = new System.Drawing.Size(75, 23);
            this.startStream.TabIndex = 0;
            this.startStream.Text = "Start Stream";
            this.startStream.UseVisualStyleBackColor = true;
            this.startStream.Click += new System.EventHandler(this.startStream_Click);
            // 
            // endStream
            // 
            this.endStream.Location = new System.Drawing.Point(84, 118);
            this.endStream.Name = "endStream";
            this.endStream.Size = new System.Drawing.Size(75, 23);
            this.endStream.TabIndex = 1;
            this.endStream.Text = "End Stream";
            this.endStream.UseVisualStyleBackColor = true;
            this.endStream.Click += new System.EventHandler(this.endStream_Click);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBox1.Location = new System.Drawing.Point(4, 4);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(147, 108);
            this.pictureBox1.TabIndex = 2;
            this.pictureBox1.TabStop = false;
            // 
            // VideoSource
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.endStream);
            this.Controls.Add(this.startStream);
            this.Name = "VideoSource";
            this.Size = new System.Drawing.Size(154, 149);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Button startStream;
        private System.Windows.Forms.Button endStream;
        private System.Windows.Forms.PictureBox pictureBox1;
    }
}
