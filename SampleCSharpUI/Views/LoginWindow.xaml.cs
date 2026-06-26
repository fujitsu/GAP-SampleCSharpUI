using System.Windows;

namespace SampleCSharpUI.Views
{
    public partial class LoginWindow : Window
    {
        public ViewModels.LoginViewModel ViewModel { get; } = new ViewModels.LoginViewModel();

        public LoginWindow(MainWindow window)
        {
            this.Owner = window;
            InitializeComponent();

            this.ViewModel.Messaged += (s, e) =>
            {
                if (e.Message == "Connect")
                {
                    App.MainVM.OnMessaged("Connect");
                    this.Close();
                }
            };

            App.MainVM.IsBusy = true;
            try
            {
                this.ViewModel.GetVersion();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(this.Owner, ex.Message, this.Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            App.MainVM.IsBusy = false;
        }
    }
}
