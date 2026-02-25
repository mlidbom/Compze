using System;
using System.Globalization;
using Compze.Contracts;
using Compze.Functional;

namespace Compze.Utilities.SystemCE;

public static class DateTimeCE
{
   //Todo: Review time zone management in all sql layers, in all code using the sql layers, and in Serialization.
   // We must have a well thought out approach for ensuring that all of this behaves sanely and consistently.
   // Start by writing tests exploring how the sql layers and the serializer deals with DateTime instances with all three different kinds when serialized/persisted with one timezone and then read/deserialized in another.
   // How are they converted? Do you get the same Kind back? Does the value change?
   //todo: Do we also need ToLocalTimeSafely?
   ///<summary>Like <see cref="DateTime.ToUniversalTime"/> except it will throw an exception if <see cref="@this"/>.Kind == <see cref="DateTimeKind.Unspecified"/> instead of assuming that Kind == <see cref="DateTimeKind.Local"/> and converting based on that assumption like <see cref="DateTime.ToUniversalTime"/> does.</summary>
   public static DateTime ToUniversalTimeSafely(this DateTime @this) => @this.AssertHasKind().ToUniversalTime();

   ///<summary>Parses a DateTime string using invariant culture to avoid locale-dependent behavior</summary>
   public static DateTime ParseInvariant(string dateTimeString) => DateTime.Parse(dateTimeString, CultureInfo.InvariantCulture);

   ///<summary>Ensures that a DateTime instance has a Kind specified so that it can be accurately stored, restored, and passed between systems with different time zones without losing information</summary>
   static DateTime AssertHasKind(this DateTime @this) =>
      Contract.Argument.Assert(@this.Kind != DateTimeKind.Unspecified,
                         () => """
                               This DateTime instance does not have a Kind specified. 
                               This means that it is impossible to accurately persist and restore, or serialize between systems, because it is impossible to know if it refers to the current TimeZone or to UTC timezone. 
                               Please make sure that all DateTime instances passed to methods which will result in them being persisted or serialized contains a Kind
                               """)._then(@this);

   public static TimeSpan TimeElapsedSince(DateTime pointInThePast) => DateTime.UtcNow - pointInThePast;

   const long TicksPerMicrosecond = TimeSpan.TicksPerMillisecond / 1000; // 10

   ///<summary>Truncates a DateTime to microsecond precision (6 fractional digits) by removing sub-microsecond ticks. This is the highest precision universally supported across all target databases (MySQL and PostgreSQL cap at microseconds).</summary>
   public static DateTime TruncateToMicroseconds(this DateTime @this) => new(@this.Ticks - @this.Ticks % TicksPerMicrosecond, @this.Kind);

}
