using PostReleaseBindingCheck.Packages;

namespace PostReleaseBindingCheck;

/// <summary>
/// The nupkg files the post-release binding check reads, as its command line names them:<br/>
/// <c>--this-release &lt;nupkg&gt;...</c> for the packages the release publishes, and<br/>
/// <c>--on-nuget-org &lt;nupkg&gt;...</c> for nuget.org's newest version of each package it does not publish.
/// </summary>
sealed class CommandLineArguments
{
   const string ThisReleaseOption = "--this-release";
   const string NuGetOrgOption = "--on-nuget-org";
   const string Usage = $"Usage: PostReleaseBindingCheck {ThisReleaseOption} <nupkg>... {NuGetOrgOption} <nupkg>...";

   CommandLineArguments(IReadOnlyList<string> thisReleaseNupkgs, IReadOnlyList<string> nuGetOrgNupkgs)
   {
      ThisReleaseNupkgs = thisReleaseNupkgs;
      NuGetOrgNupkgs = nuGetOrgNupkgs;
   }

   /// <summary>The nupkg files of the packages the release publishes: <see cref="PackageOrigin.ThisRelease"/>.</summary>
   public IReadOnlyList<string> ThisReleaseNupkgs { get; }

   /// <summary>The nupkg files of nuget.org's newest version of every other package: <see cref="PackageOrigin.NuGetOrg"/>.</summary>
   public IReadOnlyList<string> NuGetOrgNupkgs { get; }

   public static CommandLineArguments Parse(IReadOnlyList<string> arguments)
   {
      var thisReleaseNupkgs = new List<string>();
      var nuGetOrgNupkgs = new List<string>();
      List<string>? nupkgsOfCurrentOption = null;
      foreach(var argument in arguments)
      {
         switch(argument)
         {
            case ThisReleaseOption: nupkgsOfCurrentOption = thisReleaseNupkgs; break;
            case NuGetOrgOption: nupkgsOfCurrentOption = nuGetOrgNupkgs; break;
            default:
               Argument.Assert(nupkgsOfCurrentOption != null && !IsOption(argument), () => $"Unexpected argument '{argument}'. {Usage}");
               nupkgsOfCurrentOption.Add(argument);
               break;
         }
      }

      Argument.Assert(thisReleaseNupkgs.Count > 0, () => $"No package of this release given. {Usage}");
      return new CommandLineArguments(thisReleaseNupkgs, nuGetOrgNupkgs);

      static bool IsOption(string argument) => argument.StartsWith("--", StringComparison.Ordinal);
   }
}
