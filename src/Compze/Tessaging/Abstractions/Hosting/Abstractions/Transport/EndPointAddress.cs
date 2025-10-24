using Compze.Utilities.Contracts;

namespace Compze.Tessaging.Hosting.Abstractions.Transport;

public record HttpEndPointAddress
{
   internal string AspNetAddress { get; }
   internal HttpEndPointAddress(string aspNetAddress)
   {
      Assert.Argument.NotNullEmptyOrWhitespace(aspNetAddress);
      AspNetAddress = aspNetAddress;
   }
}
