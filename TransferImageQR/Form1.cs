using TransferImageQR.Presentation;

namespace TransferImageQR
{
    public partial class Form1 : Form, IMainView
    {
        private MainPresenter? _presenter;
        private bool _draftActionsEnabled;
        private bool _draftEditingEnabled = true;

        public Form1()
        {
            InitializeComponent();
        }

        public void AttachPresenter(MainPresenter presenter)
        {
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
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

        private void UpdateDraftButtonState()
        {
            var canEditDraft = _draftEditingEnabled && _draftActionsEnabled;
            removeDraftImageButton.Enabled = canEditDraft && draftListView.SelectedItems.Count == 1;
            clearDraftButton.Enabled = canEditDraft;
            createQrButton.Enabled = canEditDraft;
        }
    }
}
