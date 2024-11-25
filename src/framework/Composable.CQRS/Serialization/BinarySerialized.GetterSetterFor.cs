using System;
using System.Collections.Generic;
using System.IO;
using Composable.Contracts;

// ReSharper disable ForCanBeConvertedToForeach we do optimizations here...

namespace Composable.Serialization;

abstract partial class BinarySerialized<TInheritor> where TInheritor : BinarySerialized<TInheritor>
{
   protected static class GetterSetter
   {
      internal static MemberGetterSetter ForBoolean(MemberGetterSetter<bool>.GetterFunction getter, MemberGetterSetter<bool>.SetterFunction setter) => new BooleanGetterSetter(getter, setter);
      class BooleanGetterSetter : MemberGetterSetter<bool>
      {
         public BooleanGetterSetter(GetterFunction getter, SetterFunction setter) : base(getter, setter) {}

         internal override void Serialize(TInheritor inheritor, BinaryWriter writer) => writer.Write(Getter(inheritor));
         internal override void Deserialize(TInheritor inheritor, BinaryReader reader) => Setter(inheritor, reader.ReadBoolean());
      }

      internal static MemberGetterSetter ForByte(MemberGetterSetter<byte>.GetterFunction getter, MemberGetterSetter<byte>.SetterFunction setter) => new ByteGetterSetter(getter, setter);
      class ByteGetterSetter : MemberGetterSetter<byte>
      {
         public ByteGetterSetter(GetterFunction getter, SetterFunction setter) : base(getter, setter) {}

         internal override void Serialize(TInheritor inheritor, BinaryWriter writer) => writer.Write(Getter(inheritor));
         internal override void Deserialize(TInheritor inheritor, BinaryReader reader) => Setter(inheritor, reader.ReadByte());
      }

      internal static MemberGetterSetter ForChar(MemberGetterSetter<char>.GetterFunction getter, MemberGetterSetter<char>.SetterFunction setter) => new CharGetterSetter(getter, setter);
      class CharGetterSetter : MemberGetterSetter<char>
      {
         public CharGetterSetter(GetterFunction getter, SetterFunction setter) : base(getter, setter) {}

         internal override void Serialize(TInheritor inheritor, BinaryWriter writer) => writer.Write(Getter(inheritor));
         internal override void Deserialize(TInheritor inheritor, BinaryReader reader) => Setter(inheritor, reader.ReadChar());
      }

      internal static MemberGetterSetter ForDecimal(MemberGetterSetter<decimal>.GetterFunction getter, MemberGetterSetter<decimal>.SetterFunction setter) => new DecimalGetterSetter(getter, setter);
      class DecimalGetterSetter : MemberGetterSetter<decimal>
      {
         public DecimalGetterSetter(GetterFunction getter, SetterFunction setter) : base(getter, setter) {}

         internal override void Serialize(TInheritor inheritor, BinaryWriter writer) => writer.Write(Getter(inheritor));
         internal override void Deserialize(TInheritor inheritor, BinaryReader reader) => Setter(inheritor, reader.ReadDecimal());
      }

      internal static MemberGetterSetter ForDouble(MemberGetterSetter<double>.GetterFunction getter, MemberGetterSetter<double>.SetterFunction setter) => new DoubleGetterSetter(getter, setter);
      class DoubleGetterSetter : MemberGetterSetter<double>
      {
         public DoubleGetterSetter(GetterFunction getter, SetterFunction setter) : base(getter, setter) {}

         internal override void Serialize(TInheritor inheritor, BinaryWriter writer) => writer.Write(Getter(inheritor));
         internal override void Deserialize(TInheritor inheritor, BinaryReader reader) => Setter(inheritor, reader.ReadDouble());
      }

      internal static MemberGetterSetter ForInt16(MemberGetterSetter<short>.GetterFunction getter, MemberGetterSetter<short>.SetterFunction setter) => new Int16GetterSetter(getter, setter);
      class Int16GetterSetter : MemberGetterSetter<short>
      {
         public Int16GetterSetter(GetterFunction getter, SetterFunction setter) : base(getter, setter) {}

