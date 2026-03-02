using System;

namespace Compze.Utilities.SystemCE;

public static class CompzeEnvironment
{
   public static readonly bool IsGithubAction = Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true";
}
