using System;
using System.Collections.Generic;
using System.Linq;
using Compze.Utilities.Functional;
using Compze.Utilities.SystemCE.LinqCE;

namespace Compze.Utilities.DependencyInjection.Abstractions;

public interface IDependencyRegistrar
{
   IDependencyRegistrar Register(params ComponentRegistration[] registrations);

   IDependencyRegistrar Register(params Action<IDependencyRegistrar>[] registrationMethods)
      => registrationMethods.ForEach(it => it(this))
                            .then(this);

   IDependencyInjectionContainer Container();
   IRunMode RunMode { get; }
}

public interface IDependencyInjectionContainer : IDisposable, IAsyncDisposable
{
   IRunMode RunMode { get; }
   IDependencyRegistrar Register();
   IDependencyInjectionContainer Register(params ComponentRegistration[] registrations);
   IEnumerable<ComponentRegistration> RegisteredComponents();
   IServiceLocator ServiceLocator { get; }
   bool IsRegistered<TComponent>() where TComponent : class => RegisteredComponents().Any(it => it.ServiceTypes.Contains(typeof(TComponent)));
}

public interface IServiceLocator : IDisposable, IAsyncDisposable
{
   TComponent Resolve<TComponent>() where TComponent : class;
   TComponent[] ResolveAll<TComponent>() where TComponent : class;
   object Resolve(Type serviceType);
   IDisposable BeginScope();
}

public interface IRunMode
{
   bool IsTesting { get; }
}

enum Lifestyle
{
   Singleton,
   Scoped
}
