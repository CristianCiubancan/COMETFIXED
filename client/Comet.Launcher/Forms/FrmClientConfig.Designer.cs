namespace Comet.Launcher.Forms
{
    partial class FrmClientConfig
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmClientConfig));
            this.BtnAccept = new System.Windows.Forms.Button();
            this.BtnCancel = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.NumScreenWidth = new System.Windows.Forms.NumericUpDown();
            this.NumScreenHeight = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.NumFramesPerSecond = new System.Windows.Forms.NumericUpDown();
            this.ChkChooseScreenSize = new System.Windows.Forms.CheckBox();
            this.CmbResolution = new System.Windows.Forms.ComboBox();
            ((System.ComponentModel.ISupportInitialize)(this.NumScreenWidth)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.NumScreenHeight)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.NumFramesPerSecond)).BeginInit();
            this.SuspendLayout();
            // 
            // BtnAccept
            // 
            this.BtnAccept.BackColor = System.Drawing.Color.Transparent;
            this.BtnAccept.BackgroundImage = global::Comet.Launcher.Properties.Resources.More;
            this.BtnAccept.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.BtnAccept.FlatAppearance.BorderSize = 0;
            this.BtnAccept.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.BtnAccept.Font = new System.Drawing.Font("Segoe UI", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.BtnAccept.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.BtnAccept.Location = new System.Drawing.Point(197, 162);
            this.BtnAccept.Name = "BtnAccept";
            this.BtnAccept.Size = new System.Drawing.Size(92, 33);
            this.BtnAccept.TabIndex = 1;
            this.BtnAccept.Text = "StrSave";
            this.BtnAccept.UseVisualStyleBackColor = false;
            this.BtnAccept.Click += new System.EventHandler(this.BtnAccept_Click);
            // 
            // BtnCancel
            // 
            this.BtnCancel.BackColor = System.Drawing.Color.Transparent;
            this.BtnCancel.BackgroundImage = global::Comet.Launcher.Properties.Resources.More;
            this.BtnCancel.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.BtnCancel.FlatAppearance.BorderSize = 0;
            this.BtnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.BtnCancel.Font = new System.Drawing.Font("Segoe UI", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.BtnCancel.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.BtnCancel.Location = new System.Drawing.Point(295, 162);
            this.BtnCancel.Name = "BtnCancel";
            this.BtnCancel.Size = new System.Drawing.Size(92, 33);
            this.BtnCancel.TabIndex = 2;
            this.BtnCancel.Text = "StrCancel";
            this.BtnCancel.UseVisualStyleBackColor = false;
            this.BtnCancel.Click += new System.EventHandler(this.BtnCancel_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.Location = new System.Drawing.Point(29, 32);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(76, 15);
            this.label1.TabIndex = 3;
            this.label1.Text = "StrScreenSize";
            // 
            // NumScreenWidth
            // 
            this.NumScreenWidth.Enabled = false;
            this.NumScreenWidth.Location = new System.Drawing.Point(156, 30);
            this.NumScreenWidth.Maximum = new decimal(new int[] {
            1024,
            0,
            0,
            0});
            this.NumScreenWidth.Minimum = new decimal(new int[] {
            800,
            0,
            0,
            0});
            this.NumScreenWidth.Name = "NumScreenWidth";
            this.NumScreenWidth.Size = new System.Drawing.Size(68, 23);
            this.NumScreenWidth.TabIndex = 4;
            this.NumScreenWidth.Value = new decimal(new int[] {
            1024,
            0,
            0,
            0});
            // 
            // NumScreenHeight
            // 
            this.NumScreenHeight.Enabled = false;
            this.NumScreenHeight.Location = new System.Drawing.Point(230, 30);
            this.NumScreenHeight.Maximum = new decimal(new int[] {
            768,
            0,
            0,
            0});
            this.NumScreenHeight.Minimum = new decimal(new int[] {
            600,
            0,
            0,
            0});
            this.NumScreenHeight.Name = "NumScreenHeight";
            this.NumScreenHeight.Size = new System.Drawing.Size(68, 23);
            this.NumScreenHeight.TabIndex = 4;
            this.NumScreenHeight.Value = new decimal(new int[] {
            768,
            0,
            0,
            0});
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.BackColor = System.Drawing.Color.Transparent;
            this.label2.Location = new System.Drawing.Point(29, 88);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(115, 15);
            this.label2.TabIndex = 3;
            this.label2.Text = "StrFramesPerSecond";
            // 
            // NumFramesPerSecond
            // 
            this.NumFramesPerSecond.Location = new System.Drawing.Point(156, 86);
            this.NumFramesPerSecond.Maximum = new decimal(new int[] {
            144,
            0,
            0,
            0});
            this.NumFramesPerSecond.Minimum = new decimal(new int[] {
            40,
            0,
            0,
            0});
            this.NumFramesPerSecond.Name = "NumFramesPerSecond";
            this.NumFramesPerSecond.Size = new System.Drawing.Size(68, 23);
            this.NumFramesPerSecond.TabIndex = 4;
            this.NumFramesPerSecond.Value = new decimal(new int[] {
            40,
            0,
            0,
            0});
            // 
            // ChkChooseScreenSize
            // 
            this.ChkChooseScreenSize.AutoSize = true;
            this.ChkChooseScreenSize.BackColor = System.Drawing.Color.Transparent;
            this.ChkChooseScreenSize.Location = new System.Drawing.Point(129, 61);
            this.ChkChooseScreenSize.Name = "ChkChooseScreenSize";
            this.ChkChooseScreenSize.Size = new System.Drawing.Size(15, 14);
            this.ChkChooseScreenSize.TabIndex = 5;
            this.ChkChooseScreenSize.UseVisualStyleBackColor = false;
            this.ChkChooseScreenSize.CheckedChanged += new System.EventHandler(this.ChkChooseScreenSize_CheckedChanged);
            // 
            // CmbResolution
            // 
            this.CmbResolution.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.CmbResolution.Enabled = false;
            this.CmbResolution.FormattingEnabled = true;
            this.CmbResolution.Location = new System.Drawing.Point(156, 57);
            this.CmbResolution.Name = "CmbResolution";
            this.CmbResolution.Size = new System.Drawing.Size(142, 23);
            this.CmbResolution.TabIndex = 6;
            // 
            // FrmClientConfig
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("$this.BackgroundImage")));
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.ClientSize = new System.Drawing.Size(399, 207);
            this.Controls.Add(this.CmbResolution);
            this.Controls.Add(this.ChkChooseScreenSize);
            this.Controls.Add(this.NumScreenHeight);
            this.Controls.Add(this.NumFramesPerSecond);
            this.Controls.Add(this.NumScreenWidth);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.BtnAccept);
            this.Controls.Add(this.BtnCancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "FrmClientConfig";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Settings";
            this.Load += new System.EventHandler(this.FrmClientConfig_Load);
            ((System.ComponentModel.ISupportInitialize)(this.NumScreenWidth)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.NumScreenHeight)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.NumFramesPerSecond)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Button BtnCancel;
        private Button BtnAccept;
        private Label label1;
        private NumericUpDown NumScreenWidth;
        private NumericUpDown NumScreenHeight;
        private Label label2;
        private NumericUpDown NumFramesPerSecond;
        private CheckBox ChkChooseScreenSize;
        private ComboBox CmbResolution;
    }
}