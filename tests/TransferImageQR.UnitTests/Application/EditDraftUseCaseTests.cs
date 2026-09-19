using TransferImageQR.Application.Drafts;
using TransferImageQR.Domain.Drafts;
using Xunit;

namespace TransferImageQR.UnitTests.Application;

public sealed class EditDraftUseCaseTests
{
    [Fact]
    public void Remove_WithExistingImage_ReturnsUpdatedCount()
    {
        var draft = new TransferDraft();
        var selected = draft.Add(@"C:\images\selected.jpg");
        draft.Add(@"C:\images\remaining.png");
        var sut = new EditDraftUseCase(draft);

        var result = sut.Remove(selected.Id);

        Assert.True(result.Changed);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal("remaining.png", Assert.Single(draft.Images).FileName);
    }

    [Fact]
    public void Clear_RemovesAllImagesAndReturnsZeroCount()
    {
        var draft = new TransferDraft();
        draft.Add(@"C:\images\first.jpg");
        draft.Add(@"C:\images\second.png");
        var sut = new EditDraftUseCase(draft);

        var result = sut.Clear();

        Assert.True(result.Changed);
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(draft.Images);
    }

    [Fact]
    public void RemoveAndClear_AfterDraftConfirmation_DoNotChangeImages()
    {
        var draft = new TransferDraft();
        var image = draft.Add(@"C:\images\confirmed.jpg");
        draft.Confirm();
        var sut = new EditDraftUseCase(draft);

        var removeResult = sut.Remove(image.Id);
        var clearResult = sut.Clear();

        Assert.False(removeResult.Changed);
        Assert.False(clearResult.Changed);
        Assert.Equal(1, removeResult.TotalCount);
        Assert.Equal(1, clearResult.TotalCount);
        Assert.Collection(draft.Images, remaining => Assert.Same(image, remaining));
    }
}
