using Compze.Utilities.Contracts;

namespace Compze.Core.Tessaging.Transport.Internal;

public record HttpEndPointAddress
{
   internal string AspNetAddress { get; }
   internal HttpEndPointAddress(string aspNetAddress)
   {
      Assert.Argument.NotNullEmptyOrWhitespace(aspNetAddress);
      AspNetAddress = aspNetAddress;
   }
}
