using Compze.DependencyInjection.Abstractions;

namespace Compze.DependencyInjection;

public class ComponentRegistrationWithoutInstantiationSpec<TService> where TService : class
{
   protected IReadOnlyList<Type> ServiceTypes { get; }
   readonly Lifestyle _lifestyle;

   protected bool SingletonDependentAllowed { get; set; }
   protected bool ScopedDependentAllowed { get; set; }

   internal ComponentRegistrationWithoutInstantiationSpec(Lifestyle lifestyle, IEnumerable<Type> serviceTypes)
   {
      _lifestyle = lifestyle;
      ServiceTypes = serviceTypes.Concat([typeof(TService)]).ToList();
   }

   internal ComponentRegistration<TService> CreatedBy<TImplementation>(Func<IServiceLocatorKernel, TImplementation> factoryMethod,
                                                                       IEnumerable<Type> dependencyTypes)
      where TImplementation : TService
   {
      var implementationType = typeof(TImplementation);
      AssertImplementsAllServices(implementationType);
      return new ComponentRegistration<TService>(_lifestyle,
                                                 ServiceTypes,
                                                 InstantiationSpec.FromFactoryMethod(serviceLocator => factoryMethod(serviceLocator),
                                                                                     implementationType),
                                                 dependencyTypes,
                                                 SingletonDependentAllowed,
                                                 ScopedDependentAllowed);
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
