namespace Compze.DependencyInjection;

public class InstantiationSpec
{
   public object? SingletonInstance { get; }
   public object RunFactoryMethod(IServiceLocatorKernel kern) => FactoryMethod(kern);
   Func<IServiceLocatorKernel, object> FactoryMethod { get; }
   public Type FactoryMethodReturnType { get; }

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
