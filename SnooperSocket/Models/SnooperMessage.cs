using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SnooperSocket.Models
{
    public sealed class SnooperMessage : IDisposable
    {
        public MemoryStream Data;
        public SnooperMessageType RequestType = SnooperMessageType.Unknown;
        public string Channel = "Main";
        public string ObjectType;
        public string RawHeaderData;
        public bool IsManagedMessage;
        public bool Requesthandled = false;
        public bool IsRequest = false;

        public Dictionary<string, string> Headers = new Dictionary<string, string>();

        public T ReadObject<T>()
        {
            string EntContent = Encoding.UTF8.GetString(Data.ToArray());
            return JsonConvert.DeserializeObject<T>(EntContent);
        }
        public void Dispose()
        {
            Data.Dispose();
        }
    }
}