         internal override void Serialize(TInheritor inheritor, BinaryWriter writer) => writer.Write(Getter(inheritor));
         internal override void Deserialize(TInheritor inheritor, BinaryReader reader) => Setter(inheritor, reader.ReadInt16());
      }

      internal static MemberGetterSetter ForInt32(MemberGetterSetter<int>.GetterFunction getter, MemberGetterSetter<int>.SetterFunction setter) => new Int32GetterSetter(getter, setter);
      class Int32GetterSetter : MemberGetterSetter<int>
      {
         public Int32GetterSetter(GetterFunction getter, SetterFunction setter) : base(getter, setter) {}

         internal override void Serialize(TInheritor inheritor, BinaryWriter writer) => writer.Write(Getter(inheritor));
         internal override void Deserialize(TInheritor inheritor, BinaryReader reader) => Setter(inheritor, reader.ReadInt32());
      }

      internal static MemberGetterSetter ForInt64(MemberGetterSetter<long>.GetterFunction getter, MemberGetterSetter<long>.SetterFunction setter) => new Int64GetterSetter(getter, setter);
      class Int64GetterSetter : MemberGetterSetter<long>
      {
         public Int64GetterSetter(GetterFunction getter, SetterFunction setter) : base(getter, setter) {}

         internal override void Serialize(TInheritor inheritor, BinaryWriter writer) => writer.Write(Getter(inheritor));
         internal override void Deserialize(TInheritor inheritor, BinaryReader reader) => Setter(inheritor, reader.ReadInt64());
      }

      internal static MemberGetterSetter ForSByte(MemberGetterSetter<sbyte>.GetterFunction getter, MemberGetterSetter<sbyte>.SetterFunction setter) => new SByteGetterSetter(getter, setter);
      class SByteGetterSetter : MemberGetterSetter<sbyte>
      {
         public SByteGetterSetter(GetterFunction getter, SetterFunction setter) : base(getter, setter) {}

         internal override void Serialize(TInheritor inheritor, BinaryWriter writer) => writer.Write(Getter(inheritor));
         internal override void Deserialize(TInheritor inheritor, BinaryReader reader) => Setter(inheritor, reader.ReadSByte());
      }

      internal static MemberGetterSetter ForSingle(MemberGetterSetter<float>.GetterFunction getter, MemberGetterSetter<float>.SetterFunction setter) => new SingleGetterSetter(getter, setter);
      class SingleGetterSetter : MemberGetterSetter<float>
      {
         public SingleGetterSetter(GetterFunction getter, SetterFunction setter) : base(getter, setter) {}

         internal override void Serialize(TInheritor inheritor, BinaryWriter writer) => writer.Write(Getter(inheritor));
         internal override void Deserialize(TInheritor inheritor, BinaryReader reader) => Setter(inheritor, reader.ReadSingle());
      }

      internal static MemberGetterSetter ForString(MemberGetterSetter<string>.GetterFunction getter, MemberGetterSetter<string>.SetterFunction setter) => new StringGetterSetter(getter, setter);
      class StringGetterSetter : MemberGetterSetter<string>
      {
         public StringGetterSetter(GetterFunction getter, SetterFunction setter) : base(getter, setter) {}

         internal override void Serialize(TInheritor inheritor, BinaryWriter writer) => writer.Write(Contract.ReturnNotNull(Getter(inheritor)));
         internal override void Deserialize(TInheritor inheritor, BinaryReader reader) => Setter(inheritor, reader.ReadString());
      }

      internal static MemberGetterSetter ForUInt16(MemberGetterSetter<ushort>.GetterFunction getter, MemberGetterSetter<ushort>.SetterFunction setter) => new UInt16GetterSetter(getter, setter);
      class UInt16GetterSetter : MemberGetterSetter<ushort>
      {
         public UInt16GetterSetter(GetterFunction getter, SetterFunction setter) : base(getter, setter) {}

         internal override void Serialize(TInheritor inheritor, BinaryWriter writer) => writer.Write(Getter(inheritor));
         internal override void Deserialize(TInheritor inheritor, BinaryReader reader) => Setter(inheritor, reader.ReadUInt16());
      }

