using System;
using Compze.Core.Public.Infrastructure;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.Fluent;
using Compze.Utilities.Testing.XUnit.BDD;
using static Compze.Tests.Infrastructure.Fluent.MustActions;

// ReSharper disable InconsistentNaming

namespace Compze.Tests.Unit.Testing.Fluent;

public class When_using_NotBeSameAs : UniversalTestBase
{
   public class given_two_different_objects_with_the_same_value : When_using_NotBeSameAs
   {
      [XF] public void NotBeSameAs_does_not_throw() => new ValueWrapper<int>(42)
                                                      .Must().NotBeSameAs(new ValueWrapper<int>(42));
   }

   public class given_two_references_to_the_same_object : When_using_NotBeSameAs
   {
      readonly ValueWrapper<int> _actual = new(12);

      [XF] public void NotBeSameAs_throws_AssertionFailedException() => Invoking(() => _actual.Must().NotBeSameAs(_actual))
                                                                       .Must()
                                                                       .Throw<AssertionFailedException>();
   }
}
