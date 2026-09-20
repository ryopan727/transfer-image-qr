using TransferImageQR.Presentation;

namespace TransferImageQR;

public sealed class BackgroundSettingsForm : Form, IBackgroundSettingsView
{
    private readonly IBackgroundSettingsPresenter _presenter;
    private readonly Func<IWin32Window, string?> _selectFile;
    private readonly PictureBox _preview = new();
    private readonly Button _selectButton = new();
    private readonly Button _clearButton = new();
    private readonly NumericUpDown _opacityInput = new();
    private readonly NumericUpDown _zoomInput = new();
    private readonly NumericUpDown _offsetXInput = new();
    private readonly NumericUpDown _offsetYInput = new();
    private readonly Label _errorLabel = new();
    private bool _isApplyingSettings;

    public BackgroundSettingsForm(
        IBackgroundSettingsPresenter presenter,
        Func<IWin32Window, string?>? selectFile = null)
    {
        _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
        _selectFile = selectFile ?? SelectBackgroundFile;
        InitializeControls();
    }

    public void DisplaySettings(BackgroundViewModel background)
    {
        var nextPreview = DecodePreview(background.ImagePng);
        var previousPreview = _preview.Image;
        _preview.Image = nextPreview;
        previousPreview?.Dispose();

        _isApplyingSettings = true;
        try
        {
            _opacityInput.Value = background.OpacityPercent;
            _zoomInput.Value = background.ZoomPercent;
            _offsetXInput.Value = background.OffsetX;
            _offsetYInput.Value = background.OffsetY;
            _clearButton.Enabled = background.ImagePng is not null;
        }
        finally
        {
            _isApplyingSettings = false;
        }
    }

    public void DisplayBackgroundError(string? message)
    {
        _errorLabel.Text = message ?? string.Empty;
        _errorLabel.Visible = !string.IsNullOrEmpty(message);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        _presenter.AttachBackgroundSettingsView(this);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _presenter.DetachBackgroundSettingsView(this);
        base.OnFormClosed(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _preview.Image?.Dispose();
            _preview.Image = null;
        }

        base.Dispose(disposing);
    }

    private void InitializeControls()
    {
        Name = "backgroundSettingsForm";
        AccessibleName = "背景設定";
        Text = "背景設定";
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(620, 286);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;

        _preview.Name = "backgroundPreviewPictureBox";
        _preview.AccessibleName = "現在の背景画像";
        _preview.BackColor = Color.WhiteSmoke;
        _preview.BorderStyle = BorderStyle.FixedSingle;
        _preview.Location = new Point(20, 20);
        _preview.Size = new Size(220, 165);
        _preview.SizeMode = PictureBoxSizeMode.Zoom;

        ConfigureButton(_selectButton, "selectBackgroundButton", "背景を選択", 20);
        _selectButton.Click += SelectButton_Click;
        ConfigureButton(_clearButton, "clearBackgroundButton", "背景をクリア", 136);
        _clearButton.Enabled = false;
        _clearButton.Click += (_, _) => _presenter.ClearBackground();

        ConfigureInput(_opacityInput, "backgroundOpacityInput", "背景の不透明度", 272, 48, 0, 100, 35);
        ConfigureInput(_zoomInput, "backgroundZoomInput", "背景のサイズ", 272, 94, 25, 300, 100);
        ConfigureInput(_offsetXInput, "backgroundOffsetXInput", "背景の横位置", 272, 140, -1000, 1000, 0);
        ConfigureInput(_offsetYInput, "backgroundOffsetYInput", "背景の縦位置", 272, 186, -1000, 1000, 0);

        AddInputLabel("不透明度 (%)", 272, 27);
        AddInputLabel("サイズ (%)", 272, 73);
        AddInputLabel("横位置 (px)", 272, 119);
        AddInputLabel("縦位置 (px)", 272, 165);

        _errorLabel.Name = "backgroundErrorLabel";
        _errorLabel.AccessibleName = "背景設定エラー";
        _errorLabel.AutoEllipsis = true;
        _errorLabel.ForeColor = Color.Firebrick;
        _errorLabel.Location = new Point(272, 222);
        _errorLabel.Size = new Size(328, 38);
        _errorLabel.Visible = false;

        Controls.Add(_preview);
        Controls.Add(_selectButton);
        Controls.Add(_clearButton);
        Controls.Add(_opacityInput);
        Controls.Add(_zoomInput);
        Controls.Add(_offsetXInput);
        Controls.Add(_offsetYInput);
        Controls.Add(_errorLabel);
    }

    private void ConfigureButton(Button button, string name, string text, int left)
    {
        button.Name = name;
        button.AccessibleName = text;
        button.Text = text;
        button.Location = new Point(left, 202);
        button.Size = new Size(104, 32);
        button.UseVisualStyleBackColor = true;
    }

    private void ConfigureInput(
        NumericUpDown input,
        string name,
        string accessibleName,
        int left,
        int top,
        int minimum,
        int maximum,
        int value)
    {
        input.Name = name;
        input.AccessibleName = accessibleName;
        input.Location = new Point(left, top);
        input.Size = new Size(150, 23);
        input.Minimum = minimum;
        input.Maximum = maximum;
        input.Value = value;
        input.ValueChanged += AppearanceInput_ValueChanged;
    }

    private void AddInputLabel(string text, int left, int top)
    {
        Controls.Add(new Label
        {
            AutoSize = true,
            Location = new Point(left, top),
            Text = text,
        });
    }

    private void SelectButton_Click(object? sender, EventArgs e)
    {
        var filePath = _selectFile(this);
        if (!string.IsNullOrWhiteSpace(filePath))
        {
            _presenter.SelectBackgroundImage(filePath);
        }
    }

    private void AppearanceInput_ValueChanged(object? sender, EventArgs e)
    {
        if (_isApplyingSettings)
        {
            return;
        }

        _presenter.UpdateBackgroundAppearance(
            (int)_opacityInput.Value,
            (int)_zoomInput.Value,
            (int)_offsetXInput.Value,
            (int)_offsetYInput.Value);
    }

    private static string? SelectBackgroundFile(IWin32Window owner)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "背景画像を選択",
            Filter = "画像ファイル (*.jpg;*.jpeg;*.png;*.webp)|*.jpg;*.jpeg;*.png;*.webp",
            CheckFileExists = true,
            Multiselect = false,
        };
        return dialog.ShowDialog(owner) == DialogResult.OK ? dialog.FileName : null;
    }

    private static Image? DecodePreview(byte[]? imagePng)
    {
        if (imagePng is null)
        {
            return null;
        }

        using var stream = new MemoryStream(imagePng, writable: false);
        using var decoded = Image.FromStream(stream);
        return new Bitmap(decoded);
    }
}