      internal static MemberGetterSetter ForUInt32(MemberGetterSetter<uint>.GetterFunction getter, MemberGetterSetter<uint>.SetterFunction setter) => new UInt32GetterSetter(getter, setter);
      class UInt32GetterSetter : MemberGetterSetter<uint>
      {
         public UInt32GetterSetter(GetterFunction getter, SetterFunction setter) : base(getter, setter) {}

         internal override void Serialize(TInheritor inheritor, BinaryWriter writer) => writer.Write(Getter(inheritor));
         internal override void Deserialize(TInheritor inheritor, BinaryReader reader) => Setter(inheritor, reader.ReadUInt32());
      }

      internal static MemberGetterSetter ForUInt64(MemberGetterSetter<ulong>.GetterFunction getter, MemberGetterSetter<ulong>.SetterFunction setter) => new UInt64GetterSetter(getter, setter);
      class UInt64GetterSetter : MemberGetterSetter<ulong>
      {
         public UInt64GetterSetter(GetterFunction getter, SetterFunction setter) : base(getter, setter) {}

         internal override void Serialize(TInheritor inheritor, BinaryWriter writer) => writer.Write(Getter(inheritor));
         internal override void Deserialize(TInheritor inheritor, BinaryReader reader) => Setter(inheritor, reader.ReadUInt64());
      }


      internal static MemberGetterSetter ForDateTime(MemberGetterSetter<DateTime>.GetterFunction getter, MemberGetterSetter<DateTime>.SetterFunction setter) => new DateTimeGetterSetter(getter, setter);
      class DateTimeGetterSetter : MemberGetterSetter<DateTime>
      {
         public DateTimeGetterSetter(GetterFunction getter, SetterFunction setter) : base(getter, setter) {}

         internal override void Serialize(TInheritor inheritor, BinaryWriter writer) => writer.Write(Getter(inheritor).ToBinary());
         internal override void Deserialize(TInheritor inheritor, BinaryReader reader) => Setter(inheritor, DateTime.FromBinary(reader.ReadInt64()));
      }

      internal static MemberGetterSetter ForGuid(MemberGetterSetter<Guid>.GetterFunction getter, MemberGetterSetter<Guid>.SetterFunction setter) => new GuidGetterSetter(getter, setter);
      class GuidGetterSetter : MemberGetterSetter<Guid>
      {
         public GuidGetterSetter(GetterFunction getter, SetterFunction setter) : base(getter, setter) {}

         internal override void Serialize(TInheritor inheritor, BinaryWriter writer) => writer.Write(Getter(inheritor).ToByteArray());
         internal override void Deserialize(TInheritor inheritor, BinaryReader reader) => Setter(inheritor, new Guid(reader.ReadBytes(16)));
      }

      internal static MemberGetterSetter ForBinarySerializable<TBinarySerializable>(MemberGetterSetter<TBinarySerializable>.GetterFunction getter, MemberGetterSetter<TBinarySerializable>.SetterFunction setter)
         where TBinarySerializable : BinarySerialized<TBinarySerializable> => new BinarySerializable<TBinarySerializable>(getter, setter);

      class BinarySerializable<TBinarySerializable> : MemberGetterSetter<TBinarySerializable>
         where TBinarySerializable : BinarySerialized<TBinarySerializable>
      {
         public BinarySerializable(GetterFunction getter, SetterFunction setter) : base(getter, setter) {}

         internal override void Serialize(TInheritor inheritor, BinaryWriter writer)
         {
            var value = Getter(inheritor);

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if(value is not null)
            {
               writer.Write(true);
               value.Serialize(writer);
            } else
               // ReSharper disable HeuristicUnreachableCode
            {
               writer.Write(false);
            }
            // ReSharper restore HeuristicUnreachableCode
         }
         internal override void Deserialize(TInheritor inheritor, BinaryReader reader)
         {
            if(reader.ReadBoolean())
            {
               var instance = BinarySerialized<TBinarySerializable>.DefaultConstructor();
               instance.Deserialize(reader);
               Setter(inheritor, instance);
            } else
            {
               Setter(inheritor, default);
            }
         }
      }

