using System;
using System.IO;
using System.Threading.Tasks;
using Composable.Refactoring.Naming;
using Composable.Serialization;
using Composable.SystemCE;
using Composable.SystemCE.ThreadingCE.TasksCE;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Composable.Messaging.Buses.Implementation.Http;

class RpcController(IRemotableMessageSerializer serializer, ITypeMapper typeMapper, Inbox.HandlerExecutionEngine handlerExecutionEngine, Inbox.IMessageStorage storage) : Controller
{
   readonly ITypeMapper _typeMapper = typeMapper;
   readonly IRemotableMessageSerializer _serializer = serializer;
   readonly Inbox.HandlerExecutionEngine _handlerExecutionEngine = handlerExecutionEngine;
   readonly Inbox.IMessageStorage _storage = storage;

   class RequestData
   {
      RequestData(Guid messageId, TypeId typeId, string json, TransportMessage.InComing transportMessage)
      {
         MessageId = messageId;
         TypeId = typeId;
         Json = json;
         TransportMessage = transportMessage;
      }

      internal Guid MessageId { get; }
      internal TypeId TypeId { get; }
      internal string Json { get; }
      internal TransportMessage.InComing TransportMessage { get; }

      internal static async Task<RequestData> Create(HttpRequest request, ITypeMapper typeMapper, IRemotableMessageSerializer serializer)
      {
         var messageId = Guid.Parse(request.Headers["MessageId"][0].NotNull());
         var typeIdStr = request.Headers["PayloadTypeId"][0].NotNull();
         var typeId = new TypeId(Guid.Parse(typeIdStr));

         using var reader = new StreamReader(request.Body);
         var queryJson = await reader.ReadToEndAsync().CaF();

         var transportMessage = new TransportMessage.InComing(queryJson, typeId, [], messageId, typeMapper, serializer);
         return new RequestData(messageId, typeId, queryJson, transportMessage);
      }
   }

   [HttpPost("/internal/rpc/query")] public async Task<IActionResult> Query()
   {
      var requestData = await RequestData.Create(HttpContext.Request, _typeMapper, _serializer).CaF();

      try
      {
         var queryResultObject = (await _handlerExecutionEngine.Enqueue(requestData.TransportMessage).CaF()).NotNull();
         var responseJson = _serializer.SerializeResponse(queryResultObject);
         return Ok(responseJson);
      }
      catch(Exception exception)
      {
         return Problem(statusCode: StatusCodes.Status500InternalServerError, type: exception.GetType().FullName, detail: exception.ToString());
      }
   }

   [HttpPost("/internal/rpc/command-with-result")]
   public async Task<IActionResult> CommandWithResult()
   {
      var requestData = await RequestData.Create(HttpContext.Request, _typeMapper, _serializer).CaF();

      try
      {
         _storage.SaveIncomingMessage(requestData.TransportMessage);
         var queryResultObject = (await _handlerExecutionEngine.Enqueue(requestData.TransportMessage).CaF()).NotNull();
         var responseJson = _serializer.SerializeResponse(queryResultObject);
         return Ok(responseJson);
      }
      catch(Exception exception)
      {
         return Problem(statusCode: StatusCodes.Status500InternalServerError, type: exception.GetType().FullName, detail: exception.ToString());
      }
   }

   [HttpPost("/internal/rpc/command-no-result")]
   public async Task<IActionResult> CommandWithNoResult()
   {
      var requestData = await RequestData.Create(HttpContext.Request, _typeMapper, _serializer).CaF();

      try
      {
         _storage.SaveIncomingMessage(requestData.TransportMessage);
         await _handlerExecutionEngine.Enqueue(requestData.TransportMessage).CaF();
         return Ok();
      }
      catch(Exception exception)
      {
         return Problem(statusCode: StatusCodes.Status500InternalServerError, type: exception.GetType().FullName, detail: exception.ToString());
      }
   }
}
