using System;
using System.Threading.Tasks;
using Composable.Refactoring.Naming;
using Composable.Serialization;
using Composable.SystemCE;
using Composable.SystemCE.ThreadingCE.TasksCE;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Composable.Messaging.Buses.Implementation.Http;

class RpcController(IRemotableMessageSerializer serializer, ITypeMapper typeMapper, Inbox.HandlerExecutionEngine handlerExecutionEngine, Inbox.IMessageStorage storage) : ControllerBase(serializer, typeMapper, handlerExecutionEngine, storage)
{
   [HttpPost(HttpConstants.Routes.Rpc.Query)] public async Task<IActionResult> Query()
   {
      var incomingMessage = await CreateIncomingMessage().CaF();

      try
      {
         var queryResponse = (await HandlerExecutionEngine.Enqueue(incomingMessage).CaF()).NotNull();
         var queryResponseJson = Serializer.SerializeResponse(queryResponse);
         return Ok(queryResponseJson);
      }
      catch(Exception exception)
      {
         return Problem(statusCode: StatusCodes.Status500InternalServerError, type: exception.GetType().FullName, detail: exception.ToString());
      }
   }

   [HttpPost(HttpConstants.Routes.Rpc.CommandWithResult)]
   public async Task<IActionResult> CommandWithResult()
   {
      var incomingMessage = await CreateIncomingMessage().CaF();

      try
      {
         Storage.SaveIncomingMessage(incomingMessage);
         var commandResponse = (await HandlerExecutionEngine.Enqueue(incomingMessage).CaF()).NotNull();
         var commandResponseJson = Serializer.SerializeResponse(commandResponse);
         return Ok(commandResponseJson);
      }
      catch(Exception exception)
      {
         return Problem(statusCode: StatusCodes.Status500InternalServerError, type: exception.GetType().FullName, detail: exception.ToString());
      }
   }

   [HttpPost(HttpConstants.Routes.Rpc.CommandNoResult)]
   public async Task<IActionResult> CommandWithNoResult()
   {
      var incomingMessage = await CreateIncomingMessage().CaF();

      try
      {
         Storage.SaveIncomingMessage(incomingMessage);
         await HandlerExecutionEngine.Enqueue(incomingMessage).CaF();
         return Ok();
      }
      catch(Exception exception)
      {
         return Problem(statusCode: StatusCodes.Status500InternalServerError, type: exception.GetType().FullName, detail: exception.ToString());
      }
   }
}
