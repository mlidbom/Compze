using System;

namespace Compze.Utilities.DependencyInjection;

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
