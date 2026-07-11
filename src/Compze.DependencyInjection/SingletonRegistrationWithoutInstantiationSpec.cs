using Compze.DependencyInjection.Abstractions;

namespace Compze.DependencyInjection;

public class SingletonRegistrationWithoutInstantiationSpec<TService> : ComponentRegistrationWithoutInstantiationSpec<TService> where TService : class
{
   internal SingletonRegistrationWithoutInstantiationSpec(IEnumerable<Type> serviceTypes) : base(Lifestyle.Singleton, serviceTypes) {}

   /// <summary>
   /// When the container this component is registered in is cloned, the clone resolves this component from the source container
   /// instead of creating its own instance — both containers share one instance, and the clone does not dispose it.
   /// </summary>
   /// <remarks>
   /// Only available for singletons: for any other lifestyle, sharing instances across containers would leave disposal ownership
   /// hopelessly confused, which is why this method lives on the singleton spec alone.
   /// </remarks>
   public SingletonRegistrationWithoutInstantiationSpec<TService> DelegateToParentServiceLocatorWhenCloning()
   {
      ShouldDelegateToParentWhenCloning = true;
      return this;
   }

   /// <summary>
   /// Terminates the registration chain with an already-created instance instead of a factory method. The container never
   /// disposes such an instance — the code that created it owns its lifetime.
   /// </summary>
   public ComponentRegistration<TService> Instance(TService instance)
   {
      AssertImplementsAllServices(instance.GetType());
      return BuildRegistration(InstantiationSpec.FromInstance(instance), dependencyTypes: []);
   }
}
