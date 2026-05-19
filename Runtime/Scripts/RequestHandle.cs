using System;
using System.Threading;
using System.Threading.Tasks;

namespace VoyageForge.Bridge.Runtime
{
    public class RequestHandle<T>
    {
        private Task<Response<T>> _task;
        private CancellationTokenSource _cts = new();

        public event Action<Response<T>> OnComplete;
        public event Action<Exception> OnError;

        public RequestHandle(Task<Response<T>> task)
        {
            _task = WrapTask(task);
        }

        private async Task<Response<T>> WrapTask(Task<Response<T>> task)
        {
            try
            {
                var response = await task;
                OnComplete?.Invoke(response);
                return response;
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex);
                return null;
            }
        }

        /// <summary>
        /// await 支持
        /// </summary>
        public Task<Response<T>> Task => _task;

        /// <summary>
        /// 取消请求
        /// </summary>
        public void Cancel() => _cts.Cancel();

        /// <summary>
        /// 忽略结果释放
        /// </summary>
        public void Forget() => _ = _task;

        public CancellationToken Token => _cts.Token;
    }
}