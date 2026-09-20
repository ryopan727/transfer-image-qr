using TransferImageQR.Presentation;
using System.Drawing.Imaging;

namespace TransferImageQR
{
    public partial class Form1 : Form, IMainView
    {
        private MainPresenter? _presenter;
        private bool _draftActionsEnabled;
        private bool _draftEditingEnabled = true;
        private string? _displayedQrUrl;
        private bool _updatingLanAddresses;
        private Image? _backgroundImage;
        private int _backgroundOpacityPercent = 35;
        private int _backgroundZoomPercent = 100;
        private int _backgroundOffsetX;
        private int _backgroundOffsetY;
        private bool _isApplyingBackground;

        private readonly GroupBox backgroundSettingsGroup = new();
        private readonly Button selectBackgroundButton = new();
        private readonly Button clearBackgroundButton = new();
        private readonly NumericUpDown backgroundOpacityInput = new();
        private readonly NumericUpDown backgroundZoomInput = new();
        private readonly NumericUpDown backgroundOffsetXInput = new();
        private readonly NumericUpDown backgroundOffsetYInput = new();
        private readonly Label backgroundErrorLabel = new();
        private readonly CheckBox minimizeToTrayCheckBox = new();
        private readonly Label traySettingsErrorLabel = new();
        private NotifyIcon? _trayIcon;
        private bool _isApplyingTrayMode;
        private bool _allowExit;

        public Form1()
        {
            InitializeComponent();
            InitializeBackgroundControls();
            InitializeTrayControls();
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        }

        public void AttachPresenter(MainPresenter presenter)
        {
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
        }

        public void DisplayLanAddresses(
            IReadOnlyCollection<LanAddressViewModel> addresses,
            string? selectedAddress)
        {
            _updatingLanAddresses = true;
            try
            {
                lanAddressComboBox.Items.Clear();
                if (addresses.Count == 0)
                {
                    lanAddressComboBox.Items.Add("利用可能なLAN IPv4アドレスがありません");
                    lanAddressComboBox.SelectedIndex = 0;
                    return;
                }

                foreach (var address in addresses)
                {
                    lanAddressComboBox.Items.Add(address);
                }

                lanAddressComboBox.DisplayMember = nameof(LanAddressViewModel.DisplayName);
                lanAddressComboBox.SelectedItem = addresses.FirstOrDefault(
                    address => string.Equals(
                        address.Address,
                        selectedAddress,
                        StringComparison.Ordinal));
            }
            finally
            {
                _updatingLanAddresses = false;
            }
        }

        public void SetLanAddressSelectionEnabled(bool enabled) =>
            lanAddressComboBox.Enabled = enabled &&
                lanAddressComboBox.SelectedItem is LanAddressViewModel;

        public void AppendDraftImages(IReadOnlyCollection<DraftImageViewModel> images)
        {
            foreach (var image in images)
            {
                using var stream = new MemoryStream(image.ThumbnailPng, writable: false);
                using var decoded = Image.FromStream(stream);
                var thumbnail = new Bitmap(decoded);
                var imageKey = image.Id.ToString("N");

                draftImageList.Images.Add(imageKey, thumbnail);
                draftListView.Items.Add(new ListViewItem(image.FileName, imageKey)
                {
                    Name = imageKey,
                    ToolTipText = image.FilePath,
                });
            }

            emptyDraftLabel.Visible = draftListView.Items.Count == 0;
        }

        public void SetDraftCount(int count)
        {
            draftCountLabel.Text = $"Draft: {count}枚";
            emptyDraftLabel.Visible = count == 0;
        }

        public void SetDropEnabled(bool enabled)
        {
            dropPanel.AllowDrop = enabled;
            dropInstructionLabel.Text = enabled
                ? "画像をここにドロップ"
                : "画像を読み込んでいます…";
        }

        public void DisplayRejectedImages(IReadOnlyCollection<RejectedImageViewModel> images)
        {
            rejectionListBox.Items.Clear();
            foreach (var image in images)
            {
                rejectionListBox.Items.Add($"{image.FileName}: {image.Message}");
            }

            rejectionTitleLabel.Visible = images.Count > 0;
            rejectionListBox.Visible = images.Count > 0;
        }

