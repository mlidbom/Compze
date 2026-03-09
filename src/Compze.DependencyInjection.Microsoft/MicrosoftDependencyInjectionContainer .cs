using System.Collections.Immutable;
using Compze.Contracts;
using Compze.Internals.SystemCE.Core.ThreadingCE.TasksCE;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE;
using Compze.Internals.SystemCE.LinqCE;
using Microsoft.Extensions.DependencyInjection;

namespace Compze.DependencyInjection.Microsoft;

public sealed class MicrosoftDependencyInjectionContainer(IComponentRegistrar? register = null) : DependencyInjectionContainerBase(register), IServiceLocator, IServiceLocatorKernel, IMicrosoftContainerInternals
{
   readonly IServiceCollection _services = new ServiceCollection();
   ServiceProvider? _serviceProvider;
   bool _isDisposed;

   readonly AsyncLocal<ImmutableStack<IServiceScope>> _scopeStack = new();

   protected override DependencyInjectionContainerBase CreateEmptyClone() =>
      new MicrosoftDependencyInjectionContainer(Register().Clone());

   protected override IDependencyInjectionContainer RegisterInContainer(ComponentRegistration[] registrations)
   {
      foreach(var registration in registrations)
      {
         var lifetime = registration.Lifestyle.AsServiceLifetime();

         if(registration.InstantiationSpec.SingletonInstance is {} instance)
         {
            registration.ServiceTypes.ForEach(it => _services.AddSingleton(it, instance));
         } else
         {
            var firstServiceType = registration.ServiceTypes.First();
            var primaryDescriptor = new ServiceDescriptor(firstServiceType,
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

   ImmutableStack<IServiceScope> ScopeStack => _scopeStack.Value ?? ImmutableStack<IServiceScope>.Empty;

   IServiceProvider CurrentProvider()
   {
      Contract.State.NotDisposed(_isDisposed, this);
      var stack = ScopeStack;
      return !stack.IsEmpty ? stack.Peek().ServiceProvider : _serviceProvider._assert().NotNull();
   }

   protected override bool IsInScope() => !ScopeStack.IsEmpty;

   public TComponent Resolve<TComponent>() where TComponent : class
   {
      Contract.State.NotDisposed(_isDisposed, this);
      if(TryCreateTransientInstance(typeof(TComponent), this, out var transientInstance))
         return (TComponent)transientInstance;
      return CurrentProvider().GetRequiredService<TComponent>();
   }

   public object Resolve(Type serviceType)
   {
      Contract.State.NotDisposed(_isDisposed, this);
      if(TryCreateTransientInstance(serviceType, this, out var transientInstance))
         return transientInstance;
      return CurrentProvider().GetRequiredService(serviceType);
   }

   TComponent IServiceLocatorKernel.Resolve<TComponent>()
   {
      Contract.State.NotDisposed(_isDisposed, this);
      if(TryCreateTransientInstance(typeof(TComponent), this, out var transientInstance))
         return (TComponent)transientInstance;
      return CurrentProvider().GetRequiredService<TComponent>();
   }

   IDisposable IServiceLocator.BeginScope()
   {
      Contract.State.NotDisposed(_isDisposed, this);

      var scope = _serviceProvider._assert().NotNull().CreateAsyncScope();
      _scopeStack.Value = ScopeStack.Push(scope);

      return new Disposable(() =>
      {
         var stack = ScopeStack;
         Contract.State.Assert(!stack.IsEmpty, () => "Attempt to dispose scope from a context that is not within the scope.");
         stack.Peek().Dispose();
         _scopeStack.Value = stack.Pop();
      });
   }

   IServiceCollection IMicrosoftContainerInternals.ServiceCollection => _services;
   IServiceProvider IMicrosoftContainerInternals.ServiceProvider => _serviceProvider._assert().NotNull();

   void IMicrosoftContainerInternals.PushExternalScope(IServiceScope scope) =>
      _scopeStack.Value = ScopeStack.Push(scope);

   void IMicrosoftContainerInternals.PopExternalScope()
   {
      var stack = ScopeStack;
      Contract.State.Assert(!stack.IsEmpty, () => "Attempt to pop scope from a context that has no external scope.");
      _scopeStack.Value = stack.Pop();
   }

   public override void Dispose()
   {
      if(!_isDisposed)
      {
         Contract.State.Assert(ScopeStack.IsEmpty, () => "Scopes must be disposed before the container");
         _isDisposed = true;
         _serviceProvider?.Dispose();
         _serviceProvider = null;
      }
   }

   public override async ValueTask DisposeAsync()
   {
      if(!_isDisposed)
      {
         Contract.State.Assert(ScopeStack.IsEmpty, () => "Scopes must be disposed before the container");
         _isDisposed = true;
         if(_serviceProvider != null)
         {
            await _serviceProvider.DisposeAsync().caf();
         }

         _serviceProvider = null;
      }
   }
}
