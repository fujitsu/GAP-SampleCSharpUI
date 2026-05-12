using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;

namespace SampleCSharpUI.Commons
{
    internal class HttpHelper
    {
        private static HttpClient SendClient { get; set; } = null;

        // Stream受信イベント通知用
        internal class StreamEventArgs : EventArgs
        {
            public Guid Id { get; }
            public string EventText { get; }

            public StreamEventArgs(Guid id, string eventText)
            {
                Id = id;
                EventText = eventText;
            }
        }

        // 部分受信を購読するためのイベント（購読側で UI スレッドに戻す必要がある場合は SynchronizationContext を利用）
        internal static event EventHandler<StreamEventArgs> StreamEventReceived;

        // GET リクエスト送信 
        internal static async Task<string> GetRequestAsync(string apiPath, string idToken)
        {
            var answer = string.Empty;

            if (SendClient == null)
            {
                var ch = new HttpClientHandler();
                ch.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPlicyErrors) => true;
                SendClient = new HttpClient(ch);
            }
            if (SendClient != null)
            {
                using (var request = new HttpRequestMessage())
                {
                    request.Method = HttpMethod.Get;
                    request.RequestUri = new Uri($"https://{Config.TenantName}.generative-ai-platform.cloud.global.fujitsu.com{apiPath}");
                    request.Headers.Add("Authorization", string.Format("Bearer {0}", idToken));
                    using (var response = await SendClient.SendAsync(request))
                    {
                        response.EnsureSuccessStatusCode();
                        answer = await response.Content.ReadAsStringAsync();
                    }
                }
            }
            return answer;
        }

