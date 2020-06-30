using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooperSocket.Models
{
    public enum SnooperBytes: byte
    {
        DataStart = 246,
        HeaderDelim = 247
    }
}
