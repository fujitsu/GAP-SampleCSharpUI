using SampleCSharpUI.Commons;
using SampleCSharpUI.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using static SampleCSharpUI.Commons.APIData;

namespace SampleCSharpUI.Models
{
    internal class ChatModel : INotifyPropertyChanged
    {
        private SynchronizationContext Context { get; set; } = SynchronizationContext.Current;
        public string IdToken { get; private set; } = string.Empty;

        #region "認証関連"
        private TimeSpan ExpireInterval;
        private DispatcherTimer ExpireTimer = new DispatcherTimer();

        /// <summary>
        /// GAP接続済
        /// </summary>
        private bool _IsLogin = false;
        public bool IsLogin
        {
            get { return _IsLogin; }
            private set
            {
                if (_IsLogin != value)
                {
                    _IsLogin = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Streaming受信中
        /// </summary>
        private bool _IsStreaming = false;
        public bool IsStreaming
        {
            get { return _IsStreaming; }
            private set
            {
                if (_IsStreaming != value)
                {
                    _IsStreaming = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Represents the username associated with the current user.
        /// </summary>
        private string _UserName = string.Empty;
        public string UserName
        {
            get { return _UserName; }
            private set
            {
                if (_UserName != value)
                {
                    _UserName = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 初期設定済
        /// </summary>
        public bool IsSettings { get { return !string.IsNullOrEmpty(Config.ClientId) && !string.IsNullOrEmpty(Config.TenantName); } }

        /// <summary>
        /// GAPと接続
        /// </summary>
        /// <returns></returns>
        internal async Task ConnectAsync()
        {
            this.ExpireTimer.Stop();

            // 接続処理
            this.IdToken = string.Empty;
            this.UserName = string.Empty;
            this.IsLogin = !string.IsNullOrEmpty(this.IdToken);
            using (var id = new MicrosoftIdentityModel())
            {
                if (Config.IsPromptAuthentication)
                {
                    await id.LoginAsync(Config.ClientId, Config.TenantName);
                }
                else
                {
                    await id.LoginAsync(Config.ClientId, Config.TenantName, Config.ClientSecret);
                }
                this.IdToken = id.IdToken;
                this.UserName = id.UserName;
                this.ExpireInterval = id.ExpireInterval;
                this.ExpireTimer.Interval = this.ExpireInterval;
            }
            if (!string.IsNullOrEmpty(this.IdToken))
            {
                this.ExpireTimer.Start();
            }
            this.IsLogin = !string.IsNullOrEmpty(this.IdToken);

            // チャットルーム一覧取得
            await GetChatRoomsAsync(true);
        }

        /// <summary>
        /// GAPとの切断
        /// </summary>
        /// <returns></returns>
        internal async Task DisconnectAsync()
        {
            this.ExpireTimer.Stop();
            this.IdToken = string.Empty;
            this.UserName = string.Empty;
            this.IsLogin = !string.IsNullOrEmpty(this.IdToken);
            using (var id = new MicrosoftIdentityModel())
            {
                await id.Logout();
            }
        }
        #endregion

        #region "チャット関連プロパティ"
        public ObservableCollection<Models.TDataChatRoom> ChatRooms { get; set; } = new ObservableCollection<Models.TDataChatRoom>();
        public ObservableCollection<TMessage> Messages { get; set; } = new ObservableCollection<TMessage>();
        public ObservableCollection<TItem> APIs { get; set; } = new ObservableCollection<TItem>()
        {
            new TItem { ID = "v2", Name = "Cohere v2", FullName = "Cohere v2 Chat API" },
            new TItem { ID = "OpenAI", Name = "Cohere OpenAI", FullName = "Cohere OpenAI compatibility Chat API" }
        };

        private Models.TDataChatRoom _SelectedChatRoom = null;
        public Models.TDataChatRoom SelectedChatRoom
        {
            get { return this._SelectedChatRoom; }
            set
            {
                if (this._SelectedChatRoom != value)
                {
                    this._SelectedChatRoom = value;
                    OnPropertyChanged();

                    // 選択済チャットルームID保存
                    if (this._SelectedChatRoom != null)
                    {
                        Config.SelectedChatRoomID = this._SelectedChatRoom?.ID;
                        Config.SaveProperties();
                    }
                }
            }
        }

        private TItem _SelectedAPI = null;
        public TItem SelectedAPI
        {
            get { return _SelectedAPI; }
            set
            {
                if (_SelectedAPI != value)
                {
                    _SelectedAPI = value;
                    OnPropertyChanged();
                }
            }
        }
        #endregion

        /// <summary>
        /// コンストラクター
        /// </summary>
        public ChatModel()
        {
            this.ExpireTimer.Stop();
            this.SelectedAPI = this.APIs[0];

            // 認証キー期限切れ対応
            this.ExpireTimer.Tick += async (s, e) =>
            {
                this.ExpireTimer.Stop();
                try
                {
                    using (var id = new MicrosoftIdentityModel())
                    {
                        if (Config.IsPromptAuthentication)
                        {
                            await id.LoginAsync(Config.ClientId, Config.TenantName);
                        }
                        else
                        {
                            await id.LoginAsync(Config.ClientId, Config.TenantName, Config.ClientSecret);
                        }
                        this.IdToken = id.IdToken;
                        this.UserName = id.UserName;
                        this.ExpireInterval = id.ExpireInterval;
                        this.ExpireTimer.Interval = this.ExpireInterval;
                    }
                    if (!string.IsNullOrEmpty(this.IdToken))
                    {
                        this.ExpireTimer.Start();
                    }
                }
                catch
                {
                    this.ExpireTimer.Start();
                }
            };
        }

        #region "チャット関連"
        // チャットルーム一覧取得処理
        internal async Task GetChatRoomsAsync(bool isUseNone = true)
        {
            const string defaultChatRoomName = "General Use";
            var defaultChatRoomID = string.Empty;

            // API呼び出し
            var jsonString = await HttpHelper.GetRequestAsync("/api/v1/chats", this.IdToken);

            // 一覧取得
            this.ChatRooms.Clear();

            // ルームなしの選択を作成
            if (isUseNone)
            {
                this.ChatRooms.Add(new Models.TDataChatRoom()
                {
                    ID = string.Empty,
                    Name = "(none)"
                });
            }

            // JSONデシリアライズ
            using (var json = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonString)))
            {
                var ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(List<APIData.TChat>));
                {
                    var results = ser.ReadObject(json) as List<APIData.TChat>;
                    foreach (var item in results?.OrderByDescending((x) => x.created_date))
                    {
                        var chatRoom = new Models.TDataChatRoom()
                        {
                            ID = item.id,
                            Name = item.name,
                            ChatTemplateId = item.chat_template_id,
                            RetrieverIDs = item.retriever_ids,
                            CreateDateTime = DateTimeOffset.FromUnixTimeMilliseconds(item.created_date).ToLocalTime().DateTime,
                        };
                        this.ChatRooms.Add(chatRoom);
                    }
                    json.Close();
                }
            }

            // [General Use」名のチャットルームがないときは作成
            defaultChatRoomID = this.ChatRooms.Where((x) => x.Name == defaultChatRoomName).FirstOrDefault()?.ID;
            if (this.ChatRooms.Count == 0 || string.IsNullOrEmpty(defaultChatRoomID))
            {
                defaultChatRoomID = await this.CreateChatRoomAsync(defaultChatRoomName, string.Empty);
            }
            else if (!string.IsNullOrEmpty(defaultChatRoomID))
            {
                // 「General Use」チャットルームの会話の内容をクリア
                await ClearChatRoomAsync(defaultChatRoomID);
            }

            // 保存したChatRoomIDのチャットがあれば選択、無ければ「General Use」を選択
            if (this.ChatRooms.Count > 0)
            {
                if (!string.IsNullOrEmpty(Config.SelectedChatRoomID))
                {
                    var savedChatRoomID = this.ChatRooms.FirstOrDefault((x) => x.ID == Config.SelectedChatRoomID)?.ID;
                    if (!string.IsNullOrEmpty(savedChatRoomID))
                    {
                        // 保存したChatRoomIDのチャットを選択
                        this.SelectedChatRoom = this.ChatRooms.FirstOrDefault((x) => x.ID == savedChatRoomID);
                    }
                    else
                    {
                        // Name が defaultChatRoomID の最初の要素を取得し、なければ先頭要素を選択する
                        var target = this.ChatRooms.FirstOrDefault((x) => x.ID == defaultChatRoomID);
                        this.SelectedChatRoom = target ?? this.ChatRooms[0];
                    }
                }
                else if (isUseNone)
                {
                    // ルームなしチャットを選択
                    this.SelectedChatRoom = this.ChatRooms[0];
                }
            }
        }

        /// <summary>
        /// チャットルーム作成処理
        /// </summary>
        /// <param name="name">ルーム名</param>
        /// <param name="retrieverID">リトリーバID</param>
        /// <returns></returns>
        internal async Task<string> CreateChatRoomAsync(string name, string retrieverID)
        {
            var id = string.Empty;
            var body = new APIData.TChatRoom()
            {
                name = name,
                retriever_ids = string.IsNullOrEmpty(retrieverID) ? new string[] { } : new string[] { retrieverID },
                chat_template_id = string.IsNullOrEmpty(retrieverID) ? "builtin.chat" : "builtin.document_combine",
                model = "cohere.command-r-plus-fujitsu"
            };

            // ここで body を JSON 文字列にシリアライズして変数に格納する
            using (var ms = new MemoryStream())
            {
                var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(APIData.TChatRoom));
                {
                    serializer.WriteObject(ms, body);
                    var bodyJsonString = Encoding.UTF8.GetString(ms.ToArray());
                    var jsonString = await HttpHelper.PostRequestAsync("/api/v1/chats", this.IdToken, bodyJsonString);
                    using (var json = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonString)))
                    {
                        var ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(APIData.TChat));
                        {
                            var result = ser.ReadObject(json) as APIData.TChat;
                            var chatRoom = new Models.TDataChatRoom()
                            {
                                ID = result.id,
                                Name = result.name,
                                ChatTemplateId = result.chat_template_id,
                                RetrieverIDs = result.retriever_ids,
                            };
                            this.ChatRooms.Add(chatRoom);
                            id = chatRoom.ID;
                            json.Close();

                            // チャットルーム設定処理
                            result.chat_setting.tokens_budget = new APIData.TTokensBudget()
                            {
                                history = string.IsNullOrEmpty(retrieverID) ? 125000 : 62500,
                                documents = string.IsNullOrEmpty(retrieverID) ? 0 : 62500,
                                answer = string.IsNullOrEmpty(retrieverID) ? 2048 : 2048
                            };
                            await SettingChatRoomAsync(id, result);
                        }
                    }
                }
            }
            return id;
        }

        // チャットルーム設定処理
        private async Task SettingChatRoomAsync(string id, APIData.TChat setting)
        {
            using (var ms = new MemoryStream())
            {
                var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(APIData.TChat));
                {
                    serializer.WriteObject(ms, setting);
                    var bodyJsonString = Encoding.UTF8.GetString(ms.ToArray());
                    var jsonString = await HttpHelper.PutRequestAsync($"/api/v1/chats/{id}", this.IdToken, bodyJsonString);
                }
            }
        }

        /// <summary>
        /// 会話一覧取得処理 
        /// </summary>
        /// <param name="id">ルームID</param>
        /// <returns></returns>
        internal async Task GetChatsAsync(string id)
        {
            // 一覧取得
            this.Messages.Clear();
            if (!string.IsNullOrEmpty(id))
            {
                var jsonString = await HttpHelper.GetRequestAsync($"/api/v1/chats/{id}/messages", this.IdToken);
                using (var json = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonString)))
                {
                    var ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(List<APIData.TChatMessage>));
                    {
                        var results = ser.ReadObject(json) as List<APIData.TChatMessage>;
                        foreach (var item in results)
                        {
                            var message = new TMessage()
                            {
                                Role = item.role,
                                Content = item.content,
                                Time = DateTimeOffset.FromUnixTimeMilliseconds(item.timeunix).ToLocalTime().DateTime,
                                Refs = item.ref_chunks is null ? new List<string>() : item.ref_chunks?.Select((x) => x.text.Replace("\n\n", "\n")).ToList(),
                            };
                            message.PropertyChanged += (s, e) => { OnPropertyChanged("Messages_Item"); };
                            this.Messages.Add(message);
                        }

                        // 最新行表示
                        OnPropertyChanged("Messages_Item");
                    }
                    json.Close();
                }
            }
        }

        /// <summary>
        /// チャットルーム内会話クリア処理 
        /// </summary>
        /// <param name="id">ルームID</param>
        /// <returns></returns>
        internal async Task ClearChatRoomAsync(string id)
        {
            //　チャットルーム情報取得
            var body = new APIData.TChat();
            if (!string.IsNullOrEmpty(id))
            {
                var jsonString = await HttpHelper.GetRequestAsync($"/api/v1/chats/{id}", this.IdToken);
                using (var json = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonString)))
                {
                    var ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(APIData.TChat));
                    {
                        body = ser.ReadObject(json) as APIData.TChat;
                    }
                    json.Close();
                }

                // 会話内容クリア
                body.messages = new APIData.TChatMessage[] { };

                // ここで body を JSON 文字列にシリアライズして変数に格納する
                using (var ms = new MemoryStream())
                {
                    var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(APIData.TChat));
                    {
                        serializer.WriteObject(ms, body);
                        var bodyJsonString = Encoding.UTF8.GetString(ms.ToArray());
                        jsonString = await HttpHelper.PutRequestAsync($"/api/v1/chats/{id}", this.IdToken, bodyJsonString);
                    }
                }
            }
        }

        /// <summary>
        /// プロンプト入力(チャットルームあり)
        /// </summary>
        /// <param name="id">ルームID</param>
        /// <param name="inputText">入力</param>
        internal async Task SendRoomMessageAsync(string id, string inputText)
        {
            var content = inputText ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(content))
            {
                var body = new APIData.TChatMessage()
                {
                    role = "user",
                    content = content,
                };

                // ここで body を JSON 文字列にシリアライズして変数に格納する
                using (var ms = new MemoryStream())
                {
                    var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(APIData.TChatMessage));
                    {
                        serializer.WriteObject(ms, body);
                        var bodyJsonString = Encoding.UTF8.GetString(ms.ToArray());
                        var jsonString = await HttpHelper.PostRequestAsync($"/api/v1/chats/{id}/messages", this.IdToken, bodyJsonString);
                        using (var json = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonString)))
                        {
                            var ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(APIData.TChatMessage));
                            {
                                // 質問のメッセージ追加
                                var item = ser.ReadObject(json) as APIData.TChatMessage;
                                if (item?.role != null)
                                {
                                    this.SetMessage(item.role,
                                        item.content,
                                        DateTimeOffset.FromUnixTimeMilliseconds(item.timeunix).ToLocalTime().DateTime,
                                        item.ref_chunks is null ? new List<string>() : item.ref_chunks?.Select((x) => x.text.Replace("\n\n", "\n")).ToList());
                                }
                                else
                                {
                                    throw new Exception(jsonString);
                                }
                            }
                            json.Close();

                            // 最新行表示
                            OnPropertyChanged("Messages_Item");
                        }

                        // AIからの回答取得
                        var msgId = this.SetMessage("ai", Resources.Streaming, DateTime.UtcNow.ToLocalTime(), new List<string>());
                        OnPropertyChanged("Messages_Item");
                        try
                        {
                            jsonString = await HttpHelper.PostRequestAsync($"/api/v1/chats/{id}/messages/createNextAiMessage", this.IdToken, "{}");
                            using (var json = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonString)))
                            {
                                var ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(APIData.TChatMessage));
                                {
                                    // 回答のメッセージ追加
                                    var item = ser.ReadObject(json) as APIData.TChatMessage;
                                    this.SetMessage(msgId,
                                        item.role,
                                        item.content,
                                        DateTimeOffset.FromUnixTimeMilliseconds(item.timeunix).ToLocalTime().DateTime,
                                        item.ref_chunks is null ? new List<string>() : item.ref_chunks?.Select((x) => x.text.Replace("\n\n", "\n")).ToList());
                                }
                                json.Close();
                            }

                            // 最新行表示
                            OnPropertyChanged("Messages_Item");
                        }
                        catch (Exception ex)
                        {
                            // エラー発生時はAI回答欄を削除し、exceptionを投げる
                            this.Messages.Remove(this.Messages.Where((x) => x.Id == msgId).FirstOrDefault());
                            throw ex;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// プロンプト入力(チャットルームあり：Stream受信)
        /// </summary>
        /// <param name="id">ルームID</param>
        /// <param name="inputText">入力</param>
        internal async Task SendRoomMessageStreamingAsync(string id, string inputText)
        {
            var content = inputText ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(content))
            {
                var body = new APIData.TChatMessage()
                {
                    role = "user",
                    content = content,
                };

                // 質問のメッセージ追加
                this.SetMessage("user", content, DateTime.UtcNow.ToLocalTime(), new List<string>());

                // ここで body を JSON 文字列にシリアライズして変数に格納する
                using (var ms = new MemoryStream())
                {
                    var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(APIData.TChatMessage));
                    {
                        serializer.WriteObject(ms, body);

                        // Streaming回答表示完了待ち
                        while (this.IsStreaming)
                        {
                            await Task.Delay(100);
                        }
                        this.IsStreaming = true;

                        try
                        {
                            var bodyJsonString = Encoding.UTF8.GetString(ms.ToArray());
                            var jsonString = await HttpHelper.PostRequestAsync($"/api/v1/chats/{id}/messages", this.IdToken, bodyJsonString);
                            using (var json = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonString)))
                            {
                                var ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(APIData.TChatMessage));
                                {
                                    // 回答のメッセージ追加
                                    var item = ser.ReadObject(json) as APIData.TChatMessage;
                                    if (item?.role != null)
                                    {
                                        // 質問は送信できたので回答のStreamingを待ち合わせる
                                    }
                                    else
                                    {
                                        // ここでLLM-GRでのBlockメッセージが返却される
                                        this.SetMessage("ai", jsonString, DateTime.UtcNow.ToLocalTime(), new List<string>());

                                        // 待ち合わせ処理をSKIPする
                                        throw new Exception(jsonString);
                                    }
                                }
                                json.Close();

                                // 最新行表示
                                OnPropertyChanged("Messages_Item");
                            }

                            // AIからの回答取得
                            var msgId = this.SetMessage("ai", Resources.Streaming, DateTime.UtcNow.ToLocalTime(), new List<string>());
                            OnPropertyChanged("Messages_Item");

                            // Stream受信イベントハンドラー登録
                            HttpHelper.StreamEventReceived += async (s, e) =>
                            {
                                // data: {"type": "on_create_next_ai_message_start"}
                                // data: {"type": "on_llm_new_token", "token": "\u30cd\u30c3\u30c8\u30ef\u30fc\u30af"}
                                // data: {"type": "on_create_next_ai_message_end"}
                                try
                                {
                                    var jsonEventString = e.EventText.Replace("data: ", "");
                                    using (var json = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonEventString)))
                                    {
                                        var ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(APIData.TChatStream));
                                        {
                                            // 回答のメッセージ追加
                                            var item = ser.ReadObject(json) as APIData.TChatStream;
                                            var targetId = e.Id;
                                            switch (item.type)
                                            {
                                                case "on_create_next_ai_message_start":
                                                    {
                                                        var message = this.Messages.Where((x) => x.Id == targetId).FirstOrDefault();
                                                        if (message != null)
                                                        {
                                                            this.SetMessage(targetId,
                                                                message.Role,
                                                                string.Empty,
                                                                message.Time,
                                                                message.Refs);
                                                        }
                                                        break;
                                                    }
                                                case "on_create_next_ai_message_end":
                                                    {
                                                        //TODO: 最終的な戻り値は、ルームの中の回答から取得する
                                                        await this.GetLastAIResponseAsync(id);
                                                        this.IsStreaming = false;
                                                        break;
                                                    }
                                                default:
                                                    {
                                                        // item.token が JSON の Unicode エスケープ (例: "\u30cd...") の場合に実際の文字に変換する
                                                        var tokenText = HttpHelper.DecodeEscapedUnicode(item.token);
                                                        var message = this.Messages.Where((x) => x.Id == targetId).FirstOrDefault();
                                                        if (message != null && !string.IsNullOrEmpty(tokenText))
                                                        {
                                                            // Streamingが重複して送られてくる場合があるため、重複分は追加しない
                                                            if (!message.Content.EndsWith(tokenText, StringComparison.Ordinal))
                                                            {
                                                                this.SetMessage(targetId,
                                                                    message.Role,
                                                                    message.Content + tokenText,
                                                                    message.Time,
                                                                    message.Refs);
                                                            }
                                                        }
                                                        break;
                                                    }
                                            }
                                        }
                                        json.Close();
                                    }
                                    OnPropertyChanged("Messages_Item");
                                }
                                catch (Exception ex)
                                {
                                    this.IsStreaming = false;
                                    // エラー発生時はAI回答欄にエラーを表示する（チャットルームの履歴が保持されるので、エラーは一時的なものとして扱う）。
                                    this.SetMessage(msgId, "ai", ex.Message, DateTime.UtcNow.ToLocalTime(), new List<string>());

                                    // エラー伝搬は省略（必要であればイベントを定義して伝搬すること）
                                    //throw ex;
                                }
                            };

                            // Stream受信開始
                            this.IsStreaming = true;
                            jsonString = await HttpHelper.PostRequestStreamAsync(msgId, $"/api/v1/chats/{id}/messages/createNextAiMessage/streaming", this.IdToken, "{}");
                        }
                        catch
                        {
                            this.IsStreaming = false;

                            // エラー伝搬は省略（必要であればイベントを定義して伝搬すること）
                            //throw ex;
                        }
                    }
                }
            }
        }
        private async Task GetLastAIResponseAsync(string id)
        {
            var jsonString = await HttpHelper.GetRequestAsync($"/api/v1/chats/{id}", this.IdToken);
            using (var json = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonString)))
            {
                var ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(APIData.TChat));
                {
                    var result = ser.ReadObject(json) as APIData.TChat;
                    var item = result.messages.Where((x) => x.role == "ai").LastOrDefault();
                    var lastItem = this.Messages.Where((x) => x.Role == "ai").LastOrDefault();
                    if (lastItem != null && !string.IsNullOrEmpty(lastItem.Content))
                    {
                        SetMessage(lastItem.Id,
                            item.role, item.content,
                            DateTimeOffset.FromUnixTimeMilliseconds(item.timeunix).ToLocalTime().DateTime,
                            item.ref_chunks is null ? new List<string>() : item.ref_chunks?.Select((x) => x.text.Replace("\n\n", "\n")).ToList());
                    }

                    // 最新行表示
                    OnPropertyChanged("Messages_Item");
                }
            }
        }

        /// <summary>
        /// プロンプト入力(チャットルームなし:GAP_SimpleChat API)
        /// </summary>
        /// <param name="inputText">入力</param>
        internal async Task SendMessageSimpleChatAsync(List<TMessage> histories, float temperature, int token, string inputText, string systemPrompt)
        {
            var content = inputText ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(content))
            {
                var body = new APIData.TNonRoomRequest()
                {
                    messages = this.SetSimpleChatHistories(histories),
                    question = content,
                    model = "cohere.command-r-plus-fujitsu",
                    max_tokens = token,
                    temperature = temperature,
                    top_p = 1,
                };

                // 質問のメッセージ追加
                this.SetMessage("user", content, DateTime.UtcNow.ToLocalTime(), new List<string>());

                // ここで body を JSON 文字列にシリアライズして変数に格納する
                using (var ms = new MemoryStream())
                {
                    var msgId = this.SetMessage("ai", Resources.Streaming, DateTime.UtcNow.ToLocalTime(), new List<string>());
                    OnPropertyChanged("Messages_Item");
                    var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(APIData.TNonRoomRequest));
                    {
                        serializer.WriteObject(ms, body);
                        try
                        {
                            var bodyJsonString = Encoding.UTF8.GetString(ms.ToArray());
                            var jsonString = await HttpHelper.PostRequestAsync($"/api/v1/action/defined/text:simple_chat/call", this.IdToken, bodyJsonString);
                            using (var json = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonString)))
                            {
                                var ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(APIData.TNonRoomResponse));
                                {
                                    // 回答のメッセージ追加
                                    var item = ser.ReadObject(json) as APIData.TNonRoomResponse;
                                    if (item?.answer != null)
                                    {
                                        this.SetMessage(msgId, "ai", item.answer, DateTime.UtcNow.ToLocalTime(), new List<string>());
                                    }
                                    else
                                    {
                                        throw new Exception(jsonString);
                                    }
                                }
                                json.Close();

                                // 最新行表示
                                OnPropertyChanged("Messages_Item");
                            }
                        }
                        catch (Exception ex)
                        {
                            // エラー発生時はAI回答欄を削除し、exceptionを投げる
                            this.Messages.Remove(this.Messages.Where((x) => x.Id == msgId).FirstOrDefault());
                            throw ex;
                        }
                    }
                }
            }
        }

        // 履歴設定(チャットルームなし)
        private APIData.TChatMessage[] SetSimpleChatHistories(List<TMessage> histories)
        {
            if (histories == null || histories.Count == 0)
            {
                return new APIData.TChatMessage[] { };
            }
            else
            {
                var messages = new APIData.TChatMessage[histories.Count];
                for (int i = 0; i < histories.Count; i++)
                {
                    var history = histories[i];
                    messages[i] = new APIData.TChatMessage()
                    {
                        role = history.Role,
                        content = history.Content,
                        timeunix = new DateTimeOffset(history.Time).ToUnixTimeMilliseconds(),
                        ref_chunks = history.Refs.Select((x) => new APIData.TRefChunks() { text = x }).ToArray(),
                    };
                }
                return messages;
            }
        }

        // メッセージ追加
        private Guid SetMessage(string role, string content, DateTime time, List<string> refs, string image = null)
        {
            var msg = new TMessage()
            {
                Role = role,
                Content = content,
                Time = time,
                Refs = refs,
                Image = image
            };
            msg.PropertyChanged += (s, e) => { OnPropertyChanged("Messages_Item"); };
            this.Messages.Add(msg);
            return msg.Id;
        }

        // メッセージ更新
        private Guid SetMessage(Guid id, string role, string content, DateTime time, List<string> refs)
        {
            // 指定された ID を持つ既存メッセージを検索
            var existing = this.Messages.FirstOrDefault(m => m.Id == id);
            if (existing != null)
            {
                // 見つかったら内容を更新
                existing.Role = role;
                existing.Content = content;
                existing.Time = time;
                existing.Refs = refs ?? new List<string>();

                // コレクション内アイテムの更新を UI に通知
                return existing.Id;
            }
            else
            {
                // 見つかったら内容を追加
                return SetMessage(role, content, time, refs);
            }
        }

        /// <summary>
        /// プロンプト入力(チャットルームなし/マルチモーダル:Cohere v2 chat API)
        /// </summary>
        /// <param name="inputText">入力</param>
        internal async Task SendMessageCohereV2ChatAsync(List<TMessage> histories, float temperature, int token, string inputText, string systemPrompt, string retrieverID, string filePath)
        {
            var content = inputText?.Trim() ?? string.Empty;
            var refs = new List<string>();
            var base64ImageData = !string.IsNullOrEmpty(filePath) ? await Base64Helper.ImageFileToBase64Async(filePath) : string.Empty;
            if (!string.IsNullOrEmpty(content))
            {
                var body = new APIData.TCohereV2ChatRequest()
                {
                    model = "takane",
                    messages = this.SetCohereV2ChatHistories(histories),
                    temperature = temperature,
                    max_tokens = (uint)token,
                };

                // 既存メッセージをリスト化して末尾に要素を追加する
                var list = new System.Collections.Generic.List<APIData.TCohereV2ChatMessage>();
                if (body.messages != null && body.messages.Length > 0)
                {
                    list.AddRange(body.messages);
                }

                // システムプロンプト追加
                if (!string.IsNullOrEmpty(systemPrompt))
                {
                    list.Add(new APIData.TCohereV2ChatMessage()
                    {
                        role = "system",
                        content = new APIData.TContent[] {
                            new APIData.TContent() { type = "text", text = systemPrompt },
                        },
                    });
                }

                // 参照情報追加（Cohere v2 chat APIでは、参照情報をdocumentsオプションとして設定する）
                if (!string.IsNullOrEmpty(retrieverID))
                {
                    var refChunks = await this.SearchRetrieverAsync(retrieverID, content, 10);
                    if (refChunks != null && refChunks.Length > 0)
                    {
                        // 参照情報をリストに追加
                        refs.AddRange(refChunks?.Select(chunk => (chunk?.text?.Replace("\n\n", "\n") as string) ?? string.Empty) ?? Enumerable.Empty<string>());

                        // documentsオプションに参照情報を設定
                        body.documents = refChunks?.Select(refChunk => new APIData.TDocument
                        {
                            data = new APIData.TData
                            {
                                id = Guid.NewGuid().ToString(),
                                title = string.Empty,
                                text = refChunk?.text?.Replace("\n\n", "\n") ?? string.Empty,
                            }
                        }).ToArray();
                    }
                }

                // 入力プロンプト追加
                if (!string.IsNullOrEmpty(base64ImageData))
                {
                    list.Add(new APIData.TCohereV2ChatMessage()
                    {
                        role = "user",
                        content = new APIData.TContent[] {
                            new APIData.TContent() { type = "text", text = content },
                            new APIData.TContent() { type = "image_url", image_url = new APIData.TImageUrl() { url= $"data:image/png;base64,{base64ImageData}" } }
                        },
                    });
                }
                else
                {
                    list.Add(new APIData.TCohereV2ChatMessage()
                    {
                        role = "user",
                        content = new APIData.TContent[] {
                            new APIData.TContent() { type = "text", text = content },
                        },
                    });
                }

                body.messages = list.ToArray();

                // 質問のメッセージ追加(参照情報は表示しない)
                this.SetMessage("user", inputText, DateTime.UtcNow.ToLocalTime(), new List<string>(), filePath);

                // ここで body を JSON 文字列にシリアライズして変数に格納する
                using (var ms = new MemoryStream())
                {
                    var msgId = this.SetMessage("ai", Resources.Streaming, DateTime.UtcNow.ToLocalTime(), new List<string>());
                    OnPropertyChanged("Messages_Item");
                    var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(APIData.TCohereV2ChatRequest));
                    {
                        serializer.WriteObject(ms, body);
                        try
                        {
                            var bodyJsonString = Encoding.UTF8.GetString(ms.ToArray());
                            var jsonString = await HttpHelper.PostRequestAsync($"/api/v1/pass-through/takane/v2/chat", this.IdToken, bodyJsonString);
                            using (var json = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonString)))
                            {
                                var ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(APIData.TCohereV2ChatResponse));
                                {
                                    // 回答のメッセージ追加
                                    var item = ser.ReadObject(json) as APIData.TCohereV2ChatResponse;
                                    if (item?.id != null)
                                    {
                                        if (item.message?.citations != null)
                                        {
                                            // 参照情報がある場合は、回答のメッセージに参照情報を追加する
                                            foreach (var refItem in item.message?.citations)
                                            {
                                                Debug.WriteLine($"{refItem.text} in {string.Join(",", refItem.sources.Select((x) => x?.id).Where(id => !string.IsNullOrEmpty(id)))}");
                                            }
                                        }
                                        this.SetMessage(msgId, "ai", item.message.content[0].text, DateTime.UtcNow.ToLocalTime(), refs);
                                    }
                                    else
                                    {
                                        throw new Exception(jsonString);
                                    }
                                }
                                json.Close();

                                // 最新行表示
                                OnPropertyChanged("Messages_Item");
                            }
                        }
                        catch (Exception ex)
                        {
                            // エラー発生時はAI回答欄を削除し、exceptionを投げる
                            this.Messages.Remove(this.Messages.Where((x) => x.Id == msgId).FirstOrDefault());
                            throw ex;
                        }
                    }
                }
            }
        }
        private APIData.TCohereV2ChatMessage[] SetCohereV2ChatHistories(List<TMessage> histories)
        {
            if (histories == null || histories.Count == 0)
            {
                return new APIData.TCohereV2ChatMessage[] { };
            }
            else
            {
                var messages = new APIData.TCohereV2ChatMessage[histories.Count];
                for (int i = 0; i < histories.Count; i++)
                {
                    var history = histories[i];
                    messages[i] = new APIData.TCohereV2ChatMessage()
                    {
                        role = history.Role == "ai" ? "assistant" : "user",
                        content = new APIData.TContent[] { new APIData.TContent() { type = "text", text = history.Content } },
                    };
                }
                return messages;
            }
        }

        /// <summary>
        /// プロンプト入力(チャットルームなし/マルチモーダル:Cohere OpenAI 互換 API)
        /// </summary>
        /// <param name="inputText">入力</param>
        internal async Task SendMessageOpenAIChatAsync(List<TMessage> histories, float temperature, int token, string inputText, string systemPrompt, string retrieverID, string filePath)
        {
            var content = inputText?.Trim() ?? string.Empty;
            var refs = new List<string>();
            var base64ImageData = !string.IsNullOrEmpty(filePath) ? await Base64Helper.ImageFileToBase64Async(filePath) : string.Empty;
            if (!string.IsNullOrEmpty(content))
            {
                var body = new APIData.TCohereV2ChatRequest()
                {
                    model = "takane",
                    messages = this.SetCohereV2ChatHistories(histories),
                    temperature = temperature,
                    max_tokens = (uint)token,
                };

                // 既存メッセージをリスト化して末尾に要素を追加する
                var list = new System.Collections.Generic.List<APIData.TCohereV2ChatMessage>();
                if (body.messages != null && body.messages.Length > 0)
                {
                    list.AddRange(body.messages);
                }

                // システムプロンプト追加
                if (!string.IsNullOrEmpty(systemPrompt))
                {
                    list.Add(new APIData.TCohereV2ChatMessage()
                    {
                        role = "system",
                        content = new APIData.TContent[] { new APIData.TContent() { type = "text", text = systemPrompt } },
                    });
                }

                // 参照情報追加 (OpenAI 互換 API では、参照情報を質問文に組み込む)  
                if (!string.IsNullOrEmpty(retrieverID))
                {
                    var refChunks = await this.SearchRetrieverAsync(retrieverID, content, 10);
                    if (refChunks != null && refChunks.Length > 0)
                    {
                        refs.AddRange(refChunks?.Select(chunk => (chunk?.text.Replace("\n\n", "\n") as string) ?? string.Empty) ?? Enumerable.Empty<string>());
                        content = $"{string.Join("\n", refs)}\n\nQuestion:{content}";
                    }
                }

                // 入力プロンプト追加
                if (!string.IsNullOrEmpty(base64ImageData))
                {
                    list.Add(new APIData.TCohereV2ChatMessage()
                    {
                        role = "user",
                        content = new APIData.TContent[] {
                            new APIData.TContent() { type = "text", text = content },
                            new APIData.TContent() { type = "image_url", image_url = new APIData.TImageUrl() { url= $"data:image/png;base64,{base64ImageData}" } }
                        },
                    });
                }
                else
                {
                    list.Add(new APIData.TCohereV2ChatMessage()
                    {
                        role = "user",
                        content = new APIData.TContent[] {
                            new APIData.TContent() { type = "text", text = content },
                        },
                    });
                }

                body.messages = list.ToArray();

                // 質問のメッセージ追加(参照情報は表示しない)
                this.SetMessage("user", inputText, DateTime.UtcNow.ToLocalTime(), new List<string>(), filePath);

                // ここで body を JSON 文字列にシリアライズして変数に格納する
                using (var ms = new MemoryStream())
                {
                    var msgId = this.SetMessage("ai", Resources.Streaming, DateTime.UtcNow.ToLocalTime(), new List<string>());
                    OnPropertyChanged("Messages_Item");
                    var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(APIData.TCohereV2ChatRequest));
                    {
                        serializer.WriteObject(ms, body);
                        try
                        {
                            var bodyJsonString = Encoding.UTF8.GetString(ms.ToArray());
                            var jsonString = await HttpHelper.PostRequestAsync($"/api/v1/pass-through/takane/compatibility/v1/chat/completions", this.IdToken, bodyJsonString);
                            using (var json = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonString)))
                            {
                                var ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(APIData.TOpenAIChatResponse));
                                {
                                    // 回答のメッセージ追加
                                    var item = ser.ReadObject(json) as APIData.TOpenAIChatResponse;
                                    if (item?.id != null)
                                    {
                                        this.SetMessage(msgId, "ai", item.choices[0].message.content, DateTime.UtcNow.ToLocalTime(), refs);
                                    }
                                    else
                                    {
                                        throw new Exception(jsonString);
                                    }
                                }
                                json.Close();

                                // 最新行表示
                                OnPropertyChanged("Messages_Item");
                            }
                        }
                        catch (Exception ex)
                        {
                            // エラー発生時はAI回答欄を削除し、exceptionを投げる
                            this.Messages.Remove(this.Messages.Where((x) => x.Id == msgId).FirstOrDefault());
                            throw ex;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// リトリーバー検索
        /// </summary>
        /// <param name="id"></param>
        /// <param name="content"></param>
        /// <param name="limit"></param>
        /// <returns></returns>
        internal async Task<TRefChunks[]> SearchRetrieverAsync(string id, string content, int limit = 10)
        {
            TRefChunks[] refChunks = null;
            var body = new TRetrieverSearchRequest()
            {
                search_type = "similarity_search",
                search_text = content,
                limit = limit,
                filter = new TRetrieverFilter()
                {
                    retriever_id = new TRetrieverFilterQuery() { ids = new string[] { id } }
                }
            };

            // ここで body を JSON 文字列にシリアライズして変数に格納する
            using (var ms = new MemoryStream())
            {
                var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(TRetrieverSearchRequest));
                {
                    serializer.WriteObject(ms, body);
                    var bodyJsonString = Encoding.UTF8.GetString(ms.ToArray());
                    var jsonString = await HttpHelper.PostRequestAsync("/api/v1/retrievers/search", this.IdToken, bodyJsonString);
                    using (var json = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonString)))
                    {
                        var ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(TRetrieverSearchResponse));
                        {
                            var result = ser.ReadObject(json) as TRetrieverSearchResponse;
                            refChunks = result?.results ?? new TRefChunks[0];
                            json.Close();
                        }
                    }
                }
            }
            return refChunks;
        }

        /// <summary>
        /// 最新の入力プロンプトまでを削除する（AIからの回答がある場合は、その回答まで削除する
        /// </summary>
        /// <returns>最新の入力プロンプト</returns>
        internal async Task<string> DeleteMessageAsync()
        {
            var promptText = string.Empty;
            var lastUserMessageIndex = -1;
            for (var index = this.Messages.Count - 1; index >= 0; index--)
            {
                if (this.Messages[index].Role == "user")
                {
                    lastUserMessageIndex = index;
                    promptText = this.Messages[index].Content;
                    break;
                }
            }
            if (lastUserMessageIndex != -1)
            {
                // 最後のユーザーメッセージ以降のメッセージを削除
                var messagesToDelete = this.Messages.Count - lastUserMessageIndex;
                for (int i = 0; i < messagesToDelete; i++)
                {
                    if (!string.IsNullOrEmpty(this.SelectedChatRoom.ID))
                    {
                        // チャットルームありのときはAPI呼び出しで削除
                        var jsonString = await HttpHelper.PostRequestAsync($"/api/v1/chats/{this.SelectedChatRoom.ID}/messages/removeLastMessage", this.IdToken, string.Empty);
                    }
                    this.Messages.RemoveAt(this.Messages.Count - 1);
                }
                OnPropertyChanged("Messages_Item");
            }
            return promptText;
        }

        /// <summary>
        /// Messages の内容を Markdown 形式でファイルに保存する
        /// </summary>
        /// <param name="path">保存先のファイルパス</param>
        internal async Task SaveMessagesAsMarkdownAsync(string path)
        {
            var sb = new System.Text.StringBuilder();

            if (!string.IsNullOrEmpty(path))
            {
                // ヘッダ
                sb.AppendLine($"# Chat Export ({DateTime.Now:yyyy-MM-dd HH:mm:ss})");
                sb.AppendLine();

                foreach (var msg in this.Messages)
                {
                    var time = msg.Time.ToString("yyyy-MM-dd HH:mm:ss");

                    if (msg.Role == "user")
                    {
                        sb.AppendLine($"#### ■■■ User ({time}) ■■■");
                    }
                    else if (msg.Role == "ai")
                    {
                        sb.AppendLine($"#### ■■■ AI ({time}) ■■■");
                    }
                    else
                    {
                        sb.AppendLine($"#### ■■■ {msg.Role} ({time}) ■■■");
                    }
                    sb.AppendLine($"---");
                    sb.AppendLine(msg.Content ?? string.Empty);
                    sb.AppendLine();
                    sb.AppendLine($"---");
                }

                var content = sb.ToString();

                // 書き込みはバックグラウンドで行う（.NET Framework 4.7.2）
                await Task.Run(() => System.IO.File.WriteAllText(path, content, System.Text.Encoding.UTF8));
            }
        }
        #endregion

        /// <summary>
        /// 入力する
        /// </summary>
        /// <param name="content"></param>
        /// <exception cref="NotImplementedException"></exception>
        internal async Task SendAsync(string content, string filePath)
        {
            if (!string.IsNullOrWhiteSpace(content))
            {
                try
                {
                    if (!string.IsNullOrEmpty(this.SelectedChatRoom?.ID))
                    {
                        await this.SendRoomMessageStreamingAsync(this.SelectedChatRoom.ID, content);

                        // Streaming回答表示完了待ち
                        while (this.IsStreaming)
                        {
                            await Task.Delay(100);
                        }
                    }
                    else if (string.IsNullOrEmpty(filePath) && string.IsNullOrEmpty(Config.RetrieverID))
                    {
                        await this.SendMessageSimpleChatAsync(this.Messages.ToList(), Config.Temperature, Config.MaxTokens, content, Config.SystemPrompt);
                        OnPropertyChanged("IsStreaming");
                    }
                    else
                    {
                        if (this.SelectedAPI?.ID == "v2")
                        {
                            await this.SendMessageCohereV2ChatAsync(this.Messages.ToList(), Config.Temperature, Config.MaxTokens, content, Config.SystemPrompt, Config.RetrieverID, filePath);
                        }
                        else
                        {
                            await this.SendMessageOpenAIChatAsync(this.Messages.ToList(), Config.Temperature, Config.MaxTokens, content, Config.SystemPrompt, Config.RetrieverID, filePath);

                        }
                        OnPropertyChanged("IsStreaming");
                    }
                }
                catch
                {
                    throw;
                }
            }
        }


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

    /// <summary>
    /// チャットルーム
    /// </summary>
    public class TDataChatRoom
    {
        public string ID { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ChatTemplateId { get; set; } = string.Empty;
        public string[] RetrieverIDs { get; set; } = null;
        public DateTime CreateDateTime { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// リトリーバー
    /// </summary>
    public class TDataRetriever
    {
        public string ID { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string EmbeddingModel { get; set; } = string.Empty;
        public string[] OriginIDs { get; set; } = null;
        public DateTime CreateDateTime { get; set; } = DateTime.Now;
    }

    public class TItem
    {
        public string ID { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
    }
}