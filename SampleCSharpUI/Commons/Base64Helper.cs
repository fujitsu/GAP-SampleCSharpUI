using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace SampleCSharpUI.Commons
{
    /// <summary>
    /// 画像データをBase64エンコードするためのヘルパーメソッドを提供します。
    /// </summary>
    internal static class Base64Helper
    {
        /// <summary>
        /// 指定したファイルパスの画像ファイルを非同期に読み込み、Base64エンコードした文字列を返します。
        /// </summary>
        /// <param name="path">読み込む画像ファイルのパス。null、空白は許可されません。</param>
        /// <returns>画像データをBase64エンコードした文字列を表す非同期タスク。</returns>
        /// <exception cref="ArgumentException"><paramref name="path"/> が null または空白の場合にスローされます。</exception>
        /// <exception cref="FileNotFoundException">指定したパスのファイルが存在しない場合にスローされる可能性があります。</exception>
        /// <exception cref="UnauthorizedAccessException">ファイルアクセス権が不足している場合にスローされる可能性があります。</exception>
        /// <exception cref="NotSupportedException">ファイル サイズが int.MaxValue を超える場合にスローされます（メモリ処理不可）。</exception>
        /// <remarks>
        /// このメソッドはファイル全体をメモリに読み込んでからBase64変換を行います。
        /// 大きなファイルを扱う場合はメモリ使用量に注意してください。
        /// ファイルの部分読み込みを考慮した安全な読み取りループを使用しています。
        /// </remarks>
        internal static async Task<string> ImageFileToBase64Async(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("path is null or empty", nameof(path));

            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
            {
                if (fs.Length > int.MaxValue)
                    throw new NotSupportedException("File too large to process in memory.");

                var bytes = new byte[checked((int)fs.Length)];
                var read = 0;
                while (read < bytes.Length)
                {
                    var r = await fs.ReadAsync(bytes, read, bytes.Length - read).ConfigureAwait(false);
                    if (r == 0) break;
                    read += r;
                }

                return Convert.ToBase64String(bytes);
            }
        }

        /// <summary>
        /// 指定した URL からバイト配列を取得し、Base64 エンコードした文字列を返します。
        /// </summary>
        /// <param name="url">画像などのリソースを取得する URL。</param>
        /// <returns>取得したデータをBase64エンコードした文字列を表す非同期タスク。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="url"/> が null の場合にスローされる可能性があります。</exception>
        /// <exception cref="HttpRequestException">HTTP 要求の送信や応答の取得に失敗した場合にスローされます。</exception>
        /// <remarks>
        /// この実装では呼び出しごとに新しい <see cref="HttpClient"/> を生成しています。
        /// 長時間または高頻度で使用する場合は、ソケット枯渇を避けるために <see cref="HttpClient"/> を再利用することを検討してください。
        /// </remarks>
        internal static async Task<string> ImageUrlToBase64Async(string url)
        {
            using (var http = new HttpClient())
            {
                var bytes = await http.GetByteArrayAsync(url).ConfigureAwait(false);
                return Convert.ToBase64String(bytes);
            }
        }
    }
}
