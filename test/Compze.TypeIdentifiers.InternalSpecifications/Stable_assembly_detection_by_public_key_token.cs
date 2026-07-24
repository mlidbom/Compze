using Compze.Must;

using Compze.xUnitBDD;
using static Compze.TypeIdentifiers.InternalSpecifications.TypeNameMapperFixture;
using Compze.TypeIdentifiers._private;

// ReSharper disable InconsistentNaming
#pragma warning disable CA1052

namespace Compze.TypeIdentifiers.InternalSpecifications;

/// <summary>
/// Stability is decided from the live type's assembly public key token at lookup time — never from a snapshot of
/// the assemblies that happened to be loaded when the map was built. So an assembly is recognised as
/// stable no matter when it loads, and adding a signing token to the trusted set (e.g. the framework's
/// <c>adb9793829ddae60</c>, which signs Microsoft.Extensions.* / Microsoft.AspNetCore.*) is enough to make every
/// type it contains rename-safe.
/// </summary>
public class Stable_assembly_detection_by_public_key_token
{
   // System.Private.CoreLib is signed with this token.
   const string CoreLibToken = "7cec85d7bea7798e";

   public class A_bare_mapper_with_nothing_declared : Stable_assembly_detection_by_public_key_token
   {
      readonly TypeNameMapper _mapper = MapperWith();

      [XF] public void does_not_consider_a_signed_framework_type_stable()
         => _mapper.IsStableType(typeof(string)).Must().BeFalse();
   }

   public class After_trusting_the_assemblys_public_key_token : Stable_assembly_detection_by_public_key_token
   {
      readonly TypeNameMapper _mapper = MapperWith(stablePublicKeyTokens: [CoreLibToken]);

      [XF] public void every_type_signed_with_that_token_is_stable()
         => _mapper.IsStableType(typeof(string)).Must().BeTrue();
   }

   public class After_declaring_the_assembly_stable_by_name : Stable_assembly_detection_by_public_key_token
   {
      readonly TypeNameMapper _mapper = MapperWith(stableAssemblyNames: [typeof(string).Assembly.GetName().Name!]);

      [XF] public void a_type_from_that_assembly_is_stable()
         => _mapper.IsStableType(typeof(string)).Must().BeTrue();
   }
}
