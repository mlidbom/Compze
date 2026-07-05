using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Compze.ServiceBus.Hosting.AspNetCore.Private;

class InternalControllerFeatureProvider : ControllerFeatureProvider
{
   protected override bool IsController(TypeInfo typeInfo) => typeInfo.AsType().IsSubclassOf(typeof(Controller));
}
