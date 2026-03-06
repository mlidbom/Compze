using Compze.Contracts;
using Compze.Abstractions.Refactoring.Naming.Internal;
using Compze.Abstractions.Serialization.Internal;
using Compze.Internals.SystemCE.Core.ThreadingCE.TasksCE;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Tessaging.Implementation.TessageHandling.Inbox;
using Compze.Tessaging.Implementation.Transport.Client.Implementation.Http;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Compze.Tessaging.Hosting.AspNetCore.Private;

#pragma warning disable CA1031 //Here we catch all exceptions so we can transmit them back to the client

class TypermediaController : ControllerBase
{
   internal static void RegisterWith(IComponentRegistrar registrar) =>
      registrar.Register(
         Scoped.For<TypermediaController>()
               .CreatedBy((IRemotableTessageSerializer serializer,
                           ITypeMapper typeMapper,
                           IInbox inbox,
                           Inbox.HandlerExecutionEngine handlerExecutionEngine)
                             => new TypermediaController(serializer, typeMapper, inbox, handlerExecutionEngine)));

   TypermediaController(IRemotableTessageSerializer serializer,
                 ITypeMapper typeMapper,
                 IInbox inbox,
                 Inbox.HandlerExecutionEngine handlerExecutionEngine) : base(serializer, typeMapper, inbox, handlerExecutionEngine) {}

   [HttpPost(HttpConstants.Routes.Typermedia.Tuery)]
   public async Task<IActionResult> Tuery()
   {
      var incomingTessage = await CreateIncomingTessage().caf();

      try
      {
         var tueryResponse = (await HandlerExecutionEngine.ExecuteAsync(incomingTessage).caf())._assert().NotNull();
         var tueryResponseJson = Serializer.SerializeResponse(tueryResponse);
         return Ok(tueryResponseJson);
      }
      catch(Exception exception)
      {
         //todo: eliminate all this code duplication
         this.Log().Warning(exception, "Exception handling tuery");
         return Problem(statusCode: StatusCodes.Status500InternalServerError, type: exception.GetType().FullName, detail: exception.ToString());
      }
   }

   [HttpPost(HttpConstants.Routes.Typermedia.TommandWithResult)]
   public async Task<IActionResult> TommandWithResult()
   {
      var incomingTessage = await CreateIncomingTessage().caf();

      try
      {
         var tommandResponse = (await Inbox.ExecuteAsync(incomingTessage).caf())._assert().NotNull();
         var tommandResponseJson = Serializer.SerializeResponse(tommandResponse);
         return Ok(tommandResponseJson);
      }
      catch(Exception exception)
      {
         this.Log().Warning(exception, "Exception handling tommand with result");
         return Problem(statusCode: StatusCodes.Status500InternalServerError, type: exception.GetType().FullName, detail: exception.ToString());
      }
   }

   [HttpPost(HttpConstants.Routes.Typermedia.TommandNoResult)]
   public async Task<IActionResult> TommandWithNoResult()
   {
      var incomingTessage = await CreateIncomingTessage().caf();

      try
      {
         await Inbox.ExecuteAsync(incomingTessage).caf();
         return Ok();
      }
      catch(Exception exception)
      {
         this.Log().Warning(exception, "Exception handling tommand with no result");
         return Problem(statusCode: StatusCodes.Status500InternalServerError, type: exception.GetType().FullName, detail: exception.ToString());
      }
   }
}
