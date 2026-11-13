using System.Buffers;
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
///<remarks>A framed-connection transport is one whose connections are plain duplex byte streams: the named-pipe transport<br/>
/// and the TCP transport. The transports differ only in how a connection is listened for (<see cref="IConnectionListener"/>)<br/>
/// and opened (<see cref="IConnectionOpener"/>); what travels over the connection is this one format.</remarks>
///<remarks>Framing is explicit because a byte stream has no message boundaries of its own (message-mode pipes are<br/>
/// Windows-only; on Unix .NET pipes run over domain sockets): each frame is a fixed sequence of fields, strings as<br/>
/// little-endian int32 UTF-8 byte length + bytes.</remarks>
///<remarks>A frame reaches the connection as one write (<see cref="Frame"/>). On a socket every write is a send of its<br/>
/// own, so a frame written field by field would cross the network as a packet per field.</remarks>
static class ExchangeFraming
{
   const byte SuccessStatus = 1;
   const byte ErrorStatus = 2;

   internal static async Task WriteRequestAsync(Stream connection, TransportRequest request, CancellationToken cancellationToken)
   {
      var frame = new Frame().Byte((byte)request.Kind)
                             .String(request.TessageId.ToString())
                             .String(request.PayloadTypeIdString)
                             .String(request.Body);
      //The exactly-once kinds always carry the tessage's delivery stream position, so its presence is decided by the kind
      //byte already written — deterministic framing, no marker byte needed.
      if(KindCarriesADeliveryStreamPosition(request.Kind))
      {
         var position = request.DeliveryStreamPosition._assert().NotNull();
         frame.String(position.SenderEndpointId.Value.ToString())
              .Int64(position.SequenceNumber)
              .Int64(position.PredecessorSequenceNumber);
      }

      await frame.WriteToAsync(connection, cancellationToken).caf();
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

   internal static async Task WriteSuccessResponseAsync(Stream connection, string payload, CancellationToken cancellationToken) =>
      await new Frame().Byte(SuccessStatus)
                       .String(payload)
                       .WriteToAsync(connection, cancellationToken).caf();

   internal static async Task WriteErrorResponseAsync(Stream connection, string exceptionType, string exceptionDetail, CancellationToken cancellationToken) =>
      await new Frame().Byte(ErrorStatus)
                       .String(exceptionType)
                       .String(exceptionDetail)
                       .WriteToAsync(connection, cancellationToken).caf();

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

   static async Task<long> ReadInt64Async(Stream connection, CancellationToken cancellationToken)
   {
      var bytes = new byte[8];
      await connection.ReadExactlyAsync(bytes, cancellationToken).caf();
      return BinaryPrimitives.ReadInt64LittleEndian(bytes);
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

   ///<summary>A frame being put together for sending: its fields are collected in memory, in the order they are added,<br/>
   /// and <see cref="WriteToAsync"/> then writes the whole frame to the connection in one write.</summary>
   sealed class Frame
   {
      readonly ArrayBufferWriter<byte> _fields = new();

      internal Frame Byte(byte value)
      {
         _fields.GetSpan(1)[0] = value;
         _fields.Advance(1);
         return this;
      }

      internal Frame Int64(long value)
      {
         BinaryPrimitives.WriteInt64LittleEndian(_fields.GetSpan(sizeof(long)), value);
         _fields.Advance(sizeof(long));
         return this;
      }

      internal Frame String(string value)
      {
         var byteCount = Encoding.UTF8.GetByteCount(value);
         BinaryPrimitives.WriteInt32LittleEndian(_fields.GetSpan(sizeof(int)), byteCount);
         _fields.Advance(sizeof(int));
         Encoding.UTF8.GetBytes(value, _fields.GetSpan(byteCount));
         _fields.Advance(byteCount);
         return this;
      }

      internal async Task WriteToAsync(Stream connection, CancellationToken cancellationToken)
      {
         await connection.WriteAsync(_fields.WrittenMemory, cancellationToken).caf();
         await connection.FlushAsync(cancellationToken).caf();
      }
   }
}
