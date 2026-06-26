using SampleCSharpUI.Commons;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SampleCSharpUI.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private Models.ChatModel Model = new Models.ChatModel();

        /// <summary>
        /// IDトークン
        /// </summary>
        public string IdToken { get { return this.Model.IdToken; } }

        /// <summary>
        /// GAP接続済みかどうかを示すプロパティ
        /// </summary>
        public bool IsLogin
        {
            get { return this.Model.IsLogin; }
            set { OnPropertyChanged(); }
        }

        /// <summary>
        /// 初期設定済みかどうかを示すプロパティ
        /// </summary>
        public bool IsSettings
        {
            get { return this.Model.IsSettings; }
            set
            {
                OnMessaged("Disconnect");
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets the username associated with the current user.
        /// </summary>
        public string UserName
        {
            get { return this.Model.UserName; }
        }

        /// <summary>
        /// チャットルーム一覧を表すコレクション。
        /// Model側のObservableCollectionをそのまま公開します。Viewはこのコレクションをバインドして利用します。
        /// <para>setterはModel.ChatRoomsへ代入するだけで、追加の通知処理は行いません（Model側での通知を期待します）。</para>
        /// </summary>
        public ObservableCollection<Models.TDataChatRoom> ChatRooms
        {
            get { return this.Model.ChatRooms; }
            set { this.Model.ChatRooms = value; }
        }

        /// <summary>
        /// 利用チャットルーム
        /// </summary>
        public Models.TDataChatRoom SelectedChatRoom
        {
            get { return this.Model.SelectedChatRoom; }
            set { this.Model.SelectedChatRoom = value; }
        }

        /// <summary>
        /// 会話一覧を表すコレクション。
        /// </summary>
        public ObservableCollection<Models.TMessage> Messages
        {
            get { return this.Model.Messages; }
            set { this.Model.Messages = value; }
        }

        /// <summary>
        /// Busy表示用
        /// </summary>
        private bool _IsBusy = false;
        public bool IsBusy
        {
            get { return _IsBusy; }
            set
            {
                _IsBusy = value;
                OnPropertyChanged();
                if (value)
                {
                    App.Current.MainWindow.Cursor = System.Windows.Input.Cursors.Wait;
                }
                else
                {
                    App.Current.MainWindow.Cursor = null;
                }
            }
        }

        /// <summary>
        /// 入力用テキスト
        /// </summary>
        private string _InputText = string.Empty;
        public string InputText
        {
            get { return _InputText; }
            set
            {
                if (_InputText != value)
                {
                    _InputText = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 添付ファイル（1ファイルのみ）
        /// </summary>
        private string _AttachedFilePath;
        public string AttachedFilePath
        {
            get => _AttachedFilePath;
            set
            {
                if (_AttachedFilePath != value)
                {
                    _AttachedFilePath = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _AttachedFileName;
        public string AttachedFileName
        {
            get => _AttachedFileName;
            set
            {
                if (_AttachedFileName != value)
                {
                    _AttachedFileName = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 参照ドキュメント一覧
        /// </summary>
        private List<string> _Refs { get; set; } = new List<string>();
        public List<string> Refs
        {
            get { return _Refs; }
            set
            {
                if (_Refs != value)
                {
                    _Refs = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// コンストラクター
        /// </summary>
        public MainViewModel()
        {
            // ModelのイベントをViewModelのイベントに転送する
            this.Model.PropertyChanged += async (s, e) =>
            {
                OnPropertyChanged(e.PropertyName);

                // チャットルームが変更になったので会話一覧を取得
                if (e.PropertyName == "SelectedChatRoom" && this.SelectedChatRoom != null)
                {
                    this.IsBusy = true;
                    try
                    {
                        await this.Model.GetChatsAsync(this.SelectedChatRoom.ID);
                    }
                    catch (Exception ex)
                    {
                        // コンストラクター内で例外が発生した場合はここでキャッチしてメッセージ表示
                        OnMessaged(ex.Message);
                    }
                    this.IsBusy = false;
                }

                // Streaming完了通知を受け取った場合、処理中フラグをOFF
                if (e.PropertyName == "IsStreaming" && this.Model.IsStreaming == false)
                {
                    this.EndPreviewKeyDownCommand();
                }
            };
        }

        /// <summary>
        /// 接続処理を非同期で実行
        /// </summary>
        /// <returns></returns>
        internal async Task ConnectAsync()
        {
            // 接続情報設定済の時は、ログイン処理を実行
            if (this.Model.IsSettings)
            {
                await this.Model.ConnectAsync();
            }
            else
            {
                OnMessaged("Settings");
            }
        }

        /// <summary>
        /// 切断処理を非同期で実行
        /// </summary>
        /// <returns></returns>
        internal async Task DisconnectAsync()
        {
            // 接続情報設定済の時は、ログイン処理を実行
            if (this.Model.IsSettings)
            {
                await this.Model.DisconnectAsync();
            }
        }

        /// <summary>
        /// ルーム一覧取得
        /// </summary>
        /// <returns></returns>
        internal async Task GetChatRoomsAsync(bool isUseNone = false)
        {
            await this.Model.GetChatRoomsAsync(isUseNone);
        }

        /// <summary>
        /// チャットルーム作成処理
        /// </summary>
        /// <returns></returns>
        internal async Task<string> CreateChatRoomAsync(string name, string retrieverID)
        {
            return await this.Model.CreateChatRoomAsync(name, retrieverID);
        }


        /// <summary>
        /// チャットルーム内会話クリア処理
        /// </summary>
        /// <returns></returns>
        internal async Task ClearChatRoomAsync()
        {
            if (this.SelectedChatRoom != null)
            {
                await this.Model.ClearChatRoomAsync(this.SelectedChatRoom.ID);
                await this.Model.GetChatsAsync(this.SelectedChatRoom.ID);
            }
        }

        /// <summary>
        /// メッセージの内容をMarkdown形式で保存する
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        internal async Task SaveMessagesAsMarkdownAsync(string path)
        {
            this.IsBusy = true;
            try
            {
                await this.Model.SaveMessagesAsMarkdownAsync(path);
            }
            catch (Exception ex)
            {
                // ViewModel 内で発生したエラーは UI に通知するため OnMessaged で投げる
                OnMessaged(ex.Message);
            }
            finally
            {
                this.IsBusy = false;
            }
        }

        /// <summary>
        /// サブ画面表示
        /// </summary>
        RelayCommand<string> _ShowDialogCommand;
        public RelayCommand<string> ShowDialogCommand
        {
            get
            {
                if (_ShowDialogCommand == null)
                {
                    _ShowDialogCommand = new RelayCommand<string>((target) =>
                    {
                        this.IsBusy = true;
                        try
                        {
                            OnMessaged(target);
                        }
                        catch (Exception ex)
                        {
                            // Command内で例外が発生した場合はここでキャッチしてメッセージ表示
                            OnMessaged(ex.Message);
                        }
                        this.IsBusy = false;
                    });
                }
                return _ShowDialogCommand;
            }
            set
            {
                _ShowDialogCommand = value;
            }
        }

        /// <summary>
        /// 最新の入力メッセージまでを削除するコマンド（XAMLからバインド）
        /// </summary>
        private RelayCommand _DeleteMessageCommand;
        public RelayCommand DeleteMessageCommand
        {
            get
            {
                if (_DeleteMessageCommand == null)
                {
                    _DeleteMessageCommand = new RelayCommand(async () =>
                    {
                        this.IsBusy = true;
                        try
                        {
                            this.InputText = await this.Model.DeleteMessageAsync();
                        }
                        catch (Exception ex)
                        {
                            // Command内で例外が発生した場合はここでキャッチしてメッセージ表示
                            OnMessaged(ex.Message);
                        }
                        this.IsBusy = false;
                    });
                }
                return _DeleteMessageCommand;
            }
            set
            {
                _DeleteMessageCommand = value;
            }
        }

        /// <summary>
        /// PreviewKeyDown を受け取るコマンド（XAMLからバインド） 
        /// </summary>
        private RelayCommand<KeyEventArgs> _PreviewKeyDownCommand;
        public RelayCommand<KeyEventArgs> PreviewKeyDownCommand
        {
            get
            {
                if (_PreviewKeyDownCommand == null)
                {
                    _PreviewKeyDownCommand = new RelayCommand<KeyEventArgs>(async (target) =>
                    {
                        if (target == null) return;

                        // Enter が押され、Shift が押されていなければ送信扱いにする
                        if (target.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.Shift)
                        {
                            target.Handled = true; // 改行を抑止

                            // 処理中でなければ送信処理を実行
                            if (!this.IsBusy)
                            {
                                this.IsBusy = true;
                                try
                                {
                                    // メッセージを追加（ここではローカルに追加するのみ）
                                    var content = this.InputText.Trim();
                                    this.InputText = string.Empty;

                                    await this.Model.SendAsync(content, this.AttachedFilePath);
                                    this.AttachedFilePath = null;
                                    this.AttachedFileName = null;
                                }
                                catch (Exception ex)
                                {
                                    // Command内で例外が発生した場合はここでキャッチしてメッセージ表示
                                    OnMessaged(ex.Message);
                                }
                            }
                        }
                    });
                }
                return _PreviewKeyDownCommand;
            }
            set
            {
                _PreviewKeyDownCommand = value;
            }
        }
        private void EndPreviewKeyDownCommand()
        {
            this.IsBusy = false;
            OnMessaged("PreviewKeyDownCommand");
        }

        /// <summary>
        /// 参照ドキュメント表示コマンド
        /// </summary>
        RelayCommand<List<string>> _DisplayReferenceCommand;
        public RelayCommand<List<string>> DisplayReferenceCommand
        {
            get
            {
                if (_DisplayReferenceCommand == null)
                {
                    _DisplayReferenceCommand = new RelayCommand<List<string>>(async (refs) =>
                    {
                        this.IsBusy = true;
                        try
                        {
                            this.Refs = refs;
                            OnMessaged("DisplayReference");
                        }
                        catch (Exception ex)
                        {
                            // Command内で例外が発生した場合はここでキャッチしてメッセージ表示
                            OnMessaged(ex.Message);
                        }
                        this.IsBusy = false;
                    });
                }
                return _DisplayReferenceCommand;
            }
            set
            {
                _DisplayReferenceCommand = value;
            }
        }

        /// <summary>
        /// ダイアログ表示用イベント
        /// </summary>
        public event MessagedEventHandler Messaged;
        internal virtual void OnMessaged(String message = "")
        {
            this.Messaged?.Invoke(this, new MessageEventArgs(message));
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
}
