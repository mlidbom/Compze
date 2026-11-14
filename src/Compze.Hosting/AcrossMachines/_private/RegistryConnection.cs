using System.Buffers;
using System.Buffers.Binary;
using System.Text;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.Discovery;

namespace Compze.Hosting.AcrossMachines._private;

///<summary>The connection between a <see cref="NetworkEndpointRegistry"/> and its <see cref="EndpointRegistryServer"/>, as<br/>
/// either end uses it: what the two send each other over one TCP connection.</summary>
///<remarks>The conversation: the registry names the registry it participates in, once, and from then on sends what it<br/>
/// announces there, whenever that changes. The server sends the registry's membership, whenever that changes. Each is sent<br/>
/// as a whole set, never as a change to one, so neither end depends on what the other was sent before: a registry that<br/>
/// connects again sends what it announces now, and is sent the membership as it is now.</remarks>
///<remarks>What is sent is a sequence of fields: an int32 is little-endian, a string is its UTF-8 byte length as an int32<br/>
/// and the bytes, and an endpoint's id is the 16 bytes of its Guid. A set of announcements is their count and then each<br/>
/// one's endpoint id and address. Each thing sent reaches the connection as one write (<see cref="FieldsToSend"/>): on a<br/>
/// socket every write is a send of its own.</remarks>
sealed class RegistryConnection : IAsyncDisposable
{
   const int BytesOfAGuid = 16;

   readonly Stream _stream;

   ///<summary>The connection over <paramref name="stream"/>, which it owns from here on.</summary>
   internal RegistryConnection(Stream stream) => _stream = stream;

   internal Task SendRegistryNameAsync(string registryName, CancellationToken cancellationToken) =>
      new FieldsToSend().String(registryName)
                        .WriteToAsync(_stream, cancellationToken);

   internal Task<string> ReceiveRegistryNameAsync(CancellationToken cancellationToken) => ReceiveStringAsync(cancellationToken);

   internal Task SendAnnouncementsAsync(IReadOnlyCollection<EndpointAnnouncement> announcements, CancellationToken cancellationToken)
   {
      var fields = new FieldsToSend().Int32(announcements.Count);
      foreach(var announcement in announcements)
         fields.Guid(announcement.EndpointId.Value)
               .String(announcement.Address.Uri.AbsoluteUri);
      return fields.WriteToAsync(_stream, cancellationToken);
   }

   ///<summary>Receives the next set of announcements. Throws an <see cref="EndOfStreamException"/>, which is an<br/>
   /// <see cref="IOException"/>, when the other end closed the connection.</summary>
   internal async Task<IReadOnlyList<EndpointAnnouncement>> ReceiveAnnouncementsAsync(CancellationToken cancellationToken)
   {
      var announcements = new List<EndpointAnnouncement>();
      var count = await ReceiveInt32Async(cancellationToken).caf();
      for(var index = 0; index < count; index++)
         announcements.Add(new EndpointAnnouncement(endpointId: new EndpointId(await ReceiveGuidAsync(cancellationToken).caf()),
                                                    address: new EndpointAddress(new Uri(await ReceiveStringAsync(cancellationToken).caf()))));
      return announcements;
   }

   ///<summary>Runs the two directions of the conversation, each with a token that is cancelled once the other has ended,<br/>
   /// and returns when both have. A direction goes on until it fails or its token is cancelled, so the task ends in what<br/>
   /// ended the first one: an <see cref="IOException"/> when the connection ended. When <paramref name="cancellationToken"/><br/>
   /// was cancelled it ends in an <see cref="OperationCanceledException"/>, or returns.</summary>
   internal static async Task RunBothDirectionsUntilEitherEndsAsync(Func<CancellationToken, Task> sending, Func<CancellationToken, Task> receiving, CancellationToken cancellationToken)
   {
      using var eitherEnded = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
      var sendingEnds = sending(eitherEnded.Token);
      var receivingEnds = receiving(eitherEnded.Token);
      await Task.WhenAny(sendingEnds, receivingEnds).caf();
      await eitherEnded.CancelAsync().caf();
      await Task.WhenAll(sendingEnds, receivingEnds).caf(); //Rethrows what a direction failed with rather than the cancellation that then ended the other one.
   }

   public ValueTask DisposeAsync() => _stream.DisposeAsync();

   async Task<int> ReceiveInt32Async(CancellationToken cancellationToken) =>
      BinaryPrimitives.ReadInt32LittleEndian(await ReceiveBytesAsync(sizeof(int), cancellationToken).caf());

   async Task<Guid> ReceiveGuidAsync(CancellationToken cancellationToken) =>
      new(await ReceiveBytesAsync(BytesOfAGuid, cancellationToken).caf());

   async Task<string> ReceiveStringAsync(CancellationToken cancellationToken)
   {
      var byteLength = await ReceiveInt32Async(cancellationToken).caf();
      return Encoding.UTF8.GetString(await ReceiveBytesAsync(byteLength, cancellationToken).caf());
   }

   async Task<byte[]> ReceiveBytesAsync(int count, CancellationToken cancellationToken)
   {
      var bytes = new byte[count];
      await _stream.ReadExactlyAsync(bytes, cancellationToken).caf();
      return bytes;
   }

   ///<summary>Fields being put together for sending: they are collected in memory, in the order they are added, and<br/>
   /// <see cref="WriteToAsync"/> then writes them all to the connection in one write.</summary>
   sealed class FieldsToSend
   {
      readonly ArrayBufferWriter<byte> _fields = new();

      internal FieldsToSend Int32(int value)
      {
         BinaryPrimitives.WriteInt32LittleEndian(_fields.GetSpan(sizeof(int)), value);
         _fields.Advance(sizeof(int));
         return this;
      }

      internal FieldsToSend Guid(Guid value)
      {
         value.TryWriteBytes(_fields.GetSpan(BytesOfAGuid));
         _fields.Advance(BytesOfAGuid);
         return this;
      }

      internal FieldsToSend String(string value)
      {
         var byteLength = Encoding.UTF8.GetByteCount(value);
         Int32(byteLength);
         Encoding.UTF8.GetBytes(value, _fields.GetSpan(byteLength));
         _fields.Advance(byteLength);
         return this;
      }

      internal async Task WriteToAsync(Stream connection, CancellationToken cancellationToken)
      {
         await connection.WriteAsync(_fields.WrittenMemory, cancellationToken).caf();
         await connection.FlushAsync(cancellationToken).caf();
      }
   }
}
