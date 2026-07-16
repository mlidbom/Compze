using System.Transactions;
using Compze.Contracts;
using Compze.DependencyInjection.Abstractions;

namespace Compze.DependencyInjection;

public class InstantiationSpec
{
   public object? SingletonInstance { get; }
   public object RunFactoryMethod(IServiceResolver kern) => FactoryMethod(kern);
   Func<IServiceResolver, object> FactoryMethod { get; }
   public Type FactoryMethodReturnType { get; }

   internal static InstantiationSpec FromInstance(object instance) => new(instance);

   internal static InstantiationSpec FromFactoryMethod(Func<IServiceResolver, object> factoryMethod, Type factoryMethodReturnType) => new(factoryMethod, factoryMethodReturnType);

   ///<summary>This spec with the instantiation wrapped to first assert that an ambient transaction is present — how a<br/>
   /// <see cref="Lifestyle.UnitOfWork"/> registration's requirement rides into the backend containers, which only ever see<br/>
   /// <see cref="Lifestyle.Scoped"/> (see <see cref="ComponentRegistration.CreateBackendRegistration"/>).</summary>
   internal InstantiationSpec RequiringAmbientTransaction() =>
      FromFactoryMethod(serviceResolver =>
      {
         Contract.State.Assert(Transaction.Current != null,
                               () => $"{FactoryMethodReturnType.FullName} is registered with {nameof(Lifestyle)}.{nameof(Lifestyle.UnitOfWork)}: it participates in the enclosing unit of work, and there is no ambient transaction — no unit of work to participate in. Resolve it within ExecuteUnitOfWork.");
         return RunFactoryMethod(serviceResolver);
      }, FactoryMethodReturnType);

   InstantiationSpec(Func<IServiceResolver, object> factoryMethod, Type factoryMethodReturnType)
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
