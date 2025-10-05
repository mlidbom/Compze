using Compze.Utilities.Contracts;

namespace Compze.Hosting.Abstractions;

public record EndPointAddress
{
   internal string AspNetAddress { get; }
   internal EndPointAddress(string aspNetAddress)
   {
      Assert.Argument.NotNullEmptyOrWhitespace(aspNetAddress);
      AspNetAddress = aspNetAddress;
   }
}