        public void RemoveDraftImage(Guid imageId)
        {
            var imageKey = imageId.ToString("N");
            var item = draftListView.Items[imageKey];
            if (item is not null)
            {
                draftListView.Items.Remove(item);
            }

            var thumbnail = draftImageList.Images[imageKey];
            draftImageList.Images.RemoveByKey(imageKey);
            thumbnail?.Dispose();
            emptyDraftLabel.Visible = draftListView.Items.Count == 0;
            UpdateDraftButtonState();
        }

        public void ClearDraftImages()
        {
            draftListView.Items.Clear();
            while (draftImageList.Images.Count > 0)
            {
                var thumbnail = draftImageList.Images[0];
                draftImageList.Images.RemoveAt(0);
                thumbnail.Dispose();
            }

            emptyDraftLabel.Visible = true;
            UpdateDraftButtonState();
        }

        public void SetDraftActionsEnabled(bool enabled)
        {
            _draftActionsEnabled = enabled;
            UpdateDraftButtonState();
        }

        public void SetDraftEditingEnabled(bool enabled)
        {
            _draftEditingEnabled = enabled;
            dropPanel.AllowDrop = enabled;
            dropInstructionLabel.Text = enabled
                ? "画像をここにドロップ"
                : "転送中は画像を変更できません";
            UpdateDraftButtonState();
        }

        public void DisplayTransferSession(TransferSessionViewModel session)
        {
            sessionStateLabel.Text = session.IsExpired
                ? "状態: Expired（期限切れ）"
                : $"状態: Active（有効期限 {session.ExpiresAt.ToLocalTime():HH:mm:ss}）";
            createQrButton.Visible = false;
            newTransferButton.Visible = true;
            newTransferButton.Enabled = true;
            sessionStateTimer.Enabled = !session.IsExpired;
            draftListView.Visible = false;
            emptyDraftLabel.Visible = false;
            qrPanel.Visible = true;
            transferUrlTextBox.Text = session.TransferUrl ?? string.Empty;

            if (session.IsExpired)
            {
                qrPictureBox.Visible = false;
                qrStatusLabel.Text = "QRコードの有効期限が切れました。";
                qrStatusLabel.Visible = true;
            }
            else if (session.TransferUrl is null || session.QrCodePng is null)
            {
                qrPictureBox.Visible = false;
                qrStatusLabel.Text = "LAN用IPv4アドレスを取得できません。";
                qrStatusLabel.Visible = true;
            }
            else
            {
                if (!string.Equals(_displayedQrUrl, session.TransferUrl, StringComparison.Ordinal))
                {
                    using var stream = new MemoryStream(session.QrCodePng, writable: false);
                    using var decoded = Image.FromStream(stream);
                    ReplaceQrImage(new Bitmap(decoded));
                    _displayedQrUrl = session.TransferUrl;
                }

                qrPictureBox.Visible = true;
                qrStatusLabel.Visible = false;
            }
        }

        public void DisplayDraftState()
        {
            sessionStateLabel.Text = "状態: Draft";
            createQrButton.Visible = true;
            newTransferButton.Visible = false;
            newTransferButton.Enabled = false;
            sessionStateTimer.Enabled = false;
            qrPanel.Visible = false;
            draftListView.Visible = true;
            emptyDraftLabel.Visible = draftListView.Items.Count == 0;
            transferUrlTextBox.Clear();
            ReplaceQrImage(null);
            _displayedQrUrl = null;
        }

        public void ApplyBackground(BackgroundViewModel background)
        {
            Image? nextImage = null;
            if (background.ImagePng is not null)
            {
                using var stream = new MemoryStream(background.ImagePng, writable: false);
                using var decoded = Image.FromStream(stream);
                nextImage = new Bitmap(decoded);
            }

            var previous = _backgroundImage;
            _backgroundImage = nextImage;
            previous?.Dispose();

            _backgroundOpacityPercent = background.OpacityPercent;
            _backgroundZoomPercent = background.ZoomPercent;
            _backgroundOffsetX = background.OffsetX;
            _backgroundOffsetY = background.OffsetY;

            _isApplyingBackground = true;
            try
            {
                backgroundOpacityInput.Value = background.OpacityPercent;
                backgroundZoomInput.Value = background.ZoomPercent;
                backgroundOffsetXInput.Value = background.OffsetX;
                backgroundOffsetYInput.Value = background.OffsetY;
                clearBackgroundButton.Enabled = nextImage is not null;
            }
            finally
            {
                _isApplyingBackground = false;
            }

            Invalidate();
        }

