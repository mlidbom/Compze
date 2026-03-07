using Compze.Abstractions.Tessaging.Public;
using Compze.Internals.SystemCE.ReflectionCE;

namespace Compze.Tessaging.Implementation.Transport.Abstractions;

static class TessageTypeTranslator
{
   public static TransportTessageType TransportTessageType(this Type tessageType)
   {
      if(tessageType.Is<IExactlyOnceTevent>())
         return Abstractions.TransportTessageType.ExactlyOnceTevent;
      if(tessageType.Is<IExactlyOnceTommand>())
         return Abstractions.TransportTessageType.ExactlyOnceTommand;
      throw new ArgumentOutOfRangeException();
   }
}
