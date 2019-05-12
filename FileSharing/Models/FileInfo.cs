using System.Runtime.Serialization;

namespace FileSharing.Models
{
    [DataContract]
    public struct FileInfo
    {
        [DataMember]
        public string FileName { get; set; }

        [DataMember]
        public ulong Size { get; set; }

        [DataMember]
        public string FileType { get; set; }

    }
}
