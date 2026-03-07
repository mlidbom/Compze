using Compze.Abstractions.Refactoring.Naming.Internal;
using Compze.Abstractions.Serialization.Internal;
using Compze.Internals.SystemCE.Core.ThreadingCE.TasksCE;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Tessaging.Implementation.Transport.Client.Implementation.Http;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Compze.Tessaging.Hosting.AspNetCore.Private;
#pragma warning disable CA1031 //We catch all exceptions here to route them back to the client.

class TessagingController(IRemotableTessageSerializer serializer, ITypeMapper typeMapper, IInbox inbox)
   : ControllerBase(serializer, typeMapper, inbox)
{
   internal static void RegisterWith(IComponentRegistrar registrar) =>
      registrar.Register(Scoped.For<TessagingController>()
                               .CreatedBy((IRemotableTessageSerializer serializer,
                                           ITypeMapper typeMapper,
                                           IInbox inbox)
                                             => new TessagingController(serializer, typeMapper, inbox)));

   [HttpPost(HttpConstants.Routes.Tessaging.Tevent)]
   public async Task<IActionResult> Tevent()
   {
      var incomingTessage = await CreateIncomingTessage().caf();
      try
      {
         await Inbox.ReceiveAsync(incomingTessage).caf();
         return Ok();
      }
      catch(Exception exception)
      {
         this.Log().Warning(exception, "Exception handling tevent");
         return Problem(statusCode: StatusCodes.Status500InternalServerError, type: exception.GetType().FullName, detail: exception.ToString());
      }
   }

   [HttpPost(HttpConstants.Routes.Tessaging.Tommand)]
   public async Task<IActionResult> Tommand()
   {
      var incomingTessage = await CreateIncomingTessage().caf();

      try
      {
         await Inbox.ReceiveAsync(incomingTessage).caf();
         return Ok();
      }
      catch(Exception exception)
      {
         this.Log().Warning(exception, "Exception handling tommand");
         return Problem(statusCode: StatusCodes.Status500InternalServerError, type: exception.GetType().FullName, detail: exception.ToString());
      }
   }
}
