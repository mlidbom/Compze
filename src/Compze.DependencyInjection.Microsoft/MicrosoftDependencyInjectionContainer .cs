using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace Compze.DependencyInjection.Microsoft;

public sealed class MicrosoftDependencyInjectionContainer(IComponentRegistrar? register = null) : DependencyInjectionContainerBase(register), IServiceLocator, IServiceLocatorKernel, IMicrosoftContainerInternals
{
   readonly IServiceCollection _services = new ServiceCollection();
   ServiceProvider? _serviceProvider;
   bool _isDisposed;

   protected override DependencyInjectionContainerBase CreateEmptyClone() =>
      new MicrosoftDependencyInjectionContainer(Register().Clone());

   readonly RunOnce _registerScopedKernel = new();

   protected override IDependencyInjectionContainer RegisterInContainer(ComponentRegistration[] registrations)
   {
      _registerScopedKernel.RunIfFirstCall(() =>
      {
         _services.AddScoped<ScopedKernel>(sp => new ScopedKernel(this, sp.GetRequiredService));
         _services.AddScoped<IScopeServiceLocator>(sp => sp.GetRequiredService<ScopedKernel>());
      });

      foreach(var registration in registrations)
      {
         var lifetime = registration.Lifestyle.AsServiceLifetime();

         if(registration.InstantiationSpec.SingletonInstance is {} instance)
         {
            registration.ServiceTypes.ForEach(it => _services.AddSingleton(it, instance));
         } else
         {
            var firstServiceType = registration.ServiceTypes.First();
            var primaryDescriptor = registration.Lifestyle == Lifestyle.Scoped
               ? new ServiceDescriptor(firstServiceType,
                                       sp => registration.InstantiationSpec.RunFactoryMethod(sp.GetRequiredService<ScopedKernel>()),
                                       lifetime)
               : new ServiceDescriptor(firstServiceType,
                                       _ => registration.InstantiationSpec.RunFactoryMethod(this),
                                       lifetime);

            _services.Add(primaryDescriptor);

            foreach(var serviceType in registration.ServiceTypes.Skip(1))
            {
               _services.Add(new ServiceDescriptor(serviceType, sp => sp.GetService(firstServiceType)!, lifetime));
            }
         }
      }

      return this;
   }

   public override IServiceLocator ServiceLocator
   {
      get
      {
         if(_serviceProvider == null)
         {
            AssertLifeStyleCombinationsAreValid();
            _serviceProvider = _services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
         }

         return this;
      }
   }

   public TComponent Resolve<TComponent>() where TComponent : class
   {
      Contract.State.NotDisposed(_isDisposed, this);
      return _serviceProvider._assert().NotNull().GetRequiredService<TComponent>();
   }

   public object Resolve(Type serviceType)
   {
      Contract.State.NotDisposed(_isDisposed, this);
      return _serviceProvider._assert().NotNull().GetRequiredService(serviceType);
   }

   TComponent IServiceLocatorKernel.Resolve<TComponent>()
   {
      Contract.State.NotDisposed(_isDisposed, this);
      return _serviceProvider._assert().NotNull().GetRequiredService<TComponent>();
   }

   IServiceLocatorScope IServiceLocator.BeginScope()
   {
      Contract.State.NotDisposed(_isDisposed, this);

      var scope = _serviceProvider._assert().NotNull().CreateAsyncScope();
      var scopedKernel = scope.ServiceProvider.GetRequiredService<ScopedKernel>();

      return new ServiceLocatorScope(scopedKernel, scope.ServiceProvider, () => scope.DisposeAsync().AsTask().GetAwaiter().GetResult());
   }

   IServiceCollection IMicrosoftContainerInternals.ServiceCollection => _services;
   IServiceProvider IMicrosoftContainerInternals.ServiceProvider => _serviceProvider._assert().NotNull();

   public override void Dispose()
   {
      if(!_isDisposed)
      {
         _isDisposed = true;
         if(_serviceProvider != null)
         {
            _serviceProvider.DisposeAsync().AsTask().GetAwaiter().GetResult();
         }

         _serviceProvider = null;
      }
   }

   public override async ValueTask DisposeAsync()
   {
      if(!_isDisposed)
      {
         _isDisposed = true;
         if(_serviceProvider != null)
         {
            await _serviceProvider.DisposeAsync().caf();
         }

         _serviceProvider = null;
      }
   }

   sealed class ServiceLocatorScope(IScopeServiceLocator scopedKernel, IServiceProvider scopedProvider, Action onDispose) : IServiceLocatorScope
   {
      readonly IScopeServiceLocator _scopedKernel = scopedKernel;
      readonly IServiceProvider _scopedProvider = scopedProvider;
      readonly Action _onDispose = onDispose;

      public TComponent Resolve<TComponent>() where TComponent : class => _scopedKernel.Resolve<TComponent>();

      public object Resolve(Type serviceType) => _scopedProvider.GetRequiredService(serviceType);

      public void Dispose() => _onDispose();
   }
}