        public void DisplayBackgroundError(string? message)
        {
            backgroundErrorLabel.Text = message ?? string.Empty;
            backgroundErrorLabel.Visible = !string.IsNullOrEmpty(message);
        }

        public void SetTrayMode(bool enabled)
        {
            _isApplyingTrayMode = true;
            try
            {
                minimizeToTrayCheckBox.Checked = enabled;
                if (_trayIcon is not null)
                {
                    _trayIcon.Visible = enabled;
                }
            }
            finally
            {
                _isApplyingTrayMode = false;
            }
        }

        public void DisplayTraySettingsError(string? message)
        {
            traySettingsErrorLabel.Text = message ?? string.Empty;
            traySettingsErrorLabel.Visible = !string.IsNullOrEmpty(message);
        }

        public void HideToTray() => Hide();

        public void ShowFromTray()
        {
            Show();
            if (WindowState == FormWindowState.Minimized)
            {
                WindowState = FormWindowState.Normal;
            }

            Activate();
            BringToFront();
        }

        public void ExitApplication()
        {
            _allowExit = true;
            if (_trayIcon is not null)
            {
                _trayIcon.Visible = false;
            }

            Close();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            base.OnPaintBackground(e);
            if (_backgroundImage is null || _backgroundOpacityPercent == 0)
            {
                return;
            }

            var destination = Presentation.BackgroundImageLayout.Calculate(
                ClientSize,
                _backgroundImage.Size,
                _backgroundZoomPercent,
                _backgroundOffsetX,
                _backgroundOffsetY);
            using var attributes = new ImageAttributes();
            var matrix = new ColorMatrix { Matrix33 = _backgroundOpacityPercent / 100f };
            attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
            e.Graphics.DrawImage(
                _backgroundImage,
                destination,
                0,
                0,
                _backgroundImage.Width,
                _backgroundImage.Height,
                GraphicsUnit.Pixel,
                attributes);
        }

        private void DropPanel_DragEnter(object? sender, DragEventArgs e)
        {
            e.Effect = _presenter is not null && e.Data?.GetDataPresent(DataFormats.FileDrop) == true
                ? DragDropEffects.Copy
                : DragDropEffects.None;
        }

        private async void DropPanel_DragDrop(object? sender, DragEventArgs e)
        {
            if (_presenter is null ||
                e.Data?.GetData(DataFormats.FileDrop) is not string[] filePaths)
            {
                return;
            }

            await _presenter.AddDroppedFilesAsync(filePaths);
        }

        private void DraftListView_SelectedIndexChanged(object? sender, EventArgs e) =>
            UpdateDraftButtonState();

        private void RemoveDraftImageButton_Click(object? sender, EventArgs e)
        {
            if (_presenter is null || draftListView.SelectedItems.Count != 1)
            {
                return;
            }

            var imageKey = draftListView.SelectedItems[0].Name;
            if (Guid.TryParseExact(imageKey, "N", out var imageId))
            {
                _presenter.RemoveDraftImage(imageId);
            }
        }

        private void ClearDraftButton_Click(object? sender, EventArgs e) =>
            _presenter?.ClearDraft();

        private void CreateQrButton_Click(object? sender, EventArgs e) =>
            _presenter?.CreateTransferSession();

        private void NewTransferButton_Click(object? sender, EventArgs e) =>
            _presenter?.StartNewTransfer();

        private void SessionStateTimer_Tick(object? sender, EventArgs e) =>
            _presenter?.RefreshTransferSessionState();

        private void LanAddressComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (!_updatingLanAddresses &&
                lanAddressComboBox.SelectedItem is LanAddressViewModel selected)
            {
                _presenter?.SelectLanAddress(selected.Address);
            }
        }

