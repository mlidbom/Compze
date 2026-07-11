using Compze.DependencyInjection.Abstractions;

namespace Compze.DependencyInjection;

public class TransientRegistrationWithoutInstantiationSpec<TService> : ComponentRegistrationWithoutInstantiationSpec<TService> where TService : class
{
   internal TransientRegistrationWithoutInstantiationSpec(Lifestyle lifestyle, IEnumerable<Type> serviceTypes) : base(lifestyle, serviceTypes) {}

   /// <summary>
   /// Opts this transient in to being captured by a <see cref="Lifestyle.Singleton"/> consumer. By default that combination is
   /// rejected at <c>Build()</c> as a captive dependency: the captured instance would silently live as long as the singleton,
   /// making this component's registered lifestyle a lie at that consumption site.
   /// </summary>
   /// <remarks>
   /// Only this component's author can know that capture is safe — the component must be stateless, thread-safe, and hold no
   /// scope-bound resources — which is why the opt-in lives on the dependency's registration, not the consumer's.
   /// </remarks>
   public TransientRegistrationWithoutInstantiationSpec<TService> AllowSingletonDependent()
   {
      SingletonDependentAllowed = true;
      return this;
   }

   /// <summary>
   /// Opts this transient in to being captured by a <see cref="Lifestyle.Scoped"/> consumer. By default that combination is
   /// rejected at <c>Build()</c> as a captive dependency, for the same reason as <see cref="AllowSingletonDependent"/> —
   /// the captured instance lives as long as the consuming scope, not per-resolve as its lifestyle promises.
   /// </summary>
   public TransientRegistrationWithoutInstantiationSpec<TService> AllowScopedDependent()
   {
      ScopedDependentAllowed = true;
      return this;
   }
}
