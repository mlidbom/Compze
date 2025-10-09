using System;
using System.Linq;
using Compze.Utilities.DependencyInjection.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace Compze.Tessaging.Hosting.AspNetCore.DependencyInjection;

static class ControllerRegistrationValidator
{
   public static void AssertAllControllersCanBeInstantiated(this IServiceProvider serviceProvider, IServiceLocator serviceLocator)
   {
      var applicationPartManager = serviceProvider.GetRequiredService<ApplicationPartManager>();
      var feature = new ControllerFeature();
      applicationPartManager.PopulateFeature(feature);

      // When using the HybridServiceProvider (from CreateServiceProviderFactory),
      // creating a scope through serviceProvider already creates scopes in BOTH containers.
      // So we don't need to manually create a scope in serviceLocator.
      using var aspNetScope = serviceProvider.CreateScope();
      
      foreach(var controllerType in feature.Controllers
                                           .Where(it => it.AsType().IsSubclassOf(typeof(Controller)))
                                           .Where(it => !it.IsAbstract))
      {
         try
         {
            aspNetScope.ServiceProvider.GetRequiredService(controllerType.AsType());
         }
         catch(Exception ex)
         {
            throw new InvalidControllerRegistrationException($"Controller {controllerType.FullName} cannot be resolved.", ex);
         }
      }
   }
}

class InvalidControllerRegistrationException(string message, Exception ex) : Exception(message, ex);
