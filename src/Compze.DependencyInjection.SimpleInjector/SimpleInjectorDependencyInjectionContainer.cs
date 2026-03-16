using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Threading;
using SimpleInjector;
using SimpleInjector.Lifestyles;

namespace Compze.DependencyInjection.SimpleInjector;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class SimpleInjectorDependencyInjectionContainer : DependencyInjectionContainerBase, IServiceLocator, IServiceLocatorKernel, ISimpleInjectorContainerInternals
{
   readonly Container _container;

   public SimpleInjectorDependencyInjectionContainer(IComponentRegistrar? register = null) : base(register)
   {
      _container = new Container
                   {
                      Options =
                      {
                         DefaultScopedLifestyle = new AsyncScopedLifestyle(),
                         EnableAutoVerification = false //Verification is just too slow for our tests and the really important checks we do ourselves
                      }
                   };

      _container.ResolveUnregisteredType += (_, unregisteredTypeTeventArgs) =>
      {
         if(unregisteredTypeTeventArgs is { Handled: false, UnregisteredServiceType.IsAbstract: false })
         {
            throw new InvalidOperationException(unregisteredTypeTeventArgs.UnregisteredServiceType.ToFriendlyName() + " has not been registered.");
         }
      };
   }

   protected override IDependencyInjectionContainer RegisterInContainer(ComponentRegistration[] registrations)
   {
      foreach(var registration in registrations)
      {
         if(registration.InstantiationSpec.SingletonInstance is {} instance)
         {
            registration.ServiceTypes.ForEach(it => _container.RegisterInstance(it, instance));
         } else
         {
            var baseRegistration = registration.Lifestyle
                                                        .AsSimpleInjectorLifestyle()
                                                        .CreateRegistration(
                                                            registration.InstantiationSpec.FactoryMethodReturnType,
                                                            () => registration.InstantiationSpec.RunFactoryMethod(this),
                                                            _container);
            foreach(var serviceType in registration.ServiceTypes)
            {
               _container.AddRegistration(serviceType, baseRegistration);
            }
         }
      }

      return this;
   }

   readonly RunOnce _runVerifications = new();
   public override IServiceLocator ServiceLocator
   {
      get
      {
         _runVerifications.RunIfFirstCall(AssertLifeStyleCombinationsAreValid);
         //_container.Verify(); //Verification is just too slow for our tests and the really important checks we do ourselves
         return this;
      }
   }

   protected override DependencyInjectionContainerBase CreateEmptyClone() =>
      new SimpleInjectorDependencyInjectionContainer(Register().Clone());

   protected override IReadOnlyList<Type> ContainerFacadeServiceTypes { get; } =
      [typeof(IDependencyInjectionContainer), typeof(IServiceLocator), typeof(SimpleInjectorDependencyInjectionContainer)];

   Container ISimpleInjectorContainerInternals.Container => _container;

   protected override bool IsInScope() => global::SimpleInjector.Lifestyle.Scoped.GetCurrentScope(_container) != null;

   public TComponent Resolve<TComponent>() where TComponent : class
   {
      if(TryCreateTransientInstance(typeof(TComponent), this, out var transientInstance))
         return (TComponent)transientInstance;
      return _container.GetInstance<TComponent>();
   }

   public object Resolve(Type serviceType)
   {
      if(TryCreateTransientInstance(serviceType, this, out var transientInstance))
         return transientInstance;
      return _container.GetInstance(serviceType);
   }

   IServiceLocatorScope IServiceLocator.BeginScope()
   {
      var scope = AsyncScopedLifestyle.BeginScope(_container);
      return new ServiceLocatorScope(this, scope);
   }

   public override void Dispose() => _container.Dispose();
   public override async ValueTask DisposeAsync() => await _container.DisposeAsync().caf();

   TComponent IServiceLocatorKernel.Resolve<TComponent>()
   {
      if(TryCreateTransientInstance(typeof(TComponent), this, out var transientInstance))
         return (TComponent)transientInstance;
      return _container.GetInstance<TComponent>();
   }

   sealed class ServiceLocatorScope(SimpleInjectorDependencyInjectionContainer container, Scope scope) : IServiceLocatorScope
   {
      readonly SimpleInjectorDependencyInjectionContainer _container = container;
      readonly Scope _scope = scope;

      public TComponent Resolve<TComponent>() where TComponent : class => _container.Resolve<TComponent>();
      public object Resolve(Type serviceType) => _container.Resolve(serviceType);
      public void Dispose() => _scope.Dispose();
   }
}
