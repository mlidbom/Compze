using System;
using System.IO;
using System.Threading.Tasks;
using Compze.Contracts;
using Compze.Core.Public;
using Compze.Core.Refactoring.Naming.Internal;
using Compze.Core.Serialization.Internal;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Tessaging.Implementation.TessageHandling.Inbox;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Tessaging.Implementation.Transport.Client.Implementation.Http;
using Compze.Threading.TasksCE;
using Microsoft.AspNetCore.Mvc;

namespace Compze.Tessaging.Hosting.AspNetCore.Private;

public abstract class ControllerBase(IRemotableTessageSerializer serializer, ITypeMapper typeMapper, IInbox inbox, Inbox.HandlerExecutionEngine handlerExecutionEngine) : Controller
{
   readonly ITypeMapper _typeMapper = typeMapper;
   protected IInbox Inbox { get; } = inbox;
   protected IRemotableTessageSerializer Serializer { get; } = serializer;
   protected Inbox.HandlerExecutionEngine HandlerExecutionEngine { get; } = handlerExecutionEngine;

   protected async Task<TransportTessage.InComing> CreateIncomingTessage()
   {
      var tessageId = new TessageId(Guid.Parse(Request.Headers[HttpConstants.Headers.TessageId][0]._assert().NotNull()));
      var typeIdStr = Request.Headers[HttpConstants.Headers.PayLoadTypeId][0]._assert().NotNull();
      var typeId = new TypeId(Guid.Parse(typeIdStr));

      using var reader = new StreamReader(HttpContext.Request.Body);
      var tueryJson = await reader.ReadToEndAsync().caf();

      return new TransportTessage.InComing(tueryJson, typeId, tessageId, _typeMapper, Serializer);
   }
}
