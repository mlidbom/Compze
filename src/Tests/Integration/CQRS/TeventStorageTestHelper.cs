using System.Collections.Generic;
using System.Linq;
using Compze.Core.Tessaging.Teventive.Public;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using Compze.Utilities.SystemCE.LinqCE;

namespace Compze.Tests.Integration.CQRS;

static class TeventStorageTestHelper
{
   //Not all storage providers stores with more than 6 decimal points precision
   internal static void StripSteventhDecimalPointFromSecondFractionOnUtcUpdateTime(IReadOnlyList<ITaggregateTevent> tevents)
#pragma warning disable CS0618 // Type or member is obsolete
       => tevents.Cast<IMutableTaggregateTevent>().ForEach(tevent => tevent.SetUtcTimeStampInternal(tevent.UtcTimeStamp.AddTicks(-(tevent.UtcTimeStamp.Ticks % 10))));
#pragma warning restore CS0618 // Type or member is obsolete
}
