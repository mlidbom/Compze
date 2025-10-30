using System;

namespace Compze.Core.Time.Public;

///<summary>Simply returns DateTime.Now or DateTime.UtcNow</summary>
public class DateTimeNowTimeSource : IUtcTimeTimeSource
{
   ///<summary>Returns an instance.</summary>
   public static readonly DateTimeNowTimeSource Instance = new();

   ///<summary>Returns DateTime.UtcNow</summary>
   public DateTime UtcNow => DateTime.UtcNow;
}
