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
            components = new System.ComponentModel.Container();
            headingLabel = new Label();
            statusLabel = new Label();
            dropPanel = new Panel();
            supportedFormatsLabel = new Label();
            dropInstructionLabel = new Label();
            draftCountLabel = new Label();
            draftListView = new ListView();
            draftImageList = new ImageList(components);
            emptyDraftLabel = new Label();
            dropPanel.SuspendLayout();
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
            statusLabel.Text = "PC上の画像をiPhoneへすばやく転送します。";
            //
            // dropPanel
            //
            dropPanel.AllowDrop = true;
            dropPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            dropPanel.BackColor = Color.FromArgb(245, 248, 252);
            dropPanel.BorderStyle = BorderStyle.FixedSingle;
            dropPanel.Controls.Add(supportedFormatsLabel);
            dropPanel.Controls.Add(dropInstructionLabel);
            dropPanel.Location = new Point(36, 136);
            dropPanel.Name = "dropPanel";
            dropPanel.Size = new Size(828, 112);
            dropPanel.TabIndex = 2;
            dropPanel.DragDrop += DropPanel_DragDrop;
            dropPanel.DragEnter += DropPanel_DragEnter;
            //
            // supportedFormatsLabel
            //
            supportedFormatsLabel.Anchor = AnchorStyles.None;
            supportedFormatsLabel.AutoSize = true;
            supportedFormatsLabel.ForeColor = SystemColors.GrayText;
            supportedFormatsLabel.Location = new Point(326, 68);
            supportedFormatsLabel.Name = "supportedFormatsLabel";
            supportedFormatsLabel.Size = new Size(174, 15);
            supportedFormatsLabel.TabIndex = 1;
            supportedFormatsLabel.Text = "JPEG / PNG / WebP・複数選択可";
            //
            // dropInstructionLabel
            //
            dropInstructionLabel.Anchor = AnchorStyles.None;
            dropInstructionLabel.AutoSize = true;
            dropInstructionLabel.Font = new Font("Segoe UI", 14F, FontStyle.Bold, GraphicsUnit.Point);
            dropInstructionLabel.Location = new Point(299, 29);
            dropInstructionLabel.Name = "dropInstructionLabel";
            dropInstructionLabel.Size = new Size(227, 25);
            dropInstructionLabel.TabIndex = 0;
            dropInstructionLabel.Text = "画像をここにドロップ";
            //
            // draftCountLabel
            //
            draftCountLabel.AutoSize = true;
            draftCountLabel.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point);
            draftCountLabel.Location = new Point(36, 276);
            draftCountLabel.Name = "draftCountLabel";
            draftCountLabel.Size = new Size(77, 21);
            draftCountLabel.TabIndex = 3;
            draftCountLabel.Text = "Draft: 0枚";
            //
            // draftListView
            //
            draftListView.AccessibleName = "Draft画像一覧";
            draftListView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            draftListView.BackColor = Color.FromArgb(250, 250, 250);
            draftListView.BorderStyle = BorderStyle.FixedSingle;
            draftListView.LargeImageList = draftImageList;
            draftListView.Location = new Point(36, 310);
            draftListView.MultiSelect = false;
            draftListView.Name = "draftListView";
            draftListView.ShowItemToolTips = true;
            draftListView.Size = new Size(828, 294);
            draftListView.TabIndex = 4;
            draftListView.UseCompatibleStateImageBehavior = false;
            //
            // draftImageList
            //
            draftImageList.ColorDepth = ColorDepth.Depth32Bit;
            draftImageList.ImageSize = new Size(160, 120);
            draftImageList.TransparentColor = Color.Transparent;
            //
            // emptyDraftLabel
            //
            emptyDraftLabel.Anchor = AnchorStyles.None;
            emptyDraftLabel.AutoSize = true;
            emptyDraftLabel.BackColor = Color.FromArgb(250, 250, 250);
            emptyDraftLabel.ForeColor = SystemColors.GrayText;
            emptyDraftLabel.Location = new Point(357, 448);
            emptyDraftLabel.Name = "emptyDraftLabel";
            emptyDraftLabel.Size = new Size(186, 15);
            emptyDraftLabel.TabIndex = 5;
            emptyDraftLabel.Text = "Draftに画像はまだありません。";
            //
            // Form1
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.Window;
            ClientSize = new Size(900, 640);
            Controls.Add(emptyDraftLabel);
            Controls.Add(draftListView);
            Controls.Add(draftCountLabel);
            Controls.Add(dropPanel);
            Controls.Add(statusLabel);
            Controls.Add(headingLabel);
            MinimumSize = new Size(720, 560);
            Name = "mainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "TransferImageQR";
            dropPanel.ResumeLayout(false);
            dropPanel.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
            emptyDraftLabel.BringToFront();
        }

        #endregion

        private Label headingLabel;
        private Label statusLabel;
        private Panel dropPanel;
        private Label supportedFormatsLabel;
        private Label dropInstructionLabel;
        private Label draftCountLabel;
        private ListView draftListView;
        private ImageList draftImageList;
        private Label emptyDraftLabel;
    }
}
