namespace TransferImageQR.Infrastructure.Http;

internal static class TransferImageUnavailablePageRenderer
{
    public static string Render() =>
        """
        <!doctype html>
        <html lang="ja">
        <head>
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width, initial-scale=1">
          <meta name="color-scheme" content="light">
          <title>画像が見つかりません</title>
          <style>
            * { box-sizing: border-box; }
            html { background: #f3f5f7; color: #17202a; font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif; }
            body { display: grid; min-height: 100vh; min-height: 100svh; margin: 0; padding: 1rem; place-items: center; }
            main { width: min(100%, 32rem); padding: 2rem 1.5rem; border: 1px solid #d9e0e7; border-radius: 1rem; background: #fff; text-align: center; }
            h1 { margin: 0 0 .75rem; font-size: clamp(1.5rem, 7vw, 2rem); }
            p { margin: 0; color: #52606d; line-height: 1.7; }
          </style>
        </head>
        <body>
          <main>
            <h1>元画像が見つかりません</h1>
            <p>PCで画像を追加し直し、新しい転送を作成してください。</p>
          </main>
        </body>
        </html>
        """;
}
