using System;
using Compze.Utilities.Contracts;

namespace Compze.Core.Tessaging.Hosting.Public;

public record EndpointId
{
   // ReSharper disable once MemberCanBeInternal : It cannot serialization will fail.
   public Guid GuidValue { get; }
   public EndpointId(Guid guidValue)
   {
      Assert.Argument.Is(guidValue != Guid.Empty);
      GuidValue = guidValue;
   }

   public override string ToString() => GuidValue.ToString();
}
