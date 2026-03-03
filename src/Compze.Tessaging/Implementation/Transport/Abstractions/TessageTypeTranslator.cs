using System;
using Compze.Core.Tessaging.Public;
using Compze.Utilities.SystemCE.ReflectionCE;

namespace Compze.Tessaging.Implementation.Transport.Abstractions;

static class TessageTypeTranslator
{
   public static TransportTessageType TransportTessageType(this Type tessageType)
   {
      //tessaging
      if(tessageType.Is<IExactlyOnceTevent>())
         return Abstractions.TransportTessageType.ExactlyOnceTevent;
      if(tessageType.Is<IExactlyOnceTommand>())
         return Abstractions.TransportTessageType.ExactlyOnceTommand;
      //typermedia
      if(tessageType.Is<IRemotableTuery<object>>())
         return Abstractions.TransportTessageType.TyperMediaTuery;
      if(tessageType.Is<IAtMostOnceTommand<object>>())
         return Abstractions.TransportTessageType.TypermediaAtMostOnceTommandWithReturnValue;
      if(tessageType.Is<IAtMostOnceTypermediaTommand>())
         return Abstractions.TransportTessageType.TypermediaAtMostOnceTommand;
      else
         throw new ArgumentOutOfRangeException();
   }
}
