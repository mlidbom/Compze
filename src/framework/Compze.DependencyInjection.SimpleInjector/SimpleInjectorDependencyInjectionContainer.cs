using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Compze.Utilities.Contracts;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;
using SimpleInjector;
using SimpleInjector.Lifestyles;

namespace Compze.DependencyInjection.SimpleInjector;

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
         if (unregisteredTypeEventArgs is { Handled: false, UnregisteredServiceType.IsAbstract: false })
         {
            throw new InvalidOperationException(unregisteredTypeEventArgs.UnregisteredServiceType.ToFriendlyName() + " has not been registered.");
         }
      };
   }

   protected override void RegisterInContainer(ComponentRegistration[] registrations)
   {

      foreach(var componentRegistration in registrations)
      {
         var lifestyle = componentRegistration.Lifestyle switch
         {
            Lifestyle.Singleton => (global::SimpleInjector.Lifestyle)global::SimpleInjector.Lifestyle.Singleton,
            Lifestyle.Scoped => global::SimpleInjector.Lifestyle.Scoped,
            _ => throw new ArgumentOutOfRangeException(nameof(componentRegistration.Lifestyle))
         };

         if (componentRegistration.InstantiationSpec.SingletonInstance != null)
         {
            Assert.Argument.Is(lifestyle == global::SimpleInjector.Lifestyle.Singleton, () => $"{componentRegistration.ServiceTypes.First().FullName} tried to register using an Instance and lifestyle: {lifestyle}. Instance can only be used with {nameof(Lifestyle.Singleton)}");
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
            foreach (var someCompletelyOtherName in componentRegistration.ServiceTypes)
            {
               _container.AddRegistration(someCompletelyOtherName, baseRegistration);
            }
         } else
         {
            throw new Exception($"Invalid {nameof(InstantiationSpec)}");
         }
      }
   }

   static global::SimpleInjector.Lifestyle GetSimpleInjectorLifestyle(Lifestyle @this)
   {
      return @this switch
      {
         Lifestyle.Singleton => global::SimpleInjector.Lifestyle.Singleton,
         Lifestyle.Scoped => global::SimpleInjector.Lifestyle.Scoped,
         _ => throw new ArgumentOutOfRangeException(nameof(@this), @this, null)
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

   public override void Dispose() => _container.Dispose();

   public override ValueTask DisposeAsync() => _container.DisposeAsync();

   TComponent IServiceLocatorKernel.Resolve<TComponent>() => _container.GetInstance<TComponent>();
}