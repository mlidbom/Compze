using System.Reflection;
using Compze.Internals.SystemCE;
using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Tessaging.TessageTypes;
using Compze.Tessaging.Validation._internal;

using Compze.Tessaging.Validation._private;

namespace Compze.Tessaging.Validation;

///<summary>The design rules every tessage type must satisfy: the rules that refuse self-contradictory kind combinations — a type<br/>
/// that is both an <see cref="ITommand"/> and an <see cref="ITevent"/>, both <see cref="IRemotableTessage"/> and<br/>
/// <see cref="IStrictlyLocalTessage"/>, and their kin.</summary>
///<remarks>The framework asserts these rules itself, but only the first time a type is sent or subscribed to — which can be deep<br/>
/// into a run, and never happens at all for a declared type nothing instantiates yet. This surface exists so a test can assert a<br/>
/// whole tessage vocabulary up front: point <see cref="AssertFulfilledByAllTessageTypesIn"/> at the assemblies declaring your<br/>
/// tessage types and every violation is reported in one failure.</remarks>
public static class TessageTypeDesignRules
{
   ///<summary>Asserts that <paramref name="tessageType"/> satisfies every tessage-type design rule, throwing with the violated<br/>
   /// rule's explanation otherwise.</summary>
   public static void AssertFulfilledBy(Type tessageType) => TessageTypeInspector.AssertValid(tessageType);

   ///<summary>Sweeps every <see cref="ITessage"/> type declared in <paramref name="assemblies"/> — interfaces and classes alike,<br/>
   /// public and non-public, open generic definitions included — and asserts <see cref="AssertFulfilledBy"/> for each,<br/>
   /// reporting all violating types in one failure.</summary>
   public static void AssertFulfilledByAllTessageTypesIn(params IReadOnlyList<Assembly> assemblies)
   {
      var violations = assemblies.SelectMany(assembly => assembly.GetTypes())
                                 .Where(type => type.Is<ITessage>())
                                 .Select(TryFindViolationIn)
                                 .OfType<string>()
                                 .ToList();

      if(violations.Count != 0) throw new TessageTypeDesignViolationException(violations.Join($"{Environment.NewLine}{Environment.NewLine}"));
   }

   static string? TryFindViolationIn(Type tessageType)
   {
      try
      {
         AssertFulfilledBy(tessageType);
         return null;
      }
      catch(TessageTypeDesignViolationException designViolation) //Narrow catch for aggregation only: every violation in the swept assemblies is reported in one failure instead of one per run.
      {
         return designViolation.Violation;
      }
   }
}
