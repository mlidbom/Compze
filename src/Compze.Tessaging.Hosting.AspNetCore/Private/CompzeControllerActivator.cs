using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Compze.Tessaging.Hosting.AspNetCore.Private;

/// <summary>Controller activator that resolves controllers from the Compze scope stored in HttpContext.Items by the request middleware.</summary>
class CompzeControllerActivator : IControllerActivator
{
   internal const string CompzeScopeHttpContextItemKey = "CompzeScope";

   public object Create(ControllerContext context)
   {
      var scope = (IServiceLocatorScope)context.HttpContext.Items[CompzeScopeHttpContextItemKey]!;
      var controllerType = context.ActionDescriptor.ControllerTypeInfo.AsType();
      return scope.Resolve(controllerType);
   }

   public void Release(ControllerContext context, object controller)
   {
      // Don't dispose here - the Compze container manages lifetimes
      // Disposal happens when the scope created by the middleware ends
   }
}
