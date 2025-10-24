using System.Collections.Generic;
using System.Linq;
using Compze.Abstractions.Tessaging.Teventive.Eventstore.Public;
using Compze.Utilities.SystemCE.LinqCE;

namespace Compze.Tests.Integration.CQRS;

static class EventStorageTestHelper
{
   //Not all storage providers stores with more than 6 decimal points precision
   internal static void StripSeventhDecimalPointFromSecondFractionOnUtcUpdateTime(IReadOnlyList<IAggregateEvent> events)
#pragma warning disable CS0618 // Type or member is obsolete
       => events.Cast<IMutableAggregateEvent>().ForEach(@event => @event.SetUtcTimeStampInternal(@event.UtcTimeStamp.AddTicks(-(@event.UtcTimeStamp.Ticks % 10))));
#pragma warning restore CS0618 // Type or member is obsolete
}