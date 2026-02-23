namespace Compze.Utilities.SystemCE;

static class CompzeEnvironment
{
   public const bool IsNCrunch =
#if NCRUNCH
        true;
#else
      false;
#endif
}
