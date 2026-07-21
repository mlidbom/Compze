using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Tessaging.TessageTypes;

namespace Compze.Tessaging._private.Transport;

static class TessageTypeTranslator
{
   public static TransportTessageType TransportTessageType(this Type tessageType)
   {
      if(tessageType.Is<IPublisherTevent<IExactlyOnceTevent>>())
         return Transport.TransportTessageType.ExactlyOnceTevent;
      if(tessageType.Is<IPublisherTevent<IRemotableTevent>>()) //After the exactly-once check: an exactly-once wrapper matches this too, so order encodes "best-effort is remotable minus exactly-once".
         return Transport.TransportTessageType.BestEffortTevent;
      if(tessageType.Is<IExactlyOnceTommand>())
         return Transport.TransportTessageType.ExactlyOnceTommand;
      throw new ArgumentOutOfRangeException();
   }
}
