using Compze.Contracts;

namespace Compze.Messaging.Buses.Implementation;

public record EndPointAddress
{
   internal string AspNetAddress { get; }
   internal EndPointAddress(string aspNetAddress)
   {
      Assert.Argument.NotNullEmptyOrWhitespace(aspNetAddress);
      AspNetAddress = aspNetAddress;
   }
}