﻿using System;
using System.Collections.Generic;
using System.Linq;
using Compze.Contracts;
using Compze.SystemCE.LinqCE;
using Compze.SystemCE.ThreadingCE.ResourceAccess;

// ReSharper disable UnusedMember.Global

namespace Compze.DependencyInjection;

public static class Singleton
{
   public static SingletonRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2, TService3, TService4, TService5, TService6, TService7, TService8, TService9>() where TService1 : class => For<TService1>(EnumerableCE.OfTypes<TService2, TService3, TService4, TService5, TService6, TService7, TService8, TService9>());
   public static SingletonRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2, TService3, TService4, TService5, TService6, TService7, TService8>() where TService1 : class => For<TService1>(EnumerableCE.OfTypes<TService2, TService3, TService4, TService5, TService6, TService7, TService8>());
   public static SingletonRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2, TService3, TService4, TService5, TService6, TService7>() where TService1 : class => For<TService1>(EnumerableCE.OfTypes<TService2, TService3, TService4, TService5, TService6, TService7>());
   public static SingletonRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2, TService3, TService4, TService5, TService6>() where TService1 : class => For<TService1>(EnumerableCE.OfTypes<TService2, TService3, TService4, TService5, TService6>());
   public static SingletonRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2, TService3, TService4, TService5>() where TService1 : class => For<TService1>(EnumerableCE.OfTypes<TService2, TService3, TService4, TService5>());
   public static SingletonRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2, TService3, TService4>() where TService1 : class => For<TService1>(EnumerableCE.OfTypes<TService2, TService3, TService4>());
   public static SingletonRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2, TService3>() where TService1 : class => For<TService1>(EnumerableCE.OfTypes<TService2, TService3>());
   public static SingletonRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2>() where TService1 : class => For<TService1>(EnumerableCE.OfTypes<TService2>());
   public static SingletonRegistrationWithoutInstantiationSpec<TService> For<TService>() where TService : class => For<TService>([]);
   static SingletonRegistrationWithoutInstantiationSpec<TService> For<TService>(IEnumerable<Type> additionalServices) where TService : class => new(additionalServices);
}

public static class Scoped
{
   public static ComponentRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2, TService3, TService4, TService5, TService6, TService7, TService8, TService9>() where TService1 : class => For<TService1>(EnumerableCE.OfTypes<TService2, TService3, TService4, TService5, TService6, TService7, TService8, TService9>());
   public static ComponentRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2, TService3, TService4, TService5, TService6, TService7, TService8>() where TService1 : class => For<TService1>(EnumerableCE.OfTypes<TService2, TService3, TService4, TService5, TService6, TService7, TService8>());
   public static ComponentRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2, TService3, TService4, TService5, TService6, TService7>() where TService1 : class => For<TService1>(EnumerableCE.OfTypes<TService2, TService3, TService4, TService5, TService6, TService7>());
   public static ComponentRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2, TService3, TService4, TService5, TService6>() where TService1 : class => For<TService1>(EnumerableCE.OfTypes<TService2, TService3, TService4, TService5, TService6>());
   public static ComponentRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2, TService3, TService4, TService5>() where TService1 : class => For<TService1>(EnumerableCE.OfTypes<TService2, TService3, TService4, TService5>());
   public static ComponentRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2, TService3, TService4>() where TService1 : class => For<TService1>(EnumerableCE.OfTypes<TService2, TService3, TService4>());
   public static ComponentRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2, TService3>() where TService1 : class => For<TService1>(EnumerableCE.OfTypes<TService2, TService3>());
   public static ComponentRegistrationWithoutInstantiationSpec<TService1> For<TService1, TService2>() where TService1 : class => For<TService1>(EnumerableCE.OfTypes<TService2>());
   public static ComponentRegistrationWithoutInstantiationSpec<TService> For<TService>() where TService : class => For<TService>([]);
   static ComponentRegistrationWithoutInstantiationSpec<TService> For<TService>(IEnumerable<Type> additionalServices) where TService : class => new(Lifestyle.Scoped, additionalServices);
}

public class ComponentRegistrationWithoutInstantiationSpec<TService> where TService : class
{
   protected IReadOnlyList<Type> ServiceTypes { get; }
   readonly Lifestyle _lifestyle;
   internal ComponentRegistrationWithoutInstantiationSpec(Lifestyle lifestyle, IEnumerable<Type> serviceTypes)
   {
      _lifestyle = lifestyle;
      ServiceTypes = serviceTypes.Concat([typeof(TService)]).ToList();
   }

   internal ComponentRegistration<TService> CreatedBy<TImplementation>(Func<IServiceLocatorKernel, TImplementation> factoryMethod)
      where TImplementation : TService
   {
      var implementationType = typeof(TImplementation);
      AssertImplementsAllServices(implementationType);
      return new ComponentRegistration<TService>(_lifestyle, ServiceTypes, InstantiationSpec.FromFactoryMethod(serviceLocator => factoryMethod(serviceLocator), implementationType));
   }

   protected void AssertImplementsAllServices(Type implementationType)
   {
      var unImplementedService = ServiceTypes.FirstOrDefault(serviceType => !serviceType.IsAssignableFrom(implementationType));
      if(unImplementedService != null)
      {
         throw new ArgumentException($"{implementationType.FullName} does not implement: {unImplementedService.FullName}");
      }
   }
}

public class SingletonRegistrationWithoutInstantiationSpec<TService> : ComponentRegistrationWithoutInstantiationSpec<TService> where TService : class
{
   internal SingletonRegistrationWithoutInstantiationSpec(IEnumerable<Type> serviceTypes) : base(Lifestyle.Singleton, serviceTypes) {}

   internal ComponentRegistration<TService> Instance(TService instance)
   {
      AssertImplementsAllServices(instance.GetType());
      return new ComponentRegistration<TService>(Lifestyle.Singleton, ServiceTypes, InstantiationSpec.FromInstance(instance));
   }
}

class InstantiationSpec
{
   internal object? SingletonInstance { get; }
   internal object RunFactoryMethod(IServiceLocatorKernel kern) => FactoryMethod(kern);
   internal Func<IServiceLocatorKernel, object> FactoryMethod { get; }
   internal Type FactoryMethodReturnType { get; }

   internal static InstantiationSpec FromInstance(object instance) => new(instance);

   internal static InstantiationSpec FromFactoryMethod(Func<IServiceLocatorKernel, object> factoryMethod, Type factoryMethodReturnType) => new(factoryMethod, factoryMethodReturnType);

   InstantiationSpec(Func<IServiceLocatorKernel, object> factoryMethod, Type factoryMethodReturnType)
   {
      FactoryMethodReturnType = factoryMethodReturnType;

      FactoryMethod = factoryMethod;
   }

   InstantiationSpec(object singletonInstance)
   {
      SingletonInstance = singletonInstance;
      FactoryMethod = _ => singletonInstance;
      FactoryMethodReturnType = singletonInstance.GetType();
   }
}

public abstract class ComponentRegistration
{
   internal readonly MonitorCE Monitor = MonitorCE.WithDefaultTimeout();
   internal Guid Id { get; } = Guid.NewGuid();
   internal IEnumerable<Type> ServiceTypes { get; }
   internal InstantiationSpec InstantiationSpec { get; }
   internal Lifestyle Lifestyle { get; }
   internal abstract int ComponentIndex {get;}

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

   internal ComponentRegistration<TService> DelegateToParentServiceLocatorWhenCloning()
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