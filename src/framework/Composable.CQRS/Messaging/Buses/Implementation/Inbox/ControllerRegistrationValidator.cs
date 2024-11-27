using System;
using System.Linq;
using Composable.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace Composable.Messaging.Buses.Implementation;

static class ControllerRegistrationValidator
{
   public static void AssertAllControllersCanBeInstantiated(this IServiceProvider serviceProvider, IServiceLocator serviceLocator)
   {
      var applicationPartManager = serviceProvider.GetRequiredService<ApplicationPartManager>();
      var feature = new ControllerFeature();
      applicationPartManager.PopulateFeature(feature);

      using var aspNetScope = serviceProvider.CreateScope();
      using var serviceLocatorScope = serviceLocator.BeginScope();
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
public class InvalidControllerRegistrationException : Exception
{
   public InvalidControllerRegistrationException(string message, Exception ex) : base(message, ex) {}
}
