using System;
using System.IO;
using System.Threading.Tasks;
using Composable.Refactoring.Naming;
using Composable.Serialization;
using Composable.SystemCE;
using Composable.SystemCE.ThreadingCE.TasksCE;
using Microsoft.AspNetCore.Mvc;

namespace Composable.Messaging.Buses.Implementation.Http;

abstract class ControllerBase(IRemotableMessageSerializer serializer, ITypeMapper typeMapper, Inbox.HandlerExecutionEngine handlerExecutionEngine, Inbox.IMessageStorage storage) : Controller
{
   readonly ITypeMapper _typeMapper = typeMapper;
   protected readonly IRemotableMessageSerializer Serializer = serializer;
   protected readonly Inbox.IMessageStorage Storage = storage;
   protected readonly Inbox.HandlerExecutionEngine HandlerExecutionEngine = handlerExecutionEngine;

   protected async Task<TransportMessage.InComing> CreateIncomingMessage()
   {
      var messageId = Guid.Parse(Request.Headers["MessageId"][0].NotNull());
      var typeIdStr = Request.Headers["PayloadTypeId"][0].NotNull();
      var typeId = new TypeId(Guid.Parse(typeIdStr));

      using var reader = new StreamReader(HttpContext.Request.Body);
      var queryJson = await reader.ReadToEndAsync().CaF();

      return new TransportMessage.InComing(queryJson, typeId, [], messageId, _typeMapper, Serializer);
   }
}
