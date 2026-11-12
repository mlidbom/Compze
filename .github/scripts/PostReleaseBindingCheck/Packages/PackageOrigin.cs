namespace PostReleaseBindingCheck.Packages;

/// <summary>Where a <see cref="Package"/> of the <see cref="PostReleasePackageSet"/> comes from.</summary>
public enum PackageOrigin
{
   /// <summary>Packed from this commit: the release publishes it.</summary>
   ThisRelease,

   /// <summary>Downloaded from nuget.org: the release leaves it as it is, so consumers keep getting this build.</summary>
   NuGetOrg
}
