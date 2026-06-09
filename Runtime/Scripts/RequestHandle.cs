using System;
using System.Threading;
using System.Threading.Tasks;

namespace VoyageForge.Bridge.Runtime
{
    public class RequestHandle<T>
    {
        private CancellationTokenSource _cts = new();

        public event Action<Response<T>> OnComplete;
        public event Action<Exception> OnError;

        public RequestHandle(Task<Response<T>> task)
        {
            _ = RunAsync(task);
        }

        private async Task RunAsync(Task<Response<T>> task)
        {
            try
            {
                var response = await task;
                OnComplete?.Invoke(response);
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex);
            }
        }

        public void Cancel() => _cts.Cancel();

        public void Forget() { }

        public CancellationToken Token => _cts.Token;
    }
}
