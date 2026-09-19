using TransferImageQR.Presentation;

namespace TransferImageQR
{
    public partial class Form1 : Form, IMainView
    {
        private MainPresenter? _presenter;

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
    }
}
