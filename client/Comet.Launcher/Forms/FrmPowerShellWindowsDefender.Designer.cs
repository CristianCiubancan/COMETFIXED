namespace Comet.Launcher.Forms
{
    partial class FrmPowerShellWindowsDefender
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmPowerShellWindowsDefender));
            this.BtnCancel = new System.Windows.Forms.Button();
            this.BtnAccept = new System.Windows.Forms.Button();
            this.LinkKnowMore = new System.Windows.Forms.LinkLabel();
            this.lblMessage = new System.Windows.Forms.Label();
            this.SuspendLayout();
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
            this.BtnCancel.Location = new System.Drawing.Point(286, 153);
            this.BtnCancel.Name = "BtnCancel";
            this.BtnCancel.Size = new System.Drawing.Size(92, 33);
            this.BtnCancel.TabIndex = 0;
            this.BtnCancel.Text = "StrNo";
            this.BtnCancel.UseVisualStyleBackColor = false;
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
            this.BtnAccept.Location = new System.Drawing.Point(188, 153);
            this.BtnAccept.Name = "BtnAccept";
            this.BtnAccept.Size = new System.Drawing.Size(92, 33);
            this.BtnAccept.TabIndex = 0;
            this.BtnAccept.Text = "StrYes";
            this.BtnAccept.UseVisualStyleBackColor = false;
            // 
            // LinkKnowMore
            // 
            this.LinkKnowMore.AutoSize = true;
            this.LinkKnowMore.BackColor = System.Drawing.Color.Transparent;
            this.LinkKnowMore.Location = new System.Drawing.Point(26, 163);
            this.LinkKnowMore.Name = "LinkKnowMore";
            this.LinkKnowMore.Size = new System.Drawing.Size(75, 15);
            this.LinkKnowMore.TabIndex = 1;
            this.LinkKnowMore.TabStop = true;
            this.LinkKnowMore.Text = "StrReadMore";
            // 
            // lblMessage
            // 
            this.lblMessage.BackColor = System.Drawing.Color.Transparent;
            this.lblMessage.Location = new System.Drawing.Point(26, 22);
            this.lblMessage.Name = "lblMessage";
            this.lblMessage.Size = new System.Drawing.Size(335, 128);
            this.lblMessage.TabIndex = 2;
            this.lblMessage.Text = "StrWindowsDefenderAlert";
            // 
            // FrmPowerShellWindowsDefender
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("$this.BackgroundImage")));
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.ClientSize = new System.Drawing.Size(399, 207);
            this.Controls.Add(this.lblMessage);
            this.Controls.Add(this.LinkKnowMore);
            this.Controls.Add(this.BtnAccept);
            this.Controls.Add(this.BtnCancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "FrmPowerShellWindowsDefender";
            this.Text = "FrmPowerShellWindowsDefender";
            this.Load += new System.EventHandler(this.FrmPowerShellWindowsDefender_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Button BtnCancel;
        private Button BtnAccept;
        private LinkLabel LinkKnowMore;
        private Label lblMessage;
    }
}