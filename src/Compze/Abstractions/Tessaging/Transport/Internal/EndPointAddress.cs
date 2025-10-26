using System;
using Compze.Utilities.Contracts;

namespace Compze.Core.Tessaging.Transport.Internal;

public record HttpEndPointAddress
{
   internal Uri Uri { get; }
   internal HttpEndPointAddress(Uri uri)
   {
      Assert.Argument.NotNullEmptyOrWhitespace(uri.AbsoluteUri);
      Uri = uri;
   }
}
