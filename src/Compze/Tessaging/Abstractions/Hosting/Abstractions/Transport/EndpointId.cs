using System;
using Compze.Utilities.Contracts;

namespace Compze.Tessaging.Hosting.Abstractions.Transport;

public record EndpointId
{
   // ReSharper disable once MemberCanBeInternal : It cannot serialization will fail.
   public Guid GuidValue { get; }
   public EndpointId(Guid guidValue)
   {
      Assert.Argument.Is(guidValue != Guid.Empty);
      GuidValue = guidValue;
   }
}