        // DELETE リクエスト送信 
        internal static async Task<string> DeleteRequestAsync(string apiPath, string idToken)
        {
            var answer = string.Empty;

            if (SendClient == null)
            {
                var ch = new HttpClientHandler();
                ch.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPlicyErrors) => true;
                SendClient = new HttpClient(ch);
            }
            if (SendClient != null)
            {
                using (var request = new HttpRequestMessage())
                {
                    request.Method = HttpMethod.Delete;
                    request.RequestUri = new Uri($"https://{Config.TenantName}.generative-ai-platform.cloud.global.fujitsu.com{apiPath}");
                    request.Headers.Add("Authorization", string.Format("Bearer {0}", idToken));
                    using (var response = await SendClient.SendAsync(request))
                    {
                        response.EnsureSuccessStatusCode();
                        answer = await response.Content.ReadAsStringAsync();
                    }
                }
            }
            return answer;
        }

        // POST リクエスト送信 
        internal static async Task<string> PostRequestAsync(string apiPath, string idToken, string requestBody)
        {
            // JSON 文字列を送信する場合は StringContent を作成して共通メソッドへ渡す
            var content = new StringContent(requestBody ?? string.Empty, Encoding.UTF8, "application/json");
            return await SendRequestAsync(HttpMethod.Post, apiPath, idToken, content);
        }
        internal static async Task<string> PostRequestAsync(string apiPath, string idToken, MultipartFormDataContent requestBody)
        {
            // MultipartFormDataContent はそのまま渡せる
            return await SendRequestAsync(HttpMethod.Post, apiPath, idToken, requestBody);
        }
        internal static async Task<string> PostRequestStreamAsync(Guid id, string apiPath, string idToken, string requestBody)
        {
            var content = new StringContent(requestBody ?? string.Empty, Encoding.UTF8, "application/json");
            return await SendRequestStreamAsync(id, HttpMethod.Post, apiPath, idToken, content);
        }
        // PUT リクエスト送信
        internal static async Task<string> PutRequestAsync(string apiPath, string idToken, string requestBody)
        {
            // PUT も JSON 文字列をコンテンツ化して送信
            var content = new StringContent(requestBody ?? string.Empty, Encoding.UTF8, "application/json");
            return await SendRequestAsync(HttpMethod.Put, apiPath, idToken, content);
        }
        internal static async Task<string> PutRequestAsync(string apiPath, string idToken, MultipartFormDataContent requestBody)
        {
            // PUT も JSON 文字列をコンテンツ化して送信
            return await SendRequestAsync(HttpMethod.Put, apiPath, idToken, requestBody);
        }

        // 共通送信メソッド: HttpContent を受け取ることで文字列/マルチパート両対応
        private static async Task<string> SendRequestAsync(HttpMethod method, string apiPath, string idToken, System.Net.Http.HttpContent content = null)
        {
            var answer = string.Empty;

            // 送信用HttpClientは利用できるときは再利用する 
            if (SendClient == null)
            {
                //証明書エラーを無視(高セキュリティ環境では適切に検証すること)
                var ch = new HttpClientHandler();
                ch.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPlicyErrors) => true;
                SendClient = new HttpClient(ch);
            }

            // 送信用HttpClientが利用可能な場合に送信処理を実行
            if (SendClient != null)
            {
                using (var request = new HttpRequestMessage())
                {
                    request.Method = method;
                    request.RequestUri = new Uri($"https://{Config.TenantName}.generative-ai-platform.cloud.global.fujitsu.com{apiPath}");
                    request.Headers.Add("Authorization", string.Format("Bearer {0}", idToken));

                    // content が null の場合は Content を設定しない
                    if (content != null)
                    {
                        request.Content = content;
                    }

                    // リクエスト送信とレスポンス受信
                    using (var response = await SendClient.SendAsync(request))
                    {
                        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                        {
                            answer = await response.Content.ReadAsStringAsync();

                        }
                        else
                        {
                            response.EnsureSuccessStatusCode();
                            answer = await response.Content.ReadAsStringAsync();
                        }
                    }
                }
            }
            return answer;
        }

        // Stream受信メソッド
        private static async Task<string> SendRequestStreamAsync(Guid id, HttpMethod method, string apiPath, string idToken, System.Net.Http.HttpContent content = null)
        {
            var answer = string.Empty;

            // 送信用HttpClientは利用できるときは再利用する 
            if (SendClient == null)
            {
                //証明書エラーを無視(高セキュリティ環境では適切に検証すること)
                var ch = new HttpClientHandler();
                ch.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPlicyErrors) => true;
                SendClient = new HttpClient(ch);
            }

            // 送信用HttpClientが利用可能な場合に送信処理を実行
            if (SendClient != null)
            {
                // 呼び出し元の SynchronizationContext を保持（UI スレッドに戻したい購読者がいる場合に利用）
                var sync = SynchronizationContext.Current;
                using (var request = new HttpRequestMessage())
                {
                    request.Method = method;
                    request.RequestUri = new Uri($"https://{Config.TenantName}.generative-ai-platform.cloud.global.fujitsu.com{apiPath}");
                    request.Headers.Add("Authorization", string.Format("Bearer {0}", idToken));

                    // content が null の場合は Content を設定しない
                    if (content != null)
                    {
                        request.Content = content;
                    }

                    // リクエスト送信とレスポンス受信（ヘッダ受信後にストリームを読み始める）
                    using (var response = await SendClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                    {
                        response.EnsureSuccessStatusCode();
                        var stream = await response.Content.ReadAsStreamAsync();
                        using (var reader = new StreamReader(stream, Encoding.UTF8))
                        {
                            var line = string.Empty;
                            var sb = new StringBuilder();
                            while (!((line = await reader.ReadLineAsync()) is null))
                            {
                                if (line.Length == 0)
                                {
                                    // 1イベント終了
                                    var eventText = sb.ToString();
                                    sb.Clear();

                                    // ログ（デバッグ）
                                    Console.WriteLine("EVENT:\n" + eventText + "\n---");

                                    // イベントを通知（購読側で UI スレッドに戻したい場合は SynchronizationContext を使う）
                                    var handler = StreamEventReceived;
                                    if (handler != null)
                                    {
                                        var args = new StreamEventArgs(id, eventText);
                                        if (sync != null)
                                        {
                                            sync.Post(_ => handler(typeof(HttpHelper), args), null);
                                        }
                                        else
                                        {
                                            handler(typeof(HttpHelper), args);
                                        }
                                    }

                                    // TODO: ここで id/event/data/retry をパースして個別に通知することも可能
                                    continue;
                                }

                                // コメント（:）も含めて蓄積
                                sb.AppendLine(line);
                            }

                            // ストリームが終了したときに最後の未完のバッファがあれば通知
                            if (sb.Length > 0)
                            {
                                var remaining = sb.ToString();
                                var handler = StreamEventReceived;
                                if (handler != null)
                                {
                                    var args = new StreamEventArgs(id, remaining);
                                    if (sync != null)
                                    {
                                        sync.Post(_ => handler(typeof(HttpHelper), args), null);
                                    }
                                    else
                                    {
                                        handler(typeof(HttpHelper), args);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return answer;
        }

        /// <summary>
        /// JSON の Unicode エスケープ (例: "\u30cd\u30c3\u30c8") をデコードして実際の文字列を返す
        /// </summary>
        /// <param name="value">エスケープされた文字列</param>
        /// <returns>デコード済み文字列</returns>
        internal static string DecodeEscapedUnicode(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            // 既に普通の文字が入っている場合はそのまま返す
            if (!value.Contains("\\u"))
                return value;

            try
            {
                // Regexで \uXXXX をデコード
                return Regex.Replace(
                    value,
                    @"\\u(?<Value>[a-fA-F0-9]{4})",
                    m => ((char)int.Parse(m.Groups["Value"].Value, NumberStyles.HexNumber)).ToString()
                );
            }
            catch
            {
                // フォールバック: Regex.Unescape を試す
                try
                {
                    return Regex.Unescape(value);
                }
                catch
                {
                    // どちらも失敗したら元の文字列を返す
                    return value;
                }
            }
        }
    }
}
