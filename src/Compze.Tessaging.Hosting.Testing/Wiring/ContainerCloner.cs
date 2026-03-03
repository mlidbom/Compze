using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.DependencyInjection.Microsoft;
using Compze.Utilities.DependencyInjection.SimpleInjector;
using Compze.Utilities.Logging;
using Compze.Utilities.SystemCE.LinqCE;

namespace Compze.Tessaging.Hosting.Testing.Wiring;

public static class ContainerCloner
{
   static readonly ILogger Log = CompzeLogger.For(typeof(ContainerCloner));
   class ContainerIsClonedMarkerClass;

   static readonly IReadOnlyList<Type> TypesThatAreFacadesForTheContainer = EnumerableCE.OfTypes<IDependencyInjectionContainer, IServiceLocator, SimpleInjectorDependencyInjectionContainer>()
                                                                                        .ToList();

   public static IServiceLocator Clone(this IServiceLocator @this)
   {
#pragma warning disable CA2000//the container is disposed with the service locator
      var cloneContainer = ((IDependencyInjectionContainer)@this).Clone();
#pragma warning restore CA2000

      cloneContainer.Register(Singleton.For<IDependencyInjectionContainer>().Instance(cloneContainer));

      return cloneContainer.ServiceLocator;
   }

   public static IDependencyInjectionContainer Clone(this IDependencyInjectionContainer sourceContainer)
   {
      Log.Info($"Cloning IDependencyInjectionContainer: {sourceContainer.GetHashCode()}");
      var sourceServiceLocator = sourceContainer.ServiceLocator;
      IDependencyInjectionContainer cloneContainer = sourceContainer switch
      {
#pragma warning disable CA2000 // Dispose objects before losing scope: Review: OK-ish. We dispose the container by registering its created serviceLocator in the container. It will dispose the container when disposed.
         SimpleInjectorDependencyInjectionContainer => new SimpleInjectorDependencyInjectionContainer(sourceContainer.Register().Clone()),
         MicrosoftDependencyInjectionContainer      => new MicrosoftDependencyInjectionContainer(sourceContainer.Register().Clone()),
         _                                          => throw new ArgumentOutOfRangeException()
#pragma warning restore CA2000 // Dispose objects before losing scope
      };

      cloneContainer.Register(Singleton.For<IServiceLocator>().CreatedBy(() => cloneContainer.ServiceLocator));

      cloneContainer.Register(Singleton.For<ContainerIsClonedMarkerClass>().Instance(new ContainerIsClonedMarkerClass()));

      sourceContainer.RegisteredComponents()
                     .Where(component => TypesThatAreFacadesForTheContainer.None(facadeForTheContainer => component.ServiceTypes.Contains(facadeForTheContainer)))
                     .ForEach(action: componentRegistration => cloneContainer.Register(componentRegistration.CreateCloneRegistration(sourceServiceLocator)));

      return cloneContainer;
   }

   public static bool IsClone(this IDependencyInjectionContainer @this) => @this.IsRegistered<ContainerIsClonedMarkerClass>();
}
