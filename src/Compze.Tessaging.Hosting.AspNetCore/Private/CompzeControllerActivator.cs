using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Compze.Tessaging.Hosting.AspNetCore.Private;

/// <summary>Controller activator that resolves controllers from the Compze scope resolver stored in HttpContext.Items by the request middleware.</summary>
class CompzeControllerActivator : IControllerActivator
{
   internal const string CompzeScopeResolverHttpContextItemKey = "CompzeScopeResolver";

   public object Create(ControllerContext context)
   {
      var scopeResolver = (IScopeResolver)context.HttpContext.Items[CompzeScopeResolverHttpContextItemKey]!;
      var controllerType = context.ActionDescriptor.ControllerTypeInfo.AsType();
      return scopeResolver.Resolve(controllerType);
   }

   public void Release(ControllerContext context, object controller)
   {
      // Don't dispose here - the Compze container manages lifetimes
      // Disposal happens when the scope created by the middleware ends
   }
}
