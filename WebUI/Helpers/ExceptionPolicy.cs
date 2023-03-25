using TodoList.Business.Exceptions;


namespace TodoList.WebUI.Blazor.Helpers
{
    public class ExceptionPolicy
    {
        public ExceptionPolicy(IWebHostEnvironment env)
        {
            _env = env;
        }
        readonly IWebHostEnvironment _env;

        /// <summary>
        /// Just a helper to wrap / hide technical information of an exception (except when developping)
        /// in a generic user understandable exception. TODO : is a C# / Asp.Net Core mechanism alredy exist for that ?
        /// </summary>
        public T WrapTechnicalError<T>(Func<T> func)
        {
            try
            {
                return func();
            }
            // Catch all unexpected technical issue
            // In development we want the exact error
            catch (Exception ex) when (!(ex is UserUnderstandableException) && !_env.IsDevelopment())
            {
                // TODO: log exception
                throw new UserUnderstandableException("Sorry, a technical error happened! Support teams is working on it!", null);
            }
        }
        public void WrapTechnicalError(Action action)
        {
            WrapTechnicalError(() => { action(); return false; });
        }

        /// <summary>
        /// Just a helper to wrap / hide technical information of an exception (except when developping)
        /// in a generic user understandable exception. TODO : is a C# / Asp.Net Core mechanism alredy exist for that ?
        /// </summary>
        public async Task<T> WrapTechnicalError<T>(Task<T> task)
        {
            try
            {
                return await task.ConfigureAwait(true);
            }
            // Catch all unexpected technical issue
            // In development we want the exact error
            catch (Exception ex) when (!(ex is UserUnderstandableException) && !_env.IsDevelopment())
            {
                // TODO: log exception
                throw new UserUnderstandableException("Sorry, a technical error happened! Support teams is working on it!", null);
            }
        }
        public Task WrapTechnicalError(Task task)
        {
            return WrapTechnicalError(async () => { await task; return (object)null; });
        }
    }
}
