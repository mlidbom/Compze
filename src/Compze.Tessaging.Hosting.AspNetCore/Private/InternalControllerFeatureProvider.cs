using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Compze.Tessaging.Hosting.AspNetCore.Private;

public class InternalControllerFeatureProvider : ControllerFeatureProvider
{
   protected override bool IsController(TypeInfo typeInfo) => typeInfo.AsType().IsSubclassOf(typeof(Controller));
}
