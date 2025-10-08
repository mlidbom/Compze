using System;
using System.Collections.Generic;
using System.Linq;
using Compze.Utilities.Contracts;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

namespace Compze.Utilities.DependencyInjection;

public abstract class ComponentRegistration
{
   internal readonly MonitorCE Monitor = MonitorCE.WithDefaultTimeout();
   internal Guid Id { get; } = Guid.NewGuid();
   internal IEnumerable<Type> ServiceTypes { get; }
   internal InstantiationSpec InstantiationSpec { get; }
   internal Lifestyle Lifestyle { get; }
   internal abstract int ComponentIndex { get; }

   internal readonly int[] ServiceTypeIndexes;

   internal ComponentRegistration(Lifestyle lifestyle, IEnumerable<Type> serviceTypes, InstantiationSpec instantiationSpec)
   {
      serviceTypes = serviceTypes.ToList();

      ServiceTypeIndexes = serviceTypes.Select(ServiceTypeIndex.For).ToArray();
      Assert.Argument.Is(lifestyle == Lifestyle.Singleton || instantiationSpec.SingletonInstance == null, () => $"{nameof(InstantiationSpec.SingletonInstance)} registrations must be {nameof(Lifestyle.Singleton)}s");

      ServiceTypes = serviceTypes;
      InstantiationSpec = instantiationSpec;
      Lifestyle = lifestyle;
   }

   internal abstract ComponentRegistration CreateCloneRegistration(IServiceLocator currentLocator);

   internal abstract object Resolve(IServiceLocator serviceLocator);
}


public class ComponentRegistration<TService> : ComponentRegistration where TService : class
{
   bool ShouldDelegateToParentWhenCloning { get; set; }

   public ComponentRegistration<TService> DelegateToParentServiceLocatorWhenCloning()
   {
      Assert.State.Is(Lifestyle == Lifestyle.Singleton, () => "Only singletons can be delegated to parent container since disposal concern handling becomes very confused for any other lifestyle");
      ShouldDelegateToParentWhenCloning = true;
      return this;
   }

   internal override int ComponentIndex => ServiceTypeIndex.ForService<TService>.Index;
   internal override ComponentRegistration CreateCloneRegistration(IServiceLocator currentLocator)
   {
      if(!ShouldDelegateToParentWhenCloning)
      {
         return new ComponentRegistration<TService>(Lifestyle, ServiceTypes, InstantiationSpec);
      }

      Assert.State.Is(Lifestyle == Lifestyle.Singleton);
      //We must use singleton instance registrations when delegating because otherwise the containers will both attempt to dispose the service.
      //Instance registrations are not disposed.
      return new ComponentRegistration<TService>(
         lifestyle: Lifestyle.Singleton,
         serviceTypes: ServiceTypes,
         instantiationSpec: InstantiationSpec.FromInstance(currentLocator.Resolve<TService>())
      );
   }

   internal override object Resolve(IServiceLocator locator) => locator.Resolve<TService>();

   internal ComponentRegistration(Lifestyle lifestyle, IEnumerable<Type> serviceTypes, InstantiationSpec instantiationSpec)
      :base(lifestyle, serviceTypes, instantiationSpec)
   {}
}