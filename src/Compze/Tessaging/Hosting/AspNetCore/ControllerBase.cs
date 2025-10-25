using System;
using System.IO;
using System.Threading.Tasks;
using Compze.Abstractions.Refactoring.Naming.Internal;
using Compze.Abstractions.Serialization.Internal;
using Compze.Abstractions.Time;
using Compze.Tessaging.Implementation;
using Compze.Tessaging.Implementation.TessageHandling;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Tessaging.Implementation.Transport.Client.Http;
using Compze.Utilities.SystemCE;
using Compze.Utilities.Threading.TasksCE;
using Microsoft.AspNetCore.Mvc;

namespace Compze.Tessaging.Hosting.AspNetCore;

abstract class ControllerBase(IRemotableTessageSerializer serializer, ITypeMapper typeMapper, IInbox inbox, Inbox.HandlerExecutionEngine handlerExecutionEngine) : Controller
{
   readonly ITypeMapper _typeMapper = typeMapper;
   protected readonly IInbox Inbox = inbox;
   protected readonly IRemotableTessageSerializer Serializer = serializer;
   protected readonly Inbox.HandlerExecutionEngine HandlerExecutionEngine = handlerExecutionEngine;

   protected async Task<TransportTessage.InComing> CreateIncomingTessage()
   {
      var tessageId = Guid.Parse(Request.Headers[HttpConstants.Headers.TessageId][0].NotNull());
      var typeIdStr = Request.Headers[HttpConstants.Headers.PayLoadTypeId][0].NotNull();
      var typeId = new TypeId(Guid.Parse(typeIdStr));

      using var reader = new StreamReader(HttpContext.Request.Body);
      var queryJson = await reader.ReadToEndAsync().caf();

      return new TransportTessage.InComing(queryJson, typeId, [], tessageId, _typeMapper, Serializer);
   }
}
