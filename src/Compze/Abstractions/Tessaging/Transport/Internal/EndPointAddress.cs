using System;
using Compze.Utilities.Contracts;

namespace Compze.Core.Tessaging.Transport.Internal;

public record EndPointAddress
{
   internal Uri Uri { get; }
   internal EndPointAddress(Uri uri)
   {
      Assert.Argument.NotNullEmptyOrWhitespace(uri.AbsoluteUri);
      Uri = uri;
   }
}
