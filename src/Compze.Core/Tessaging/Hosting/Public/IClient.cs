using Compze.Core.Tessaging.Typermedia.Public;

namespace Compze.Core.Tessaging.Hosting.Public;

public interface IClient : IAsyncDisposable
{
   void ExecuteRequest(Action<IRemoteTypermediaNavigator> request);
   TResult ExecuteRequest<TResult>(Func<IRemoteTypermediaNavigator, TResult> request);
   Task<TResult> ExecuteRequestAsync<TResult>(Func<IRemoteTypermediaNavigator, Task<TResult>> request);
   Task ExecuteRequestAsync(Func<IRemoteTypermediaNavigator, Task> request);
}
