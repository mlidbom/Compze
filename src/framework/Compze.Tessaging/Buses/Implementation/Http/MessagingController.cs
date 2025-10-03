using System;
using System.Threading.Tasks;
using Compze.Abstractions.Internal.Refactoring.Naming;
using Compze.Serialization.Abstractions;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Compze.Tessaging.Tessaging.Buses.Implementation.Http;

class MessagingController(IRemotableMessageSerializer serializer, ITypeMapper typeMapper, Inbox.HandlerExecutionEngine handlerExecutionEngine, Inbox.IMessageStorage storage) : ControllerBase(serializer, typeMapper, handlerExecutionEngine, storage)
{
   [HttpPost(HttpConstants.Routes.Messaging.Event)]
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

   [HttpPost(HttpConstants.Routes.Messaging.Command)]
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
