using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace SampleCSharpUI.Commons
{
    internal partial class Config       
    {
        internal static string ClientId { get; set; }
        internal static string TenantName { get; set; }
        internal static bool IsUseOSWebView { get; set; }
        internal static bool IsPromptAuthentication { get; set; }
        internal static string ClientSecret { get; set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        static Config()
        {
            // プロパティ値の取得（起動時）
            LoadProperties();
        }

        /// <summary>
        /// プロパティ値の取得
        /// </summary>
        internal static void LoadProperties()
        {
            //保存元のファイル名
            var dataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var settingPath = dataPath + "\\FujitsuGAP\\Settings";
            var fileName = settingPath + "\\Properties.json";

            try
            {
                if (System.IO.File.Exists(fileName))
                {
                    using (var sr = new System.IO.StreamReader(fileName, new System.Text.UTF8Encoding(false)))
                    {
                        var items = new TPropertiesData();
                        var jsonString = sr.ReadToEnd();
                        using (var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonString)))
                        {
                            var serializer = new DataContractJsonSerializer(typeof(TPropertiesData));
                            var recvData = (TPropertiesData)serializer.ReadObject(ms);

                            Config.ClientId = recvData.ClientId;
                            Config.TenantName = recvData.TenantName;
                            Config.IsUseOSWebView = recvData.IsUseOSWebView;
                            Config.IsPromptAuthentication = !recvData.IsNotPromptAuthentication;
                            Config.ClientSecret = CryptographyHelper.DecryptStringe(recvData.ClientSecret);

                            // 必要に応じて config を返すか、他の方法で利用してください
                        }
                    }
                }
                else
                {
                    Config.ClientId = string.Empty;
                    Config.TenantName = string.Empty;
                    Config.IsUseOSWebView = false;
                    Config.IsPromptAuthentication = true;
                    Config.ClientSecret = string.Empty;
                }
            }
            catch { }
        }

        /// <summary>
        /// プロパティ値の保存
        /// </summary>
        internal static void SaveProperties()
        {
            try
            {
                //保存元のファイル名
                var dataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var settingPath = dataPath + "\\FujitsuGAP\\Settings";
                var fileName = settingPath + "\\Properties.json";

                // フォルダ (ディレクトリ) が存在しているかどうか確認する
                if (System.IO.Directory.Exists(settingPath) == false)
                {
                    // フォルダ (ディレクトリ) を作成する
                    System.IO.Directory.CreateDirectory(settingPath);
                }

                // ファイルが存在する場合
                if (System.IO.File.Exists(fileName) == true)
                {
                    //ファイルの属性を取得する
                    var attr = System.IO.File.GetAttributes(fileName);

                    //読み取り専用属性の場合
                    if ((attr & System.IO.FileAttributes.ReadOnly) == System.IO.FileAttributes.ReadOnly)
                    {
                        //読み取り専用属性を削除する
                        System.IO.File.SetAttributes(fileName, attr & (~System.IO.FileAttributes.ReadOnly));
                    }
                }

                //書き込むファイルを開く
                using (var ms = new MemoryStream())
                {
                    var serializer = new DataContractJsonSerializer(typeof(TPropertiesData));
                    var st = new TPropertiesData();

                    // Loadがはしらないように内部変数から取得する
                    st.ClientId = Config.ClientId;
                    st.TenantName = Config.TenantName;
                    st.IsUseOSWebView = Config.IsUseOSWebView;
                    st.IsNotPromptAuthentication = !Config.IsPromptAuthentication;
                    st.ClientSecret = CryptographyHelper.EncryptString(Config.ClientSecret);

                    using (var writer = JsonReaderWriterFactory.CreateJsonWriter(ms, System.Text.Encoding.UTF8, true, true, "  "))
                    {
                        serializer.WriteObject(writer, st);
                        writer.Flush();
                        var jsonString = System.Text.Encoding.UTF8.GetString(ms.ToArray());
                        using (var sw = new System.IO.StreamWriter(fileName, false, new System.Text.UTF8Encoding(false)))
                        {
                            sw.WriteLine(jsonString);

                            //ファイルを確実にクローズしてファイル破損を防ぐ
                            sw.Flush();
                            sw.Close();
                        }
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// プロパティファイルフォーマット
        /// </summary>
        [DataContract]
        private class TPropertiesData
        {
            [DataMember]
            public string ClientId { get; set; }
            [DataMember]
            public string TenantName { get; set; }
            [DataMember]
            public bool IsUseOSWebView { get; set; }
            [DataMember]
            public bool IsNotPromptAuthentication { get; set; }
            [DataMember]
            public string ClientSecret { get; set; }
        }
    }
}
