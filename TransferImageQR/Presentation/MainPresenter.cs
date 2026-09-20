using System.Net;
using TransferImageQR.Application.Backgrounds;
using TransferImageQR.Application.Drafts;
using TransferImageQR.Application.Sessions;
using TransferImageQR.Domain.Sessions;
using TransferImageQR.Application.Transfers;
using TransferImageQR.Application.Tray;
using TransferImageQR.Application.AutoStart;

namespace TransferImageQR.Presentation;

public sealed class MainPresenter(
    IMainView view,
    IAddImagesToDraftUseCase addImagesToDraftUseCase,
    IEditDraftUseCase editDraftUseCase,
    ITransferSessionUseCase transferSessionUseCase,
    ITransferQrCodeService transferQrCodeService,
    ILanAddressProvider? lanAddressProvider = null,
    IBackgroundCustomizationUseCase? backgroundCustomizationUseCase = null,
    ITraySettingsUseCase? traySettingsUseCase = null,
    IAutoStartSettingsUseCase? autoStartSettingsUseCase = null,
    IHttpServerEndpoint? serverEndpoint = null,
    bool serverStartFailed = false)
{
    private bool _isAdding;
    private bool _isEditingEnabled = true;
    private int _draftCount;
    private TransferQrCode? _transferQrCode;
    private IReadOnlyList<LanAddressOption> _lanAddressOptions = [];
    private IPAddress? _selectedLanAddress;
    private bool _minimizeToTray;

    public void LoadAutoStartSettings()
    {
        if (autoStartSettingsUseCase is not null)
        {
            ApplyAutoStartSettingsResult(autoStartSettingsUseCase.Load());
        }
    }

    public void SetAutoStartEnabled(bool enabled)
    {
        if (autoStartSettingsUseCase is not null)
        {
            ApplyAutoStartSettingsResult(autoStartSettingsUseCase.SetEnabled(enabled));
        }
    }

    public void LoadTraySettings()
    {
        if (traySettingsUseCase is not null)
        {
            ApplyTraySettingsResult(traySettingsUseCase.Load());
        }
    }

    public void SetMinimizeToTray(bool enabled)
    {
        if (traySettingsUseCase is not null)
        {
            ApplyTraySettingsResult(traySettingsUseCase.SetMinimizeToTray(enabled));
        }
    }

    public bool RequestWindowClose()
    {
        if (!_minimizeToTray)
        {
            return false;
        }

        view.HideToTray();
        return true;
    }

    public void ShowMainWindow() => view.ShowFromTray();

    public void ExitApplication() => view.ExitApplication();

    public void LoadBackground()
    {
        if (backgroundCustomizationUseCase is not null)
        {
            ApplyBackgroundResult(backgroundCustomizationUseCase.Load());
        }
    }

    public void SelectBackgroundImage(string filePath)
    {
        if (backgroundCustomizationUseCase is not null)
        {
            ApplyBackgroundResult(backgroundCustomizationUseCase.SelectImage(filePath));
        }
    }

    public void UpdateBackgroundAppearance(
        int opacityPercent,
        int zoomPercent,
        int offsetX,
        int offsetY)
    {
        if (backgroundCustomizationUseCase is not null)
        {
            ApplyBackgroundResult(backgroundCustomizationUseCase.UpdateAppearance(
                opacityPercent,
                zoomPercent,
                offsetX,
                offsetY));
        }
    }

    public void ClearBackground()
    {
        if (backgroundCustomizationUseCase is not null)
        {
            ApplyBackgroundResult(backgroundCustomizationUseCase.Clear());
        }
    }

    public void Initialize()
    {
        _lanAddressOptions = lanAddressProvider?.GetIPv4Addresses() ?? [];
        if (_selectedLanAddress is null ||
            !_lanAddressOptions.Any(option => option.Address.Equals(_selectedLanAddress)))
        {
            _selectedLanAddress = _lanAddressOptions.FirstOrDefault()?.Address;
        }

        DisplayLanAddressSelection();
        view.SetLanAddressSelectionEnabled(_isEditingEnabled && _lanAddressOptions.Count > 0);
        DisplayNetworkDiagnostics();
    }

    public void SelectLanAddress(string? address)
    {
        if (!_isEditingEnabled || string.IsNullOrWhiteSpace(address))
        {
            return;
        }

        var selected = _lanAddressOptions.FirstOrDefault(
            option => string.Equals(
                option.Address.ToString(),
                address,
                StringComparison.Ordinal));
        if (selected is not null)
        {
            _selectedLanAddress = selected.Address;
            DisplayNetworkDiagnostics();
        }
    }

    public async Task AddDroppedFilesAsync(
        IReadOnlyCollection<string> filePaths,
        CancellationToken cancellationToken = default)
    {
        if (!_isEditingEnabled || _isAdding || filePaths.Count == 0)
        {
            return;
        }

        _isAdding = true;
        view.SetDropEnabled(false);
        view.SetDraftActionsEnabled(false);

        try
        {
            var result = await addImagesToDraftUseCase.ExecuteAsync(filePaths, cancellationToken);
            var viewModels = result.AddedImages
                .Select(image => new DraftImageViewModel(
                    image.Id,
                    image.FilePath,
                    image.FileName,
                    image.ThumbnailPng))
                .ToArray();

            if (viewModels.Length > 0)
            {
                view.AppendDraftImages(viewModels);
            }

            UpdateDraftState(result.TotalCount);
            view.DisplayRejectedImages(result.RejectedImages
                .Select(rejection => new RejectedImageViewModel(
                    rejection.FileName,
                    ToUserMessage(rejection.Reason)))
                .ToArray());
        }
        finally
        {
            _isAdding = false;
            view.SetDropEnabled(_isEditingEnabled);
            view.SetDraftActionsEnabled(_isEditingEnabled && _draftCount > 0);
        }
    }

    public void RemoveDraftImage(Guid imageId)
    {
        if (!_isEditingEnabled || _isAdding)
        {
            return;
        }

        var result = editDraftUseCase.Remove(imageId);
        if (result.Changed)
        {
            view.RemoveDraftImage(imageId);
        }

        UpdateDraftState(result.TotalCount);
    }

    public void ClearDraft()
    {
        if (!_isEditingEnabled || _isAdding)
        {
            return;
        }

        var result = editDraftUseCase.Clear();
        if (result.Changed)
        {
            view.ClearDraftImages();
        }

        UpdateDraftState(result.TotalCount);
    }

    public void SetDraftEditingEnabled(bool enabled)
    {
        _isEditingEnabled = enabled;
        view.SetDropEnabled(enabled && !_isAdding);
        view.SetDraftActionsEnabled(enabled && !_isAdding && _draftCount > 0);
        view.SetDraftEditingEnabled(enabled);
        view.SetLanAddressSelectionEnabled(enabled && _lanAddressOptions.Count > 0);
    }

    public void CreateTransferSession()
    {
        if (!_isEditingEnabled || _isAdding)
        {
            return;
        }

        var result = transferSessionUseCase.Create();
        if (!result.Success)
        {
            return;
        }

        _transferQrCode = result.Session is null
            ? null
            : transferQrCodeService.Create(result.Session.Token, _selectedLanAddress);
        SetDraftEditingEnabled(false);
        RefreshTransferSessionState();
    }

    public void RefreshTransferSessionState()
    {
        var status = transferSessionUseCase.GetCurrentStatus();
        if (status is null)
        {
            view.DisplayDraftState();
            return;
        }

        view.DisplayTransferSession(new TransferSessionViewModel(
            status.State == TransferSessionState.Expired,
            status.ExpiresAt,
            _transferQrCode?.Url.AbsoluteUri,
            _transferQrCode?.PngBytes));
    }

    public void StartNewTransfer()
    {
        if (_isAdding)
        {
            return;
        }

        transferSessionUseCase.StartNewTransfer();
        _transferQrCode = null;
        view.ClearDraftImages();
        view.DisplayRejectedImages([]);
        UpdateDraftState(0);
        SetDraftEditingEnabled(true);
        DisplayLanAddressSelection();
        view.DisplayDraftState();
    }

    private void UpdateDraftState(int count)
    {
        _draftCount = count;
        view.SetDraftCount(count);
        view.SetDraftActionsEnabled(_isEditingEnabled && count > 0);
    }

    private void DisplayLanAddressSelection() =>
        view.DisplayLanAddresses(
            _lanAddressOptions
                .Select(option => new LanAddressViewModel(
                    option.Address.ToString(),
                    $"{option.InterfaceName} — {option.Address}"))
                .ToArray(),
            _selectedLanAddress?.ToString());

    private void DisplayNetworkDiagnostics() =>
        view.DisplayNetworkDiagnostics(new NetworkDiagnosticsViewModel(
            serverEndpoint?.IsRunning == true,
            serverStartFailed,
            _selectedLanAddress?.ToString(),
            serverEndpoint?.Port ?? 0));

    private void ApplyBackgroundResult(BackgroundCustomizationResult result)
    {
        view.ApplyBackground(new BackgroundViewModel(
            result.ImagePng,
            result.Settings.OpacityPercent,
            result.Settings.ZoomPercent,
            result.Settings.OffsetX,
            result.Settings.OffsetY));
        view.DisplayBackgroundError(result.Error switch
        {
            BackgroundCustomizationError.None => null,
            BackgroundCustomizationError.ImageUnreadable =>
                "背景画像を読み込めません。",
            BackgroundCustomizationError.SettingsCouldNotBeSaved =>
                "背景設定を保存できません。",
            _ => "背景設定を反映できませんでした。",
        });
    }

    private void ApplyTraySettingsResult(TraySettingsResult result)
    {
        _minimizeToTray = result.Settings.MinimizeToTray;
        view.SetTrayMode(_minimizeToTray);
        view.DisplayTraySettingsError(result.Error == TraySettingsError.None
            ? null
            : "常駐設定を保存できません。");
    }

    private void ApplyAutoStartSettingsResult(AutoStartSettingsResult result)
    {
        view.SetAutoStartMode(result.Enabled);
        view.DisplayAutoStartError(result.Error == AutoStartSettingsError.None
            ? null
            : "自動起動設定を変更できません。");
    }

    private static string ToUserMessage(DraftImageRejectionReason reason) =>
        reason switch
        {
            DraftImageRejectionReason.UnsupportedFormat => "対応していない形式です。",
            DraftImageRejectionReason.FileTooLarge => "ファイルサイズが10MBを超えています。",
            DraftImageRejectionReason.DraftLimitReached => "Draftは最大20枚です。",
            DraftImageRejectionReason.UnreadableImage => "画像を読み込めません。",
            DraftImageRejectionReason.FileFormatMismatch => "拡張子と画像形式が一致しません。",
            DraftImageRejectionReason.DraftNotEditable => "転送中はDraftを変更できません。",
            _ => "画像を追加できません。",
        };
}
