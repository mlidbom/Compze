namespace Compze.Tessaging.Implementation.Transport.Abstractions;

enum TransportTessageType
{
   //0 is intentionally NOT used, it marks the possibility of serialization errors etc.
   Invalid = 0,
   ExactlyOnceTevent = 1,
   ExactlyOnceTommand = 2,
   TypermediaAtMostOnceTommand = 3,
   TypermediaAtMostOnceTommandWithReturnValue = 4,
   TyperMediaTuery = 5
}