namespace Compze.Threading._private.Utilities;

static class CompzeEnvironment
{
   public const bool IsNCrunch =
#if NCRUNCH
        true;
#else
      false;
#endif
}
