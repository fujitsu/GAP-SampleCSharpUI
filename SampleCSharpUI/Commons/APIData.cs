using System.Runtime.Serialization;

namespace SampleCSharpUI.Commons
{
    /// <summary>
    /// APIデータクラス
    /// </summary>
    public class APIData
    {

        [DataContract]
        public class TChatRoom
        {
            [DataMember]
            public string id { get; set; }
            [DataMember]
            public string name { get; set; }
            [DataMember]
            public string[] retriever_ids { get; set; }
            [DataMember]
            public string chat_template_id { get; set; }
            [DataMember]
            public string model { get; set; }
        }

        [DataContract]
        public class TChat
        {
            [DataMember]
            public string id { get; set; }
            [DataMember]
            public string name { get; set; }
            [DataMember]
            public string owner { get; set; }
            [DataMember(Name = "public")]
            public string pub { get; set; }
            [DataMember]
            public string[] retriever_ids { get; set; }
            [DataMember]
            public string chat_template_id { get; set; }
            [DataMember]
            public TChatMessage[] messages { get; set; }
            [DataMember]
            public bool is_deleted { get; set; }
            [DataMember]
            public long created_date { get; set; }
            [DataMember]
            public object deleted_date { get; set; }
            [DataMember]
            public object updated_date { get; set; }
            [DataMember]
            public TChatSetting chat_setting { get; set; }
            [DataMember]
            public object[] check_settings { get; set; }
            [DataMember]
            public TPhishingUrlCheckSetting phishing_url_check_setting { get; set; }
            [DataMember]
            public object urls { get; set; }
        }

        [DataContract]
        public class TChatSetting
        {
            [DataMember]
            public float temperature { get; set; }
            [DataMember]
            public int max_tokens { get; set; }
            [DataMember]
            public string model { get; set; }
            [DataMember]
            public TTokensBudget tokens_budget { get; set; }
            [DataMember]
            public int max_documents_retrieve { get; set; }
        }

        [DataContract]
        public class TTokensBudget
        {
            [DataMember]
            public int history { get; set; }
            [DataMember]
            public int documents { get; set; }
            [DataMember]
            public int answer { get; set; }
        }

        [DataContract]
        public class TPhishingUrlCheckSetting
        {
            [DataMember]
            public bool enabled { get; set; }
        }

        [DataContract]
        public class TChatMessage
        {
            [DataMember]
            public string role { get; set; }
            [DataMember]
            public string content { get; set; }
            [DataMember]
            public long timeunix { get; set; }
            [DataMember]
            public TRefChunks[] ref_chunks { get; set; }
            [DataMember]
            public object check_results { get; set; }
            [DataMember]
            public object alternative_content { get; set; }
            [DataMember]
            public bool is_content_private { get; set; }
            [DataMember]
            public object create_method { get; set; }
        }

        [DataContract]
        public class TRefChunks
        {
            [DataMember]
            public string text { get; set; }
            [DataMember]
            public string retriever_id { get; set; }
            [DataMember]
            public string origin { get; set; }
            [DataMember]
            public object section { get; set; }
            [DataMember]
            public object subsection { get; set; }
            [DataMember]
            public int chunk_number { get; set; }
        }

        [DataContract]
        public class TRetrieverRoom
        {
            [DataMember]
            public string name { get; set; }
        }

        [DataContract]
        public class TRetrievers
        {
            [DataMember]
            public TRetriever[] results { get; set; }
        }

        [DataContract]
        public class TRetriever
        {
            [DataMember]
            public string id { get; set; }
            [DataMember]
            public string name { get; set; }
            [DataMember]
            public string owner { get; set; }
            [DataMember(Name = "public")]
            public string pub { get; set; }
            [DataMember]
            public string[] origin_ids { get; set; }
            [DataMember]
            public string embedding_model { get; set; }
            [DataMember]
            public bool is_deleted { get; set; }
            [DataMember]
            public long created_at { get; set; }
            [DataMember]
            public long? updated_at { get; set; }
            [DataMember]
            public long? deleted_at { get; set; }
            [DataMember]
            public string key { get; set; }
            [DataMember]
            public string origin_type { get; set; }
            [DataMember]
            public string target_storage { get; set; }
        }

        [DataContract]
        public class TNonRoomRequest
        {
            [DataMember]
            public TChatMessage[] messages { get; set; }
            [DataMember]
            public string question { get; set; }
            [DataMember]
            public string model { get; set; }
            [DataMember]
            public int max_tokens { get; set; }
            [DataMember]
            public float temperature { get; set; }
            [DataMember]
            public int top_p { get; set; }
        }

        [DataContract]
        public class TNonRoomResponse
        {
            [DataMember]
            public string answer { get; set; }
            [DataMember]
            public TChatMessage[] messages { get; set; }
        }

        [DataContract]
        public class TChatStream
        {
            [DataMember]
            public string type { get; set; }
            [DataMember]
            public string token { get; set; }
        }

        [DataContract]
        public class TCohereV2ChatRequest
        {
            [DataMember]
            public string model { get; set; }
            [DataMember]
            public TCohereV2ChatMessage[] messages { get; set; }
            [DataMember]
            public float temperature { get; set; }
            [DataMember]
            public uint max_tokens { get; set; }

        }

        [DataContract]
        public class TCohereV2ChatMessage
        {
            [DataMember]
            public string role { get; set; }
            [DataMember]
            public TContent[] content { get; set; }
        }

        [DataContract]
        public class TContent
        {
            [DataMember]
            public string type { get; set; }
            [DataMember]
            public string text { get; set; }
            [DataMember]
            public TImageUrl image_url { get; set; }
        }

        [DataContract]
        public class TImageUrl
        {
            [DataMember]
            public string url { get; set; }
        }

        [DataContract]
        public class TCohereV2ChatResponse
        {
            [DataMember]
            public string id { get; set; }
            [DataMember]
            public TCohereV2ChatMessage message { get; set; }
            [DataMember]
            public string finish_reason { get; set; }

        }
    }
}
