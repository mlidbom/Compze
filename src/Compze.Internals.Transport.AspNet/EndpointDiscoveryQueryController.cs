using Compze.Contracts;
using Compze.TypeIdentifiers;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Compze.Internals.Transport.AspNet;

#pragma warning disable CA1031

public class EndpointDiscoveryQueryController : Controller
{
   readonly ITypeMap _typeMap;
   readonly EndpointDiscoveryQueryExecutor _executor;

   public static void RegisterWith(IComponentRegistrar registrar) =>
      registrar.Register(
         Scoped.For<EndpointDiscoveryQueryController>()
               .CreatedBy((ITypeMap typeMap,
                           EndpointDiscoveryQueryExecutor executor)
                             => new EndpointDiscoveryQueryController(typeMap, executor)));

   EndpointDiscoveryQueryController(ITypeMap typeMap,
                                 EndpointDiscoveryQueryExecutor executor)
   {
      _typeMap = typeMap;
      _executor = executor;
   }

   [HttpPost(HttpConstants.Routes.EndpointDiscovery.Query)]
   public async Task<IActionResult> Query()
   {
      var typeIdStr = Request.Headers[HttpConstants.Headers.PayLoadTypeId][0]._assert().NotNull();
      var queryType = _typeMap.GetId(typeIdStr).Type;

      using var reader = new StreamReader(HttpContext.Request.Body);
      var json = await reader.ReadToEndAsync().caf();

      var query = EndpointDiscoverySerializer.DeserializeQuery(queryType, json);

      try
      {
         var result = await RunOutsideScope(() => _executor.ExecuteQuery(query)).caf();
         var resultJson = EndpointDiscoverySerializer.SerializeResult(result);
         return Ok(resultJson);
      }
      catch(Exception exception)
      {
         this.Log().Warning(exception, "Exception handling endpoint-discovery query");
         return Problem(statusCode: StatusCodes.Status500InternalServerError, type: exception.GetType().FullName, detail: exception.ToString());
      }
   }

   static Task<T> RunOutsideScope<T>(Func<T> action)
   {
      using(ExecutionContext.SuppressFlow())
         return Task.Run(action);
   }
}
