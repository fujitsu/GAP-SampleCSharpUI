using SampleCSharpUI.Commons;
using SampleCSharpUI.Views;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace SampleCSharpUI.ViewModels
{
    public class ManageChatRoomsViewModel : INotifyPropertyChanged
    {
        private Models.ChatRoomModel Model = new Models.ChatRoomModel();
        private Models.RetrieverModel RetrieverModel = new Models.RetrieverModel();

        /// <summary>
        /// ルーム一覧を表すコレクション
        /// </summary>
        public ObservableCollection<Models.TDataChatRoom> ChatRooms
        {
            get { return App.MainVM.ChatRooms; }
        }

        /// <summary>
        /// 対象ルーム
        /// </summary>
        private Models.TDataChatRoom _ChatRoom;
        public Models.TDataChatRoom ChatRoom
        {
            get { return this._ChatRoom; }
            set
            {
                this._ChatRoom = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// ルーム名
        /// </summary>
        public string ChatRoomName
        {
            get { return this.Model.ChatRoomName; }
            set { this.Model.ChatRoomName = value; }
        }

        /// <summary>
        /// ランダム性
        /// </summary>
        public float Temperature
        {
            get { return this.Model.Temperature; }
            set { this.Model.Temperature = value; }
        }

        /// <summary>
        /// 履歴トークン数
        /// </summary>
        public int HistoryTokenBudget
        {
            get { return this.Model.HistoryTokenBudget; }
            set { this.Model.HistoryTokenBudget = value; }
        }

        /// <summary>
        /// 参照文書トークン数
        /// </summary>
        public int DocumentTokenBudget
        {
            get { return this.Model.DocumentTokenBudget; }
            set { this.Model.DocumentTokenBudget = value; }
        }

        /// <summary>
        /// 回答トークン
        /// </summary>
        public int AnswerTokenBudget
        {
            get { return this.Model.AnswerTokenBudget; }
            set { this.Model.AnswerTokenBudget = value; }
        }

        /// <summary>
        /// 最大トークン
        /// </summary>
        public int MaxTokens
        {
            get { return this.Model.MaxTokens; }
            set { this.Model.MaxTokens = value; }
        }

        /// <summary>
        /// システムプロンプト
        /// </summary>
        public string SystemPrompt
        {
            get { return this.Model.SystemPrompt; }
            set { this.Model.SystemPrompt = value; }
        }

        /// <summary>
        /// リトリーバー一覧を表すコレクション
        /// </summary>
        public ObservableCollection<Models.TDataRetriever> Retrievers
        {
            get { return this.RetrieverModel.Retrievers; }
        }

        /// <summary>
        /// 利用リトリーバー
        /// </summary>
        public Models.TDataRetriever SelectedRetriever
        {
            get { return this.RetrieverModel.SelectedRetriever; }
            set { this.RetrieverModel.SelectedRetriever = value; }
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
                OnPropertyChanged("IsBusy");
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
        /// コンストラクター
        /// </summary>
        public ManageChatRoomsViewModel()
        {
            this.Model.PropertyChanged += (s, e) => { OnPropertyChanged(e.PropertyName); };
            this.RetrieverModel.PropertyChanged += (s, e) => { OnPropertyChanged(e.PropertyName); };
            App.MainVM.PropertyChanged += (s, e) => { OnPropertyChanged(e.PropertyName); };
        }

        /// <summary>
        /// ルームー一覧取得
        /// </summary>
        /// <returns></returns>
        internal async Task GetChatRoomsAsync()
        {
            await App.MainVM.GetChatRoomsAsync();
        }

        /// <summary>
        /// データ取得
        /// </summary>
        /// <param name="id"></param>
        internal async Task GetEditDataAsync(string id)
        {
            await this.RetrieverModel.GetRetrieversAsync(true);
            this.SelectedRetriever = await this.Model.GetChatRoomAsync(id, this.Retrievers.ToList());
        }

        /// <summary>
        /// ルーム編集
        /// </summary>
        RelayCommand<Models.TDataChatRoom> _EditRoomCommand;
        public RelayCommand<Models.TDataChatRoom> EditRoomCommand
        {
            get
            {
                if (_EditRoomCommand == null)
                {
                    _EditRoomCommand = new RelayCommand<Models.TDataChatRoom>(async (target) =>
                    {
                        this.IsBusy = true;
                        try
                        {
                            //編集画面を表示
                            this.ChatRoom = target;
                            OnMessaged("Edit");
                        }
                        catch (Exception ex)
                        {
                            OnMessaged(ex.Message);
                        }
                        finally
                        {
                            this.IsBusy = false;
                        }
                    });
                }
                return _EditRoomCommand;
            }
            set
            {
                _EditRoomCommand = value;
            }
        }

        /// <summary>
        /// 設定保存
        /// </summary>
        RelayCommand _SaveCommand;
        public RelayCommand SaveCommand
        {
            get
            {
                if (_SaveCommand == null)
                {
                    _SaveCommand = new RelayCommand(async () =>
                    {
                        this.IsBusy = true;
                        try
                        {
                            await this.Model.SetChatRoomAsync(this.SelectedRetriever.ID);
                            OnMessaged("");
                        }
                        catch (Exception ex)
                        {
                            OnMessaged(ex.Message);
                        }
                        finally
                        {
                            this.IsBusy = false;
                        }
                    });
                }
                return _SaveCommand;
            }
            set
            {
                _SaveCommand = value;
            }
        }

        /// <summary>
        /// ルーム削除(確認ダイアログあり)
        /// </summary>
        RelayCommand<Models.TDataChatRoom> _DeleteRoomCommand;
        public RelayCommand<Models.TDataChatRoom> DeleteRoomCommand
        {
            get
            {
                if (_DeleteRoomCommand == null)
                {
                    _DeleteRoomCommand = new RelayCommand<Models.TDataChatRoom>((target) =>
                    {
                        this.IsBusy = true;
                        try
                        {
                            // 削除
                            this.ChatRoom = target;
                            OnMessaged("ConfirmationRemove");
                        }
                        catch (Exception ex)
                        {
                            OnMessaged(ex.Message);
                        }
                        finally
                        {
                            this.IsBusy = false;
                        }
                    });
                }
                return _DeleteRoomCommand;
            }
            set
            {
                _DeleteRoomCommand = value;
            }
        }

        /// <summary>
        /// ルーム削除
        /// </summary>
        internal async void DeleteRoomAsync()
        {
            // 削除　
            await this.Model.DeleteChatRoomAsync(this.ChatRoom?.ID);

            // 再取得
            await App.MainVM.GetChatRoomsAsync();
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