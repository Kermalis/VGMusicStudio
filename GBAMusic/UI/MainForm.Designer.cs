namespace GBAMusic.UI
{
    partial class MainForm
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
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.playButton = new System.Windows.Forms.Button();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.songNumerical = new System.Windows.Forms.NumericUpDown();
            this.stopButton = new System.Windows.Forms.Button();
            this.pauseButton = new System.Windows.Forms.Button();
            this.tempoLabel = new System.Windows.Forms.Label();
            this.creatorLabel = new System.Windows.Forms.Label();
            this.gameLabel = new System.Windows.Forms.Label();
            this.codeLabel = new System.Windows.Forms.Label();
            this.trackInfoControl1 = new TrackInfoControl();
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.songNumerical)).BeginInit();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(515, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.openToolStripMenuItem.Size = new System.Drawing.Size(146, 22);
            this.openToolStripMenuItem.Text = "Open";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.OpenROM);
            // 
            // playButton
            // 
            this.playButton.Enabled = false;
            this.playButton.Location = new System.Drawing.Point(375, 24);
            this.playButton.Name = "playButton";
            this.playButton.Size = new System.Drawing.Size(75, 23);
            this.playButton.TabIndex = 1;
            this.playButton.Text = "Start";
            this.playButton.UseVisualStyleBackColor = true;
            this.playButton.Click += new System.EventHandler(this.Play);
            // 
            // songNumerical
            // 
            this.songNumerical.Location = new System.Drawing.Point(455, 24);
            this.songNumerical.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.songNumerical.Name = "songNumerical";
            this.songNumerical.Size = new System.Drawing.Size(45, 20);
            this.songNumerical.TabIndex = 3;
            this.songNumerical.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // stopButton
            // 
            this.stopButton.Enabled = false;
            this.stopButton.Location = new System.Drawing.Point(455, 56);
            this.stopButton.Name = "stopButton";
            this.stopButton.Size = new System.Drawing.Size(75, 23);
            this.stopButton.TabIndex = 4;
            this.stopButton.Text = "Stop";
            this.stopButton.UseVisualStyleBackColor = true;
            this.stopButton.Click += new System.EventHandler(this.Stop);
            // 
            // pauseButton
            // 
            this.pauseButton.Enabled = false;
            this.pauseButton.Location = new System.Drawing.Point(375, 56);
            this.pauseButton.Name = "pauseButton";
            this.pauseButton.Size = new System.Drawing.Size(75, 23);
            this.pauseButton.TabIndex = 5;
            this.pauseButton.Text = "Pause";
            this.pauseButton.UseVisualStyleBackColor = true;
            this.pauseButton.Click += new System.EventHandler(this.Pause);
            // 
            // tempoLabel
            // 
            this.tempoLabel.AutoSize = true;
            this.tempoLabel.Location = new System.Drawing.Point(379, 82);
            this.tempoLabel.Name = "tempoLabel";
            this.tempoLabel.Size = new System.Drawing.Size(67, 13);
            this.tempoLabel.TabIndex = 6;
            this.tempoLabel.Text = "Tempo - 120";
            this.tempoLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // creatorLabel
            // 
            this.creatorLabel.AutoSize = true;
            this.creatorLabel.Location = new System.Drawing.Point(385, 198);
            this.creatorLabel.Name = "creatorLabel";
            this.creatorLabel.Size = new System.Drawing.Size(63, 13);
            this.creatorLabel.TabIndex = 8;
            this.creatorLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // gameLabel
            // 
            this.gameLabel.AutoSize = true;
            this.gameLabel.Location = new System.Drawing.Point(385, 211);
            this.gameLabel.Name = "gameLabel";
            this.gameLabel.Size = new System.Drawing.Size(63, 13);
            this.gameLabel.TabIndex = 9;
            this.gameLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // codeLabel
            // 
            this.codeLabel.AutoSize = true;
            this.codeLabel.Location = new System.Drawing.Point(385, 236);
            this.codeLabel.Name = "codeLabel";
            this.codeLabel.Size = new System.Drawing.Size(63, 13);
            this.codeLabel.TabIndex = 10;
            this.codeLabel.Text = "Game Code";
            this.codeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // trackInfoControl1
            // 
            this.trackInfoControl1.Font = new System.Drawing.Font("Microsoft Tai Le", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.trackInfoControl1.Location = new System.Drawing.Point(0, 24);
            this.trackInfoControl1.Name = "trackInfoControl1";
            this.trackInfoControl1.Size = new System.Drawing.Size(375, 603);
            this.trackInfoControl1.TabIndex = 7;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(627, 627);
            this.Controls.Add(this.codeLabel);
            this.Controls.Add(this.gameLabel);
            this.Controls.Add(this.creatorLabel);
            this.Controls.Add(this.tempoLabel);
            this.Controls.Add(this.pauseButton);
            this.Controls.Add(this.stopButton);
            this.Controls.Add(this.songNumerical);
            this.Controls.Add(this.trackInfoControl1);
            this.Controls.Add(this.playButton);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MainForm";
            this.Text = "GBAMusic by Kermalis";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.songNumerical)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private GBAMusic.UI.TrackInfoControl trackInfoControl1;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.Button playButton;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.NumericUpDown songNumerical;
        private System.Windows.Forms.Button stopButton;
        private System.Windows.Forms.Button pauseButton;
        private System.Windows.Forms.Label tempoLabel;
        private System.Windows.Forms.Label creatorLabel;
        private System.Windows.Forms.Label gameLabel;
        private System.Windows.Forms.Label codeLabel;
    }
}

