using System;
using System.Collections.Concurrent;
using Compze.Utilities.GenericAbstractions.Wrappers;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.ReflectionCE;
using Newtonsoft.Json;

namespace Compze.Serialization.Newtonsoft.Private.PrimitiveWrappers;

public class ReadOnlyRecordStructPrimitiveWrapperConverter : JsonConverter
{
   class WrappedTypeHelpers
   {
      public WrappedTypeHelpers(Type wrappedType, Func<object, object> construct)
      {
         WrappedType = wrappedType;
         Construct = construct;
      }

      public Type WrappedType { get; init; }
      public Func<object, object> Construct { get; init; }
   }

   readonly ConcurrentDictionary<Type, bool> _handlesType = new();
   readonly ConcurrentDictionary<Type, LazyCE<WrappedTypeHelpers>> _wrappedTypeHelpers = new();

   public override bool CanConvert(Type serializedType) =>
      _handlesType.GetOrAdd(serializedType, potentialWrapperType => potentialWrapperType.Implements(typeof(IReadonlyRecordStructPrimitiveWrapper<>)));

   public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) =>
      serializer.Serialize(writer,
                           value
                             .NotNull(() => $"{typeof(IReadonlyRecordStructPrimitiveWrapper<>).Name} must be readonly record structs, yet this instance is null")
                             .CastTo<IStructValueWrapper>().UntypedValue);

   public override object? ReadJson(JsonReader reader,
                                    Type typeToRead,
                                    object? existingValue,
                                    JsonSerializer serializer)
   {
      var helpers = _wrappedTypeHelpers.GetOrAdd(typeToRead,
                                                 wrapperType =>
                                                 {
                                                    return new LazyCE<WrappedTypeHelpers>(() =>
                                                    {
                                                       if(!wrapperType.IsValueType)
                                                          throw new Exception($"""{typeof(IReadonlyRecordStructPrimitiveWrapper<>).Name} must be readonly record structs, yet {wrapperType.FullName} is not a value type""");

                                                       var wrappedPrimitiveType = wrapperType.GetGenericInterface(typeof(IReadonlyRecordStructPrimitiveWrapper<>))
                                                                                             .GetGenericArguments()[0];
                                                       var constructor = (Func<object, object>)Constructor.Compile.ForType(typeToRead).WithArgumentTypes(wrappedPrimitiveType);
                                                       return new WrappedTypeHelpers(wrappedPrimitiveType, constructor);
                                                    });
                                                 }).Value;

      var primitiveValue = serializer.Deserialize(reader, helpers.WrappedType).NotNull();
      return helpers.Construct(primitiveValue);
   }
}
