using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooperSocket.Models
{
    public class SnooperStackMessage
    {
        public Stream Data;
        public byte[] Header;
        //public Dictionary<string, string> Headers = new Dictionary<string, string>();
        public TaskCompletionSource<object> CompletionSource = new TaskCompletionSource<object>();
    }
}
