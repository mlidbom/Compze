namespace Compze.Internals.Logging;

/// <summary>Describes the meaning and importance of a log event.</summary>
/// <remarks>The numeric values ascend with severity — <see cref="Trace"/> is the lowest and <see cref="Critical"/> the
/// highest — so a configured minimum level enables everything at least as severe as it. This matches the ordering of
/// <c>Microsoft.Extensions.Logging.LogLevel</c>, the vocabulary most .NET developers already carry.</remarks>
public enum LogLevel
{
   /// <summary>Something that happens often enough that tracking every occurence would make the logs unmanageably large or hard to read, or logging too slow to use in practice in production.</summary>
   Trace = 0,

   /// <summary>Nothing a user could observe happened, but tracking these helps us understand the behavior of the code.</summary>
   Debug = 1,

   /// <summary>Something of significant interest happened. Something you normally want and need to know about.</summary>
   Info = 2,

   /// <summary>Something is likely to be broken, functionality or availability is at risk or degraded.</summary>
   Warning = 3,

   /// <summary>Users are facing errors, invariants are broken, or data is lost.</summary>
   Error = 4,

   /// <summary>You may get an urgent phone call when one of these occurs.</summary>
   Critical = 5
}
