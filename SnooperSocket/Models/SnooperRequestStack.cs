using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnooperSocket.Models
{
    public class SnooperRequestStack
    {
        public List<SnooperRequest> Requests = new List<SnooperRequest>();

        public SnooperRequest GetRequestByID(string ID)
        {
            foreach(var Request in Requests)
            {
                if (Request.RequestID == ID) return Request;
            }
            return null;
        }
        public void ReleaseRequest(string RequestID, SnooperMessage Response)
        {
            foreach (var Request in Requests)
            {
                if (Request.RequestID == RequestID)
                {
                    Request.Response = Response;
                    Request.HasResponse = true;
                    Requests.Remove(Request);
                    break;
                }
            }
        }

        public void StackRequest(SnooperRequest Request)
        {
            Requests.Add(Request);
        }


    }
}
