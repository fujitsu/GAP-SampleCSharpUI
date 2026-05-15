using SampleCSharpUI.Commons;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SampleCSharpUI.Models
{
    internal class RetrieverModel : INotifyPropertyChanged
    {
        // IDトークン(有効期限があるのでMainVMから参照)
        private string IdToken { get { return App.MainVM.IdToken; } }

        /// <summary>
        /// リトリーバー一覧を表すコレクション
        /// </summary>
        public ObservableCollection<Models.TDataRetriever> Retrievers { get; set; } = new ObservableCollection<Models.TDataRetriever>();

        /// <summary>
        /// 利用リトリーバー
        /// </summary>
        private Models.TDataRetriever _SelectedRetriever = null;
        public Models.TDataRetriever SelectedRetriever
        {
            get { return this._SelectedRetriever; }
            set
            {
                if (this._SelectedRetriever != value)
                {
                    this._SelectedRetriever = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 対象リトリーバ
        /// </summary>
        private APIData.TRetriever _RetrieverData = null;
        public APIData.TRetriever RetrieverData
        {
            get { return this._RetrieverData; }
            set
            {
                this._RetrieverData = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// リトリーバ名
        /// </summary>
        private string _RetrieverName = string.Empty;
        public string RetrieverName
        {
            get { return _RetrieverName; }
            set
            {
                _RetrieverName = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// コンストラクター
        /// </summary>
        public RetrieverModel()
        {
        }

        /// <summary>
        /// リトリーバー一覧取得
        /// </summary>
        /// <returns></returns>
        internal async Task GetRetrieversAsync(bool isUseNone = false)
        {
            // API呼び出し
            var jsonString = await HttpHelper.GetRequestAsync("/api/v1/retrievers", this.IdToken);

            // 一覧取得
            this.Retrievers.Clear();

            // リトリーバーなしの選択を作成
            if (isUseNone)
            {
                this.Retrievers.Add(new Models.TDataRetriever()
                {
                    ID = string.Empty,
                    Name = "(none)"
                });
            }

            // JSONデシリアライズ
            using (var json = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonString)))
            {
                var ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(APIData.TRetrievers));
                {
                    var result = ser.ReadObject(json) as APIData.TRetrievers;
                    foreach (var item in result.results?.OrderByDescending((x)=> x.created_at))
                    {
                        var retriever = new Models.TDataRetriever()
                        {
                            ID = item.id,
                            Name = item.name,
                            CreateDateTime = DateTimeOffset.FromUnixTimeMilliseconds(item.created_at).ToLocalTime().DateTime,
                        };
                        this.Retrievers.Add(retriever);
                    }
                    json.Close();
                }
            }

            if (this.Retrievers.Count > 0)
            {
                this.SelectedRetriever = this.Retrievers[0];
            }
        }

        /// <summary>
        /// リトリーバー取得処理
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        internal async Task GetRetrieverAsync(string id)
        {
            var jsonString = await HttpHelper.GetRequestAsync($"/api/v1/retrievers/{id}", this.IdToken);
            using (var json = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonString)))
            {
                var ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(APIData.TRetriever));
                {
                    this.RetrieverData = ser.ReadObject(json) as APIData.TRetriever;
                    this.RetrieverName = this.RetrieverData.name.Trim();
                    json.Close();
                }
            }
        }

        /// <summary>
        /// リトリーバー作成処理
        /// </summary>
        /// <param name="retrieverName"></param>
        /// <param name="urls">URL指定</param>
        /// <param name="fileNames">ファイル/フォルダ指定</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        internal async Task<string> CreateRetrieverFromDataAsync(string retrieverName, List<string> urls, List<string> fileNames)
        {
            var item = await CreateRetrieverAsync(retrieverName);
            var id = item?.id;

            // アップロードファイルからリトリーバーを作成
            await EmbbedingFromDataAsync(item, urls, fileNames);
            return id;
        }

        /// <summary>
        /// リトリーバー更新処理
        /// </summary>
        /// <param name="retriever"></param>
        /// <param name="urls"></param>
        /// <param name="fileNames"></param>
        /// <returns></returns>
        /// <remarks>APIの仕様上、リトリーバー更新はできないため、新規作成して差し替える</remarks>
        internal async Task<string> SetRetrieverFromDataAsync(APIData.TRetriever retriever, string name, List<string> urls, List<string> fileNames)
        {
            // 新規リトリーバ作成
            var newRetrieverID = await CreateRetrieverFromDataAsync(name, urls, fileNames);
            try
            {
                // 既存チャットルームのリトリーバー差し替え(ただし自分がownerのルームのみ)
                using (var roomModel = new ChatRoomModel())
                {
                    await roomModel.ReplaceRetrieverInOwnedRoomsAsync(retriever.id, newRetrieverID);
                }

                // 既存リトリーバー削除
                await DeleteRetrieverAsync(retriever.id);
            }
            catch (Exception)
            {
                // 失敗したら新規作成したリトリーバーを削除
                await DeleteRetrieverAsync(retriever.id);
                throw;
            }
            return newRetrieverID;
        }

        // リトリーバー領域作成処理
        private async Task<APIData.TRetriever> CreateRetrieverAsync(string retrieverName)
        {
            var result = new APIData.TRetriever();
            var body = new APIData.TRetrieverRoom()
            {
                name = retrieverName,
            };

            // ここで body を JSON 文字列にシリアライズして変数に格納する
            using (var ms = new MemoryStream())
            {
                var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(APIData.TRetrieverRoom));
                {
                    serializer.WriteObject(ms, body);
                    var bodyJsonString = Encoding.UTF8.GetString(ms.ToArray());
                    var jsonString = await HttpHelper.PostRequestAsync("/api/v1/retrievers", this.IdToken, bodyJsonString);
                    using (var json = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonString)))
                    {
                        var ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(APIData.TRetriever));
                        {
                            result = ser.ReadObject(json) as APIData.TRetriever;
                            json.Close();
                        }
                    }
                }
            }
            return result;
        }

        // ベクター化処理
        private async Task<string> EmbbedingFromDataAsync(APIData.TRetriever retriever, List<string> urls, List<string> fileNames)
        {
            var id = retriever.id;
            var fileIds = new List<string>();

            // URLからのアップロード
            if (urls != null)
            {
                var items = await UploadFromUrlsAsync(retriever.name, urls);
                foreach (var item in items)
                {
                    fileIds.Add(item);
                }
            }

            // ファイルリストからのアップロード
            if (fileNames.Count > 0)
            {
                var items = await UploadFromFilesAsync(retriever.name, fileNames);
                foreach (var item in items)
                {
                    fileIds.Add(item);
                }
            }

            try
            {
                if (!string.IsNullOrEmpty(id))
                {
                    // ベクターデータ作成処理
                    await EmbeddingAsync(id, fileIds);
                }
            }
            catch (Exception ex)
            {
                // RAGデータをクリーンアップするために、ここでリトリーバー削除処理を追加する。  
                if (!string.IsNullOrEmpty(id))
                {
                    // ベクターデータ作成処理
                    await DeleteRetrieverAsync(id);
                }

                // 例外発生時のエラー処理は呼び出し元で行う。
                throw new Exception("ベクターデータ作成に失敗しました。", ex);
            }
            finally
            {
                // 例外の有無にかかわらずファイル削除処理は実行する
                foreach (var fileId in fileIds)
                {
                    await DeleteFileAsync(fileId);
                }
            }
            return id;
        }

        // URLからのアップロード
        private async Task<List<string>> UploadFromUrlsAsync(string retrieverName, List<string> urls)
        {
            var id = string.Empty;
            var fileIds = new List<string>();

            // 登録するUrlを設定
            var prefix = $"{DateTime.UtcNow.ToLocalTime().ToString("yyyyMMdd")}_";
            var body = new TUploadDatas()
            {
                items = urls.Select(item => new TUploadData()
                {
                    name = $"{prefix}{item}",
                    url = item
                }).ToArray()
            };

            // ここで body を JSON 文字列にシリアライズして変数に格納する
            using (var ms = new MemoryStream())
            {
                var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(TUploadDatas));
                {
                    serializer.WriteObject(ms, body);
                    var bodyJsonString = Encoding.UTF8.GetString(ms.ToArray());
                    var requestBody = new MultipartFormDataContent();
                    var jsonContent = new StringContent(bodyJsonString, Encoding.UTF8, "application/json");
                    jsonContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                    {
                        Name = "body",
                    };
                    requestBody.Add(jsonContent);
                    var jsonString = await HttpHelper.PostRequestAsync("/api/v1/files/from-url", this.IdToken, requestBody);
                    using (var json = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonString)))
                    {
                        var ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(TFiles));
                        {
                            var result = ser.ReadObject(json) as TFiles;

                            // LINQで書き換え: fileIdsを結果のid列から生成
                            fileIds = (result?.results ?? new TFile[0]).Select(item => item.id).ToList();
                            json.Close();
                        }
                    }
                }
            }
            return fileIds;
        }

        // ファイルLからのアップロード
        private async Task<List<string>> UploadFromFilesAsync(string retrieverName, List<string> fileNames)
        {
            var id = string.Empty;
            var fileIds = new List<string>();

            // 登録するUrlを設定
            var prefix = $"{DateTime.UtcNow.ToLocalTime().ToString("yyyyMMdd")}_";
            var body = new TUploadDatas()
            {
                items = fileNames.Select(item => new TUploadData()
                {
                    name = (item != null && item.StartsWith(prefix)) ? System.IO.Path.GetFileName(item) : $"{prefix}{System.IO.Path.GetFileName(item)}",
                    url = item
                }).ToArray()
            };

            // ここで body を JSON 文字列にシリアライズして変数に格納する
            using (var ms = new MemoryStream())
            {
                var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(TUploadDatas));
                {
                    serializer.WriteObject(ms, body);
                    var bodyJsonString = Encoding.UTF8.GetString(ms.ToArray());
                    var requestBody = new MultipartFormDataContent();
                    foreach (var path in body.items)
                    {
                        // 実ファイルのパスを渡している前提 (path がフルパス)
                        // 要件に合わせてファイルの読み込み方法を調整してください
                        byte[] fileBytes;
                        try
                        {
                            fileBytes = File.ReadAllBytes(path.url);
                        }
                        catch (Exception)
                        {
                            // ファイル読み込み失敗時はスキップ
                            continue;
                        }

                        // MIME タイプを調べる
                        var fileContent = new ByteArrayContent(fileBytes);
                        var mime = "application/octet-stream";
                        try
                        {
                            if (!string.IsNullOrEmpty(path.name))
                            {
                                var detected = GetMimeType(path.name);
                                if (!string.IsNullOrEmpty(detected))
                                {
                                    mime = detected;
                                }
                            }
                        }
                        catch
                        {
                            mime = "application/octet-stream";
                        }
                        fileContent.Headers.ContentType = new MediaTypeHeaderValue(mime);

                        // Content-Disposition を設定（name="files"; filename="gand.html" 形式）
                        // Add(HttpContent, name, fileName) を使うと自動で Content-Disposition が正しく設定される
                        requestBody.Add(fileContent, "files", path.name);
                    }
                    var jsonContent = new StringContent(bodyJsonString, Encoding.UTF8, "application/json");
                    jsonContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                    {
                        Name = "body",
                    };
                    requestBody.Add(jsonContent);
                    var jsonString = await HttpHelper.PostRequestAsync("/api/v1/files", this.IdToken, requestBody);
                    using (var json = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonString)))
                    {
                        var ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(TFiles));
                        {
                            var result = ser.ReadObject(json) as TFiles;

                            // LINQで書き換え: fileIdsを結果のid列から生成
                            fileIds = (result?.results ?? new TFile[0]).Select(item => item.id).ToList();
                            json.Close();
                        }
                    }
                }
            }
            return fileIds;
        }

        // ベクターデータ作成処理
        private async Task EmbeddingAsync(string id, List<string> fileIds)
        {
            // リトリーバー作成API呼び出し
            var body = new TFileIDs()
            {
                file_ids = fileIds.ToArray(),
            };

            // ここで body を JSON 文字列にシリアライズして変数に格納する
            using (var ms = new MemoryStream())
            {
                var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(TFileIDs));
                {
                    serializer.WriteObject(ms, body);
                    var bodyJsonString = Encoding.UTF8.GetString(ms.ToArray());
                    var jsonString = await HttpHelper.PostRequestAsync($"/api/v1/retrievers/{id}/process/embeddings", this.IdToken, bodyJsonString);
                }
            }

            // 処理完了までポーリング
            var originalIds = new List<string>();
            while (true)
            {
                var jsonString = await HttpHelper.GetRequestAsync($"/api/v1/retrievers/{id}", this.IdToken);
                using (var json = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonString)))
                {
                    var ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(APIData.TRetriever));
                    {
                        var result = ser.ReadObject(json) as APIData.TRetriever;
                        originalIds = result.origin_ids?.ToList();
                    }
                }
                if (fileIds.Count >= originalIds?.Count) break;
                await Task.Delay(1000);
            }
        }

        // RAG削除処理
        internal async Task DeleteRetrieverAsync(string id)
        {
            await HttpHelper.DeleteRequestAsync($"/api/v1/retrievers/{id}", this.IdToken);
        }

        // ファイル削除処理
        private async Task DeleteFileAsync(string id)
        {
            await HttpHelper.DeleteRequestAsync($"/api/v1/files/{id}", this.IdToken);
        }

        // System.WebのMimeMappingは.NET Framework専用で、.NET Core/5+では利用できません。
        // 代替として、拡張子からMIMEタイプを判定する簡易メソッドを追加します。
        private string GetMimeType(string fileName)
        {
            var ext = Path.GetExtension(fileName)?.ToLowerInvariant();
            switch (ext)
            {
                case ".txt": return "text/plain";
                case ".htm":
                case ".html": return "text/html";
                case ".csv": return "text/csv";
                case ".json": return "application/json";
                case ".pdf": return "application/pdf";
                case ".doc": return "application/msword";
                case ".docx": return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                case ".xls": return "application/vnd.ms-excel";
                case ".xlsx": return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                case ".png": return "image/png";
                case ".jpg":
                case ".jpeg": return "image/jpeg";
                case ".gif": return "image/gif";
                default: return "application/octet-stream";
            }
        }

        #region "JSON関連"
        [DataContract]
        public class TUploadDatas
        {
            [DataMember(Name = "params")]
            public TUploadData[] items { get; set; }
        }

        [DataContract]
        public class TUploadData
        {
            [DataMember]
            public string name { get; set; }
            [DataMember]
            public string url { get; set; }
        }

        [DataContract]
        public class TFiles
        {
            [DataMember]
            public TFile[] results { get; set; }
        }

        [DataContract]
        public class TFile
        {
            [DataMember]
            public string id { get; set; }
            [DataMember]
            public string name { get; set; }
            [DataMember]
            public string owner { get; set; }
            [DataMember(Name = "public")]
            public string pub { get; set; }
            [DataMember]
            public string[] origin_ids { get; set; }
            [DataMember]
            public bool is_deleted { get; set; }
            [DataMember]
            public long created_at { get; set; }
            [DataMember]
            public long? updated_at { get; set; }
            [DataMember]
            public long? deleted_at { get; set; }
            [DataMember]
            public string key { get; set; }
            [DataMember]
            public string content_type { get; set; }
            [DataMember]
            public string file_extension { get; set; }
            [DataMember]
            public string source_name { get; set; }
            [DataMember]
            public string storage_path { get; set; }
            [DataMember]
            public string target_storage { get; set; }
            [DataMember]
            public Metadata metadata { get; set; }
            [DataMember]
            public string task_id { get; set; }
        }

        [DataContract]
        public class Metadata
        {
        }

        [DataContract]
        public class TFileIDs
        {
            [DataMember]
            public string[] file_ids { get; set; }
        }
        #endregion

        // プロパティが変更されたときに通知するイベント
        public event PropertyChangedEventHandler PropertyChanged;

        // プロパティ変更通知を発行するメソッド
        protected virtual void OnPropertyChanged([CallerMemberName] String propertyName = "")
        {
            var handler = this.PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}