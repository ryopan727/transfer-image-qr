using TransferImageQR.Presentation;
using System.Drawing.Imaging;

namespace TransferImageQR
{
    public partial class Form1 : Form, IMainView
    {
        private static readonly Color DefaultDropSurfaceColor = Color.FromArgb(245, 248, 252);
        private static readonly Color DefaultDraftSurfaceColor = Color.FromArgb(250, 250, 250);
        private static readonly Color TranslucentDropSurfaceColor = Color.FromArgb(176, 245, 248, 252);
        private static readonly Color DraftSurfaceVeilColor = Color.FromArgb(168, 250, 250, 250);

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
        private readonly MenuStrip mainMenuStrip = new();
        private readonly ToolStripMenuItem settingsMenuItem = new();
        private readonly ToolStripMenuItem backgroundSettingsMenuItem = new();
        private readonly CheckBox minimizeToTrayCheckBox = new();
        private readonly Label traySettingsErrorLabel = new();
        private readonly CheckBox autoStartCheckBox = new();
        private readonly Label autoStartErrorLabel = new();
        private NotifyIcon? _trayIcon;
        private bool _isApplyingTrayMode;
        private bool _isApplyingAutoStartMode;
        private bool _allowExit;
        private Func<Form>? _backgroundSettingsFormFactory;
        private Form? _backgroundSettingsForm;

        public Form1()
        {
            InitializeComponent();
            InitializeMainSurface();
            InitializeSettingsMenu();
            InitializeTrayControls();
            InitializeAutoStartControls();
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            SizeChanged += Form1_SizeChanged;
            ApplicationFonts.ApplyTo(this);
            if (_trayIcon?.ContextMenuStrip is { } trayMenu)
            {
                ApplicationFonts.ApplyTo(trayMenu);
            }
        }

        public void AttachPresenter(MainPresenter presenter)
        {
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
        }

        public void AttachBackgroundSettingsFormFactory(Func<Form> factory)
        {
            _backgroundSettingsFormFactory = factory ??
                throw new ArgumentNullException(nameof(factory));
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

        public void DisplayNetworkDiagnostics(NetworkDiagnosticsViewModel diagnostics)
        {
            var message = diagnostics switch
            {
                { ServerStartFailed: true } =>
                    "HTTPサーバーを起動できません。使用中のポートやWindows Firewallを確認してください。",
                { ServerRunning: true, SelectedAddress: not null, Port: > 0 } => null,
                { ServerRunning: true } =>
                    "LAN用IPv4アドレスを取得できません。同一LANへの接続を確認してください。",
                _ => "HTTPサーバーは停止しています。",
            };

            statusLabel.Text = message ?? string.Empty;
            statusLabel.ForeColor = Color.Firebrick;
            statusLabel.Visible = message is not null;
        }

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

            UpdateDraftSurfaceAppearance();
            Invalidate(true);
        }

