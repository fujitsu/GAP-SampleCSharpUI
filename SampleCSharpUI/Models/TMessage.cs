using SampleCSharpUI.Commons;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SampleCSharpUI.Models
{
    public class TMessage : INotifyPropertyChanged
    {
        private Guid _Id { get; set; } = Guid.NewGuid();
        public Guid Id
        {
            get { return _Id; }
        }

        private string _Role { get; set; } = string.Empty;
        public string Role
        {
            get { return _Role; }
            internal set
            {
                if (_Role != value)
                {
                    _Role = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _Content { get; set; } = string.Empty;
        public string Content
        {
            get { return _Content; }
            internal set
            {
                if (_Content != value)
                {
                    _Content = value;
                    OnPropertyChanged();
                }
            }
        }

        private DateTime _Time { get; set; } = DateTime.UtcNow;
        public DateTime Time
        {
            get { return _Time; }
            internal set
            {
                if (_Time != value)
                {
                    _Time = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 参照ドキュメント
        /// </summary>
        private List<string> _Refs { get; set; } = new List<string>();
        public List<string> Refs
        {
            get { return _Refs; }
            internal set
            {
                if (_Refs != value)
                {
                    _Refs = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 添付イメージファイル名
        /// </summary>
        private string _Image { get; set; } = string.Empty;
        public string Image
        {
            get { return _Image; }
            internal set
            {
                if (_Image != value)
                {
                    _Image = value;
                    OnPropertyChanged();
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
}
