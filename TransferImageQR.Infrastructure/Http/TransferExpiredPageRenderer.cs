namespace TransferImageQR.Infrastructure.Http;

internal static class TransferExpiredPageRenderer
{
    public static string Render() =>
        """
        <!doctype html>
        <html lang="ja">
        <head>
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width, initial-scale=1">
          <meta name="color-scheme" content="light">
          <title>転送期限切れ</title>
          <style>
            * { box-sizing: border-box; }
            html { background: #f3f5f7; color: #17202a; font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif; }
            body { display: grid; min-height: 100vh; min-height: 100svh; margin: 0; padding: max(1rem, env(safe-area-inset-top)) max(1rem, env(safe-area-inset-right)) max(1rem, env(safe-area-inset-bottom)) max(1rem, env(safe-area-inset-left)); place-items: center; }
            main { width: min(100%, 32rem); padding: 2rem 1.5rem; border: 1px solid #d9e0e7; border-radius: 1rem; background: #fff; box-shadow: 0 4px 18px rgb(23 32 42 / 10%); text-align: center; }
            .icon { margin: 0 0 .75rem; font-size: 2.5rem; line-height: 1; }
            h1 { margin: 0 0 .75rem; font-size: clamp(1.5rem, 7vw, 2rem); line-height: 1.3; }
            p { margin: 0; color: #52606d; font-size: 1rem; line-height: 1.7; }
          </style>
        </head>
        <body>
          <main>
            <p class="icon" aria-hidden="true">⌛</p>
            <h1>この転送は期限切れです</h1>
            <p>PCで新しい転送を作成し、新しいQRコードを読み直してください。</p>
          </main>
        </body>
        </html>
        """;
}
