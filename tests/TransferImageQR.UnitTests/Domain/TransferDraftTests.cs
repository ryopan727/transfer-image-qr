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
}
