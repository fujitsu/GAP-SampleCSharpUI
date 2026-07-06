using SampleCSharpUI.ViewModels;
using System;
using System.Windows;

namespace SampleCSharpUI.Views
{
    public partial class ManageChatRoomsWindow : Window
    {
        public ManageChatRoomsViewModel ViewModel { get; } = new ManageChatRoomsViewModel();

        /// <summary>
        /// コンストラクター
        /// </summary>
        public ManageChatRoomsWindow(MainWindow window)
        {
            this.Owner = window;
            InitializeComponent();

            this.Loaded += async (s, e) =>
            {
                this.ViewModel.IsBusy = true;
                try
                {
                    // 初期接続
                    await this.ViewModel.GetChatRoomsAsync();
                }
                catch (Exception ex)
                {
                    // ViewModelの処理で例外が発生した場合はここでキャッチしてメッセージ表示
                    MessageBox.Show(this.Owner, ex.Message, this.Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
                this.ViewModel.IsBusy = false;
            };

            this.Unloaded += async (s, e) =>
            {
                this.ViewModel.IsBusy = true;
                try
                {
                    await App.MainVM.GetChatRoomsAsync();
                }
                catch (Exception ex)
                {
                    // ViewModelの処理で例外が発生した場合はここでキャッチしてメッセージ表示
                    MessageBox.Show(this.Owner, ex.Message, this.Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
                this.ViewModel.IsBusy = false;
            };

            this.ViewModel.Messaged += async (s, e) =>
            {
                this.ViewModel.IsBusy = true;
                try
                {
                    switch (e.Message)
                    {
                        case "Edit":
                            {
                                // 編集画面を表示
                                var subWindow = new EditChatRoomWindow(this.ViewModel.ChatRoom) { Owner = this };
                                if (subWindow.ShowDialog() == true)
                                {
                                    // ルーム編集
                                }
                                subWindow = null;

                                // チャットルーム一覧を再取得
                                await this.ViewModel.GetChatRoomsAsync();
                            }
                            break;
                        case "ConfirmationRemove":
                            {
                                // 削除ボタン押下
                                var result = MessageBox.Show(this.Owner, "削除します。よろしいですか。", this.Title, MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
                                if (result == MessageBoxResult.No)
                                {
                                    // いいえが選択された場合
                                }
                                else
                                {
                                    this.ViewModel.DeleteRoomAsync();
                                }
                            }
                            break;
                        default:
                            MessageBox.Show(this.Owner, e.Message, this.Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    // ViewModelの処理で例外が発生した場合はここでキャッチしてメッセージ表示
                    MessageBox.Show(this.Owner, ex.Message, this.Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
                this.ViewModel.IsBusy = false;
            };
        }
    }
}
