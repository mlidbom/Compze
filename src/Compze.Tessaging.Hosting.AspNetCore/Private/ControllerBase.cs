using Compze.Contracts;
using Compze.Abstractions.Public;
using Compze.Abstractions.Refactoring.Naming.Internal;
using Compze.Abstractions.Serialization.Internal;
using Compze.Internals.SystemCE.Core.ThreadingCE.TasksCE;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Internals.Transport;
using Microsoft.AspNetCore.Mvc;

namespace Compze.Tessaging.Hosting.AspNetCore.Private;

public abstract class ControllerBase(IRemotableTessageSerializer serializer, ITypeMapper typeMapper, IInbox inbox) : Controller
{
   readonly IRemotableTessageSerializer _serializer = serializer;
   readonly ITypeMapper _typeMapper = typeMapper;
   protected IInbox Inbox { get; } = inbox;

   protected async Task<TransportTessage.InComing> CreateIncomingTessage()
   {
      var tessageId = new TessageId(Guid.Parse(Request.Headers[HttpConstants.Headers.TessageId][0]._assert().NotNull()));
      var typeIdStr = Request.Headers[HttpConstants.Headers.PayLoadTypeId][0]._assert().NotNull();
      var typeId = new TypeId(Guid.Parse(typeIdStr));

      using var reader = new StreamReader(HttpContext.Request.Body);
      var tueryJson = await reader.ReadToEndAsync().caf();

      return new TransportTessage.InComing(tueryJson, typeId, tessageId, _typeMapper, _serializer);
   }
}
