using Compze.Utilities.Testing.Must;
using Compze.Utilities.Testing.XUnit.BDD;

namespace Compze.Functional.Specifications.ObjectCE;

public class ToStringNotNull_method
{
   [XF] public void returns_string_representation_of_object() =>
      42.ToStringNotNull().Must().Be("42");

   [XF] public void returns_non_null_string_for_object_with_ToString() =>
      new object().ToStringNotNull().Must().NotBeNull();
}
