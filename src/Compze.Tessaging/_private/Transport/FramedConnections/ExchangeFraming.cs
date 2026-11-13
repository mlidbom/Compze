using System.Buffers.Binary;
using System.Text;
using Compze.Contracts;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging._internal.Transport;

namespace Compze.Tessaging._private.Transport.FramedConnections;

///<summary>The wire format of the framed-connection transports: how a <see cref="TransportRequest"/> and its response are<br/>
/// written to and read from a connection's byte stream. One request frame is answered by exactly one response frame, in<br/>
/// lockstep: an exchange.</summary>
///<remarks>A framed-connection transport is one whose connections are plain duplex byte streams, such as the named-pipe<br/>
/// transport. The transports differ only in how a connection is listened for (<see cref="IConnectionListener"/>) and opened<br/>
/// (<see cref="IConnectionOpener"/>); what travels over the connection is this one format.</remarks>
///<remarks>Framing is explicit because a byte stream has no message boundaries of its own (message-mode pipes are<br/>
/// Windows-only; on Unix .NET pipes run over domain sockets): each frame is a fixed sequence of fields, strings as<br/>
/// little-endian int32 UTF-8 byte length + bytes.</remarks>
static class ExchangeFraming
{
   const byte SuccessStatus = 1;
   const byte ErrorStatus = 2;

   internal static async Task WriteRequestAsync(Stream connection, TransportRequest request, CancellationToken cancellationToken)
   {
      await WriteByteAsync(connection, (byte)request.Kind, cancellationToken).caf();
      await WriteStringAsync(connection, request.TessageId.ToString(), cancellationToken).caf();
      await WriteStringAsync(connection, request.PayloadTypeIdString, cancellationToken).caf();
      await WriteStringAsync(connection, request.Body, cancellationToken).caf();
      //The exactly-once kinds always carry the tessage's delivery stream position, so its presence is decided by the kind
      //byte already written — deterministic framing, no marker byte needed.
      if(KindCarriesADeliveryStreamPosition(request.Kind))
      {
         var position = request.DeliveryStreamPosition._assert().NotNull();
         await WriteStringAsync(connection, position.SenderEndpointId.Value.ToString(), cancellationToken).caf();
         await WriteInt64Async(connection, position.SequenceNumber, cancellationToken).caf();
         await WriteInt64Async(connection, position.PredecessorSequenceNumber, cancellationToken).caf();
      }
      await connection.FlushAsync(cancellationToken).caf();
   }

   ///<summary>Reads the next request frame. Throws <see cref="EndOfStreamException"/> if the client closed the connection — before the first byte that is the normal end of a connection.</summary>
   internal static async Task<TransportRequest> ReadRequestAsync(Stream connection, CancellationToken cancellationToken)
   {
      var kind = (TransportRequestKind)await ReadByteAsync(connection, cancellationToken).caf();
      var tessageId = new TessageId(Guid.Parse(await ReadStringAsync(connection, cancellationToken).caf()));
      var payloadTypeIdString = await ReadStringAsync(connection, cancellationToken).caf();
      var body = await ReadStringAsync(connection, cancellationToken).caf();
      if(!KindCarriesADeliveryStreamPosition(kind)) return new TransportRequest(kind, tessageId, payloadTypeIdString, body);

      var senderEndpointId = new EndpointId(Guid.Parse(await ReadStringAsync(connection, cancellationToken).caf()));
      var sequenceNumber = await ReadInt64Async(connection, cancellationToken).caf();
      var predecessorSequenceNumber = await ReadInt64Async(connection, cancellationToken).caf();
      return new TransportRequest(kind, tessageId, payloadTypeIdString, body, new DeliveryStreamPosition(senderEndpointId, sequenceNumber, predecessorSequenceNumber));
   }

   static bool KindCarriesADeliveryStreamPosition(TransportRequestKind kind) =>
      kind is TransportRequestKind.ExactlyOnceTevent or TransportRequestKind.ExactlyOnceTommand;

   internal static async Task WriteSuccessResponseAsync(Stream connection, string payload, CancellationToken cancellationToken)
   {
      await WriteByteAsync(connection, SuccessStatus, cancellationToken).caf();
      await WriteStringAsync(connection, payload, cancellationToken).caf();
      await connection.FlushAsync(cancellationToken).caf();
   }

   internal static async Task WriteErrorResponseAsync(Stream connection, string exceptionType, string exceptionDetail, CancellationToken cancellationToken)
   {
      await WriteByteAsync(connection, ErrorStatus, cancellationToken).caf();
      await WriteStringAsync(connection, exceptionType, cancellationToken).caf();
      await WriteStringAsync(connection, exceptionDetail, cancellationToken).caf();
      await connection.FlushAsync(cancellationToken).caf();
   }

   ///<summary>Reads the response frame answering a request: the success payload, or the error the handler threw, to be rethrown client-side.</summary>
   internal static async Task<ExchangeResponse> ReadResponseAsync(Stream connection, CancellationToken cancellationToken)
   {
      var status = await ReadByteAsync(connection, cancellationToken).caf();
      if(status == SuccessStatus)
         return ExchangeResponse.Success(await ReadStringAsync(connection, cancellationToken).caf());

      var exceptionType = await ReadStringAsync(connection, cancellationToken).caf();
      var exceptionDetail = await ReadStringAsync(connection, cancellationToken).caf();
      return ExchangeResponse.Error(exceptionType, exceptionDetail);
   }

   static async Task WriteByteAsync(Stream connection, byte value, CancellationToken cancellationToken) =>
      await connection.WriteAsync(new[] { value }, cancellationToken).caf();

   static async Task WriteInt64Async(Stream connection, long value, CancellationToken cancellationToken)
   {
      var bytes = new byte[8];
      BinaryPrimitives.WriteInt64LittleEndian(bytes, value);
      await connection.WriteAsync(bytes, cancellationToken).caf();
   }

   static async Task<long> ReadInt64Async(Stream connection, CancellationToken cancellationToken)
   {
      var bytes = new byte[8];
      await connection.ReadExactlyAsync(bytes, cancellationToken).caf();
      return BinaryPrimitives.ReadInt64LittleEndian(bytes);
   }

   static async Task WriteStringAsync(Stream connection, string value, CancellationToken cancellationToken)
   {
      var bytes = Encoding.UTF8.GetBytes(value);
      var lengthPrefix = new byte[4];
      BinaryPrimitives.WriteInt32LittleEndian(lengthPrefix, bytes.Length);
      await connection.WriteAsync(lengthPrefix, cancellationToken).caf();
      await connection.WriteAsync(bytes, cancellationToken).caf();
   }

   static async Task<byte> ReadByteAsync(Stream connection, CancellationToken cancellationToken)
   {
      var buffer = new byte[1];
      await connection.ReadExactlyAsync(buffer, cancellationToken).caf();
      return buffer[0];
   }

   static async Task<string> ReadStringAsync(Stream connection, CancellationToken cancellationToken)
   {
      var lengthPrefix = new byte[4];
      await connection.ReadExactlyAsync(lengthPrefix, cancellationToken).caf();
      var length = BinaryPrimitives.ReadInt32LittleEndian(lengthPrefix);
      var bytes = new byte[length];
      await connection.ReadExactlyAsync(bytes, cancellationToken).caf();
      return Encoding.UTF8.GetString(bytes);
   }
}
