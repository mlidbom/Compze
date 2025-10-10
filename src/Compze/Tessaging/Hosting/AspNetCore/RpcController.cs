using System;
using System.Threading.Tasks;
using Compze.Abstractions.Internal.Refactoring.Naming;
using Compze.Serialization;
using Compze.Tessaging.Hosting.Implementation;
using Compze.Tessaging.Hosting.Implementation.Http;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE;
using Compze.Threading.TasksCE;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Compze.Tessaging.Hosting.AspNetCore;

class RpcController : ControllerBase
{
   internal static void RegisterWith(IDependencyRegistrar registrar) =>
      registrar.Register(
         Scoped.For<RpcController>()
               .CreatedBy((IRemotableMessageSerializer serializer,
                           ITypeMapper typeMapper,
                           Inbox.HandlerExecutionEngine handlerExecutionEngine,
                           Inbox.IMessageStorage messageStorage)
                             => new RpcController(serializer, typeMapper, handlerExecutionEngine, messageStorage)));

   RpcController(IRemotableMessageSerializer serializer,
                 ITypeMapper typeMapper,
                 Inbox.HandlerExecutionEngine handlerExecutionEngine,
                 Inbox.IMessageStorage storage) : base(serializer, typeMapper, handlerExecutionEngine, storage) {}

   [HttpPost(HttpConstants.Routes.Rpc.Query)]
   public async Task<IActionResult> Query()
   {
      var incomingMessage = await CreateIncomingMessage().caf();

      try
      {
         var queryResponse = (await HandlerExecutionEngine.Enqueue(incomingMessage).caf()).NotNull();
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
      var incomingMessage = await CreateIncomingMessage().caf();

      try
      {
         Storage.SaveIncomingMessage(incomingMessage);
         var commandResponse = (await HandlerExecutionEngine.Enqueue(incomingMessage).caf()).NotNull();
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
