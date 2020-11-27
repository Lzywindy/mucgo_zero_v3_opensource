namespace MUCGO_zero_CS
{
    partial class BoardUI
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BoardUI));
            this.BoardPanel = new System.Windows.Forms.PictureBox();
            this.colorDialog = new System.Windows.Forms.ColorDialog();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.BlackCapture = new System.Windows.Forms.ToolStripStatusLabel();
            this.BlackCount = new System.Windows.Forms.ToolStripStatusLabel();
            this.WhiteCapture = new System.Windows.Forms.ToolStripStatusLabel();
            this.WhiteCount = new System.Windows.Forms.ToolStripStatusLabel();
            this.WhoTurn = new System.Windows.Forms.ToolStripStatusLabel();
            this.Steps = new System.Windows.Forms.ToolStripStatusLabel();
            this.ThinkingStatement = new System.Windows.Forms.ToolStripProgressBar();
            this.AdvantageTitle = new System.Windows.Forms.ToolStripStatusLabel();
            this.TuresName = new System.Windows.Forms.ToolStripStatusLabel();
            this.PosLocation = new System.Windows.Forms.ToolStripStatusLabel();
            this.BasicCtrlPanel = new System.Windows.Forms.GroupBox();
            this.NewGame = new System.Windows.Forms.Button();
            this.BackUpBT = new System.Windows.Forms.Button();
            this.Resign = new System.Windows.Forms.Button();
            this.GamePlaySelect = new System.Windows.Forms.GroupBox();
            this.CBoxModelSel = new System.Windows.Forms.ComboBox();
            this.ModelSelectCBox = new System.Windows.Forms.ComboBox();
            this.PlayCheck = new System.Windows.Forms.CheckBox();
            this.TerrainCheck = new System.Windows.Forms.CheckBox();
            this.EnabledPosCheck = new System.Windows.Forms.CheckBox();
            this.OpenFile = new System.Windows.Forms.Button();
            this.SaveFile = new System.Windows.Forms.Button();
            this.Quit = new System.Windows.Forms.Button();
            this.UpdateTimer = new System.Windows.Forms.Timer(this.components);
            this.openFileDialogOldNN = new System.Windows.Forms.OpenFileDialog();
            this.ThinkingWorker = new System.ComponentModel.BackgroundWorker();
            ((System.ComponentModel.ISupportInitialize)(this.BoardPanel)).BeginInit();
            this.statusStrip.SuspendLayout();
            this.BasicCtrlPanel.SuspendLayout();
            this.GamePlaySelect.SuspendLayout();
            this.SuspendLayout();
            // 
            // BoardPanel
            // 
            this.BoardPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(0)))));
            this.BoardPanel.Location = new System.Drawing.Point(0, -2);
            this.BoardPanel.Name = "BoardPanel";
            this.BoardPanel.Size = new System.Drawing.Size(714, 673);
            this.BoardPanel.TabIndex = 0;
            this.BoardPanel.TabStop = false;
            // 
            // statusStrip
            // 
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.BlackCapture,
            this.BlackCount,
            this.WhiteCapture,
            this.WhiteCount,
            this.WhoTurn,
            this.Steps,
            this.ThinkingStatement,
            this.AdvantageTitle,
            this.TuresName,
            this.PosLocation});
            this.statusStrip.Location = new System.Drawing.Point(0, 674);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(862, 26);
            this.statusStrip.TabIndex = 23;
            this.statusStrip.Text = "statusStrip1";
            // 
            // BlackCapture
            // 
            this.BlackCapture.ActiveLinkColor = System.Drawing.Color.White;
            this.BlackCapture.AutoSize = false;
            this.BlackCapture.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.BlackCapture.BorderStyle = System.Windows.Forms.Border3DStyle.RaisedInner;
            this.BlackCapture.Name = "BlackCapture";
            this.BlackCapture.Size = new System.Drawing.Size(30, 21);
            this.BlackCapture.Text = "    ";
            // 
            // BlackCount
            // 
            this.BlackCount.AutoSize = false;
            this.BlackCount.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right)));
            this.BlackCount.BorderStyle = System.Windows.Forms.Border3DStyle.RaisedOuter;
            this.BlackCount.Name = "BlackCount";
            this.BlackCount.Size = new System.Drawing.Size(25, 21);
            this.BlackCount.Text = "0";
            // 
            // WhiteCapture
            // 
            this.WhiteCapture.AutoSize = false;
            this.WhiteCapture.BackColor = System.Drawing.SystemColors.HighlightText;
            this.WhiteCapture.BorderStyle = System.Windows.Forms.Border3DStyle.SunkenInner;
            this.WhiteCapture.Name = "WhiteCapture";
            this.WhiteCapture.Size = new System.Drawing.Size(30, 21);
            this.WhiteCapture.Text = "    ";
            // 
            // WhiteCount
            // 
            this.WhiteCount.AutoSize = false;
            this.WhiteCount.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right)));
            this.WhiteCount.BorderStyle = System.Windows.Forms.Border3DStyle.RaisedOuter;
            this.WhiteCount.Name = "WhiteCount";
            this.WhiteCount.Size = new System.Drawing.Size(25, 21);
            this.WhiteCount.Text = "0";
            // 
            // WhoTurn
            // 
            this.WhoTurn.AutoSize = false;
            this.WhoTurn.BackColor = System.Drawing.SystemColors.Desktop;
            this.WhoTurn.Name = "WhoTurn";
            this.WhoTurn.Size = new System.Drawing.Size(30, 21);
            // 
            // Steps
            // 
            this.Steps.AutoSize = false;
            this.Steps.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right)));
            this.Steps.BorderStyle = System.Windows.Forms.Border3DStyle.RaisedOuter;
            this.Steps.Name = "Steps";
            this.Steps.Size = new System.Drawing.Size(45, 21);
            this.Steps.Text = "1";
            // 
            // ThinkingStatement
            // 
            this.ThinkingStatement.Name = "ThinkingStatement";
            this.ThinkingStatement.Size = new System.Drawing.Size(100, 20);
            // 
            // AdvantageTitle
            // 
            this.AdvantageTitle.AutoSize = false;
            this.AdvantageTitle.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right)));
            this.AdvantageTitle.BorderStyle = System.Windows.Forms.Border3DStyle.RaisedOuter;
            this.AdvantageTitle.Name = "AdvantageTitle";
            this.AdvantageTitle.Size = new System.Drawing.Size(150, 21);
            // 
            // TuresName
            // 
            this.TuresName.AutoSize = false;
            this.TuresName.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right)));
            this.TuresName.BorderStyle = System.Windows.Forms.Border3DStyle.RaisedOuter;
            this.TuresName.Name = "TuresName";
            this.TuresName.Size = new System.Drawing.Size(131, 21);
            // 
            // PosLocation
            // 
            this.PosLocation.AutoSize = false;
            this.PosLocation.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right)));
            this.PosLocation.BorderStyle = System.Windows.Forms.Border3DStyle.RaisedOuter;
            this.PosLocation.Name = "PosLocation";
            this.PosLocation.Size = new System.Drawing.Size(120, 21);
            // 
            // BasicCtrlPanel
            // 
            this.BasicCtrlPanel.Controls.Add(this.NewGame);
            this.BasicCtrlPanel.Controls.Add(this.BackUpBT);
            this.BasicCtrlPanel.Controls.Add(this.Resign);
            this.BasicCtrlPanel.Location = new System.Drawing.Point(720, 473);
            this.BasicCtrlPanel.Name = "BasicCtrlPanel";
            this.BasicCtrlPanel.Size = new System.Drawing.Size(135, 154);
            this.BasicCtrlPanel.TabIndex = 32;
            this.BasicCtrlPanel.TabStop = false;
            this.BasicCtrlPanel.Text = "基础控制";
            // 
            // NewGame
            // 
            this.NewGame.Location = new System.Drawing.Point(28, 20);
            this.NewGame.Name = "NewGame";
            this.NewGame.Size = new System.Drawing.Size(84, 38);
            this.NewGame.TabIndex = 16;
            this.NewGame.Text = "新    局";
            this.NewGame.UseVisualStyleBackColor = true;
            // 
            // BackUpBT
            // 
            this.BackUpBT.Location = new System.Drawing.Point(28, 64);
            this.BackUpBT.Name = "BackUpBT";
            this.BackUpBT.Size = new System.Drawing.Size(84, 38);
            this.BackUpBT.TabIndex = 15;
            this.BackUpBT.Text = "悔    棋";
            this.BackUpBT.UseVisualStyleBackColor = true;
            // 
            // Resign
            // 
            this.Resign.Location = new System.Drawing.Point(28, 108);
            this.Resign.Name = "Resign";
            this.Resign.Size = new System.Drawing.Size(84, 38);
            this.Resign.TabIndex = 24;
            this.Resign.Text = "认    输";
            this.Resign.UseVisualStyleBackColor = true;
            this.Resign.Click += new System.EventHandler(this.Resign_Click);
            // 
            // GamePlaySelect
            // 
            this.GamePlaySelect.Controls.Add(this.CBoxModelSel);
            this.GamePlaySelect.Controls.Add(this.ModelSelectCBox);
            this.GamePlaySelect.Controls.Add(this.PlayCheck);
            this.GamePlaySelect.Controls.Add(this.TerrainCheck);
            this.GamePlaySelect.Controls.Add(this.EnabledPosCheck);
            this.GamePlaySelect.Location = new System.Drawing.Point(721, 327);
            this.GamePlaySelect.Name = "GamePlaySelect";
            this.GamePlaySelect.Size = new System.Drawing.Size(134, 140);
            this.GamePlaySelect.TabIndex = 31;
            this.GamePlaySelect.TabStop = false;
            this.GamePlaySelect.Text = "游戏选项";
            // 
            // CBoxModelSel
            // 
            this.CBoxModelSel.DisplayMember = "string";
            this.CBoxModelSel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.CBoxModelSel.FormattingEnabled = true;
            this.CBoxModelSel.Items.AddRange(new object[] {
            "人机对弈",
            "自对弈训练"});
            this.CBoxModelSel.Location = new System.Drawing.Point(7, 20);
            this.CBoxModelSel.Name = "CBoxModelSel";
            this.CBoxModelSel.Size = new System.Drawing.Size(121, 20);
            this.CBoxModelSel.TabIndex = 30;
            this.CBoxModelSel.ValueMember = "string";
            // 
            // ModelSelectCBox
            // 
            this.ModelSelectCBox.DisplayMember = "string";
            this.ModelSelectCBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ModelSelectCBox.FormattingEnabled = true;
            this.ModelSelectCBox.Location = new System.Drawing.Point(7, 46);
            this.ModelSelectCBox.Name = "ModelSelectCBox";
            this.ModelSelectCBox.Size = new System.Drawing.Size(121, 20);
            this.ModelSelectCBox.TabIndex = 0;
            this.ModelSelectCBox.ValueMember = "string";
            // 
            // PlayCheck
            // 
            this.PlayCheck.AutoSize = true;
            this.PlayCheck.Location = new System.Drawing.Point(7, 72);
            this.PlayCheck.Name = "PlayCheck";
            this.PlayCheck.Size = new System.Drawing.Size(72, 16);
            this.PlayCheck.TabIndex = 29;
            this.PlayCheck.Text = "准备对弈";
            this.PlayCheck.UseVisualStyleBackColor = true;
            // 
            // TerrainCheck
            // 
            this.TerrainCheck.AutoSize = true;
            this.TerrainCheck.Location = new System.Drawing.Point(7, 94);
            this.TerrainCheck.Name = "TerrainCheck";
            this.TerrainCheck.Size = new System.Drawing.Size(72, 16);
            this.TerrainCheck.TabIndex = 26;
            this.TerrainCheck.Text = "区域查看";
            this.TerrainCheck.UseVisualStyleBackColor = true;
            // 
            // EnabledPosCheck
            // 
            this.EnabledPosCheck.AutoSize = true;
            this.EnabledPosCheck.Location = new System.Drawing.Point(7, 116);
            this.EnabledPosCheck.Name = "EnabledPosCheck";
            this.EnabledPosCheck.Size = new System.Drawing.Size(96, 16);
            this.EnabledPosCheck.TabIndex = 25;
            this.EnabledPosCheck.Text = "可着子点查看";
            this.EnabledPosCheck.UseVisualStyleBackColor = true;
            this.EnabledPosCheck.CheckedChanged += new System.EventHandler(this.EnabledPosCheck_CheckedChanged);
            // 
            // OpenFile
            // 
            this.OpenFile.Location = new System.Drawing.Point(748, 12);
            this.OpenFile.Name = "OpenFile";
            this.OpenFile.Size = new System.Drawing.Size(84, 38);
            this.OpenFile.TabIndex = 17;
            this.OpenFile.Text = "打    开";
            this.OpenFile.UseVisualStyleBackColor = true;
            // 
            // SaveFile
            // 
            this.SaveFile.Location = new System.Drawing.Point(748, 56);
            this.SaveFile.Name = "SaveFile";
            this.SaveFile.Size = new System.Drawing.Size(84, 38);
            this.SaveFile.TabIndex = 18;
            this.SaveFile.Text = "保    存";
            this.SaveFile.UseVisualStyleBackColor = true;
            // 
            // Quit
            // 
            this.Quit.Location = new System.Drawing.Point(748, 100);
            this.Quit.Name = "Quit";
            this.Quit.Size = new System.Drawing.Size(84, 38);
            this.Quit.TabIndex = 19;
            this.Quit.Text = "退  出";
            this.Quit.UseVisualStyleBackColor = true;
            // 
            // openFileDialogOldNN
            // 
            this.openFileDialogOldNN.FileName = "加载旧神经网络";
            // 
            // BoardUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(862, 700);
            this.ControlBox = false;
            this.Controls.Add(this.BasicCtrlPanel);
            this.Controls.Add(this.GamePlaySelect);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.BoardPanel);
            this.Controls.Add(this.Quit);
            this.Controls.Add(this.SaveFile);
            this.Controls.Add(this.OpenFile);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "BoardUI";
            this.Text = "MUCGO Zero";
            ((System.ComponentModel.ISupportInitialize)(this.BoardPanel)).EndInit();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.BasicCtrlPanel.ResumeLayout(false);
            this.GamePlaySelect.ResumeLayout(false);
            this.GamePlaySelect.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        #endregion
        private System.Windows.Forms.PictureBox BoardPanel;
        private System.Windows.Forms.ColorDialog colorDialog;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel BlackCapture;
        private System.Windows.Forms.ToolStripStatusLabel BlackCount;
        private System.Windows.Forms.ToolStripStatusLabel WhiteCapture;
        private System.Windows.Forms.ToolStripStatusLabel WhiteCount;
        private System.Windows.Forms.ToolStripProgressBar ThinkingStatement;
        private System.Windows.Forms.ToolStripStatusLabel WhoTurn;
        private System.Windows.Forms.ToolStripStatusLabel Steps;
        private System.Windows.Forms.ToolStripStatusLabel AdvantageTitle;
        private System.Windows.Forms.Button OpenFile;
        private System.Windows.Forms.CheckBox TerrainCheck;
        private System.Windows.Forms.Button SaveFile;
        private System.Windows.Forms.CheckBox EnabledPosCheck;
        private System.Windows.Forms.Button Quit;
        private System.Windows.Forms.Button Resign;
        private System.Windows.Forms.Button BackUpBT;
        private System.Windows.Forms.Button NewGame;
        private System.Windows.Forms.CheckBox PlayCheck;
        private System.Windows.Forms.Timer UpdateTimer;
        private System.Windows.Forms.GroupBox GamePlaySelect;
        private System.Windows.Forms.ComboBox ModelSelectCBox;
        private System.Windows.Forms.ToolStripStatusLabel TuresName;
        private System.Windows.Forms.GroupBox BasicCtrlPanel;
        private System.Windows.Forms.OpenFileDialog openFileDialogOldNN;
        private System.Windows.Forms.ToolStripStatusLabel PosLocation;
        private System.ComponentModel.BackgroundWorker ThinkingWorker;
        private System.Windows.Forms.ComboBox CBoxModelSel;
    }
}