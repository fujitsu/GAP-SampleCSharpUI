using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SampleCSharpUI.Views
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private SynchronizationContext Context { get; set; } = SynchronizationContext.Current;

        public ViewModels.MainViewModel ViewModel { get; } = App.MainVM;

        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += async (s, e) =>
            {
                this.ViewModel.IsBusy = true;
                try
                {
                    if (App.MainVM.IsSettings)
                    {
                        // 初期接続
                        App.MainVM.OnMessaged("Disconnect");
                    }
                    else
                    {
                        App.MainVM.OnMessaged("Settings");
                    }
                }
                catch (Exception ex)
                {
                    // ViewModelの処理で例外が発生した場合はここでキャッチしてメッセージ表示
                    App.MainVM.OnMessaged(ex.Message);
                }
                this.ViewModel.IsBusy = false;
            };

            // サブ画面表示
            App.MainVM.Messaged += async (s, e) =>
            {
                this.ViewModel.IsBusy = true;
                try
                {
                    switch (e.Message)
                    {
                        case "Login":
                            {
                                await App.MainVM.ConnectAsync();
                            }
                            break;
                        case "Connect":
                            {
                                await App.MainVM.ConnectAsync();
                            }
                            break;

                        case "Disconnect":
                            {
                                await App.MainVM.DisconnectAsync();

                                // ログイン画面を表示
                                this.ShowDialog(new LoginWindow(this) { Owner = this });
                            }
                            break;

                        case "CreateRetriever":
                            {
                                this.ShowDialog(new CreateRetrieverWindow(this) { Owner = this });
                            }
                            break;

                        case "ManageRetrievers":
                            {
                                this.ShowDialog(new ManageRetrieversWindow(this) { Owner = this });
                            }
                            break;

                        case "Settings":
                            {
                                this.ShowDialog(new SettingsWindow(this) { Owner = this });
                            }
                            break;

                        case "CreateChatRoom":
                            {
                                this.ShowDialog(new CreateChatRoomWindow(this) { Owner = this });
                            }
                            break;

                        case "ManageChatRooms":
                            {
                                this.ShowDialog(new ManageChatRoomsWindow(this) { Owner = this });
                            }
                            break;

                        case "ClearChatRoom":
                            {
                                await App.MainVM.ClearChatRoomAsync();
                            }
                            break;

                        case "EditChatRoomSettings":
                            {
                                // 編集画面を表示
                                var subWindow = new EditChatRoomWindow(this.ViewModel.SelectedChatRoom) { Owner = this };
                                if (subWindow.ShowDialog() == true && !string.IsNullOrEmpty(App.MainVM.SelectedChatRoom?.ID))
                                {
                                    // チャットルーム一覧を再取得
                                    await this.ViewModel.GetChatRoomsAsync();
                                }
                                subWindow = null;
                            }
                            break;

                        case "About":
                            {
                                this.ShowDialog(new AboutWindow(this) { Owner = this });
                            }
                            break;

                        case "PreviewKeyDownCommand":
                            {
                                this.Focus();
                            }
                            break;

                        case "DisplayReference":
                            {
                                this.ShowDialog(new DisplayReferenceWindow(this) { Owner = this });
                            }
                            break;

                        case "Save":
                            {
                                var dlg = new Microsoft.Win32.SaveFileDialog()
                                {
                                    Filter = Properties.Resources.SaveFilter,
                                    DefaultExt = "md",
                                    FileName = $"Chat_{DateTime.Now:yyyyMMdd_HHmmss}.md",
                                    AddExtension = true,
                                    OverwritePrompt = true
                                };
                                var result = dlg.ShowDialog(this);
                                if (result == true)
                                {
                                    await this.ViewModel.SaveMessagesAsMarkdownAsync(dlg.FileName);
                                    MessageBox.Show(this, Properties.Resources.Saved, this.Title, MessageBoxButton.OK, MessageBoxImage.Information);
                                }
                            }
                            break;

                        default:
                            {
                                // ViewModelの処理で例外が発生した場合はここでキャッチしてメッセージ表示
                                try
                                {
                                    MessageBox.Show(this, e.Message, this.Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                                }
                                catch (Exception) { }
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    try
                    {
                        MessageBox.Show(this, ex.Message, this.Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    }
                    catch (Exception) { }
                }
                this.ViewModel.IsBusy = false;
            };

            App.MainVM.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "Messages_Item")
                {
                    // 最新メッセージが追加されたときに最下部までスクロールする
                    var border = VisualTreeHelper.GetChild(this.listBoxMessage, 0) as Border;
                    if (border != null)
                    {
                        var listBoxScroll = border.Child as ScrollViewer;
                        if (listBoxScroll != null)
                        {
                            // スクロールバーを末尾に移動 
                            listBoxScroll.ScrollToEnd();
                        }
                    }
                }
                else if (e.PropertyName == "SelectedChatRoom")
                {
                    this.ViewModel.AttachedFileName = string.Empty;
                    this.ViewModel.AttachedFilePath = string.Empty;
                }
            };
        }

        // メイン画面を使用不可にして疑似的なDialogとする（サブスクリーン含め移動などは可能）
        private void ShowDialog(Window target)
        {
            target.Closed += (_, __) =>
            {
                this.IsEnabled = true;
                this.Activate();
            };
            this.IsEnabled = false;
            target.Show();
        }

        // --- 添付関連の公開メソッド（XAML の CallMethodAction から呼び出す） ---
        public void OpenAttachFile()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog()
            {
                Title = Properties.Resources.AttachFile,
                // 画像ファイルのみ許可（png, jpeg, jpg, gif, webp）
                Filter = Properties.Resources.AttachFileFilter,
                Multiselect = false
            };

            var result = dlg.ShowDialog(this);
            if (result == true)
            {
                // 追加の安全チェック: 拡張子を厳密に確認する（大文字小文字を無視）
                var ext = System.IO.Path.GetExtension(dlg.FileName) ?? string.Empty;
                ext = ext.ToLowerInvariant();
                if (ext == ".png" || ext == ".jpeg" || ext == ".jpg" || ext == ".gif" || ext == ".webp")
                {
                    // ViewModel 側のプロパティへ設定
                    this.ViewModel.AttachedFilePath = dlg.FileName;
                    this.ViewModel.AttachedFileName = string.IsNullOrEmpty(dlg.FileName) ? null : System.IO.Path.GetFileName(dlg.FileName);
                }
                else
                {
                    MessageBox.Show(this, Properties.Resources.InvalidFileType, this.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        public void RemoveAttachedFile()
        {
            this.ViewModel.AttachedFilePath = null;
            this.ViewModel.AttachedFileName = null;
        }
    }
}
