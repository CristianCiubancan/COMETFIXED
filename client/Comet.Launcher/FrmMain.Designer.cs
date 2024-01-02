using Comet.Launcher.Controls;

namespace Comet.Launcher
{
    partial class FrmMain
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmMain));
            this.TitlePanel = new System.Windows.Forms.Panel();
            this.MinimizeButton = new System.Windows.Forms.Button();
            this.LabelVersion = new System.Windows.Forms.Label();
            this.CloseButton = new System.Windows.Forms.Button();
            this.TitleLabel = new System.Windows.Forms.Label();
            this.BtnPlay = new System.Windows.Forms.Button();
            this.GeneralProgressBar = new Comet.Launcher.Controls.ColorProgressBar();
            this.PanelReadyToPlay = new System.Windows.Forms.Panel();
            this.LabelReadyProgress = new System.Windows.Forms.Label();
            this.PanelProgressDoingSomething = new System.Windows.Forms.Panel();
            this.LabelProgressStatus = new System.Windows.Forms.Label();
            this.NotifyBar = new System.Windows.Forms.NotifyIcon(this.components);
            this.LblPingDisplay = new System.Windows.Forms.Label();
            this.SettingsButton = new System.Windows.Forms.Button();
            this.TitlePanel.SuspendLayout();
            this.PanelReadyToPlay.SuspendLayout();
            this.PanelProgressDoingSomething.SuspendLayout();
            this.SuspendLayout();
            // 
            // TitlePanel
            // 
            this.TitlePanel.BackColor = System.Drawing.Color.Transparent;
            this.TitlePanel.Controls.Add(this.MinimizeButton);
            this.TitlePanel.Controls.Add(this.LabelVersion);
            this.TitlePanel.Controls.Add(this.CloseButton);
            this.TitlePanel.Controls.Add(this.TitleLabel);
            this.TitlePanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.TitlePanel.Location = new System.Drawing.Point(0, 0);
            this.TitlePanel.Name = "TitlePanel";
            this.TitlePanel.Size = new System.Drawing.Size(1000, 30);
            this.TitlePanel.TabIndex = 0;
            this.TitlePanel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.TitlePanel_MouseDown);
            this.TitlePanel.MouseMove += new System.Windows.Forms.MouseEventHandler(this.TitlePanel_MouseMove);
            // 
            // MinimizeButton
            // 
            this.MinimizeButton.BackColor = System.Drawing.Color.Transparent;
            this.MinimizeButton.FlatAppearance.BorderSize = 0;
            this.MinimizeButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.MinimizeButton.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.MinimizeButton.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.MinimizeButton.Location = new System.Drawing.Point(942, 0);
            this.MinimizeButton.Name = "MinimizeButton";
            this.MinimizeButton.Size = new System.Drawing.Size(30, 30);
            this.MinimizeButton.TabIndex = 10;
            this.MinimizeButton.Text = "-";
            this.MinimizeButton.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.MinimizeButton.UseVisualStyleBackColor = false;
            this.MinimizeButton.Click += new System.EventHandler(this.MinimizeButton_Click);
            // 
            // LabelVersion
            // 
            this.LabelVersion.AutoSize = true;
            this.LabelVersion.BackColor = System.Drawing.Color.Transparent;
            this.LabelVersion.Font = new System.Drawing.Font("Segoe UI", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.LabelVersion.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.LabelVersion.Location = new System.Drawing.Point(671, 4);
            this.LabelVersion.Name = "LabelVersion";
            this.LabelVersion.Size = new System.Drawing.Size(56, 20);
            this.LabelVersion.TabIndex = 7;
            this.LabelVersion.Text = "Versão:";
            // 
            // CloseButton
            // 
            this.CloseButton.BackColor = System.Drawing.Color.Transparent;
            this.CloseButton.FlatAppearance.BorderSize = 0;
            this.CloseButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.CloseButton.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.CloseButton.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.CloseButton.Location = new System.Drawing.Point(970, 0);
            this.CloseButton.Name = "CloseButton";
            this.CloseButton.Size = new System.Drawing.Size(30, 30);
            this.CloseButton.TabIndex = 9;
            this.CloseButton.Text = "X";
            this.CloseButton.UseVisualStyleBackColor = false;
            this.CloseButton.Click += new System.EventHandler(this.CloseButton_Click);
            // 
            // TitleLabel
            // 
            this.TitleLabel.Dock = System.Windows.Forms.DockStyle.Left;
            this.TitleLabel.Font = new System.Drawing.Font("Segoe UI Semibold", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.TitleLabel.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.TitleLabel.Location = new System.Drawing.Point(0, 0);
            this.TitleLabel.Name = "TitleLabel";
            this.TitleLabel.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.TitleLabel.Size = new System.Drawing.Size(665, 30);
            this.TitleLabel.TabIndex = 0;
            this.TitleLabel.Text = "%TITLE%";
            this.TitleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.TitleLabel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.TitlePanel_MouseDown);
            this.TitleLabel.MouseMove += new System.Windows.Forms.MouseEventHandler(this.TitlePanel_MouseMove);
            // 
            // BtnPlay
            // 
            this.BtnPlay.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("BtnPlay.BackgroundImage")));
            this.BtnPlay.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.BtnPlay.Enabled = false;
            this.BtnPlay.FlatAppearance.BorderSize = 0;
            this.BtnPlay.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.BtnPlay.Location = new System.Drawing.Point(796, 14);
            this.BtnPlay.Name = "BtnPlay";
            this.BtnPlay.Size = new System.Drawing.Size(192, 74);
            this.BtnPlay.TabIndex = 1;
            this.BtnPlay.UseVisualStyleBackColor = true;
            this.BtnPlay.Visible = false;
            this.BtnPlay.Click += new System.EventHandler(this.BtnPlay_Click);
            this.BtnPlay.MouseDown += new System.Windows.Forms.MouseEventHandler(this.BtnPlay_MouseDown);
            this.BtnPlay.MouseEnter += new System.EventHandler(this.BtnPlay_MouseEnter);
            this.BtnPlay.MouseLeave += new System.EventHandler(this.BtnPlay_MouseLeave);
            this.BtnPlay.MouseUp += new System.Windows.Forms.MouseEventHandler(this.BtnPlay_MouseUp);
            // 
            // GeneralProgressBar
            // 
            this.GeneralProgressBar.BarColor = System.Drawing.Color.FromArgb(((int)(((byte)(210)))), ((int)(((byte)(148)))), ((int)(((byte)(75)))));
            this.GeneralProgressBar.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(243)))), ((int)(((byte)(205)))), ((int)(((byte)(143)))));
            this.GeneralProgressBar.FillStyle = Comet.Launcher.Controls.ColorProgressBar.FillStyles.Solid;
            this.GeneralProgressBar.Location = new System.Drawing.Point(12, 41);
            this.GeneralProgressBar.Maximum = ((long)(100));
            this.GeneralProgressBar.Minimum = ((long)(0));
            this.GeneralProgressBar.Name = "GeneralProgressBar";
            this.GeneralProgressBar.Size = new System.Drawing.Size(976, 20);
            this.GeneralProgressBar.Step = ((long)(10));
            this.GeneralProgressBar.TabIndex = 4;
            this.GeneralProgressBar.Value = ((long)(36));
            // 
            // PanelReadyToPlay
            // 
            this.PanelReadyToPlay.BackColor = System.Drawing.Color.Transparent;
            this.PanelReadyToPlay.Controls.Add(this.LabelReadyProgress);
            this.PanelReadyToPlay.Controls.Add(this.BtnPlay);
            this.PanelReadyToPlay.Location = new System.Drawing.Point(0, 500);
            this.PanelReadyToPlay.Name = "PanelReadyToPlay";
            this.PanelReadyToPlay.Size = new System.Drawing.Size(1000, 100);
            this.PanelReadyToPlay.TabIndex = 5;
            this.PanelReadyToPlay.Visible = false;
            // 
            // LabelReadyProgress
            // 
            this.LabelReadyProgress.Dock = System.Windows.Forms.DockStyle.Left;
            this.LabelReadyProgress.Font = new System.Drawing.Font("Segoe UI Semibold", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.LabelReadyProgress.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.LabelReadyProgress.Location = new System.Drawing.Point(0, 0);
            this.LabelReadyProgress.Name = "LabelReadyProgress";
            this.LabelReadyProgress.Size = new System.Drawing.Size(774, 100);
            this.LabelReadyProgress.TabIndex = 2;
            this.LabelReadyProgress.Text = "Status Message";
            this.LabelReadyProgress.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // PanelProgressDoingSomething
            // 
            this.PanelProgressDoingSomething.BackColor = System.Drawing.Color.Transparent;
            this.PanelProgressDoingSomething.Controls.Add(this.LabelProgressStatus);
            this.PanelProgressDoingSomething.Controls.Add(this.GeneralProgressBar);
            this.PanelProgressDoingSomething.Location = new System.Drawing.Point(0, 500);
            this.PanelProgressDoingSomething.Name = "PanelProgressDoingSomething";
            this.PanelProgressDoingSomething.Size = new System.Drawing.Size(1000, 100);
            this.PanelProgressDoingSomething.TabIndex = 6;
            this.PanelProgressDoingSomething.Visible = false;
            // 
            // LabelProgressStatus
            // 
            this.LabelProgressStatus.AutoSize = true;
            this.LabelProgressStatus.Font = new System.Drawing.Font("Segoe UI", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.LabelProgressStatus.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.LabelProgressStatus.Location = new System.Drawing.Point(12, 18);
            this.LabelProgressStatus.Name = "LabelProgressStatus";
            this.LabelProgressStatus.Size = new System.Drawing.Size(45, 20);
            this.LabelProgressStatus.TabIndex = 5;
            this.LabelProgressStatus.Text = "Mock";
            // 
            // NotifyBar
            // 
            this.NotifyBar.BalloonTipText = "Comet is keep your client safe.";
            this.NotifyBar.BalloonTipTitle = "Comet - Updater";
            this.NotifyBar.Icon = ((System.Drawing.Icon)(resources.GetObject("NotifyBar.Icon")));
            this.NotifyBar.Text = "Comet - Updater";
            this.NotifyBar.Visible = true;
            this.NotifyBar.BalloonTipClicked += new System.EventHandler(this.NotifyBar_BalloonTipClicked);
            this.NotifyBar.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.NotifyBar_MouseDoubleClick);
            // 
            // LblPingDisplay
            // 
            this.LblPingDisplay.AutoSize = true;
            this.LblPingDisplay.BackColor = System.Drawing.Color.Transparent;
            this.LblPingDisplay.Font = new System.Drawing.Font("Segoe UI Semibold", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.LblPingDisplay.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.LblPingDisplay.Location = new System.Drawing.Point(12, 477);
            this.LblPingDisplay.Name = "LblPingDisplay";
            this.LblPingDisplay.Size = new System.Drawing.Size(69, 20);
            this.LblPingDisplay.TabIndex = 7;
            this.LblPingDisplay.Text = "Ping: ???";
            // 
            // SettingsButton
            // 
            this.SettingsButton.BackColor = System.Drawing.Color.Transparent;
            this.SettingsButton.BackgroundImage = global::Comet.Launcher.Properties.Resources.Settings;
            this.SettingsButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.SettingsButton.FlatAppearance.BorderSize = 0;
            this.SettingsButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.SettingsButton.Location = new System.Drawing.Point(924, 430);
            this.SettingsButton.Name = "SettingsButton";
            this.SettingsButton.Size = new System.Drawing.Size(64, 64);
            this.SettingsButton.TabIndex = 8;
            this.SettingsButton.UseVisualStyleBackColor = false;
            this.SettingsButton.Click += new System.EventHandler(this.BtnSettings_Click);
            // 
            // FrmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("$this.BackgroundImage")));
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.ClientSize = new System.Drawing.Size(1000, 600);
            this.ControlBox = false;
            this.Controls.Add(this.SettingsButton);
            this.Controls.Add(this.LblPingDisplay);
            this.Controls.Add(this.PanelProgressDoingSomething);
            this.Controls.Add(this.PanelReadyToPlay);
            this.Controls.Add(this.TitlePanel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "FrmMain";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "%TITLE%";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FrmMain_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.FrmMain_FormClosed);
            this.Load += new System.EventHandler(this.FrmMain_Load);
            this.LocationChanged += new System.EventHandler(this.FrmMain_LocationChanged);
            this.TitlePanel.ResumeLayout(false);
            this.TitlePanel.PerformLayout();
            this.PanelReadyToPlay.ResumeLayout(false);
            this.PanelProgressDoingSomething.ResumeLayout(false);
            this.PanelProgressDoingSomething.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Panel TitlePanel;
        private Label TitleLabel;
        private Button CloseButton;
        private Button BtnPlay;
        private ColorProgressBar GeneralProgressBar;
        private Panel PanelReadyToPlay;
        private Label LabelReadyProgress;
        private Panel PanelProgressDoingSomething;
        private Label LabelProgressStatus;
        private Label LabelVersion;
        private NotifyIcon NotifyBar;
        private Button MinimizeButton;
        private Label LblPingDisplay;
        private Button SettingsButton;
    }
}