using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace FileSharing.Utils
{
    public static class Json
    {
        public static TObject Deserialize<TObject>(string json)
        {
            DataContractJsonSerializer deSerializer = new DataContractJsonSerializer(typeof(TObject));
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                return (TObject)deSerializer.ReadObject(stream);
            }
        }

        public static string Serialize<TObject>(TObject data)
        {
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(data.GetType());
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, data);
                var json = stream.ToArray();
                return Encoding.UTF8.GetString(json);
            }
        }

        public static byte[] SerializeBytes<TObject>(TObject data)
        {
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(data.GetType());
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, data);
                return stream.ToArray();
            }
        }
    }
}
