using Compze.Contracts;

namespace Compze.Abstractions.Hosting.Public;

///<summary>Where a transport listens, as a value: the URI another endpoint or client connects to. Which transport an address belongs to is the holding component's business — see the address extension properties on <see cref="IEndpoint"/>.</summary>
public class EndpointAddress : IEquatable<EndpointAddress>
{
   public Uri Uri { get; }
   public EndpointAddress(Uri uri)
   {
      Argument.NotNullEmptyOrWhitespace(uri.AbsoluteUri);
      Uri = uri;
   }

   //Value semantics: the routers' reconciliation compares desired and connected addresses by value (hash sets and Contains).
   public bool Equals(EndpointAddress? other) => other is not null && other.GetType() == GetType() && other.Uri == Uri;
   public override bool Equals(object? obj) => Equals(obj as EndpointAddress);
   public override int GetHashCode() => Uri.GetHashCode();
   public override string ToString() => Uri.ToString();

   public static bool operator ==(EndpointAddress? left, EndpointAddress? right) => Equals(left, right);
   public static bool operator !=(EndpointAddress? left, EndpointAddress? right) => !Equals(left, right);
}
