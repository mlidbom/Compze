using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Compze.Utilities.Contracts;
using Compze.Utilities.DependencyInjection.Abstractions;
using SimpleInjector;
using SimpleInjector.Lifestyles;
using Lifestyle = Compze.Utilities.DependencyInjection.Abstractions.Lifestyle;

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
      foreach(var componentRegistration in registrations)
      {
         var lifestyle = componentRegistration.Lifestyle switch
         {
            Lifestyle.Singleton => (global::SimpleInjector.Lifestyle)global::SimpleInjector.Lifestyle.Singleton,
            Lifestyle.Scoped    => global::SimpleInjector.Lifestyle.Scoped,
            _                   => throw new ArgumentOutOfRangeException(nameof(componentRegistration.Lifestyle))
         };

         if(componentRegistration.InstantiationSpec.SingletonInstance != null)
         {
            foreach(var serviceType in componentRegistration.ServiceTypes)
            {
               _container.RegisterInstance(serviceType, componentRegistration.InstantiationSpec.SingletonInstance);
            }

            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract ReSharper incorrectly believes nullable reference types to deliver runtime guarantees.
         } else if(componentRegistration.InstantiationSpec.FactoryMethod != null)
         {
            var baseRegistration = GetSimpleInjectorLifestyle(componentRegistration.Lifestyle)
              .CreateRegistration(
                  componentRegistration.InstantiationSpec.FactoryMethodReturnType,
                  () => componentRegistration.InstantiationSpec.RunFactoryMethod(this),
                  _container);
            foreach(var someCompletelyOtherName in componentRegistration.ServiceTypes)
            {
               _container.AddRegistration(someCompletelyOtherName, baseRegistration);
            }
         } else
         {
            throw new Exception($"Invalid {nameof(InstantiationSpec)}");
         }
      }
      return this;
    }

   static global::SimpleInjector.Lifestyle GetSimpleInjectorLifestyle(Lifestyle @this)
   {
      return @this switch
      {
         Lifestyle.Singleton => global::SimpleInjector.Lifestyle.Singleton,
         Lifestyle.Scoped    => global::SimpleInjector.Lifestyle.Scoped,
         _                   => throw new ArgumentOutOfRangeException(nameof(@this), @this, null)
      };
   }

   bool _verified;

   public override IServiceLocator ServiceLocator
   {
      get
      {
         if(!_verified)
         {
            _verified = true;
            _container.Verify();
         }

         return this;
      }
   }

   public TComponent Resolve<TComponent>() where TComponent : class => _container.GetInstance<TComponent>();
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
