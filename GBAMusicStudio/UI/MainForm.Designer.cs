namespace GBAMusicStudio.UI
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.playButton = new System.Windows.Forms.Button();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.songNumerical = new System.Windows.Forms.NumericUpDown();
            this.stopButton = new System.Windows.Forms.Button();
            this.pauseButton = new System.Windows.Forms.Button();
            this.creatorLabel = new System.Windows.Forms.Label();
            this.gameLabel = new System.Windows.Forms.Label();
            this.codeLabel = new System.Windows.Forms.Label();
            this.pianoControl = new Sanford.Multimedia.Midi.UI.PianoControl();
            this.trackInfoControl = new GBAMusicStudio.UI.TrackInfoControl();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.songsComboBox = new ImageComboBox.ImageComboBox();
            this.imageList1 = new System.Windows.Forms.ImageList(this.components);
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.songNumerical)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(525, 24);
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
            this.playButton.ForeColor = System.Drawing.Color.DarkGreen;
            this.playButton.Location = new System.Drawing.Point(3, 3);
            this.playButton.Name = "playButton";
            this.playButton.Size = new System.Drawing.Size(75, 23);
            this.playButton.TabIndex = 1;
            this.playButton.Text = "Play";
            this.playButton.UseVisualStyleBackColor = true;
            this.playButton.Click += new System.EventHandler(this.Play);
            // 
            // songNumerical
            // 
            this.songNumerical.Enabled = false;
            this.songNumerical.Location = new System.Drawing.Point(246, 4);
            this.songNumerical.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.songNumerical.Name = "songNumerical";
            this.songNumerical.Size = new System.Drawing.Size(45, 23);
            this.songNumerical.TabIndex = 3;
            this.songNumerical.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // stopButton
            // 
            this.stopButton.Enabled = false;
            this.stopButton.ForeColor = System.Drawing.Color.MediumVioletRed;
            this.stopButton.Location = new System.Drawing.Point(164, 3);
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
            this.pauseButton.ForeColor = System.Drawing.Color.DarkSlateBlue;
            this.pauseButton.Location = new System.Drawing.Point(84, 3);
            this.pauseButton.Name = "pauseButton";
            this.pauseButton.Size = new System.Drawing.Size(75, 23);
            this.pauseButton.TabIndex = 5;
            this.pauseButton.Text = "Pause";
            this.pauseButton.UseVisualStyleBackColor = true;
            this.pauseButton.Click += new System.EventHandler(this.Pause);
            // 
            // creatorLabel
            // 
            this.creatorLabel.AutoSize = true;
            this.creatorLabel.Location = new System.Drawing.Point(3, 42);
            this.creatorLabel.Name = "creatorLabel";
            this.creatorLabel.Size = new System.Drawing.Size(72, 13);
            this.creatorLabel.TabIndex = 8;
            this.creatorLabel.Text = "Creator Name";
            this.creatorLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // gameLabel
            // 
            this.gameLabel.AutoSize = true;
            this.gameLabel.Location = new System.Drawing.Point(3, 29);
            this.gameLabel.Name = "gameLabel";
            this.gameLabel.Size = new System.Drawing.Size(66, 13);
            this.gameLabel.TabIndex = 9;
            this.gameLabel.Text = "Game Name";
            this.gameLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // codeLabel
            // 
            this.codeLabel.AutoSize = true;
            this.codeLabel.Location = new System.Drawing.Point(3, 55);
            this.codeLabel.Name = "codeLabel";
            this.codeLabel.Size = new System.Drawing.Size(63, 13);
            this.codeLabel.TabIndex = 10;
            this.codeLabel.Text = "Game Code";
            this.codeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // pianoControl
            // 
            this.pianoControl.Location = new System.Drawing.Point(4, 72);
            this.pianoControl.Name = "pianoControl";
            this.pianoControl.NoteOnColor = System.Drawing.Color.FromArgb(0xA7, 0x44, 0xDD);
            this.pianoControl.Size = new System.Drawing.Size(520, 50);
            this.pianoControl.TabIndex = 12;
            // 
            // trackInfoControl
            // 
            this.trackInfoControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.trackInfoControl.Font = new System.Drawing.Font("Microsoft Tai Le", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.trackInfoControl.Location = new System.Drawing.Point(0, 0);
            this.trackInfoControl.Name = "trackInfoControl";
            this.trackInfoControl.Size = new System.Drawing.Size(525, 690);
            this.trackInfoControl.TabIndex = 13;
            // 
            // imageList1
            // 
            this.imageList1.ColorDepth = System.Windows.Forms.ColorDepth.Depth16Bit;
            this.imageList1.ImageSize = new System.Drawing.Size(64, 64);
            this.imageList1.Images.Add(((System.Drawing.Image)(resources.GetObject("PlaylistIcon"))));
            this.imageList1.Images.Add(((System.Drawing.Image)(resources.GetObject("SongIcon"))));
            this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // songsComboBox
            // 
            this.songsComboBox.Enabled = false;
            this.songsComboBox.ImageList = this.imageList1;
            this.songsComboBox.Indent = 15;
            this.songsComboBox.Location = new System.Drawing.Point(299, 4);
            this.songsComboBox.Name = "songsComboBox";
            this.songsComboBox.Size = new System.Drawing.Size(225, 23);
            // 
            // splitContainer1
            // 
            this.splitContainer1.BackColor = System.Drawing.Color.FromArgb(85, 50, 125);
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainer1.IsSplitterFixed = true;
            this.splitContainer1.Location = new System.Drawing.Point(0, 24);
            this.splitContainer1.Margin = new System.Windows.Forms.Padding(0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.playButton);
            this.splitContainer1.Panel1.Controls.Add(this.creatorLabel);
            this.splitContainer1.Panel1.Controls.Add(this.gameLabel);
            this.splitContainer1.Panel1.Controls.Add(this.codeLabel);
            this.splitContainer1.Panel1.Controls.Add(this.pauseButton);
            this.splitContainer1.Panel1.Controls.Add(this.stopButton);
            this.splitContainer1.Panel1.Controls.Add(this.songNumerical);
            this.splitContainer1.Panel1.Controls.Add(this.songsComboBox);
            this.splitContainer1.Panel1.Controls.Add(this.pianoControl);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.trackInfoControl);
            this.splitContainer1.Size = new System.Drawing.Size(525, 746);
            this.splitContainer1.SplitterDistance = 125;
            this.splitContainer1.SplitterWidth = 1;
            this.splitContainer1.TabIndex = 11;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(528, 825);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.MinimumSize = new System.Drawing.Size(544, 863);
            this.Name = "MainForm";
            this.Text = "GBA Music Studio";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.songNumerical)).EndInit();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.Button playButton;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.NumericUpDown songNumerical;
        private System.Windows.Forms.Button stopButton;
        private System.Windows.Forms.Button pauseButton;
        private System.Windows.Forms.Label creatorLabel;
        private System.Windows.Forms.Label gameLabel;
        private System.Windows.Forms.Label codeLabel;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private Sanford.Multimedia.Midi.UI.PianoControl pianoControl;
        private GBAMusicStudio.UI.TrackInfoControl trackInfoControl;
        private ImageComboBox.ImageComboBox songsComboBox;
        private System.Windows.Forms.ImageList imageList1;
    }
}

