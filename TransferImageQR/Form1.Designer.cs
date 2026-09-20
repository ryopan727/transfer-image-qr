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
            if (disposing)
            {
                _backgroundImage?.Dispose();
                _backgroundImage = null;
                draftListView?.BackgroundImage?.Dispose();
                if (draftListView is not null)
                {
                    draftListView.BackgroundImage = null;
                }
                qrPictureBox?.Image?.Dispose();
                components?.Dispose();
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
            lanAddressLabel = new Label();
            lanAddressComboBox = new ComboBox();
            dropPanel = new Panel();
            supportedFormatsLabel = new Label();
            dropInstructionLabel = new Label();
            draftCountLabel = new Label();
            draftListView = new ListView();
            draftImageList = new ImageList(components);
            emptyDraftLabel = new Label();
            rejectionTitleLabel = new Label();
            rejectionListBox = new ListBox();
            removeDraftImageButton = new Button();
            clearDraftButton = new Button();
            createQrButton = new Button();
            sessionStateLabel = new Label();
            newTransferButton = new Button();
            sessionStateTimer = new System.Windows.Forms.Timer(components);
            qrPanel = new Panel();
            qrInstructionLabel = new Label();
            transferUrlTextBox = new TextBox();
            qrStatusLabel = new Label();
            qrPictureBox = new PictureBox();
            dropPanel.SuspendLayout();
            qrPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)qrPictureBox).BeginInit();
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
            // lanAddressLabel
            //
            lanAddressLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lanAddressLabel.AutoSize = true;
            lanAddressLabel.Location = new Point(512, 94);
            lanAddressLabel.Name = "lanAddressLabel";
            lanAddressLabel.Size = new Size(125, 15);
            lanAddressLabel.TabIndex = 2;
            lanAddressLabel.Text = "転送に使うLANアドレス";
            //
            // lanAddressComboBox
            //
            lanAddressComboBox.AccessibleName = "転送に使うLANアドレス";
            lanAddressComboBox.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lanAddressComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            lanAddressComboBox.Enabled = false;
            lanAddressComboBox.FormattingEnabled = true;
            lanAddressComboBox.Location = new Point(643, 90);
            lanAddressComboBox.Name = "lanAddressComboBox";
            lanAddressComboBox.Size = new Size(221, 23);
            lanAddressComboBox.TabIndex = 3;
            lanAddressComboBox.SelectedIndexChanged += LanAddressComboBox_SelectedIndexChanged;
            //
            // sessionStateLabel
            //
            sessionStateLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            sessionStateLabel.AutoSize = false;
            sessionStateLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            sessionStateLabel.Location = new Point(536, 48);
            sessionStateLabel.Name = "sessionStateLabel";
            sessionStateLabel.Size = new Size(328, 24);
            sessionStateLabel.TabIndex = 2;
            sessionStateLabel.Text = "状態: Draft";
            sessionStateLabel.TextAlign = ContentAlignment.MiddleRight;
            //
            // dropPanel
            //
            dropPanel.AllowDrop = true;
            dropPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            dropPanel.BackColor = Color.FromArgb(245, 248, 252);
            dropPanel.BorderStyle = BorderStyle.FixedSingle;
            dropPanel.Controls.Add(supportedFormatsLabel);
            dropPanel.Controls.Add(dropInstructionLabel);
            dropPanel.Location = new Point(36, 212);
            dropPanel.Name = "dropPanel";
            dropPanel.Size = new Size(828, 112);
            dropPanel.TabIndex = 3;
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
            draftCountLabel.Location = new Point(36, 352);
            draftCountLabel.Name = "draftCountLabel";
            draftCountLabel.Size = new Size(77, 21);
            draftCountLabel.TabIndex = 4;
            draftCountLabel.Text = "Draft: 0枚";
            //
            // removeDraftImageButton
            //
            removeDraftImageButton.AccessibleName = "選択画像を削除";
            removeDraftImageButton.Enabled = false;
            removeDraftImageButton.Location = new Point(483, 346);
            removeDraftImageButton.Name = "removeDraftImageButton";
            removeDraftImageButton.Size = new Size(120, 32);
            removeDraftImageButton.TabIndex = 5;
            removeDraftImageButton.Text = "選択画像を削除";
            removeDraftImageButton.UseVisualStyleBackColor = true;
            removeDraftImageButton.Click += RemoveDraftImageButton_Click;
            //
            // clearDraftButton
            //
            clearDraftButton.AccessibleName = "Draftを全クリア";
            clearDraftButton.Enabled = false;
            clearDraftButton.Location = new Point(609, 346);
            clearDraftButton.Name = "clearDraftButton";
            clearDraftButton.Size = new Size(120, 32);
            clearDraftButton.TabIndex = 6;
            clearDraftButton.Text = "全クリア";
            clearDraftButton.UseVisualStyleBackColor = true;
            clearDraftButton.Click += ClearDraftButton_Click;
            //
            // createQrButton
            //
            createQrButton.AccessibleName = "QR作成";
            createQrButton.Enabled = false;
            createQrButton.Location = new Point(735, 346);
            createQrButton.Name = "createQrButton";
            createQrButton.Size = new Size(129, 32);
            createQrButton.TabIndex = 7;
            createQrButton.Text = "QR作成";
            createQrButton.UseVisualStyleBackColor = true;
            createQrButton.Click += CreateQrButton_Click;
            //
            // newTransferButton
            //
            newTransferButton.AccessibleName = "新しい転送";
            newTransferButton.Enabled = false;
            newTransferButton.Location = new Point(735, 346);
            newTransferButton.Name = "newTransferButton";
            newTransferButton.Size = new Size(129, 32);
            newTransferButton.TabIndex = 8;
            newTransferButton.Text = "新しい転送";
            newTransferButton.UseVisualStyleBackColor = true;
            newTransferButton.Visible = false;
            newTransferButton.Click += NewTransferButton_Click;
            //
            // sessionStateTimer
            //
            sessionStateTimer.Interval = 1000;
            sessionStateTimer.Tick += SessionStateTimer_Tick;
            //
            // qrPanel
            //
            qrPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            qrPanel.BackColor = Color.White;
            qrPanel.BorderStyle = BorderStyle.FixedSingle;
            qrPanel.Controls.Add(qrInstructionLabel);
            qrPanel.Controls.Add(transferUrlTextBox);
            qrPanel.Controls.Add(qrStatusLabel);
            qrPanel.Controls.Add(qrPictureBox);
            qrPanel.Location = new Point(36, 386);
            qrPanel.Name = "qrPanel";
            qrPanel.Size = new Size(828, 294);
            qrPanel.TabIndex = 9;
            qrPanel.Visible = false;
            //
            // qrPictureBox
            //
            qrPictureBox.AccessibleName = "転送用QRコード";
            qrPictureBox.BackColor = Color.White;
            qrPictureBox.Location = new Point(20, 20);
            qrPictureBox.Name = "qrPictureBox";
            qrPictureBox.Size = new Size(252, 252);
            qrPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            qrPictureBox.TabIndex = 0;
            qrPictureBox.TabStop = false;
            //
            // qrInstructionLabel
            //
            qrInstructionLabel.AutoSize = true;
            qrInstructionLabel.Font = new Font("Segoe UI", 13F, FontStyle.Bold, GraphicsUnit.Point);
            qrInstructionLabel.Location = new Point(304, 52);
            qrInstructionLabel.Name = "qrInstructionLabel";
            qrInstructionLabel.Size = new Size(369, 25);
            qrInstructionLabel.TabIndex = 1;
            qrInstructionLabel.Text = "iPhoneの標準カメラで読み取ってください";
            //
            // transferUrlTextBox
            //
            transferUrlTextBox.AccessibleName = "転送URL";
            transferUrlTextBox.Location = new Point(304, 94);
            transferUrlTextBox.Name = "transferUrlTextBox";
            transferUrlTextBox.ReadOnly = true;
            transferUrlTextBox.Size = new Size(492, 23);
            transferUrlTextBox.TabIndex = 2;
            //
            // qrStatusLabel
            //
            qrStatusLabel.AutoSize = true;
            qrStatusLabel.Font = new Font("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Point);
            qrStatusLabel.ForeColor = Color.Firebrick;
            qrStatusLabel.Location = new Point(304, 141);
            qrStatusLabel.Name = "qrStatusLabel";
            qrStatusLabel.Size = new Size(282, 20);
            qrStatusLabel.TabIndex = 3;
            qrStatusLabel.Text = "LAN用IPv4アドレスを取得できません。";
            qrStatusLabel.Visible = false;
            //
            // draftListView
            //
            draftListView.AccessibleName = "Draft画像一覧";
            draftListView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            draftListView.BackColor = Color.FromArgb(250, 250, 250);
            draftListView.BorderStyle = BorderStyle.FixedSingle;
            draftListView.LargeImageList = draftImageList;
            draftListView.Location = new Point(36, 386);
            draftListView.MultiSelect = false;
            draftListView.Name = "draftListView";
            draftListView.ShowItemToolTips = true;
            draftListView.Size = new Size(828, 294);
            draftListView.TabIndex = 9;
            draftListView.UseCompatibleStateImageBehavior = false;
            draftListView.SelectedIndexChanged += DraftListView_SelectedIndexChanged;
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
            emptyDraftLabel.Location = new Point(357, 524);
            emptyDraftLabel.Name = "emptyDraftLabel";
            emptyDraftLabel.Size = new Size(186, 15);
            emptyDraftLabel.TabIndex = 10;
            emptyDraftLabel.Text = "Draftに画像はまだありません。";
            //
            // rejectionTitleLabel
            //
            rejectionTitleLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            rejectionTitleLabel.AutoSize = true;
            rejectionTitleLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            rejectionTitleLabel.ForeColor = Color.Firebrick;
            rejectionTitleLabel.Location = new Point(36, 690);
            rejectionTitleLabel.Name = "rejectionTitleLabel";
            rejectionTitleLabel.Size = new Size(103, 15);
            rejectionTitleLabel.TabIndex = 11;
            rejectionTitleLabel.Text = "追加できない画像";
            rejectionTitleLabel.Visible = false;
            //
            // rejectionListBox
            //
            rejectionListBox.AccessibleName = "追加できない画像一覧";
            rejectionListBox.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            rejectionListBox.ForeColor = Color.Firebrick;
            rejectionListBox.FormattingEnabled = true;
            rejectionListBox.HorizontalScrollbar = true;
            rejectionListBox.ItemHeight = 15;
            rejectionListBox.Location = new Point(36, 712);
            rejectionListBox.Name = "rejectionListBox";
            rejectionListBox.Size = new Size(828, 64);
            rejectionListBox.TabIndex = 12;
            rejectionListBox.Visible = false;
            //
            // Form1
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.Window;
            ClientSize = new Size(900, 796);
            Controls.Add(rejectionListBox);
            Controls.Add(rejectionTitleLabel);
            Controls.Add(newTransferButton);
            Controls.Add(createQrButton);
            Controls.Add(clearDraftButton);
            Controls.Add(removeDraftImageButton);
            Controls.Add(emptyDraftLabel);
            Controls.Add(draftListView);
            Controls.Add(qrPanel);
            Controls.Add(draftCountLabel);
            Controls.Add(dropPanel);
            Controls.Add(statusLabel);
            Controls.Add(lanAddressComboBox);
            Controls.Add(lanAddressLabel);
            Controls.Add(sessionStateLabel);
            Controls.Add(headingLabel);
            MinimumSize = new Size(916, 835);
            Name = "mainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "TransferImageQR";
            dropPanel.ResumeLayout(false);
            dropPanel.PerformLayout();
            qrPanel.ResumeLayout(false);
            qrPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)qrPictureBox).EndInit();
            ResumeLayout(false);
            PerformLayout();
            emptyDraftLabel.BringToFront();
        }

        #endregion

        private Label headingLabel;
        private Label statusLabel;
        private Label lanAddressLabel;
        private ComboBox lanAddressComboBox;
        private Panel dropPanel;
        private Label supportedFormatsLabel;
        private Label dropInstructionLabel;
        private Label draftCountLabel;
        private ListView draftListView;
        private ImageList draftImageList;
        private Label emptyDraftLabel;
        private Label rejectionTitleLabel;
        private ListBox rejectionListBox;
        private Button removeDraftImageButton;
        private Button clearDraftButton;
        private Button createQrButton;
        private Label sessionStateLabel;
        private Button newTransferButton;
        private System.Windows.Forms.Timer sessionStateTimer;
        private Panel qrPanel;
        private PictureBox qrPictureBox;
        private Label qrInstructionLabel;
        private TextBox transferUrlTextBox;
        private Label qrStatusLabel;
    }
}
