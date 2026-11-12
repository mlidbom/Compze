using PostReleaseBindingCheck.Packages;

namespace PostReleaseBindingCheck.Failures;

/// <summary>
/// A reason one package of the <see cref="PostReleasePackageSet"/> would fail at runtime against another package of it,<br/>
/// such as a method it calls that the other package no longer has.
/// </summary>
public sealed class BindingFailure
{
   internal BindingFailure(Package referencingPackage, Package referencedPackage, BindingFailureKind kind, string explanation)
   {
      ReferencingPackage = referencingPackage;
      ReferencedPackage = referencedPackage;
      Kind = kind;
      Explanation = explanation;
   }

   /// <summary>The package that would fail: the one whose code uses the <see cref="ReferencedPackage"/>.</summary>
   public Package ReferencingPackage { get; }

   /// <summary>The package whose types or members the <see cref="ReferencingPackage"/> uses.</summary>
   public Package ReferencedPackage { get; }

   public BindingFailureKind Kind { get; }

   /// <summary>What fails, naming the type or member: <c>System.Void Compze.Logging.CompzeLogger::set_LoggerFactoryMethod(...) is missing</c>.</summary>
   public string Explanation { get; }

   public override string ToString() => $"{ReferencingPackage} against {ReferencedPackage}: {Explanation}";
}
