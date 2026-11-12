using PostReleaseBindingCheck.Failures;
using PostReleaseBindingCheck.Packages;

namespace PostReleaseBindingCheck;

/// <summary>
/// The command-line entry point of the post-release binding check (see README.md): loads the
/// <see cref="PostReleasePackageSet"/> from the nupkg files named by the <see cref="CommandLineArguments"/>,<br/>
/// prints the <see cref="BindingFailureReport"/> when it finds any <see cref="BindingFailure"/>, and exits with 1 then.<br/>
/// Silent, with exit code 0, when the set binds.
/// </summary>
static class Program
{
   static int Main(string[] arguments)
   {
      var commandLine = CommandLineArguments.Parse(arguments);
      using var packageSet = PostReleasePackageSet.Load(commandLine.ThisReleaseNupkgs, commandLine.NuGetOrgNupkgs);
      var failures = packageSet.FindBindingFailures();
      if(failures.Count == 0) return 0;

      Console.WriteLine(new BindingFailureReport(failures));
      return 1;
   }
}