        private void InitializeBackgroundControls()
        {
            headingLabel.BackColor = Color.FromArgb(245, 248, 252);
            statusLabel.BackColor = Color.FromArgb(245, 248, 252);
            sessionStateLabel.BackColor = Color.FromArgb(245, 248, 252);
            draftCountLabel.BackColor = Color.FromArgb(245, 248, 252);

            backgroundSettingsGroup.Name = "backgroundSettingsGroup";
            backgroundSettingsGroup.AccessibleName = "背景設定";
            backgroundSettingsGroup.Text = "背景設定";
            backgroundSettingsGroup.BackColor = Color.FromArgb(245, 248, 252);
            backgroundSettingsGroup.Location = new Point(36, 124);
            backgroundSettingsGroup.Size = new Size(828, 68);
            backgroundSettingsGroup.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            backgroundSettingsGroup.TabIndex = 3;

            ConfigureButton(selectBackgroundButton, "selectBackgroundButton", "背景を選択", 12, SelectBackgroundButton_Click);
            ConfigureButton(clearBackgroundButton, "clearBackgroundButton", "背景をクリア", 118, ClearBackgroundButton_Click);
            selectBackgroundButton.TabIndex = 0;
            clearBackgroundButton.TabIndex = 1;
            clearBackgroundButton.Enabled = false;

            AddSettingInput("不透明度%", backgroundOpacityInput, "backgroundOpacityInput", 232, 0, 100, 35, 2);
            AddSettingInput("サイズ%", backgroundZoomInput, "backgroundZoomInput", 366, 25, 300, 100, 3);
            AddSettingInput("横px", backgroundOffsetXInput, "backgroundOffsetXInput", 488, -1000, 1000, 0, 4);
            AddSettingInput("縦px", backgroundOffsetYInput, "backgroundOffsetYInput", 588, -1000, 1000, 0, 5);

            backgroundErrorLabel.Name = "backgroundErrorLabel";
            backgroundErrorLabel.AccessibleName = "背景設定エラー";
            backgroundErrorLabel.AutoEllipsis = true;
            backgroundErrorLabel.ForeColor = Color.Firebrick;
            backgroundErrorLabel.Location = new Point(688, 25);
            backgroundErrorLabel.Size = new Size(128, 32);
            backgroundErrorLabel.Visible = false;

            backgroundSettingsGroup.Controls.Add(backgroundErrorLabel);
            Controls.Add(backgroundSettingsGroup);
            backgroundSettingsGroup.BringToFront();
        }

        private void InitializeTrayControls()
        {
            minimizeToTrayCheckBox.Name = "minimizeToTrayCheckBox";
            minimizeToTrayCheckBox.AccessibleName = "閉じたときトレイに格納";
            minimizeToTrayCheckBox.AutoSize = true;
            minimizeToTrayCheckBox.BackColor = Color.FromArgb(245, 248, 252);
            minimizeToTrayCheckBox.Location = new Point(340, 90);
            minimizeToTrayCheckBox.TabIndex = 2;
            minimizeToTrayCheckBox.Text = "閉じたときトレイに格納";
            minimizeToTrayCheckBox.CheckedChanged += MinimizeToTrayCheckBox_CheckedChanged;

            traySettingsErrorLabel.Name = "traySettingsErrorLabel";
            traySettingsErrorLabel.AccessibleName = "常駐設定エラー";
            traySettingsErrorLabel.AutoEllipsis = true;
            traySettingsErrorLabel.BackColor = Color.FromArgb(245, 248, 252);
            traySettingsErrorLabel.Font = new Font("Segoe UI", 8F, FontStyle.Regular, GraphicsUnit.Point);
            traySettingsErrorLabel.ForeColor = Color.Firebrick;
            traySettingsErrorLabel.Location = new Point(340, 109);
            traySettingsErrorLabel.Size = new Size(166, 15);
            traySettingsErrorLabel.Visible = false;

            var trayMenu = new ContextMenuStrip(components);
            var showMenuItem = new ToolStripMenuItem("表示")
            {
                Name = "showMainWindowMenuItem",
            };
            showMenuItem.Click += TrayShow_Click;
            var exitMenuItem = new ToolStripMenuItem("終了")
            {
                Name = "exitApplicationMenuItem",
            };
            exitMenuItem.Click += TrayExit_Click;
            trayMenu.Items.Add(showMenuItem);
            trayMenu.Items.Add(exitMenuItem);

            _trayIcon = new NotifyIcon(components)
            {
                ContextMenuStrip = trayMenu,
                Icon = SystemIcons.Application,
                Text = "TransferImageQR",
                Visible = false,
            };
            _trayIcon.DoubleClick += TrayShow_Click;

            Controls.Add(minimizeToTrayCheckBox);
            Controls.Add(traySettingsErrorLabel);
            minimizeToTrayCheckBox.BringToFront();
            traySettingsErrorLabel.BringToFront();
            FormClosing += Form1_FormClosing;
        }

