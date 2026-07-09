using SampleCSharpUI.Models;
using SampleCSharpUI.ViewModels;
using System;
using System.Windows;

namespace SampleCSharpUI.Views
{
    public partial class EditChatRoomWindow : Window
    {
        public ManageChatRoomsViewModel ViewModel { get; } = new ManageChatRoomsViewModel();

        /// <summary>
        /// コンストラクター
        /// </summary>
        public EditChatRoomWindow(TDataChatRoom room)
        {
            InitializeComponent();

            var rootElement = this.Content as FrameworkElement;
            if (rootElement != null)
            {
                rootElement.Visibility = Visibility.Hidden;
            }

            this.Loaded += async (s, e) =>
            {
                this.ViewModel.IsBusy = true;
                try
                {
                    // 初期値設定
                    this.ViewModel.ChatRoom = room;
                    await this.ViewModel.GetEditDataAsync(room?.ID);
                }
                catch (Exception ex)
                {
                    // ViewModelの処理で例外が発生した場合はここでキャッチしてメッセージ表示
                    MessageBox.Show(this.Owner, ex.Message, this.Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
                finally
                {
                    rootElement.Visibility = Visibility.Visible;
                    this.ViewModel.IsBusy = false;
                }
            };

            this.ViewModel.Messaged += (s, e) =>
            {
                if (e.Message == "")
                {
                    // Saveボタン押下時
                    try
                    {
                        this.DialogResult = true;
                    }
                    catch
                    {
                        this.Close();
                    }
                }
                else
                {
                    MessageBox.Show(this.Owner, e.Message, this.Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
            };
        }
    }
}