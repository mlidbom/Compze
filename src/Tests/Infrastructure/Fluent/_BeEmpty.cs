using System.Collections.Generic;
using System.Linq;

namespace Compze.Tests.Infrastructure.Fluent;

public static class _BeEmpty
{
   public static IMust<TCollection> BeEmpty<TCollection>(this IMust<TCollection> must,
                                                                            string message = "Expected an empty collection")
      where TCollection : IEnumerable<object>
      => must.Satisfy(it => !it.Any(), () => message);
}
