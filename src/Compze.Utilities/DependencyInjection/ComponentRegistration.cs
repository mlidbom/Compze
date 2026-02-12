using System;
using System.Collections.Generic;
using System.Linq;
using Compze.Utilities.Contracts;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Utilities.DependencyInjection;

public abstract class ComponentRegistration
{
   public IReadOnlySet<Type> ServiceTypes { get; }
   public InstantiationSpec InstantiationSpec { get; }
   public Lifestyle Lifestyle { get; }
   public IReadOnlyList<Type> DependencyTypes { get; }
   public bool ProvidesService(Type service) => ServiceTypes.Contains(service);

   protected ComponentRegistration(Lifestyle lifestyle,
                                  IEnumerable<Type> serviceTypes,
                                  InstantiationSpec instantiationSpec,
                                  IEnumerable<Type> dependencyTypes)
   {
      serviceTypes = serviceTypes.ToList();

      Assert.Argument.Is(
         lifestyle == Lifestyle.Singleton || instantiationSpec.SingletonInstance == null,
         () => $"{nameof(InstantiationSpec.SingletonInstance)} registrations must be {nameof(Lifestyle.Singleton)}s");

      ServiceTypes = serviceTypes.ToHashSet();
      InstantiationSpec = instantiationSpec;
      Lifestyle = lifestyle;
      DependencyTypes = dependencyTypes.ToList();
   }

   public abstract ComponentRegistration CreateCloneRegistration(IServiceLocator currentLocator);

   public abstract object Resolve(IServiceLocator serviceLocator);
}

public class ComponentRegistration<TService> : ComponentRegistration where TService : class
{
   bool ShouldDelegateToParentWhenCloning { get; set; }

   public ComponentRegistration<TService> DelegateToParentServiceLocatorWhenCloning()
   {
      Assert.State.Is(
         Lifestyle == Lifestyle.Singleton,
         () => "Only singletons can be delegated to parent container since disposal concern handling becomes very confused for any other lifestyle");
      ShouldDelegateToParentWhenCloning = true;
      return this;
   }

   public override ComponentRegistration CreateCloneRegistration(IServiceLocator currentLocator)
   {
      if(!ShouldDelegateToParentWhenCloning)
      {
         return new ComponentRegistration<TService>(Lifestyle, ServiceTypes, InstantiationSpec, DependencyTypes);
      }

      Assert.State.Is(Lifestyle == Lifestyle.Singleton, () => "Only Singletons can delegate to parent container when cloning, because otherwise both containers would attempt to dispose the component");
      return new ComponentRegistration<TService>(////Instance registrations are not disposed.
         lifestyle: Lifestyle.Singleton,
         serviceTypes: ServiceTypes,
         instantiationSpec: InstantiationSpec.FromInstance(currentLocator.Resolve<TService>()),
         dependencyTypes: DependencyTypes
      );
   }

   public override object Resolve(IServiceLocator locator) => locator.Resolve<TService>();

   public ComponentRegistration(Lifestyle lifestyle,
                                  IEnumerable<Type> serviceTypes,
                                  InstantiationSpec instantiationSpec,
                                  IEnumerable<Type> dependencyTypes)
      : base(lifestyle, serviceTypes, instantiationSpec, dependencyTypes) {}
}
