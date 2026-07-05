using Compze.ServiceBus.Implementation.TessageHandling.Abstractions;
using Compze.Contracts;
using Compze.Abstractions.Public;
using Compze.TypeIdentifiers;
using Compze.Abstractions.Serialization.Internal;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.ServiceBus.Implementation.Transport.Abstractions;
using Compze.Internals.Transport;
using Microsoft.AspNetCore.Mvc;

namespace Compze.Tessaging.Hosting.AspNetCore.Private;

public abstract class ControllerBase(IRemotableTessageSerializer serializer, ITypeMap typeMap, IInbox inbox) : Controller
{
   readonly IRemotableTessageSerializer _serializer = serializer;
   readonly ITypeMap _typeMap = typeMap;
   protected IInbox Inbox { get; } = inbox;

   protected async Task<TransportTessage.InComing> CreateIncomingTessage()
   {
      var tessageId = new TessageId(Guid.Parse(Request.Headers[HttpConstants.Headers.TessageId][0]._assert().NotNull()));
      var typeIdStr = Request.Headers[HttpConstants.Headers.PayLoadTypeId][0]._assert().NotNull();

      using var reader = new StreamReader(HttpContext.Request.Body);
      var tueryJson = await reader.ReadToEndAsync().caf();

      return new TransportTessage.InComing(tueryJson, typeIdStr, tessageId, _typeMap, _serializer);
   }
}
