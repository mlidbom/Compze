using System;
using Compze.Contracts;

namespace Compze.Core.Tessaging.Transport.Internal;

public record EndPointAddress
{
   public Uri Uri { get; }
   public EndPointAddress(Uri uri)
   {
      ContractAssertion.Argument.NotNullEmptyOrWhitespace(uri.AbsoluteUri);
      Uri = uri;
   }
}
