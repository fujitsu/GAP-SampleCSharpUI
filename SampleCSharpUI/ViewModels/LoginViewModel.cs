using SampleCSharpUI.Commons;
using SampleCSharpUI.Models;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SampleCSharpUI.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private AboutModel Model = new AboutModel();

        public string Company { get { return this.Model.Company; } }
        public string AppInfo { get { return this.Model.AppInfo; } }

        /// <summary>
        /// コンストラクター
        /// </summary>
        public LoginViewModel()
        {
            this.Model.PropertyChanged += (s, e) => { OnPropertyChanged(e.PropertyName); };
        }

        /// <summary>
        ///  バージョン情報取得
        /// </summary>
        internal void GetVersion()
        {
            this.Model.GetVersion();
        }

        /// <summary>
        /// ログイン
        /// </summary>
        RelayCommand<string> _LoginCommand;
        public RelayCommand<string> LoginCommand
        {
            get
            {
                if (_LoginCommand == null)
                {
                    _LoginCommand = new RelayCommand<string>((target) =>
                    {
                        OnMessaged("Connect");
                    });
                }
                return _LoginCommand;
            }
            set
            {
                _LoginCommand = value;
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
