using System.Globalization;
using System.Text;
using PostReleaseBindingCheck.Packages;

namespace PostReleaseBindingCheck.Failures;

/// <summary>
/// The text the post-release binding check prints when the <see cref="PostReleasePackageSet"/> does not bind:<br/>
/// its <see cref="BindingFailure"/>s grouped by the pair of packages each is between.
/// </summary>
sealed class BindingFailureReport
{
   readonly IReadOnlyList<BindingFailure> _failures;

   public BindingFailureReport(IReadOnlyList<BindingFailure> failures) => _failures = failures;

   public override string ToString()
   {
      var report = new StringBuilder();
      report.AppendLine("Post-release binding check: once this release is out, these Compze packages would fail at runtime against each other.");
      foreach(var packagePair in _failures.GroupBy(failure => (failure.ReferencingPackage, failure.ReferencedPackage)))
      {
         report.AppendLine();
         report.AppendLine(CultureInfo.InvariantCulture, $"{packagePair.Key.ReferencingPackage} against {packagePair.Key.ReferencedPackage}:");
         foreach(var failure in packagePair) report.AppendLine(CultureInfo.InvariantCulture, $"  {failure.Kind}: {failure.Explanation}");
      }

      return report.ToString();
   }
}
