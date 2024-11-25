﻿using System;
using System.IO;
using System.Threading.Tasks;
using Composable.Refactoring.Naming;
using Composable.Serialization;
using Composable.SystemCE;
using Composable.SystemCE.ThreadingCE.TasksCE;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Composable.Messaging.Buses.Implementation;

class QueryController(IRemotableMessageSerializer serializer, ITypeMapper typeMapper, Inbox.HandlerExecutionEngine handlerExecutionEngine) : Controller
{
   readonly ITypeMapper _typeMapper = typeMapper;
   readonly IRemotableMessageSerializer _serializer = serializer;
   readonly Inbox.HandlerExecutionEngine _handlerExecutionEngine = handlerExecutionEngine;

   [HttpPost("/internal/rpc/query")] public async Task<IActionResult> Query()
   {
      var messageId = Guid.Parse(HttpContext.Request.Headers["MessageId"][0].NotNull());
      var typeIdStr = HttpContext.Request.Headers["PayloadTypeId"][0].NotNull();
      var typeId = new TypeId(Guid.Parse(typeIdStr));

      using var reader = new StreamReader(Request.Body);
      var queryJson = await reader.ReadToEndAsync().CaF();

      var transportMessage = new TransportMessage.InComing(queryJson, typeId, [], messageId, _typeMapper, _serializer);

      try
      {
         var queryResultObject = (await _handlerExecutionEngine.Enqueue(transportMessage).CaF()).NotNull();
         var responseJson = _serializer.SerializeResponse(queryResultObject);
         return Ok(responseJson);
      }
      catch(Exception exception)
      {
         return Problem(statusCode: StatusCodes.Status500InternalServerError, type: exception.GetType().FullName, detail: exception.ToString());
      }
   }
}
