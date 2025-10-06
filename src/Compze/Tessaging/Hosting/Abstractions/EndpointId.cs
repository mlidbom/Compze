using System;
using Compze.Utilities.Contracts;
using Newtonsoft.Json;

namespace Compze.Tessaging.Hosting.Abstractions;

public record EndpointId
{
   // ReSharper disable once MemberCanBeInternal : It cannot serialization will fail.
   public Guid GuidValue { get; }
   [JsonConstructor]public EndpointId(Guid guidValue)
   {
      Assert.Argument.Is(guidValue != Guid.Empty);
      GuidValue = guidValue;
   }
}
