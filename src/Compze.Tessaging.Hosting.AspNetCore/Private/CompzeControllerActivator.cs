using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Compze.Tessaging.Hosting.AspNetCore.Private;

/// <summary>Custom controller activator that creates controllers using Compze's DI container.</summary>
public class  CompzeControllerActivator : IControllerActivator
{
   public static void RegisterWith(IComponentRegistrar register)
      => register.Register(Singleton.For<CompzeControllerActivator>()
                                    .CreatedBy((IServiceLocator serviceLocator) => new CompzeControllerActivator(serviceLocator)));

   readonly IServiceLocator _serviceLocator;

   CompzeControllerActivator(IServiceLocator serviceLocator) => _serviceLocator = serviceLocator;

   public object Create(ControllerContext context)
   {
      var controllerType = context.ActionDescriptor.ControllerTypeInfo.AsType();
      return _serviceLocator.Resolve(controllerType);
   }

   public void Release(ControllerContext context, object controller)
   {
      // Don't dispose here - the Compze container manages lifetimes
      // Disposal happens when the scope created by the middleware ends
   }
}
