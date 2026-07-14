using System.IO.Pipes;
using Compze.Abstractions.Hosting.Public;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Internals.Transport.NamedPipes;

///<summary>The client side of the named-pipe transport: sends one <see cref="TransportRequest"/> to the<br/>
/// <see cref="NamedPipeTransportServer"/> at an <see cref="EndpointAddress"/> and returns the response payload —<br/>
/// the same-machine, no-web-stack counterpart of posting to an HTTP transport route.</summary>
///<remarks>Each send opens its own connection — cheap for a local pipe, and with the callers' sequential send disciplines it<br/>
/// preserves send order the same way one-request-at-a-time HTTP posting does.</remarks>
public static class NamedPipeTransportClient
{
   static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(10);

   ///<summary>Sends <paramref name="request"/> to the server at <paramref name="address"/> and returns the response payload.<br/>
   /// A handler exception on the server side surfaces here as a <see cref="MessageDispatchingFailedException"/> carrying the server-side exception's type and detail.</summary>
   public static async Task<string> SendAsync(TransportRequest request, EndpointAddress address, CancellationToken cancellationToken = default)
   {
      var pipeName = NamedPipeAddress.PipeNameFrom(address);
#pragma warning disable CA2000 //It IS disposed: the await using on the next line owns it; the analyzer just cannot see through the caf() wrapper.
      var pipe = new NamedPipeClientStream(serverName: ".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
#pragma warning restore CA2000
      await using var _ = pipe.caf();

      try
      {
         await pipe.ConnectAsync((int)ConnectTimeout.TotalMilliseconds, cancellationToken).caf();
      }
      catch(TimeoutException exception)
      {
         throw new MessageDispatchingFailedException($"""
                                                      Timed out connecting to the named-pipe transport server.
                                                      Address:    {address.Uri}
                                                      Kind:       {request.Kind}
                                                      Type:       {request.PayloadTypeIdString}
                                                      {exception}
                                                      """);
      }

      await NamedPipeFraming.WriteRequestAsync(pipe, request, cancellationToken).caf();
      var response = await NamedPipeFraming.ReadResponseAsync(pipe, cancellationToken).caf();

      if(!response.Succeeded)
      {
         throw new MessageDispatchingFailedException($"""
                                                      Address:    {address.Uri}
                                                      Kind:       {request.Kind}
                                                      Type:       {request.PayloadTypeIdString}
                                                      Body:
                                                      {request.Body}

                                                      Exception Type: {response.ExceptionType}
                                                      Exception Tessage: {response.ExceptionDetail}
                                                      """);
      }

      return response.Payload;
   }
}
