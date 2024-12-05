using Compze.Contracts.Deprecated;

namespace Compze.Messaging.Buses.Implementation;

public record EndPointAddress
{
   internal string AspNetAddress { get; }
   internal EndPointAddress(string aspNetAddress)
   {
      Contract.ArgumentNotNullEmptyOrWhitespace(aspNetAddress, nameof(aspNetAddress));
      AspNetAddress = aspNetAddress;
   }
}