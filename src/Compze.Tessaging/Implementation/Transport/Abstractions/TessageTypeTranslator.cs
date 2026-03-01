using System;
using Compze.Core.Tessaging.Public;
using Compze.Utilities.SystemCE.ReflectionCE;

namespace Compze.Tessaging.Implementation.Transport.Abstractions;

internal static class TessageTypeTranslator
{
   public static TransportTessageType TransportTessageType(this ITessage tessage) =>
      tessage.GetType().TransportTessageType();

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

   public static Type TessageType(this TransportTessageType transportTessageType)
   {
      switch(transportTessageType)
      {
         //Tessaging
         case Abstractions.TransportTessageType.ExactlyOnceTevent:
            return typeof(IExactlyOnceTevent);
         case Abstractions.TransportTessageType.ExactlyOnceTommand:
            return typeof(IExactlyOnceTommand);
         //Typermedia
         case Abstractions.TransportTessageType.TypermediaAtMostOnceTommand:
            return typeof(IAtMostOnceTypermediaTommand);
         case Abstractions.TransportTessageType.TypermediaAtMostOnceTommandWithReturnValue:
            return typeof(IAtMostOnceTommand<object>);
         case Abstractions.TransportTessageType.TyperMediaTuery:
            return typeof(IRemotableTuery<object>);
         case Abstractions.TransportTessageType.Invalid:
         default:
            throw new Exception($"The {nameof(TransportTessageType)} enum should never have numeric value {transportTessageType} this likely indicates a serialization error or some such");
      }
   }
}
