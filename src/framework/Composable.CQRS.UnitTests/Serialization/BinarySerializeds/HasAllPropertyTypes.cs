using System;
using System.Collections.Generic;
using Composable.Serialization;

namespace Composable.Tests.Serialization.BinarySerializeds;

class HasAllPropertyTypes : BinarySerialized<HasAllPropertyTypes>
{
   public static HasAllPropertyTypes CreateInstanceWithSaneValues() => new(true, 2, 'a', new decimal(3.2), 4.1, 5, 6, 7, 8, 9, 10, 11.1f, 12, "13", Guid.Parse("00000000-0000-0000-0000-000000000014"), DateTime.FromBinary(15));

   protected override IEnumerable<MemberGetterSetter> CreateGetterSetters() =>
   [
      GetterSetter.ForBoolean(it => it.Boolean, (it, value) => it.Boolean = value),
      GetterSetter.ForByte(it => it.Byte, (it, value) => it.Byte = value),
      GetterSetter.ForChar(it => it.Char, (it, value) => it.Char = value),
      GetterSetter.ForDecimal(it => it.Decimal, (it, value) => it.Decimal = value),
      GetterSetter.ForChar(it => it.Char, (it, value) => it.Char = value),
      GetterSetter.ForDouble(it => it.Double, (it, value) => it.Double = value),
      GetterSetter.ForInt16(it => it.Int16, (it, value) => it.Int16 = value),
      GetterSetter.ForInt32(it => it.Int32, (it, value) => it.Int32 = value),
      GetterSetter.ForInt64(it => it.Int64, (it, value) => it.Int64 = value),
      GetterSetter.ForSByte(it => it.SByte, (it, value) => it.SByte = value),
      GetterSetter.ForSingle(it => it.Single, (it, value) => it.Single = value),
      GetterSetter.ForString(it => it.String, (it, value) => it.String = value),
      GetterSetter.ForUInt16(it => it.UInt16, (it, value) => it.UInt16 = value),
      GetterSetter.ForUInt32(it => it.UInt32, (it, value) => it.UInt32 = value),
      GetterSetter.ForUInt64(it => it.UInt64, (it, value) => it.UInt64 = value),
      GetterSetter.ForDateTime(it => it.DateTime, (it, value) => it.DateTime = value),
      GetterSetter.ForGuid(it => it.Guid, (it, value) => it.Guid = value),
      GetterSetter.ForBinarySerializable(it => it.RecursiveProperty, (it, value) => it.RecursiveProperty = value),
      GetterSetter.ForBinarySerializableList(it => it.RecursiveListProperty, (it, value) => it.RecursiveListProperty = value),
      GetterSetter.ForBinarySerializableArray(it => it.RecursiveArrayProperty, (it, value) => it.RecursiveArrayProperty = value)
   ];

   public HasAllPropertyTypes() {}

   public HasAllPropertyTypes(bool boolean, byte b, char c, decimal @decimal, double d, short int16, int int32, long int64, ushort uInt16, uint uInt32, ulong uInt64, float single, sbyte sByte, string s, Guid guid, DateTime dateTime)
   {
      Boolean = boolean;
      Byte = b;
      Char = c;
      Decimal = @decimal;
      Double = d;
      Int16 = int16;
      Int32 = int32;
      Int64 = int64;
      UInt16 = uInt16;
      UInt32 = uInt32;
      UInt64 = uInt64;
      Single = single;
      SByte = sByte;
      String = s;
      Guid = guid;
      DateTime = dateTime;
   }

   public HasAllPropertyTypes RecursiveProperty { get; set; }
   public List<HasAllPropertyTypes> RecursiveListProperty { get; set; }
   public HasAllPropertyTypes[] RecursiveArrayProperty { get; set; }

   public Boolean Boolean { get; set; }
   public Byte Byte { get; set; }
   public char Char { get; set; }
   public Decimal Decimal { get; set; }
   public Double Double { get; set; }
   public Int16 Int16 { get; set; }
   public Int32 Int32 { get; set; }
   public Int64 Int64 { get; set; }
   public UInt16 UInt16 { get; set; }
   public UInt32 UInt32 { get; set; }
   public UInt64 UInt64 { get; set; }
   public Single Single { get; set; }
   public SByte SByte { get; set; }
   public String String { get; set; }
   public Guid Guid { get; set; }
   public DateTime DateTime { get; set; }
}