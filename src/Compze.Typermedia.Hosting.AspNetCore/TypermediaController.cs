using Compze.Contracts;
using Compze.TypeIdentifiers;
using Compze.Abstractions.Serialization.Internal;
using Compze.Abstractions.Tessaging.Public;
using Compze.Internals.Transport;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Compze.Typermedia.Hosting.AspNetCore;

#pragma warning disable CA1031 //Here we catch all exceptions so we can transmit them back to the client

public class TypermediaController : Controller
{
   readonly IRemotableTessageSerializer _serializer;
   readonly ITypeMap _typeMap;
   readonly TypermediaHandlerExecutor _executor;

   public static void RegisterWith(IComponentRegistrar registrar) =>
      registrar.Register(
         Scoped.For<TypermediaController>()
               .CreatedBy((IRemotableTessageSerializer serializer,
                           ITypeMap typeMap,
                           TypermediaHandlerExecutor executor)
                             => new TypermediaController(serializer, typeMap, executor)));

   TypermediaController(IRemotableTessageSerializer serializer,
                 ITypeMap typeMap,
                 TypermediaHandlerExecutor executor)
   {
      _serializer = serializer;
      _typeMap = typeMap;
      _executor = executor;
   }

   [HttpPost(HttpConstants.Routes.Typermedia.Tuery)]
   public async Task<IActionResult> Tuery()
   {
      var tessage = await DeserializeTessageFromRequest().caf();

      try
      {
         var tueryResponse = await Task.Run(() => _executor.ExecuteTuery(tessage)).caf();
         var tueryResponseJson = _serializer.SerializeResponse(tueryResponse);
         return Ok(tueryResponseJson);
      }
      catch(Exception exception)
      {
         this.Log().Warning(exception, "Exception handling tuery");
         return Problem(statusCode: StatusCodes.Status500InternalServerError, type: exception.GetType().FullName, detail: exception.ToString());
      }
   }

   [HttpPost(HttpConstants.Routes.Typermedia.TommandWithResult)]
   public async Task<IActionResult> TommandWithResult()
   {
      var tessage = await DeserializeTessageFromRequest().caf();

      try
      {
         var tommandResponse = await Task.Run(() => _executor.ExecuteTommandWithResult(tessage)).caf();
         var tommandResponseJson = _serializer.SerializeResponse(tommandResponse);
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
      var tessage = await DeserializeTessageFromRequest().caf();

      try
      {
         await Task.Run(() => _executor.ExecuteVoidTommand((IAtMostOnceTypermediaTommand)tessage)).caf();
         return Ok();
      }
      catch(Exception exception)
      {
         this.Log().Warning(exception, "Exception handling tommand with no result");
         return Problem(statusCode: StatusCodes.Status500InternalServerError, type: exception.GetType().FullName, detail: exception.ToString());
      }
   }

   async Task<ITessage> DeserializeTessageFromRequest()
   {
      var typeIdStr = Request.Headers[HttpConstants.Headers.PayLoadTypeId][0]._assert().NotNull();
      var tessageType = _typeMap.GetId(typeIdStr).Type;

      using var reader = new StreamReader(HttpContext.Request.Body);
      var json = await reader.ReadToEndAsync().caf();

      return (ITessage)_serializer.DeserializeTessage(tessageType, json);
   }

}
