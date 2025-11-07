namespace Compze.Utilities.Misc;

static class CompzeEnvironment
{
   internal const bool IsNCrunch =
#if NCRUNCH
        true;
#else
      false;
#endif
}