      internal static MemberGetterSetter ForBinarySerializableList<TBinarySerializable>(MemberGetterSetter<List<TBinarySerializable>>.GetterFunction getter, MemberGetterSetter<List<TBinarySerializable>>.SetterFunction setter)
         where TBinarySerializable : BinarySerialized<TBinarySerializable> => new BinarySerializableList<TBinarySerializable>(getter, setter);

      class BinarySerializableList<TBinarySerializable> : MemberGetterSetter<List<TBinarySerializable>>
         where TBinarySerializable : BinarySerialized<TBinarySerializable>
      {
         public BinarySerializableList(GetterFunction getter, SetterFunction setter) : base(getter, setter) {}

         internal override void Serialize(TInheritor inheritor, BinaryWriter writer)
         {
            var list = Getter(inheritor);
            if(list != null)
            {
               writer.Write(true);
               writer.Write(list.Count);
               foreach(var serializable in list)
               {
                  // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                  if(serializable is not null)
                  {
                     writer.Write(true);
                     serializable.Serialize(writer);
                  } else
                  {
                     writer.Write(false);
                  }
               }
            } else
               // ReSharper disable HeuristicUnreachableCode
            {
               writer.Write(false);
            }
            // ReSharper restore HeuristicUnreachableCode
         }

         internal override void Deserialize(TInheritor inheritor, BinaryReader reader)
         {
            if(reader.ReadBoolean())
            {
               var count = reader.ReadInt32();
               var list = new List<TBinarySerializable>(count);
               for(var index = 0; index < count; index++)
               {
                  if(reader.ReadBoolean())
                  {
                     var instance = BinarySerialized<TBinarySerializable>.DefaultConstructor();
                     list.Add(instance);
                     instance.Deserialize(reader);
                  } else
                  {
                     list.Add(default!);
                  }
               }
               Setter(inheritor, list);
            }
            else
            {
               Setter(inheritor, null);
            }
         }
      }

      internal static MemberGetterSetter ForBinarySerializableArray<TBinarySerializable>(MemberGetterSetter<TBinarySerializable[]>.GetterFunction getter, MemberGetterSetter<TBinarySerializable[]>.SetterFunction setter)
         where TBinarySerializable : BinarySerialized<TBinarySerializable> => new BinarySerializableArray<TBinarySerializable>(getter, setter);

      class BinarySerializableArray<TBinarySerializable> : MemberGetterSetter<TBinarySerializable[]>
         where TBinarySerializable : BinarySerialized<TBinarySerializable>
      {
         public BinarySerializableArray(GetterFunction getter, SetterFunction setter) : base(getter, setter) {}

         internal override void Serialize(TInheritor inheritor, BinaryWriter writer)
         {
            var list = Getter(inheritor);
            if(list != null)
            {
               writer.Write(true);
               writer.Write(list.Length);
               foreach(var serializable in list)
               {
                  // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                  if(serializable is not null)
                  {
                     writer.Write(true);
                     serializable.Serialize(writer);
                  } else
                  {
                     writer.Write(false);
                  }
               }
            } else
               // ReSharper disable HeuristicUnreachableCode
            {
               writer.Write(false);
            }
            // ReSharper restore HeuristicUnreachableCode
         }

         internal override void Deserialize(TInheritor inheritor, BinaryReader reader)
         {
            if(reader.ReadBoolean())
            {
               var count = reader.ReadInt32();
               var array = new TBinarySerializable[count];
               for(var index = 0; index < count; index++)
               {
                  if(reader.ReadBoolean())
                  {
                     var instance = array[index] = BinarySerialized<TBinarySerializable>.DefaultConstructor();
                     instance.Deserialize(reader);
                  } else
                  {
                     array[index] = default!;
                  }
               }
               Setter(inheritor, array);
            }
            else
            {
               Setter(inheritor, null);
            }
         }
      }
   }
}