        public void DisplayBackgroundError(string? message)
        {
            settingsMenuItem.Text = string.IsNullOrEmpty(message) ? "設定" : "設定 ⚠";
            settingsMenuItem.ToolTipText = message ?? string.Empty;
            settingsMenuItem.AccessibleDescription = message;
            backgroundSettingsMenuItem.ToolTipText = message ?? string.Empty;
            backgroundSettingsMenuItem.AccessibleDescription = message;
            backgroundSettingsMenuItem.ForeColor = string.IsNullOrEmpty(message)
                ? SystemColors.ControlText
                : Color.Firebrick;
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

        public void SetAutoStartMode(bool enabled)
        {
            _isApplyingAutoStartMode = true;
            try
            {
                autoStartCheckBox.Checked = enabled;
            }
            finally
            {
                _isApplyingAutoStartMode = false;
            }
        }

        public void DisplayAutoStartError(string? message)
        {
            autoStartErrorLabel.Text = message ?? string.Empty;
            autoStartErrorLabel.Visible = !string.IsNullOrEmpty(message);
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
            DrawCustomBackground(e.Graphics);
        }

        private void DrawCustomBackground(Graphics graphics)
        {
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
            graphics.DrawImage(
                _backgroundImage,
                destination,
                0,
                0,
                _backgroundImage.Width,
                _backgroundImage.Height,
                GraphicsUnit.Pixel,
                attributes);
        }

        private void Form1_SizeChanged(object? sender, EventArgs e) =>
            UpdateDraftSurfaceAppearance();

        private void UpdateDraftSurfaceAppearance()
        {
            if (draftListView.IsDisposed || dropPanel.IsDisposed)
            {
                return;
            }

            var hasVisibleBackground = _backgroundImage is not null && _backgroundOpacityPercent > 0;
            dropPanel.BackColor = hasVisibleBackground
                ? TranslucentDropSurfaceColor
                : DefaultDropSurfaceColor;

            var previousBackground = draftListView.BackgroundImage;
            draftListView.BackgroundImage = hasVisibleBackground
                ? CreateDraftListBackground()
                : null;
            draftListView.BackColor = DefaultDraftSurfaceColor;
            emptyDraftLabel.BackColor = hasVisibleBackground
                ? Color.FromArgb(208, DefaultDraftSurfaceColor)
                : DefaultDraftSurfaceColor;
            previousBackground?.Dispose();

            dropPanel.Invalidate(true);
            draftListView.Invalidate();
            emptyDraftLabel.Invalidate();
        }

        private Bitmap? CreateDraftListBackground()
        {
            if (_backgroundImage is null ||
                draftListView.ClientSize.Width <= 0 ||
                draftListView.ClientSize.Height <= 0)
            {
                return null;
            }

            var background = new Bitmap(
                draftListView.ClientSize.Width,
                draftListView.ClientSize.Height);
            using var graphics = Graphics.FromImage(background);
            graphics.Clear(BackColor);
            var state = graphics.Save();
            graphics.TranslateTransform(-draftListView.Left, -draftListView.Top);
            DrawCustomBackground(graphics);
            graphics.Restore(state);

            using var veil = new SolidBrush(DraftSurfaceVeilColor);
            graphics.FillRectangle(veil, new Rectangle(Point.Empty, background.Size));
            return background;
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

        private void InitializeMainSurface()
        {
            headingLabel.BackColor = Color.FromArgb(245, 248, 252);
            statusLabel.BackColor = Color.FromArgb(245, 248, 252);
            statusLabel.AccessibleName = "ネットワーク診断";
            statusLabel.AutoSize = false;
            statusLabel.Location = new Point(36, 85);
            statusLabel.Size = new Size(460, 34);
            statusLabel.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            sessionStateLabel.BackColor = Color.FromArgb(245, 248, 252);
            draftCountLabel.BackColor = Color.FromArgb(245, 248, 252);
            dropPanel.Location = new Point(36, 132);
            draftCountLabel.Location = new Point(36, 272);
            removeDraftImageButton.Location = new Point(483, 266);
            clearDraftButton.Location = new Point(609, 266);
            createQrButton.Location = new Point(735, 266);
            newTransferButton.Location = new Point(735, 266);
            draftListView.Location = new Point(36, 306);
            draftListView.Size = new Size(828, 374);
            qrPanel.Location = new Point(36, 306);
            qrPanel.Size = new Size(828, 374);
            emptyDraftLabel.Location = new Point(357, 494);
        }

        private void InitializeSettingsMenu()
        {
            mainMenuStrip.Name = "mainMenuStrip";
            mainMenuStrip.AccessibleName = "Main Menu";
            mainMenuStrip.Dock = DockStyle.Top;

            settingsMenuItem.Name = "settingsMenuItem";
            settingsMenuItem.AccessibleName = "設定";
            settingsMenuItem.Text = "設定";

            backgroundSettingsMenuItem.Name = "backgroundSettingsMenuItem";
            backgroundSettingsMenuItem.AccessibleName = "背景設定";
            backgroundSettingsMenuItem.Text = "背景設定";
            backgroundSettingsMenuItem.Click += BackgroundSettingsMenuItem_Click;

            settingsMenuItem.DropDownItems.Add(backgroundSettingsMenuItem);
            mainMenuStrip.Items.Add(settingsMenuItem);
            Controls.Add(mainMenuStrip);
            MainMenuStrip = mainMenuStrip;
            mainMenuStrip.BringToFront();
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

        private void InitializeAutoStartControls()
        {
            autoStartCheckBox.Name = "autoStartCheckBox";
            autoStartCheckBox.AccessibleName = "Windowsログイン時に起動";
            autoStartCheckBox.AutoSize = true;
            autoStartCheckBox.BackColor = Color.FromArgb(245, 248, 252);
            autoStartCheckBox.Location = new Point(340, 50);
            autoStartCheckBox.TabIndex = 1;
            autoStartCheckBox.Text = "Windowsログイン時に起動";
            autoStartCheckBox.CheckedChanged += AutoStartCheckBox_CheckedChanged;

            autoStartErrorLabel.Name = "autoStartErrorLabel";
            autoStartErrorLabel.AccessibleName = "自動起動設定エラー";
            autoStartErrorLabel.AutoEllipsis = true;
            autoStartErrorLabel.BackColor = Color.FromArgb(245, 248, 252);
            autoStartErrorLabel.Font = new Font("Segoe UI", 8F, FontStyle.Regular, GraphicsUnit.Point);
            autoStartErrorLabel.ForeColor = Color.Firebrick;
            autoStartErrorLabel.Location = new Point(340, 69);
            autoStartErrorLabel.Size = new Size(166, 15);
            autoStartErrorLabel.Visible = false;

            Controls.Add(autoStartCheckBox);
            Controls.Add(autoStartErrorLabel);
            autoStartCheckBox.BringToFront();
            autoStartErrorLabel.BringToFront();
        }

        private void AutoStartCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            if (!_isApplyingAutoStartMode)
            {
                _presenter?.SetAutoStartEnabled(autoStartCheckBox.Checked);
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

        private void BackgroundSettingsMenuItem_Click(object? sender, EventArgs e)
        {
            if (_backgroundSettingsForm is { IsDisposed: false })
            {
                _backgroundSettingsForm.Show();
                _backgroundSettingsForm.Activate();
                _backgroundSettingsForm.BringToFront();
                return;
            }

            if (_backgroundSettingsFormFactory is null)
            {
                return;
            }

            _backgroundSettingsForm = _backgroundSettingsFormFactory();
            _backgroundSettingsForm.FormClosed += (_, _) => _backgroundSettingsForm = null;
            _backgroundSettingsForm.Show(this);
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
