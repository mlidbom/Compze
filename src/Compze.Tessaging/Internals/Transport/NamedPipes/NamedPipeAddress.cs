using Compze.Abstractions.Hosting.Public;
using Compze.Contracts;

namespace Compze.Tessaging.Internals.Transport.NamedPipes;

///<summary>The named-pipe transport's <see cref="EndpointAddress"/> scheme: <c>compze.pipe://localhost/&lt;pipe-name&gt;</c>.<br/>
/// A pipe name is generated fresh per server start — the same-machine analog of the HTTP transport's dynamically allocated ports.</summary>
public static class NamedPipeAddress
{
   ///<summary>The URI scheme identifying an address served by the named-pipe transport.</summary>
   public const string Scheme = "compze.pipe";

   ///<summary>A pipe name no other server is using: the named-pipe analog of asking the OS for a free port.</summary>
   public static string NewUniquePipeName() => $"Compze.{Guid.NewGuid():N}";

   ///<summary>The <see cref="EndpointAddress"/> a server listening on <paramref name="pipeName"/> publishes for clients to connect to.</summary>
   public static EndpointAddress CreateLocalAddressForPipe(string pipeName) => new(new Uri($"{Scheme}://localhost/{pipeName}"));

   ///<summary>The pipe name to connect to for <paramref name="address"/>. Asserts that the address belongs to this transport.</summary>
   public static string PipeNameFrom(EndpointAddress address)
   {
      Argument.Assert(address.Uri.Scheme == Scheme);
      return address.Uri.AbsolutePath.Trim('/');
   }
}
