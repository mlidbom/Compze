using System.Data.SqlTypes;
using System.Globalization;
using System.Numerics;
using Compze.Contracts;

namespace Compze.Tessaging.Teventive.TeventStore.Internal.SqlLayer.Abstractions;

public readonly struct ReadOrder : IComparable<ReadOrder>, IEquatable<ReadOrder>
{
   const int IntegerDigits = 20;  // Maximum digits in the integer part
   const int FractionDigits = 19; // Number of digits in the fractional part (always exactly 19)
   const int TotalDigits = 38;    // Total precision: integer + fractional digits (for decimal(38,19))

   readonly BigInteger _integerPart;
   readonly BigInteger _fractionPart;

   public long IntegerPart => (long)_integerPart;
   public long FractionPart => (long)_fractionPart;
   public override string ToString() => $"{_integerPart}.{_fractionPart:D19}";

   public static readonly ReadOrder Zero = new(0, 0);

   static long _temporaryPlaceholderCounter = 1; //It may be ridiculous, but should we ever have long.MaxValue events, starting this at 1 will avoid a collision.
   static readonly BigInteger TemporaryPlaceholderIntegerPart = long.MaxValue;

   // Each call returns a unique ReadOrder with max integer part and an incrementing fractional part.
   // These are ephemeral — they exist in the UNIQUE index only within an uncommitted transaction
   // and are immediately replaced by the real InsertionOrder value via the subsequent UPDATE.
   // Using unique values eliminates contention on the UNIQUE ReadOrder index during concurrent INSERTs.
   public static ReadOrder NextTemporaryPlaceholder() =>
      new(TemporaryPlaceholderIntegerPart, Interlocked.Increment(ref _temporaryPlaceholderCounter));

   public static ReadOrder FromParts(long integerPart, long fractionPart) => new(integerPart, fractionPart);

   static readonly BigInteger MaxOffset = BigInteger.Parse("1".PadRight(IntegerDigits, '0'), CultureInfo.InvariantCulture);

   ReadOrder(BigInteger integerPart, BigInteger fractionPart)
   {
      if(integerPart < 0) throw new ArgumentException($"{nameof(integerPart)} Must be >= 0");
      if(fractionPart < 0) throw new ArgumentException($"{nameof(fractionPart)} Must be >= 0");

      _integerPart = integerPart;
      _fractionPart = fractionPart;
   }

   public SqlDecimal ToSqlDecimal() => ToCorrectIntegerAndFractionDigits(SqlDecimal.Parse(ToString()));

   internal ReadOrder NextIntegerOrder => new(_integerPart + 1, 0);

   public static ReadOrder Parse(string value, bool bypassScaleTest = false)
   {
      var parts = value.Split(".");
      Argument.Assert(parts.Length == 2);
      var order = parts[0];
      var offset = parts[1];
      if(order[0] == '-') throw new ArgumentException("We do not use negative numbers");
      if(offset[0] == '-') throw new ArgumentException("We do not use negative numbers");

      if(!bypassScaleTest)
      {
         if(offset.Length != FractionDigits) throw new ArgumentException($"Got number with {offset.Length} fraction digits. It must be exactly {FractionDigits}", nameof(value));
      }

      return new ReadOrder(BigInteger.Parse(order, CultureInfo.InvariantCulture), BigInteger.Parse(offset.PadRight(FractionDigits, '0'), CultureInfo.InvariantCulture));
   }

   public static ReadOrder FromSqlDecimal(SqlDecimal value) => Parse(value.ToString());

   public static ReadOrder[] CreateOrdersForTeventsBetween(int numberOfTevents, ReadOrder rangeStart, ReadOrder rangeEnd)
   {
      if(rangeEnd._integerPart - rangeStart._integerPart > 1) throw new ArgumentException("We should only ever insert between two adjacent tevents.");

      BigInteger rangeSize;
      if(rangeEnd._integerPart > rangeStart._integerPart)
      {
         rangeSize = MaxOffset + rangeEnd._fractionPart - rangeStart._fractionPart; //We are allowed to overflow onto the next Order value
      } else
      {
         rangeSize = rangeEnd._fractionPart - rangeStart._fractionPart;
      }

      var increment = rangeSize / (numberOfTevents + 1);
      if(increment < 1)
         throw new ArgumentException("Range too small to fit tevents.");

      var offSetsFromStartRange = 1.Through(numberOfTevents).Select(index => rangeStart._fractionPart + index * increment).ToArray();
      var result = offSetsFromStartRange.Select(offset =>
      {
         if(offset < MaxOffset) //We are still between the range start and the next integer Order value
         {
            return new ReadOrder(rangeStart._integerPart, offset);
         } else //Offset has overflowed to the next Order value
         {
            var order = rangeStart._integerPart + 1;
            offset -= MaxOffset;
            return new ReadOrder(order, offset);
         }
      }).ToArray();

      return result._assert(it => it.All(order => order > rangeStart))       //We are staying within the specified range
                   ._assert(it => it.All(order => order < rangeEnd))         //We are staying within the specified range
                   ._assert(it => it.Distinct().Count() == numberOfTevents); //Each ReadOrder is unique
   }

   static SqlDecimal ToCorrectIntegerAndFractionDigits(SqlDecimal value) => SqlDecimal.ConvertToPrecScale(value, TotalDigits, FractionDigits);

   public bool Equals(ReadOrder other) => _integerPart.Equals(other._integerPart) && _fractionPart.Equals(other._fractionPart);
   public override bool Equals(object? obj) => obj is ReadOrder other && Equals(other);
   public override int GetHashCode() => HashCode.Combine(_integerPart, _fractionPart);
   public static bool operator ==(ReadOrder left, ReadOrder right) => left.Equals(right);
   public static bool operator !=(ReadOrder left, ReadOrder right) => !left.Equals(right);

   public int CompareTo(ReadOrder other)
   {
      var orderComparison = _integerPart.CompareTo(other._integerPart);
      return orderComparison != 0 ? orderComparison : _fractionPart.CompareTo(other._fractionPart);
   }

   public static bool operator <(ReadOrder left, ReadOrder right) => left.CompareTo(right) < 0;
   public static bool operator >(ReadOrder left, ReadOrder right) => left.CompareTo(right) > 0;
   public static bool operator <=(ReadOrder left, ReadOrder right) => left.CompareTo(right) <= 0;
   public static bool operator >=(ReadOrder left, ReadOrder right) => left.CompareTo(right) >= 0;
}
