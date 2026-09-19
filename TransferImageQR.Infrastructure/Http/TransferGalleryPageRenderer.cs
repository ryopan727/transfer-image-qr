using System.Net;
using System.Text;
using TransferImageQR.Domain.Sessions;

namespace TransferImageQR.Infrastructure.Http;

internal static class TransferGalleryPageRenderer
{
    public static string Render(TransferSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        var html = new StringBuilder();
        html.Append(
            """
            <!doctype html>
            <html lang="ja">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <meta name="color-scheme" content="light">
              <title>転送画像</title>
              <style>
                * { box-sizing: border-box; }
                html { background: #f3f5f7; color: #17202a; font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif; }
                body { margin: 0; min-width: 0; }
                main { width: min(100%, 60rem); margin: 0 auto; padding: max(1rem, env(safe-area-inset-top)) max(1rem, env(safe-area-inset-right)) max(1.5rem, env(safe-area-inset-bottom)) max(1rem, env(safe-area-inset-left)); }
                h1 { margin: 0; font-size: clamp(1.5rem, 6vw, 2rem); line-height: 1.25; }
                .count { margin: .4rem 0 1.25rem; color: #52606d; }
                .gallery { display: grid; grid-template-columns: repeat(auto-fit, minmax(min(10rem, 100%), 1fr)); gap: .875rem; }
                .image-card { display: block; min-width: 0; overflow: hidden; border: 1px solid #d9e0e7; border-radius: .8rem; background: #fff; color: inherit; text-decoration: none; box-shadow: 0 2px 8px rgb(23 32 42 / 8%); touch-action: manipulation; }
                .image-card:focus-visible { outline: .2rem solid #1769aa; outline-offset: .15rem; }
                .thumbnail { aspect-ratio: 1; overflow: hidden; background: #e8edf2; }
                .thumbnail img { display: block; width: 100%; height: 100%; object-fit: cover; }
                .file-name { min-height: 3rem; margin: 0; padding: .75rem; overflow-wrap: anywhere; font-size: .95rem; font-weight: 600; line-height: 1.45; }
                @media (max-width: 24rem) {
                  .gallery { grid-template-columns: repeat(2, minmax(0, 1fr)); gap: .625rem; }
                  .file-name { padding: .65rem; font-size: .875rem; }
                }
              </style>
            </head>
            <body>
              <main>
                <h1>転送画像</h1>
            """);
        html.Append("    <p class=\"count\">")
            .Append(session.Images.Count)
            .AppendLine("枚の画像</p>");
        html.AppendLine("    <section class=\"gallery\" aria-label=\"転送画像一覧\">");

        foreach (var image in session.Images)
        {
            var imagePath = WebUtility.HtmlEncode(
                $"/transfer/{Uri.EscapeDataString(session.Token)}/images/{image.Id}");
            var fileName = WebUtility.HtmlEncode(image.FileName);
            html.Append("      <a class=\"image-card\" href=\"")
                .Append(imagePath)
                .AppendLine("\">")
                .Append("        <div class=\"thumbnail\"><img src=\"")
                .Append(imagePath)
                .Append("\" alt=\"")
                .Append(fileName)
                .AppendLine("\" loading=\"lazy\" decoding=\"async\"></div>")
                .Append("        <p class=\"file-name\">")
                .Append(fileName)
                .AppendLine("</p>")
                .AppendLine("      </a>");
        }

        html.Append(
            """
                </section>
              </main>
            </body>
            </html>
            """);
        return html.ToString();
    }
}
