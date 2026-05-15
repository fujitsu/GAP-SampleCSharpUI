using SampleCSharpUI.Commons;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace SampleCSharpUI.ViewModels
{
    public class ManageRetrieversViewModel : INotifyPropertyChanged
    {
        private Models.RetrieverModel Model = new Models.RetrieverModel();

        /// <summary>
        /// リトリーバ一覧を表すコレクション
        /// </summary>
        public ObservableCollection<Models.TDataRetriever> Retrievers
        {
            get { return this.Model.Retrievers; }
        }

        /// <summary>
        /// 対象リトリーバ一
        /// </summary>
        private Models.TDataRetriever _Retriever;
        public Models.TDataRetriever Retriever
        {
            get { return this._Retriever; }
            set
            {
                this._Retriever = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 処理メッセージ
        /// </summary>
        private string _Message = string.Empty;
        public string Message
        {
            get { return _Message; }
            set
            {
                _Message = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// リトリーバーID
        /// </summary>
        private string _ID = string.Empty;
        public string ID
        {
            get { return _ID; }
            set
            {
                _ID = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// リトリーバー名
        /// </summary>
        public string RetrieverName
        {
            get { return this.Model.RetrieverName; }
            set { this.Model.RetrieverName = value; }
        }

        /// <summary>
        /// データ参照先ファイル
        /// </summary>
        private string _FileName = string.Empty;
        public string FileName
        {
            get { return _FileName; }
            set
            {
                _FileName = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// データ参照先フォルダ
        /// </summary>
        private string _FolderName = string.Empty;
        public string FolderName
        {
            get { return _FolderName; }
            set
            {
                _FolderName = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// データ参照先URL
        /// </summary>
        private string _Url = string.Empty;
        public string Url
        {
            get { return _Url; }
            set
            {
                _Url = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// データ参照先URL使用フラグ
        /// </summary>
        private bool _IsUseUrl = false;
        public bool IsUseUrl
        {
            get { return _IsUseUrl; }
            set
            {
                _IsUseUrl = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// データ参照先ファイル使用フラグ
        /// </summary>
        private bool _IsUseFile = false;
        public bool IsUseFile
        {
            get { return _IsUseFile; }
            set
            {
                _IsUseFile = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// データ参照先フォルダ使用フラグ
        /// </summary>
        private bool _IsUseFolder = false;
        public bool IsUseFolder
        {
            get { return _IsUseFolder; }
            set
            {
                _IsUseFolder = value;
                OnPropertyChanged();
            }
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
        /// コンストラクター
        /// </summary>
        public ManageRetrieversViewModel()
        {
            this.Model.PropertyChanged += (s, e) => { OnPropertyChanged(e.PropertyName); };
        }

        /// <summary>
        /// リトリーバー一覧取得
        /// </summary>
        /// <returns></returns>
        internal async Task GetRetrieversAsync()
        {
            await this.Model.GetRetrieversAsync(false);
        }

        /// <summary>
        /// データ取得
        /// </summary>
        /// <param name="id"></param>
        internal async Task GetEditDataAsync(string id)
        {
            this.ID = id;   
            await this.Model.GetRetrieverAsync(id);
        }

        /// <summary>
        /// リトリーバー編集
        /// </summary>
        RelayCommand<Models.TDataRetriever> _EditRetrieverCommand;
        public RelayCommand<Models.TDataRetriever> EditRetrieverCommand
        {
            get
            {
                if (_EditRetrieverCommand == null)
                {
                    _EditRetrieverCommand = new RelayCommand<Models.TDataRetriever>(async (target) =>
                    {
                        this.IsBusy = true;
                        try
                        {
                            //編集画面を表示
                            this.Retriever = target;
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
                return _EditRetrieverCommand;
            }
            set
            {
                _EditRetrieverCommand = value;
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
                            var files = SetFiles();
                            this.Message = "登録処理中…";
                            if (this.IsUseUrl || this.IsUseFile || this.IsUseFolder)
                            {
                                this.ID = await this.Model.CreateRetrieverFromDataAsync(
                                    this.RetrieverName,
                                    this.IsUseUrl ? new List<string>() { this.Url } : null,
                                    files);
                                if (!string.IsNullOrEmpty(this.ID))
                                {
                                    var item = this.Model.Retrievers.FirstOrDefault((x) => x.ID == this.ID);
                                    if (item != null)
                                    {
                                        this.Model.SelectedRetriever = item;
                                    }
                                }
                                this.Message = "登録完了";
                            }
                            else
                            {
                                this.Message = "データ参照先が指定されていません。";
                            }
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
        /// 設定更新
        /// </summary>
        RelayCommand _UpdateCommand;
        public RelayCommand UpdateCommand
        {
            get
            {
                if (_UpdateCommand == null)
                {
                    _UpdateCommand = new RelayCommand(async () =>
                    {
                        this.IsBusy = true;
                        try
                        {
                            var files = SetFiles(); 
                            this.Message = "更新処理中…";

                            if (this.IsUseUrl || this.IsUseFile || this.IsUseFolder)
                            {
                                this.ID = await this.Model.SetRetrieverFromDataAsync(
                                    this.Model.RetrieverData,
                                    this.RetrieverName,
                                    this.IsUseUrl ? new List<string>() { this.Url } : null,
                                    files);
                                if (!string.IsNullOrEmpty(this.ID))
                                {
                                    var item = this.Model.Retrievers.FirstOrDefault((x) => x.ID == this.ID);
                                    if (item != null)
                                    {
                                        this.Model.SelectedRetriever = item;
                                    }
                                }
                                this.Message = "更新完了";
                            }
                            else
                            {
                                this.Message = "データ参照先が指定されていません。";
                            }
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
                return _UpdateCommand;
            }
            set
            {
                _UpdateCommand = value;
            }
        }

        // ファイルリスト設定
        private List<string> SetFiles()
        {
            var files = new List<string>() { };
            if (this.IsUseFile)
            {
                files.Add(this.FileName);
            }
            if (this.IsUseFolder)
            {
                // Filter = "RAG Files (*.txt;*.text;*.pdf;*.docx;*.pptx;*.html;*.md;*.csv;*.xlsx)|*.txt;*.text;*.pdf;*.docx;*.pptx;*.html;*.md;*.csv;*.xlsx",
                var allowedExts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                                    {
                                        ".txt", ".text", ".pdf", ".docx", ".pptx", ".html", ".md", ".csv", ".xlsx"
                                    };
                foreach (var file in System.IO.Directory.GetFiles(this.FolderName))
                {
                    var ext = System.IO.Path.GetExtension(file);
                    if (!string.IsNullOrEmpty(ext) && allowedExts.Contains(ext))
                    {
                        files.Add(file);
                    }
                }
            }

            return files;
        }

        /// <summary>
        /// ファイル指定ダイアログ
        /// </summary>
        RelayCommand _SelectFileCommand;
        public RelayCommand SelectFileCommand
        {
            get
            {
                if (_SelectFileCommand == null)
                {
                    _SelectFileCommand = new RelayCommand(async () =>
                    {
                        OnMessaged("SelectFile");
                    });
                }
                return _SelectFileCommand;
            }
            set
            {
                _SelectFileCommand = value;
            }
        }

        /// <summary>
        /// ファイル指定ダイアログ
        /// </summary>
        RelayCommand _SelectFolderCommand;
        public RelayCommand SelectFolderCommand
        {
            get
            {
                if (_SelectFolderCommand == null)
                {
                    _SelectFolderCommand = new RelayCommand(async () =>
                    {
                        OnMessaged("SelectFolder");
                    });
                }
                return _SelectFolderCommand;
            }
            set
            {
                _SelectFolderCommand = value;
            }
        }

        /// <summary>
        /// リトリーバー削除(確認ダイアログあり)
        /// </summary>
        RelayCommand<Models.TDataRetriever> _DeleteRetrieverCommand;
        public RelayCommand<Models.TDataRetriever> DeleteRetrieverCommand
        {
            get
            {
                if (_DeleteRetrieverCommand == null)
                {
                    _DeleteRetrieverCommand = new RelayCommand<Models.TDataRetriever>((target) =>
                    {
                        this.IsBusy = true;
                        try
                        {
                            // 削除
                            this.Retriever = target;
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
                return _DeleteRetrieverCommand;
            }
            set
            {
                _DeleteRetrieverCommand = value;
            }
        }

        /// <summary>
        /// リトリーバー削除
        /// </summary>
        internal async void DeleteRetrieverAsync()
        {
            // 削除　
            await this.Model.DeleteRetrieverAsync(this.Retriever?.ID);

            // 再取得
            await this.Model.GetRetrieversAsync(false);
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