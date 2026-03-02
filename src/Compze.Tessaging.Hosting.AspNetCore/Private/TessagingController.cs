using System;
using System.Threading.Tasks;
using Compze.Core.Refactoring.Naming.Internal;
using Compze.Core.Serialization.Internal;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Tessaging.Implementation.TessageHandling.Inbox;
using Compze.Tessaging.Implementation.Transport.Client.Implementation.Http;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Threading.TasksCE;
using Compze.Utilities.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Compze.Tessaging.Hosting.AspNetCore.Private;
#pragma warning disable CA1031 //We catch all exceptions here to route them back to the client.

internal class TessagingController : ControllerBase
{
   internal static void RegisterWith(IComponentRegistrar registrar) =>
      registrar.Register(Scoped.For<TessagingController>()
                               .CreatedBy((IRemotableTessageSerializer serializer,
                                           ITypeMapper typeMapper,
                                           IInbox inbox,
                                           Inbox.HandlerExecutionEngine handlerExecutionEngine)
                                             => new TessagingController(serializer, typeMapper, inbox, handlerExecutionEngine)));

   public TessagingController(IRemotableTessageSerializer serializer, ITypeMapper typeMapper, IInbox inbox, Inbox.HandlerExecutionEngine handlerExecutionEngine) :
      base(serializer, typeMapper, inbox, handlerExecutionEngine) {}

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
