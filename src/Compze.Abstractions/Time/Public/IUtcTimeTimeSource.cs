namespace Compze.Abstractions.Time.Public;

///<summary>
/// Provides the service of telling what the current UTC time is.
/// In order to make things testable calling DateTime.UtcNow directly is discouraged.
/// </summary>
public interface IUtcTimeTimeSource
{
   ///<summary>Returns the current time as UTC time.</summary>
   DateTime UtcNow { get; }
}