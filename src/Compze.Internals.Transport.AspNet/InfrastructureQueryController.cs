using Compze.Contracts;
using Compze.Abstractions.Refactoring.Naming.Internal;
using Compze.Abstractions.Serialization.Internal;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Compze.Internals.Transport.AspNet;

#pragma warning disable CA1031

public class InfrastructureQueryController : Controller
{
   readonly IRemotableTessageSerializer _serializer;
   readonly IStructuralTypeMapper _typeMapper;
   readonly InfrastructureQueryExecutor _executor;

   public static void RegisterWith(IComponentRegistrar registrar) =>
      registrar.Register(
         Scoped.For<InfrastructureQueryController>()
               .CreatedBy((IRemotableTessageSerializer serializer,
                           IStructuralTypeMapper typeMapper,
                           InfrastructureQueryExecutor executor)
                             => new InfrastructureQueryController(serializer, typeMapper, executor)));

   InfrastructureQueryController(IRemotableTessageSerializer serializer,
                                 IStructuralTypeMapper typeMapper,
                                 InfrastructureQueryExecutor executor)
   {
      _serializer = serializer;
      _typeMapper = typeMapper;
      _executor = executor;
   }

   [HttpPost(HttpConstants.Routes.Infrastructure.Query)]
   public async Task<IActionResult> Query()
   {
      var typeIdStr = Request.Headers[HttpConstants.Headers.PayLoadTypeId][0]._assert().NotNull();
      var typeId = new MappedTypeId(Guid.Parse(typeIdStr));
      var queryType = _typeMapper.GetType(typeId);

      using var reader = new StreamReader(HttpContext.Request.Body);
      var json = await reader.ReadToEndAsync().caf();

      var query = _serializer.DeserializeTessage(queryType, json);

      try
      {
         var result = await RunOutsideScope(() => _executor.ExecuteQuery(query)).caf();
         var resultJson = _serializer.SerializeResponse(result);
         return Ok(resultJson);
      }
      catch(Exception exception)
      {
         this.Log().Warning(exception, "Exception handling infrastructure query");
         return Problem(statusCode: StatusCodes.Status500InternalServerError, type: exception.GetType().FullName, detail: exception.ToString());
      }
   }

   static Task<T> RunOutsideScope<T>(Func<T> action)
   {
      using(ExecutionContext.SuppressFlow())
         return Task.Run(action);
   }
}
