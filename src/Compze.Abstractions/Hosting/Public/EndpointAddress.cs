using Compze.Contracts;

namespace Compze.Abstractions.Hosting.Public;

public record EndpointAddress
{
   public Uri Uri { get; }
   public EndpointAddress(Uri uri)
   {
      Argument.NotNullEmptyOrWhitespace(uri.AbsoluteUri);
      Uri = uri;
   }
}
