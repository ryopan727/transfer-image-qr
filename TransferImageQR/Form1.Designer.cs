namespace TransferImageQR
{
    partial class Form1
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
            headingLabel = new Label();
            statusLabel = new Label();
            SuspendLayout();
            //
            // headingLabel
            //
            headingLabel.AutoSize = true;
            headingLabel.Font = new Font("Segoe UI", 24F, FontStyle.Bold, GraphicsUnit.Point);
            headingLabel.Location = new Point(32, 32);
            headingLabel.Name = "headingLabel";
            headingLabel.Size = new Size(258, 45);
            headingLabel.TabIndex = 0;
            headingLabel.Text = "TransferImageQR";
            //
            // statusLabel
            //
            statusLabel.AutoSize = true;
            statusLabel.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);
            statusLabel.ForeColor = SystemColors.GrayText;
            statusLabel.Location = new Point(36, 91);
            statusLabel.Name = "statusLabel";
            statusLabel.Size = new Size(297, 20);
            statusLabel.TabIndex = 1;
            statusLabel.Text = "画像転送機能を追加するための準備ができました。";
            //
            // Form1
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.Window;
            ClientSize = new Size(720, 420);
            Controls.Add(statusLabel);
            Controls.Add(headingLabel);
            MinimumSize = new Size(600, 360);
            Name = "mainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "TransferImageQR";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label headingLabel;
        private Label statusLabel;
    }
}
