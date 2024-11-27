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

   static async Task<TransportMessage.InComing> CreateIncomingMessage(HttpRequest request, ITypeMapper typeMapper, IRemotableMessageSerializer serializer)
   {
      var messageId = Guid.Parse(request.Headers["MessageId"][0].NotNull());
      var typeIdStr = request.Headers["PayloadTypeId"][0].NotNull();
      var typeId = new TypeId(Guid.Parse(typeIdStr));

      using var reader = new StreamReader(request.Body);
      var queryJson = await reader.ReadToEndAsync().CaF();

      return new TransportMessage.InComing(queryJson, typeId, [], messageId, typeMapper, serializer);
   }

   [HttpPost(Routes.Rpc.Query)] public async Task<IActionResult> Query()
   {
      var incomingMessage = await CreateIncomingMessage(HttpContext.Request, _typeMapper, _serializer).CaF();

      try
      {
         var queryResultObject = (await _handlerExecutionEngine.Enqueue(incomingMessage).CaF()).NotNull();
         var responseJson = _serializer.SerializeResponse(queryResultObject);
         return Ok(responseJson);
      }
      catch(Exception exception)
      {
         return Problem(statusCode: StatusCodes.Status500InternalServerError, type: exception.GetType().FullName, detail: exception.ToString());
      }
   }

   [HttpPost(Routes.Rpc.CommandWithResult)]
   public async Task<IActionResult> CommandWithResult()
   {
      var incomingMessage = await CreateIncomingMessage(HttpContext.Request, _typeMapper, _serializer).CaF();

      try
      {
         _storage.SaveIncomingMessage(incomingMessage);
         var queryResultObject = (await _handlerExecutionEngine.Enqueue(incomingMessage).CaF()).NotNull();
         var responseJson = _serializer.SerializeResponse(queryResultObject);
         return Ok(responseJson);
      }
      catch(Exception exception)
      {
         return Problem(statusCode: StatusCodes.Status500InternalServerError, type: exception.GetType().FullName, detail: exception.ToString());
      }
   }

   [HttpPost(Routes.Rpc.CommandNoResult)]
   public async Task<IActionResult> CommandWithNoResult()
   {
      var incomingMessage = await CreateIncomingMessage(HttpContext.Request, _typeMapper, _serializer).CaF();

      try
      {
         _storage.SaveIncomingMessage(incomingMessage);
         await _handlerExecutionEngine.Enqueue(incomingMessage).CaF();
         return Ok();
      }
      catch(Exception exception)
      {
         return Problem(statusCode: StatusCodes.Status500InternalServerError, type: exception.GetType().FullName, detail: exception.ToString());
      }
   }
}
