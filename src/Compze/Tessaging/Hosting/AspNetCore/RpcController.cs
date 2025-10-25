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
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Tessaging.Implementation.TessageHandling.Inbox;
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

   [HttpPost(HttpConstants.Routes.Rpc.Tuery)]
   public async Task<IActionResult> Tuery()
   {
      var incomingTessage = await CreateIncomingTessage().caf();

      try
      {
         var tueryResponse = (await HandlerExecutionEngine.Enqueue(incomingTessage).caf()).NotNull();
         var tueryResponseJson = Serializer.SerializeResponse(tueryResponse);
         return Ok(tueryResponseJson);
      }
      catch(Exception exception)
      {
         //todo: eliminate all this code duplication
         return Problem(statusCode: StatusCodes.Status500InternalServerError, type: exception.GetType().FullName, detail: exception.ToString());
      }
   }

   [HttpPost(HttpConstants.Routes.Rpc.TommandWithResult)]
   public async Task<IActionResult> TommandWithResult()
   {
      var incomingTessage = await CreateIncomingTessage().caf();

      try
      {
         var tommandResponse = (await Inbox.Receive(incomingTessage).caf()).NotNull();
         var tommandResponseJson = Serializer.SerializeResponse(tommandResponse);
         return Ok(tommandResponseJson);
      }
      catch(Exception exception)
      {
         return Problem(statusCode: StatusCodes.Status500InternalServerError, type: exception.GetType().FullName, detail: exception.ToString());
      }
   }

   [HttpPost(HttpConstants.Routes.Rpc.TommandNoResult)]
   public async Task<IActionResult> TommandWithNoResult()
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
