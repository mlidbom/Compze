namespace Compze.Utilities.Misc;

public static class CompzeEnvironment
{
   public const bool IsNCrunch =
#if NCRUNCH
        true;
#else
      false;
#endif
}
