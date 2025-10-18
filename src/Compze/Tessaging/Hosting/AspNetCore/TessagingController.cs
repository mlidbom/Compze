using System;
using System.Threading.Tasks;
using Compze.Abstractions.Internal.Refactoring.Naming;
using Compze.Serialization;
using Compze.Tessaging.Hosting.Implementation;
using Compze.Tessaging.Hosting.Implementation.Abstractions;
using Compze.Tessaging.Hosting.Implementation.Http;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Threading.TasksCE;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Compze.Tessaging.Hosting.AspNetCore;

class TessagingController : ControllerBase
{
   internal static void RegisterWith(IComponentRegistrar registrar) =>
      registrar.Register(Scoped.For<TessagingController>()
                               .CreatedBy((IRemotableMessageSerializer serializer,
                                           ITypeMapper typeMapper,
                                           IInbox inbox,
                                           Inbox.HandlerExecutionEngine handlerExecutionEngine)
                                             => new TessagingController(serializer, typeMapper, inbox, handlerExecutionEngine)));

   public TessagingController(IRemotableMessageSerializer serializer, ITypeMapper typeMapper, IInbox inbox, Inbox.HandlerExecutionEngine handlerExecutionEngine) :
      base(serializer, typeMapper, inbox, handlerExecutionEngine) {}

   [HttpPost(HttpConstants.Routes.Tessaging.Event)]
   public async Task<IActionResult> Event()
   {
      var incomingMessage = await CreateIncomingMessage().caf();
      try
      {
         await Inbox.Receive(incomingMessage).caf();
         return Ok();
      }
      catch(Exception exception)
      {
         return Problem(statusCode: StatusCodes.Status500InternalServerError, type: exception.GetType().FullName, detail: exception.ToString());
      }
   }

   [HttpPost(HttpConstants.Routes.Tessaging.Command)]
   public async Task<IActionResult> Command()
   {
      var incomingMessage = await CreateIncomingMessage().caf();

      try
      {
         await Inbox.Receive(incomingMessage).caf();
         return Ok();
      }
      catch(Exception exception)
      {
         return Problem(statusCode: StatusCodes.Status500InternalServerError, type: exception.GetType().FullName, detail: exception.ToString());
      }
   }
}
