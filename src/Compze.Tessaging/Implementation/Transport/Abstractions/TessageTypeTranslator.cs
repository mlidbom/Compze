using Compze.Abstractions.Tessaging.Public;
using Compze.Internals.SystemCE.ReflectionCE;

namespace Compze.Tessaging.Implementation.Transport.Abstractions;

static class TessageTypeTranslator
{
   public static TransportTessageType TransportTessageType(this Type tessageType)
   {
      if(tessageType.Is<IPublisherTevent<IExactlyOnceTevent>>())
         return Abstractions.TransportTessageType.ExactlyOnceTevent;
      if(tessageType.Is<IPublisherTevent<IRemotableTevent>>()) //After the exactly-once check: an exactly-once wrapper matches this too, so order encodes "best-effort is remotable minus exactly-once".
         return Abstractions.TransportTessageType.BestEffortTevent;
      if(tessageType.Is<IExactlyOnceTommand>())
         return Abstractions.TransportTessageType.ExactlyOnceTommand;
      throw new ArgumentOutOfRangeException();
   }
}
