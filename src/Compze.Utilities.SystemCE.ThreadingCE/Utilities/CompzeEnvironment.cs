namespace Compze.Utilities.SystemCE.ThreadingCE.Utilities;

static class CompzeEnvironment
{
   public const bool IsNCrunch =
#if NCRUNCH
        true;
#else
      false;
#endif
}
