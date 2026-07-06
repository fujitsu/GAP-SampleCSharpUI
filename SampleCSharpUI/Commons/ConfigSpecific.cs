using SampleCSharpUI.Commons;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace SampleCSharpUI.Commons
{
    internal static partial class Config
    {
        // 使用中チャットルームID
        internal static string SelectedChatRoomID
        {
            get
            {
                LoadSpecificProperties();
                return RecData.SelectedChatRoomID;
            }
            set
            {
                RecData.SelectedChatRoomID = value;
                SaveSpecificProperties();
            }
        }

        // システムプロンプト
        internal static string SystemPrompt
        {
            get
            {
                LoadSpecificProperties();
                return RecData.SystemPrompt;
            }
            set
            {
                RecData.SystemPrompt = value;
                SaveSpecificProperties();
            }
        }

        // チャットルームなし Temperature
        internal static float Temperature
        {
            get
            {
                LoadSpecificProperties();
                return RecData.Temperature;
            }
            set
            {
                RecData.Temperature = value;
                SaveSpecificProperties();
            }
        }

        // チャットルームなし MaxTokens
        internal static int MaxTokens
        {
            get
            {
                LoadSpecificProperties();
                return RecData.MaxTokens;
            }
            set
            {
                RecData.MaxTokens = value;
                SaveSpecificProperties();
            }
        }

        // チャットルームなし RAG
        internal static string RetrieverID
        {
            get
            {
                LoadSpecificProperties();
                return RecData.RetrieverID;
            }
            set
            {
                RecData.RetrieverID = value;
                SaveSpecificProperties();
            }
        }

        private static TSpecificPropertiesData RecData = new TSpecificPropertiesData()
        {
            SelectedChatRoomID = string.Empty,
            Temperature = 0.7f,
            MaxTokens = 1024,
        };

        /// <summary>
        /// プロパティ値の取得
        /// </summary>
        internal static void LoadSpecificProperties()
        {
            //保存元のファイル名
            var dataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var settingPath = dataPath + "\\FujitsuGAP\\Settings";
            var fileName = settingPath + "\\Properties_SampleCSharpUI.json";

            try
            {
                if (System.IO.File.Exists(fileName))
                {
                    using (var sr = new System.IO.StreamReader(fileName, new System.Text.UTF8Encoding(false)))
                    {
                        var items = new TSpecificPropertiesData();
                        var jsonString = sr.ReadToEnd();
                        using (var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonString)))
                        {
                            var serializer = new DataContractJsonSerializer(typeof(TSpecificPropertiesData));
                            RecData = (TSpecificPropertiesData)serializer.ReadObject(ms);
                        }
                    }
                }
                else
                {
                    RecData = new TSpecificPropertiesData()
                    {
                        SelectedChatRoomID = string.Empty,
                        SystemPrompt = "以下は、人間とAIの親しみやすい会話です。このAIはおしゃべり好きで、文脈に基づいて多くの具体的な詳細を伝えてくれます。質問の答えがわからない場合、AIは正直に「わからない」と答えます。",
                        Temperature = 0.3f,  // Takane 省略値
                        MaxTokens = 8192,    // Takane 省略値
                        RetrieverID = string.Empty,
                    };
                }
            }
            catch { }
        }

        /// <summary>
        /// プロパティ値の保存
        /// </summary>
        internal static void SaveSpecificProperties()
        {
            try
            {
                //保存元のファイル名
                var dataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var settingPath = dataPath + "\\FujitsuGAP\\Settings";
                var fileName = settingPath + "\\Properties_SampleCSharpUI.json";

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
                    var serializer = new DataContractJsonSerializer(typeof(TSpecificPropertiesData));
                    using (var writer = JsonReaderWriterFactory.CreateJsonWriter(ms, System.Text.Encoding.UTF8, true, true, "  "))
                    {
                        serializer.WriteObject(writer, RecData);
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
        private class TSpecificPropertiesData
        {
            [DataMember]
            public string SelectedChatRoomID { get; set; }
            [DataMember]
            public string SystemPrompt { get; set; }
            [DataMember]
            public float Temperature { get; set; }
            [DataMember]
            public int MaxTokens { get; set; }
            [DataMember]
            public string RetrieverID { get; set; }
        }
    }
}
