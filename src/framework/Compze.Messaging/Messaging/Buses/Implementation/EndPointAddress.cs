using Compze.Contracts;
using Compze.DDD;

namespace Compze.Messaging.Buses.Implementation;

public record EndPointAddress
{
   public string AspNetAddress { get; }
   internal EndPointAddress(string aspNetAddress)
   {
      Contract.ArgumentNotNullEmptyOrWhitespace(aspNetAddress, nameof(aspNetAddress));
      AspNetAddress = aspNetAddress;
   }
}