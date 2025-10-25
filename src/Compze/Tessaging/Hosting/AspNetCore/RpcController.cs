using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE;
using Compze.Utilities.Threading.TasksCE;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Compze.Abstractions.Refactoring.Naming.Internal;
using Compze.Abstractions.Serialization.Internal;
using Compze.Tessaging.Implementation.TessageHandling;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Tessaging.Implementation.Transport.Client.Http;

namespace Compze.Tessaging.Hosting.AspNetCore;

class RpcController : ControllerBase
{
   internal static void RegisterWith(IComponentRegistrar registrar) =>
      registrar.Register(
         Scoped.For<RpcController>()
               .CreatedBy((IRemotableTessageSerializer serializer,
                           ITypeMapper typeMapper,
                           IInbox inbox,
                           Inbox.HandlerExecutionEngine handlerExecutionEngine)
                             => new RpcController(serializer, typeMapper, inbox, handlerExecutionEngine)));

   RpcController(IRemotableTessageSerializer serializer,
                 ITypeMapper typeMapper,
                 IInbox inbox,
                 Inbox.HandlerExecutionEngine handlerExecutionEngine) : base(serializer, typeMapper, inbox, handlerExecutionEngine) {}

   [HttpPost(HttpConstants.Routes.Rpc.Query)]
   public async Task<IActionResult> Query()
   {
      var incomingTessage = await CreateIncomingTessage().caf();

      try
      {
         var queryResponse = (await HandlerExecutionEngine.Enqueue(incomingTessage).caf()).NotNull();
         var queryResponseJson = Serializer.SerializeResponse(queryResponse);
         return Ok(queryResponseJson);
      }
      catch(Exception exception)
      {
         //todo: eliminate all this code duplication
         return Problem(statusCode: StatusCodes.Status500InternalServerError, type: exception.GetType().FullName, detail: exception.ToString());
      }
   }

   [HttpPost(HttpConstants.Routes.Rpc.CommandWithResult)]
   public async Task<IActionResult> CommandWithResult()
   {
      var incomingTessage = await CreateIncomingTessage().caf();

      try
      {
         var commandResponse = (await Inbox.Receive(incomingTessage).caf()).NotNull();
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
      var incomingTessage = await CreateIncomingTessage().caf();

      try
      {
         await Inbox.Receive(incomingTessage).caf();
         return Ok();
      }
      catch(Exception exception)
      {
         return Problem(statusCode: StatusCodes.Status500InternalServerError, type: exception.GetType().FullName, detail: exception.ToString());
      }
   }
}
