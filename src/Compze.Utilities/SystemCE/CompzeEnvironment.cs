namespace Compze.Utilities.SystemCE;

public static class CompzeEnvironment
{
   public const bool IsNCrunch =
#if NCRUNCH
        true;
#else
      false;
#endif
}
