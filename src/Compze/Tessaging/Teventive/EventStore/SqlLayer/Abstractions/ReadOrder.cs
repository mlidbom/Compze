using System;
using System.Data.SqlTypes;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Compze.Utilities.Contracts;
using Compze.Utilities.SystemCE.LinqCE;

namespace Compze.Tessaging.Teventive.EventStore.SqlLayer.Abstractions;

public readonly struct ReadOrder : IComparable<ReadOrder>, IEquatable<ReadOrder>
{
   const int IntegerDigits = 20;     // Maximum digits in the integer part
   const int FractionDigits = 19;    // Number of digits in the fractional part (always exactly 19)
   const int TotalDigits = 38;       // Total precision: integer + fractional digits (for decimal(38,19))
   const int ZeroFractionalPart = 0; // Zero fractional part for sequential events (e.g., 1.0, 2.0, 3.0)

   readonly BigInteger _order;
   readonly BigInteger _offSet;

   public override string ToString() => $"{FormatIntegerPart(_order)}.{FormatFractionalPart(_offSet)}";

   static string FormatIntegerPart(BigInteger integerPart) 
      => integerPart.ToString(CultureInfo.InvariantCulture).PadLeft(IntegerDigits, '0');

   static string FormatFractionalPart(BigInteger fractionalPart) 
      => fractionalPart.ToString($"D{FractionDigits}", CultureInfo.InvariantCulture);

   public static string CreateSqliteIntegerToReadOrderExpression(string integerColumnName)
      => $"printf('%0{IntegerDigits}d.%0{FractionDigits}d', {integerColumnName}, {ZeroFractionalPart})";

   public static readonly ReadOrder Zero = new(0, 0);

   public static ReadOrder FromLong(long readOrder) => new(readOrder, 0);

   static readonly BigInteger MaxOffset = BigInteger.Parse("1".PadRight(IntegerDigits, '0'), CultureInfo.InvariantCulture);

   ReadOrder(BigInteger order, BigInteger offSet)
   {
      if(order < 0) throw new ArgumentException($"{nameof(order)} Must be >= 0");
      if(offSet < 0) throw new ArgumentException($"{nameof(offSet)} Must be >= 0");

      _order = order;
      _offSet = offSet;
   }

   public SqlDecimal ToSqlDecimal() => ToCorrectIntegerAndFractionDigits(SqlDecimal.Parse(ToString()));

   public ReadOrder NextIntegerOrder => new(_order + 1, 0);

   public static ReadOrder Parse(string value, bool bypassScaleTest = false)
   {
      var parts = value.Split(".");
      Assert.Argument.Is(parts.Length == 2);
      var order = parts[0];
      var offset = parts[1];
      if(order[0] == '-') throw new ArgumentException("We do not use negative numbers");
      if(offset[0] == '-') throw new ArgumentException("We do not use negative numbers");

      if(!bypassScaleTest)
      {
         if(offset.Length != FractionDigits) throw new ArgumentException($"Got number with {offset.Length} decimal numbers. It must be exactly {FractionDigits}", nameof(value));
      }

      return new ReadOrder(BigInteger.Parse(order, CultureInfo.InvariantCulture), BigInteger.Parse(offset.PadRight(FractionDigits, '0'), CultureInfo.InvariantCulture));
   }

   public static ReadOrder FromSqlDecimal(SqlDecimal value) => Parse(value.ToString());

   public static ReadOrder[] CreateOrdersForEventsBetween(int numberOfEvents, ReadOrder rangeStart, ReadOrder rangeEnd)
   {
      if(rangeEnd._order - rangeStart._order > 1)  throw new ArgumentException("We should only ever insert between two adjacent events.");

      BigInteger rangeSize;
      if(rangeEnd._order > rangeStart._order)
      {
         rangeSize = MaxOffset + rangeEnd._offSet - rangeStart._offSet; //We are allowed to overflow onto the next Order value
      } else
      {
         rangeSize = rangeEnd._offSet - rangeStart._offSet;
      }

      var increment = rangeSize / (numberOfEvents + 1);
      if(increment < 1)
         throw new ArgumentException("Range too small to fit events.");

      var offSetsFromStartRange = 1.Through(numberOfEvents).Select(index => rangeStart._offSet + index * increment).ToArray();
      var result = offSetsFromStartRange.Select(offset =>
      {
         if(offset < MaxOffset) //We are still between the range start and the next integer Order value
         {
            return new ReadOrder(rangeStart._order, offset);
         } else //Offset has overflowed to the next Order value
         {
            var order = rangeStart._order + 1;
            offset -= MaxOffset;
            return new ReadOrder(order, offset);
         }
      }).ToArray();

      Assert.Result.Is(result.All(order => order > rangeStart)) //We are staying within the specified range
            .Is(result.All(order => order < rangeEnd))          //We are staying within the specified range
            .Is(result.Distinct().Count() == numberOfEvents);   //Each ReadOrder is unique

      return result;
   }

   static SqlDecimal ToCorrectIntegerAndFractionDigits(SqlDecimal value) => SqlDecimal.ConvertToPrecScale(value, TotalDigits, FractionDigits);

   public bool Equals(ReadOrder other) => _order.Equals(other._order) && _offSet.Equals(other._offSet);
   public override bool Equals(object? obj) => obj is ReadOrder other && Equals(other);
   public override int GetHashCode() => HashCode.Combine(_order, _offSet);
   public static bool operator ==(ReadOrder left, ReadOrder right) => left.Equals(right);
   public static bool operator !=(ReadOrder left, ReadOrder right) => !left.Equals(right);

   public int CompareTo(ReadOrder other)
   {
      var orderComparison = _order.CompareTo(other._order);
      return orderComparison != 0 ? orderComparison : _offSet.CompareTo(other._offSet);
   }

   public static bool operator <(ReadOrder left, ReadOrder right) => left.CompareTo(right) < 0;
   public static bool operator >(ReadOrder left, ReadOrder right) => left.CompareTo(right) > 0;
   public static bool operator <=(ReadOrder left, ReadOrder right) => left.CompareTo(right) <= 0;
   public static bool operator >=(ReadOrder left, ReadOrder right) => left.CompareTo(right) >= 0;
}