using System;
using System.Threading.Tasks;
using Compze.Abstractions.Internal.Refactoring.Naming;
using Compze.Serialization;
using Compze.Tessaging.Hosting.Implementation;
using Compze.Tessaging.Hosting.Implementation.Http;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Compze.Tessaging.Hosting.AspNetCore;

class TessagingController : ControllerBase
{
   internal static void RegisterWith(IDependencyRegistrar registrar) =>
      registrar.Register(Scoped.For<TessagingController>()
                               .CreatedBy((IRemotableMessageSerializer serializer,
                                           ITypeMapper typeMapper,
                                           Inbox.HandlerExecutionEngine handlerExecutionEngine,
                                           Inbox.IMessageStorage messageStorage)
                                             => new TessagingController(serializer, typeMapper, handlerExecutionEngine, messageStorage)));

   public TessagingController(IRemotableMessageSerializer serializer, ITypeMapper typeMapper, Inbox.HandlerExecutionEngine handlerExecutionEngine, Inbox.IMessageStorage storage) : base(serializer, typeMapper, handlerExecutionEngine, storage) {}

   [HttpPost(HttpConstants.Routes.Tessaging.Event)]
   public async Task<IActionResult> Event()
   {
      var incomingMessage = await CreateIncomingMessage().caf();
      try
      {
         Storage.SaveIncomingMessage(incomingMessage);
         await HandlerExecutionEngine.Enqueue(incomingMessage).caf();
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
         Storage.SaveIncomingMessage(incomingMessage);
         await HandlerExecutionEngine.Enqueue(incomingMessage).caf();
         return Ok();
      }
      catch(Exception exception)
      {
         return Problem(statusCode: StatusCodes.Status500InternalServerError, type: exception.GetType().FullName, detail: exception.ToString());
      }
   }
}
