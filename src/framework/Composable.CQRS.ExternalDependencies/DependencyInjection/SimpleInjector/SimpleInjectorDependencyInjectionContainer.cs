using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.SystemCE.ThreadingCE.TasksCE;
using SimpleInjector;
using SimpleInjector.Lifestyles;

namespace Composable.DependencyInjection.SimpleInjector;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class SimpleInjectorDependencyInjectionContainer : IDependencyInjectionContainer, IServiceLocator, IServiceLocatorKernel
{
   readonly Container _container;
   readonly List<ComponentRegistration> _registeredComponents = [];
   internal SimpleInjectorDependencyInjectionContainer(IRunMode runMode)
   {
      RunMode = runMode;
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

   public IRunMode RunMode { get; }
   public void Register(params ComponentRegistration[] registrations)
   {
      _registeredComponents.AddRange(registrations);

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
            Contract.Assert.That(lifestyle == global::SimpleInjector.Lifestyle.Singleton, "Instance can only be used with singletons.");
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

   public IEnumerable<ComponentRegistration> RegisteredComponents() => _registeredComponents;

   bool _verified;

   IServiceLocator IDependencyInjectionContainer.CreateServiceLocator()
   {
      if(!_verified)
      {
         _verified = true;
         _container.Verify();
      }
      return this;
   }

   public TComponent Resolve<TComponent>() where TComponent : class => _container.GetInstance<TComponent>();
   public TComponent[] ResolveAll<TComponent>() where TComponent : class => _container.GetAllInstances<TComponent>().ToArray();
   IDisposable IServiceLocator.BeginScope() => AsyncScopedLifestyle.BeginScope(_container);



   public void Dispose() => _container.Dispose();

   public async ValueTask DisposeAsync() => await _container.DisposeAsync().NoMarshalling();

   TComponent IServiceLocatorKernel.Resolve<TComponent>() => _container.GetInstance<TComponent>();
}