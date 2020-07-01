using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SnooperSocket.Models
{
    public class SnooperRequest
    {
        public string RequestID;
        public bool HasResponse = false;
        public SnooperMessage Response;

        public void Wait()
        {
            while(!HasResponse)
            {
                Thread.Sleep(100);
            }
        }

    }
}
