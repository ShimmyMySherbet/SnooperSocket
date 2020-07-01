using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooperSocket.Models
{
    public enum SnooperMessageType
    {
        Unknown = -1,
        Binary = 0,
        Object = 1,
        Request = 2,
        Response = 3
    }
}
