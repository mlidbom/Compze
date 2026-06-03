using Compze.DependencyInjection.Abstractions;

namespace Compze.DependencyInjection;

public class TransientRegistrationWithoutInstantiationSpec<TService> : ComponentRegistrationWithoutInstantiationSpec<TService> where TService : class
{
   internal TransientRegistrationWithoutInstantiationSpec(Lifestyle lifestyle, IEnumerable<Type> serviceTypes) : base(lifestyle, serviceTypes) {}

   public TransientRegistrationWithoutInstantiationSpec<TService> AllowSingletonDependent()
   {
      SingletonDependentAllowed = true;
      return this;
   }

   public TransientRegistrationWithoutInstantiationSpec<TService> AllowScopedDependent()
   {
      ScopedDependentAllowed = true;
      return this;
   }
}
