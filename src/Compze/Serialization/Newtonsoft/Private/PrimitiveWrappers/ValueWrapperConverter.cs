using System;
using System.Collections.Concurrent;
using Compze.Core.Public.Infrastructure;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.ReflectionCE;
using Newtonsoft.Json;

namespace Compze.Serialization.Newtonsoft.Private.PrimitiveWrappers;

public class ValueWrapperConverter : JsonConverter
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
      _handlesType.GetOrAdd(serializedType,
                            potentialWrapperType =>
                               !potentialWrapperType.IsAbstract &&
                               potentialWrapperType.InHerits(typeof(ValueWrapper<>)));

   public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
   {
      if(value == null)
      {
         writer.WriteNull();
      } else
      {
         serializer.Serialize(writer, value.CastTo<ISingleUntypedPrimitiveValueWrapper>().UntypedPrimitiveValue);
      }
   }

   public override object? ReadJson(JsonReader reader,
                                    Type typeToRead,
                                    object? existingValue,
                                    JsonSerializer serializer)
   {
      if(reader.TokenType == JsonToken.Null)
         return null;

      var helpers = _wrappedTypeHelpers.GetOrAdd(typeToRead,
                                                 wrapperType =>
                                                 {
                                                    return new LazyCE<WrappedTypeHelpers>(() =>
                                                    {
                                                       var wrappedPrimitiveType = wrapperType.GetGenericBaseClass(typeof(ValueWrapper<>))
                                                                                             .GetGenericArguments()[0];
                                                       var constructor = Constructor.Compile.ForType(typeToRead)
                                                                                            .WithArgument(wrappedPrimitiveType);

                                                       return new WrappedTypeHelpers(wrappedPrimitiveType, constructor);
                                                    });
                                                 }).Value;

      var primitiveValue = serializer.Deserialize(reader, helpers.WrappedType);
      return helpers.Construct(primitiveValue.NotNull());
   }
}