        private void MinimizeToTrayCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            if (!_isApplyingTrayMode)
            {
                _presenter?.SetMinimizeToTray(minimizeToTrayCheckBox.Checked);
            }
        }

        private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (_allowExit ||
                e.CloseReason is CloseReason.WindowsShutDown or
                    CloseReason.TaskManagerClosing or
                    CloseReason.ApplicationExitCall)
            {
                return;
            }

            if (_presenter?.RequestWindowClose() == true)
            {
                e.Cancel = true;
            }
        }

        private void TrayShow_Click(object? sender, EventArgs e) =>
            _presenter?.ShowMainWindow();

        private void TrayExit_Click(object? sender, EventArgs e) =>
            _presenter?.ExitApplication();

        private void ConfigureButton(
            Button button,
            string name,
            string text,
            int left,
            EventHandler clickHandler)
        {
            button.Name = name;
            button.AccessibleName = text;
            button.Text = text;
            button.Location = new Point(left, 25);
            button.Size = new Size(100, 28);
            button.UseVisualStyleBackColor = true;
            button.Click += clickHandler;
            backgroundSettingsGroup.Controls.Add(button);
        }

        private void AddSettingInput(
            string labelText,
            NumericUpDown input,
            string name,
            int left,
            int minimum,
            int maximum,
            int value,
            int tabIndex)
        {
            var label = new Label
            {
                AutoSize = true,
                Location = new Point(left, 31),
                Text = labelText,
            };
            input.Name = name;
            input.AccessibleName = $"背景{labelText}";
            input.Location = new Point(left + label.PreferredWidth + 4, 27);
            input.Size = new Size(58, 23);
            input.Minimum = minimum;
            input.Maximum = maximum;
            input.Value = value;
            input.TabIndex = tabIndex;
            input.ValueChanged += BackgroundAppearanceInput_ValueChanged;
            backgroundSettingsGroup.Controls.Add(label);
            backgroundSettingsGroup.Controls.Add(input);
        }

        private void SelectBackgroundButton_Click(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Title = "背景画像を選択",
                Filter = "画像ファイル (*.jpg;*.jpeg;*.png;*.webp)|*.jpg;*.jpeg;*.png;*.webp",
                CheckFileExists = true,
                Multiselect = false,
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                _presenter?.SelectBackgroundImage(dialog.FileName);
            }
        }

        private void ClearBackgroundButton_Click(object? sender, EventArgs e) =>
            _presenter?.ClearBackground();

        private void BackgroundAppearanceInput_ValueChanged(object? sender, EventArgs e)
        {
            if (_isApplyingBackground)
            {
                return;
            }

            _presenter?.UpdateBackgroundAppearance(
                (int)backgroundOpacityInput.Value,
                (int)backgroundZoomInput.Value,
                (int)backgroundOffsetXInput.Value,
                (int)backgroundOffsetYInput.Value);
        }

        private void UpdateDraftButtonState()
        {
            var canEditDraft = _draftEditingEnabled && _draftActionsEnabled;
            removeDraftImageButton.Enabled = canEditDraft && draftListView.SelectedItems.Count == 1;
            clearDraftButton.Enabled = canEditDraft;
            createQrButton.Enabled = canEditDraft;
        }

        private void ReplaceQrImage(Image? image)
        {
            var previous = qrPictureBox.Image;
            qrPictureBox.Image = image;
            previous?.Dispose();
        }
    }
}
