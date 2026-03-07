using Compze.Contracts;
using Compze.Abstractions.Public;
using Compze.Abstractions.Refactoring.Naming.Internal;
using Compze.Abstractions.Serialization.Internal;
using Compze.Abstractions.Tessaging.Public;
using Compze.Internals.SystemCE.Core.ThreadingCE.TasksCE;
using Compze.Tessaging.Implementation.Transport.Client.Implementation.Http;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Typermedia.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Compze.Tessaging.Hosting.AspNetCore.Private;

#pragma warning disable CA1031 //Here we catch all exceptions so we can transmit them back to the client

class TypermediaController : Controller
{
   readonly IRemotableTessageSerializer _serializer;
   readonly ITypeMapper _typeMapper;
   readonly TypermediaHandlerExecutor _executor;

   internal static void RegisterWith(IComponentRegistrar registrar) =>
      registrar.Register(
         Scoped.For<TypermediaController>()
               .CreatedBy((IRemotableTessageSerializer serializer,
                           ITypeMapper typeMapper,
                           TypermediaHandlerExecutor executor)
                             => new TypermediaController(serializer, typeMapper, executor)));

   TypermediaController(IRemotableTessageSerializer serializer,
                 ITypeMapper typeMapper,
                 TypermediaHandlerExecutor executor)
   {
      _serializer = serializer;
      _typeMapper = typeMapper;
      _executor = executor;
   }

   [HttpPost(HttpConstants.Routes.Typermedia.Tuery)]
   public async Task<IActionResult> Tuery()
   {
      var tessage = await DeserializeTessageFromRequest().caf();

      try
      {
         var tueryResponse = await RunOutsideScope(() => _executor.ExecuteTuery(tessage)).caf();
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
         var tommandResponse = await RunOutsideScope(() => _executor.ExecuteTommandWithResult(tessage)).caf();
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
         await RunOutsideScope(() => { _executor.ExecuteVoidTommand((IAtMostOnceTypermediaTommand)tessage); return (object?)null; }).caf();
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
      var typeId = new TypeId(Guid.Parse(typeIdStr));
      var tessageType = _typeMapper.GetType(typeId);

      using var reader = new StreamReader(HttpContext.Request.Body);
      var json = await reader.ReadToEndAsync().caf();

      return _serializer.DeserializeTessage(tessageType, json);
   }

   // The ASP.NET middleware creates a DI scope (via ExecuteInIsolatedScopeAsync) that flows through AsyncLocal.
   // The executor needs to create its own scope, but nested scopes aren't supported.
   // SuppressFlow prevents the middleware's scope from flowing into Task.Run.
   static Task<T> RunOutsideScope<T>(Func<T> action)
   {
      using(ExecutionContext.SuppressFlow())
         return Task.Run(action);
   }
}
