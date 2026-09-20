namespace TransferImageQR.Application.Backgrounds;

public interface IBackgroundImageLoader
{
    byte[] LoadAsPng(string filePath);
}
