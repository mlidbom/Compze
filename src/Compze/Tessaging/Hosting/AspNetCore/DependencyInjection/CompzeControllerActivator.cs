using System;
using Compze.Utilities.DependencyInjection.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Compze.Tessaging.Hosting.AspNetCore.DependencyInjection;

/// <summary>Custom controller activator that creates controllers using Compze's DI container.</summary>
class CompzeControllerActivator : IControllerActivator
{
   readonly IServiceLocator _serviceLocator;

   public CompzeControllerActivator(IServiceLocator serviceLocator) => _serviceLocator = serviceLocator;

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
