using System.Runtime.Serialization;

namespace FileSharing.Models
{
    [DataContract]
    public struct ContentInfo
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public ulong Size { get; set; }

        [DataMember]
        public string Extension { get; set; }

        [DataMember]
        public ContentType ContentType { get; set; }

    }
}
