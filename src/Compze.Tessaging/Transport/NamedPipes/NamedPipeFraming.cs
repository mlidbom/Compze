using System.Buffers.Binary;
using System.Text;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Abstractions;

namespace Compze.Tessaging.Transport.NamedPipes;

///<summary>The named-pipe transport's wire format: how a <see cref="TransportRequest"/> and its response are written to<br/>
/// and read from the pipe's byte stream. One request frame is answered by exactly one response frame, in lockstep.</summary>
///<remarks>Framing is explicit because a pipe is a byte stream (message-mode pipes are Windows-only; on Unix .NET pipes run over<br/>
/// domain sockets): each frame is a fixed sequence of fields, strings as little-endian int32 UTF-8 byte length + bytes.</remarks>
static class NamedPipeFraming
{
   const byte SuccessStatus = 1;
   const byte ErrorStatus = 2;

   internal static async Task WriteRequestAsync(Stream pipe, TransportRequest request, CancellationToken cancellationToken)
   {
      await WriteByteAsync(pipe, (byte)request.Kind, cancellationToken).caf();
      await WriteStringAsync(pipe, request.TessageId.ToString(), cancellationToken).caf();
      await WriteStringAsync(pipe, request.PayloadTypeIdString, cancellationToken).caf();
      await WriteStringAsync(pipe, request.Body, cancellationToken).caf();
      await pipe.FlushAsync(cancellationToken).caf();
   }

   ///<summary>Reads the next request frame. Throws <see cref="EndOfStreamException"/> if the client closed the pipe — before the first byte that is the normal end of a connection.</summary>
   internal static async Task<TransportRequest> ReadRequestAsync(Stream pipe, CancellationToken cancellationToken)
   {
      var kind = (TransportRequestKind)await ReadByteAsync(pipe, cancellationToken).caf();
      var tessageId = new TessageId(Guid.Parse(await ReadStringAsync(pipe, cancellationToken).caf()));
      var payloadTypeIdString = await ReadStringAsync(pipe, cancellationToken).caf();
      var body = await ReadStringAsync(pipe, cancellationToken).caf();
      return new TransportRequest(kind, tessageId, payloadTypeIdString, body);
   }

   internal static async Task WriteSuccessResponseAsync(Stream pipe, string payload, CancellationToken cancellationToken)
   {
      await WriteByteAsync(pipe, SuccessStatus, cancellationToken).caf();
      await WriteStringAsync(pipe, payload, cancellationToken).caf();
      await pipe.FlushAsync(cancellationToken).caf();
   }

   internal static async Task WriteErrorResponseAsync(Stream pipe, string exceptionType, string exceptionDetail, CancellationToken cancellationToken)
   {
      await WriteByteAsync(pipe, ErrorStatus, cancellationToken).caf();
      await WriteStringAsync(pipe, exceptionType, cancellationToken).caf();
      await WriteStringAsync(pipe, exceptionDetail, cancellationToken).caf();
      await pipe.FlushAsync(cancellationToken).caf();
   }

   ///<summary>Reads the response frame answering a request: the success payload, or the error the handler threw, to be rethrown client-side.</summary>
   internal static async Task<NamedPipeTransportResponse> ReadResponseAsync(Stream pipe, CancellationToken cancellationToken)
   {
      var status = await ReadByteAsync(pipe, cancellationToken).caf();
      if(status == SuccessStatus)
         return NamedPipeTransportResponse.Success(await ReadStringAsync(pipe, cancellationToken).caf());

      var exceptionType = await ReadStringAsync(pipe, cancellationToken).caf();
      var exceptionDetail = await ReadStringAsync(pipe, cancellationToken).caf();
      return NamedPipeTransportResponse.Error(exceptionType, exceptionDetail);
   }

   static async Task WriteByteAsync(Stream pipe, byte value, CancellationToken cancellationToken) =>
      await pipe.WriteAsync(new[] { value }, cancellationToken).caf();

   static async Task WriteStringAsync(Stream pipe, string value, CancellationToken cancellationToken)
   {
      var bytes = Encoding.UTF8.GetBytes(value);
      var lengthPrefix = new byte[4];
      BinaryPrimitives.WriteInt32LittleEndian(lengthPrefix, bytes.Length);
      await pipe.WriteAsync(lengthPrefix, cancellationToken).caf();
      await pipe.WriteAsync(bytes, cancellationToken).caf();
   }

   static async Task<byte> ReadByteAsync(Stream pipe, CancellationToken cancellationToken)
   {
      var buffer = new byte[1];
      await pipe.ReadExactlyAsync(buffer, cancellationToken).caf();
      return buffer[0];
   }

   static async Task<string> ReadStringAsync(Stream pipe, CancellationToken cancellationToken)
   {
      var lengthPrefix = new byte[4];
      await pipe.ReadExactlyAsync(lengthPrefix, cancellationToken).caf();
      var length = BinaryPrimitives.ReadInt32LittleEndian(lengthPrefix);
      var bytes = new byte[length];
      await pipe.ReadExactlyAsync(bytes, cancellationToken).caf();
      return Encoding.UTF8.GetString(bytes);
   }
}

///<summary>What came back over the pipe for a request: either the handler's success payload, or the exception it threw (as type name + detail) for the client to rethrow.</summary>
class NamedPipeTransportResponse
{
   internal bool Succeeded { get; }
   internal string Payload { get; }
   internal string ExceptionType { get; }
   internal string ExceptionDetail { get; }

   internal static NamedPipeTransportResponse Success(string payload) => new(succeeded: true, payload: payload, exceptionType: "", exceptionDetail: "");
   internal static NamedPipeTransportResponse Error(string exceptionType, string exceptionDetail) => new(succeeded: false, payload: "", exceptionType: exceptionType, exceptionDetail: exceptionDetail);

   NamedPipeTransportResponse(bool succeeded, string payload, string exceptionType, string exceptionDetail)
   {
      Succeeded = succeeded;
      Payload = payload;
      ExceptionType = exceptionType;
      ExceptionDetail = exceptionDetail;
   }
}
