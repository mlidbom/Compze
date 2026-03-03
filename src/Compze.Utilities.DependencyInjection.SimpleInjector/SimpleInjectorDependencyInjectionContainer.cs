using System;
using System.Linq;
using System.Threading.Tasks;
using Compze.SystemCE.ThreadingCE.TasksCE;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Threading;
using SimpleInjector;
using SimpleInjector.Lifestyles;

namespace Compze.Utilities.DependencyInjection.SimpleInjector;

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

   Container ISimpleInjectorContainerInternals.Container => _container;

   public TComponent Resolve<TComponent>() where TComponent : class => _container.GetInstance<TComponent>();
   public object Resolve(Type serviceType) => _container.GetInstance(serviceType);
   IDisposable IServiceLocator.BeginScope() => AsyncScopedLifestyle.BeginScope(_container);

   public override void Dispose() => _container.Dispose();
   public override async ValueTask DisposeAsync() => await _container.DisposeAsync().caf();

   TComponent IServiceLocatorKernel.Resolve<TComponent>() => _container.GetInstance<TComponent>();
}
