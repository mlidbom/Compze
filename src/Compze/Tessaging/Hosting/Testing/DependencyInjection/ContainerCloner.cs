using System;
using System.Collections.Generic;
using System.Linq;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.DependencyInjection.Microsoft;
using Compze.Utilities.DependencyInjection.SimpleInjector;
using Compze.Utilities.SystemCE.LinqCE;

namespace Compze.Tessaging.Hosting.Testing.DependencyInjection;

static class ContainerCloner
{
   static readonly IReadOnlyList<Type> TypesThatAreFacadesForTheContainer = EnumerableCE.OfTypes<IDependencyInjectionContainer, IServiceLocator, SimpleInjectorDependencyInjectionContainer>()
                                                                                        .ToList();

   public static IServiceLocator Clone(this IServiceLocator @this)
   {
      var sourceContainer = (IDependencyInjectionContainer)@this;


      IDependencyInjectionContainer cloneContainer = sourceContainer switch
      {
#pragma warning disable CA2000 // Dispose objects before losing scope: Review: OK-ish. We dispose the container by registering its created serviceLocator in the container. It will dispose the container when disposed.
         SimpleInjectorDependencyInjectionContainer _ => new SimpleInjectorDependencyInjectionContainer(sourceContainer.RunMode),
         MicrosoftDependencyInjectionContainer => new MicrosoftDependencyInjectionContainer(sourceContainer.RunMode),
         _ => throw new ArgumentOutOfRangeException()
#pragma warning restore CA2000 // Dispose objects before losing scope
      };

      cloneContainer.Register(Singleton.For<IServiceLocator>().CreatedBy(() => cloneContainer.ServiceLocator));
      cloneContainer.Register(Singleton.For<IDependencyInjectionContainer>().Instance(cloneContainer));

      sourceContainer.RegisteredComponents()
                     .Where(component => TypesThatAreFacadesForTheContainer.None(facadeForTheContainer => component.ServiceTypes.Contains(facadeForTheContainer)))
                     .ForEach(action: componentRegistration => cloneContainer.Register(componentRegistration.CreateCloneRegistration(@this)));

      return cloneContainer.ServiceLocator;
   }
}