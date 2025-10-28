using System.Collections.Generic;
using System.Linq;

namespace Compze.Tests.Infrastructure.Fluent;

public static class StringBe
{
   public static IAssertionBuilder<object>? Equal(this IAssertionBuilder<object> must, string expected)
      => must.Satisfy(it => Equals(it, expected), $"""
                                                   expected:
                                                   ====
                                                   {expected}
                                                   ====
                                                   but was:
                                                   ====
                                                   {must.Subject}
                                                   ====
                                                   """);
}
