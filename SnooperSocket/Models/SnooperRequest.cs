using System.Threading;
using System.Threading.Tasks;

namespace SnooperSocket.Models
{
    public class SnooperRequest
    {
        public string RequestID;
        public bool HasResponse = false;
        public SnooperMessage Response;
        public TaskCompletionSource<SnooperMessage> CompletionSource = new TaskCompletionSource<SnooperMessage>();

        public void Wait()
        {
            SpinWait.SpinUntil(() => HasResponse);
        }

        public async Task<SnooperMessage> WaitAsync() => await CompletionSource.Task;
    }
}