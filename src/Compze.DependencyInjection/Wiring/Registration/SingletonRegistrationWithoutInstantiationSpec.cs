using Compze.Contracts;

namespace Compze.DependencyInjection.Wiring.Registration;

public class SingletonRegistrationWithoutInstantiationSpec<TService> : ComponentRegistrationWithoutInstantiationSpec<TService> where TService : class
{
   internal SingletonRegistrationWithoutInstantiationSpec(IEnumerable<Type> serviceTypes, bool isComponentSetMember = false) : base(Lifestyle.Singleton, serviceTypes, isComponentSetMember) {}

   /// <summary>
   /// When the container this component is registered in is cloned, the clone resolves this component from the source container
   /// instead of creating its own instance — both containers share one instance, and the clone does not dispose it.
   /// </summary>
   /// <remarks>
   /// Only available for singletons: for any other lifestyle, sharing instances across containers would leave disposal ownership
   /// hopelessly confused, which is why this method lives on the singleton spec alone.
   /// </remarks>
   /// <remarks>
   /// Not available for a component set member (<c>ForSet(...)</c>): its service type is never singularly resolvable, so there is
   /// no single instance to fetch from the source container.
   /// </remarks>
   public SingletonRegistrationWithoutInstantiationSpec<TService> DelegateToParentServiceLocatorWhenCloning()
   {
      Contract.Argument.Assert(!IsComponentSetMember,
         () => $"{nameof(DelegateToParentServiceLocatorWhenCloning)} is not supported for a component set member — its service type cannot be singularly resolved from the parent.");
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
