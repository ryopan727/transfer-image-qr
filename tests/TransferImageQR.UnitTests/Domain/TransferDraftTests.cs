using TransferImageQR.Domain.Drafts;
using Xunit;

namespace TransferImageQR.UnitTests.Domain;

public sealed class TransferDraftTests
{
    [Fact]
    public void Add_AppendsImagesInInputOrder()
    {
        var draft = new TransferDraft();

        var first = draft.Add(@"C:\images\first.jpg");
        var second = draft.Add(@"C:\images\second.png");

        Assert.Collection(
            draft.Images,
            image => Assert.Same(first, image),
            image => Assert.Same(second, image));
        Assert.Equal("first.jpg", first.FileName);
        Assert.Equal("second.png", second.FileName);
    }

    [Fact]
    public void Add_WhenCalledAgain_PreservesExistingImages()
    {
        var draft = new TransferDraft();
        draft.Add(@"C:\images\existing.webp");

        draft.Add(@"C:\images\additional.jpeg");

        Assert.Equal(2, draft.Images.Count);
        Assert.Equal("existing.webp", draft.Images[0].FileName);
        Assert.Equal("additional.jpeg", draft.Images[1].FileName);
    }

    [Fact]
    public void TryAdd_WhenDraftAlreadyContainsTwentyImages_RejectsTheTwentyFirst()
    {
        var draft = new TransferDraft();
        for (var index = 0; index < TransferDraft.MaximumImageCount; index++)
        {
            Assert.True(draft.TryAdd($@"C:\images\image-{index}.jpg", out _));
        }

        var added = draft.TryAdd(@"C:\images\image-21.jpg", out var image);

        Assert.False(added);
        Assert.Null(image);
        Assert.Equal(20, draft.Images.Count);
    }

    [Fact]
    public void Remove_WithExistingImageId_RemovesOnlyTheSelectedImage()
    {
        var draft = new TransferDraft();
        var first = draft.Add(@"C:\images\first.jpg");
        var second = draft.Add(@"C:\images\second.png");

        var removed = draft.Remove(first.Id);

        Assert.True(removed);
        Assert.Collection(draft.Images, image => Assert.Same(second, image));
    }

    [Fact]
    public void Remove_WithUnknownImageId_LeavesDraftUnchanged()
    {
        var draft = new TransferDraft();
        var image = draft.Add(@"C:\images\first.jpg");

        var removed = draft.Remove(Guid.NewGuid());

        Assert.False(removed);
        Assert.Collection(draft.Images, remaining => Assert.Same(image, remaining));
    }

    [Fact]
    public void Clear_WithImages_RemovesEveryImage()
    {
        var draft = new TransferDraft();
        draft.Add(@"C:\images\first.jpg");
        draft.Add(@"C:\images\second.png");

        draft.Clear();

        Assert.Empty(draft.Images);
    }
}
