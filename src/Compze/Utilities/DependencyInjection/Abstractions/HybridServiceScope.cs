using System;
using System.Threading.Tasks;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;
using Microsoft.Extensions.DependencyInjection;

namespace Compze.Utilities.DependencyInjection.Abstractions;

/// <summary>
/// A service scope that manages both Compze and Microsoft DI scopes.
/// </summary>
class HybridServiceScope : IServiceScope, IAsyncDisposable
{
   readonly IDisposable _compzeScope;
   readonly IServiceScope _microsoftScope;

   public HybridServiceScope(IDependencyInjectionContainer compzeContainer, IServiceScope microsoftScope, IDisposable compzeScope)
   {
      _microsoftScope = microsoftScope;
      _compzeScope = compzeScope;
      ServiceProvider = new HybridServiceProvider(compzeContainer, microsoftScope.ServiceProvider);
   }

   public IServiceProvider ServiceProvider { get; }

   public void Dispose()
   {
      _compzeScope.Dispose();
      _microsoftScope.Dispose();
   }

   public async ValueTask DisposeAsync()
   {
      _compzeScope.Dispose();

      if(_microsoftScope is IAsyncDisposable asyncDisposable)
      {
         await asyncDisposable.DisposeAsync().caf();
      } else
      {
         _microsoftScope.Dispose();
      }
   }
}
