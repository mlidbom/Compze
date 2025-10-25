using System;
using Compze.Tests.Infrastructure;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Utilities.Testing.XUnit.BDD;
using FluentAssertions;
using ReadOrder = Compze.Sql.Common.TeventStore.Abstractions.ReadOrder;
using static FluentAssertions.FluentActions;

namespace Compze.Tests.Unit.Internals.Sql.TeventStore;

 public class ReadOrderTests : UniversalTestBase
{
   [XF] public void Parse_followed_by_ToString_always_results_in_identical_string()
   {
      var maxValue = $"{long.MaxValue}.{long.MaxValue}";

      ReadOrder.Parse(maxValue).ToString().Should().Be(maxValue);
      ReadOrder.Parse(CreateString(1, 1)).ToString().Should().Be(CreateString(1, 1));
   }

   [XF] public void Parse_throws_on_negative_numbers()
   {
      Invoking(() => ReadOrder.Parse(CreateString(0, -1)))
        .Should().Throw<ArgumentException>().Which.Message.Should().Contain("negative");
   }

   [XF] public void Parse_requires_exactly_19_decimal_point_numbers()
   {
      1.Through(18).ForEach(
         num => Invoking(() => ReadOrder.Parse($"1.{new string('1', num)}"))
               .Should().Throw<ArgumentException>().Which
               .Message.Should().Contain("fraction digits"));

      ReadOrder.Parse($"1.{new string('1', 19)}");

      20.Through(40).ForEach(
         num => Invoking(() => ReadOrder.Parse($"1.{new string('1', num)}"))
               .Should().Throw<ArgumentException>().Which
               .Message.Should().Contain("fraction digits"));
   }

   [XF] public void RoundTripping_SqlDecimal_results_in_same_value()
   {
      TestValue(Create(1, 2));
      return;

      static void TestValue(ReadOrder value)
      {
         var stringValue = value.ToString();
         var sql = value.ToSqlDecimal();
         ReadOrder.FromSqlDecimal(sql).Should().Be(value);

         sql.ToString().Should().Be(stringValue);
         value.ToString().Should().Be(stringValue);

         value.ToSqlDecimal().Should().Be(sql);

         ReadOrder.FromSqlDecimal(sql).ToString().Should().Be(stringValue);
      }
   }

   [XF] public void InsertionIntervals()
   {
      // Test to verify CreateOrdersForTeventsBetween works correctly
      var orders1 = ReadOrder.CreateOrdersForTeventsBetween(2, Create(1, 0), Create(2, 0));
      orders1.Should().HaveCount(2);

      var orders2 = ReadOrder.CreateOrdersForTeventsBetween(2, Create(1, 10), Create(1, 3000));
      orders2.Should().HaveCount(2);
   }

   [XF] public void CreateOrdersForTeventsBetween_Fills_Small_Gap_Around_Integer_Limit()
   {
      var rangeStart = ReadOrder.Parse("1.9999999999999999997");
      var rangeEnd = ReadOrder.Parse("2.0000000000000000003");

      var orders = ReadOrder.CreateOrdersForTeventsBetween(numberOfTevents: 5, rangeStart: rangeStart, rangeEnd: rangeEnd);

      orders[0].Should().Be(ReadOrder.Parse("1.9999999999999999998"));
      orders[1].Should().Be(ReadOrder.Parse("1.9999999999999999999"));
      orders[2].Should().Be(ReadOrder.Parse("2.0000000000000000000"));
      orders[3].Should().Be(ReadOrder.Parse("2.0000000000000000001"));
      orders[4].Should().Be(ReadOrder.Parse("2.0000000000000000002"));
   }

   [XF] public void CreateOrdersForTeventsBetween_Fills_Minimum_Gap_Around_Integer_Limit()
   {
      var rangeStart = ReadOrder.Parse("1.9999999999999999999");
      var rangeEnd = ReadOrder.Parse("2.0000000000000000001");

      var orders = ReadOrder.CreateOrdersForTeventsBetween(numberOfTevents: 1, rangeStart: rangeStart, rangeEnd: rangeEnd);

      orders[0].Should().Be(ReadOrder.Parse("2.0000000000000000000"));
   }

   [XF] public void CreateOrdersForTeventsBetween_Fills_Small_Gap_in_middle_of_offset()
   {
      var rangeStart = ReadOrder.Parse("1.5999999999999999993");
      var rangeEnd = ReadOrder.Parse("1.5999999999999999999");

      var orders = ReadOrder.CreateOrdersForTeventsBetween(numberOfTevents: 5, rangeStart: rangeStart, rangeEnd: rangeEnd);

      orders[0].Should().Be(ReadOrder.Parse("1.5999999999999999994"));
      orders[1].Should().Be(ReadOrder.Parse("1.5999999999999999995"));
      orders[2].Should().Be(ReadOrder.Parse("1.5999999999999999996"));
      orders[3].Should().Be(ReadOrder.Parse("1.5999999999999999997"));
      orders[4].Should().Be(ReadOrder.Parse("1.5999999999999999998"));
   }

   [XF] public void CreateOrdersForTeventsBetween_Fills_Minimum_Gap_in_middle_of_offset()
   {
      var rangeStart = ReadOrder.Parse("1.5999999999999999993");
      var rangeEnd = ReadOrder.Parse("1.5999999999999999995");

      var orders = ReadOrder.CreateOrdersForTeventsBetween(numberOfTevents: 1, rangeStart: rangeStart, rangeEnd: rangeEnd);

      orders[0].Should().Be(ReadOrder.Parse("1.5999999999999999994"));
   }

   [XF] public void CreateOrdersForTeventsBetween_Throws_InvalidOperationException_if_gap_is_too_small()
   {
      var rangeStart = ReadOrder.Parse("1.9999999999999999997");
      var rangeEnd = ReadOrder.Parse("2.0000000000000000003");

      Invoking(() => ReadOrder.CreateOrdersForTeventsBetween(numberOfTevents: 6, rangeStart: rangeStart, rangeEnd: rangeEnd)).Should().Throw<ArgumentException>();
   }

   static ReadOrder Create(long order, long offset) => ReadOrder.Parse($"{order}.{offset:D19}");
   static string CreateString(int order, int value) => $"{order}.{DecimalPlaces(value)}";
   static string DecimalPlaces(int number) => new(number.ToStringInvariant()[0], 19);
}
