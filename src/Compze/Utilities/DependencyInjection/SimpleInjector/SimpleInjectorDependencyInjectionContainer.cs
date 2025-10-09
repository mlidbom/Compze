using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE.LinqCE;
using SimpleInjector;
using SimpleInjector.Lifestyles;

namespace Compze.Utilities.DependencyInjection.SimpleInjector;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class SimpleInjectorDependencyInjectionContainer : DependencyInjectionContainerBase, IServiceLocator, IServiceLocatorKernel
{
   readonly Container _container;

   public SimpleInjectorDependencyInjectionContainer(IRunMode runMode) : base(runMode)
   {
      _container = new Container();
      _container.Options.DefaultScopedLifestyle = new AsyncScopedLifestyle();

      _container.ResolveUnregisteredType += (_, unregisteredTypeEventArgs) =>
      {
         if(unregisteredTypeEventArgs is { Handled: false, UnregisteredServiceType.IsAbstract: false })
         {
            throw new InvalidOperationException(unregisteredTypeEventArgs.UnregisteredServiceType.ToFriendlyName() + " has not been registered.");
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

   bool _verificationStarted;

   public override IServiceLocator ServiceLocator
   {
      get
      {
         if(!_verificationStarted)
         {
            _verificationStarted = true;
            _container.Verify();
         }

         return this;
      }
   }

   public TComponent Resolve<TComponent>() where TComponent : class => _container.GetInstance<TComponent>();
   public object Resolve(Type serviceType) => _container.GetInstance(serviceType);
   public TComponent[] ResolveAll<TComponent>() where TComponent : class => _container.GetAllInstances<TComponent>().ToArray();
   IDisposable IServiceLocator.BeginScope() => AsyncScopedLifestyle.BeginScope(_container);

   [SuppressMessage("Design", "CA1063:Implement IDisposable Correctly", Justification = "CA1063 reports 'Dispose should be public and sealed' but incorrectly flags the protected Dispose(bool) override instead of recognizing this sealed class already has a sealed public Dispose() inherited from the base class.")]
   protected override void Dispose(bool disposing)
   {
      if(disposing)
      {
         _container.Dispose();
      }

      base.Dispose(disposing);
   }

   protected override async ValueTask DisposeAsyncCore()
   {
      await _container.DisposeAsync().ConfigureAwait(false);
      await base.DisposeAsyncCore().ConfigureAwait(false);
   }

   TComponent IServiceLocatorKernel.Resolve<TComponent>() => _container.GetInstance<TComponent>();
}
