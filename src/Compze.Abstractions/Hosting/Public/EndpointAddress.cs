using Compze.Contracts;

namespace Compze.Abstractions.Hosting.Public;

///<summary>Where a transport listens, as a value: the URI another endpoint or client connects to. Which transport an address belongs to is the holding component's business — see the address extension properties on <see cref="IEndpoint"/>.</summary>
public record EndpointAddress
{
   public Uri Uri { get; }
   public EndpointAddress(Uri uri)
   {
      Argument.NotNullEmptyOrWhitespace(uri.AbsoluteUri);
      Uri = uri;
   }
}
