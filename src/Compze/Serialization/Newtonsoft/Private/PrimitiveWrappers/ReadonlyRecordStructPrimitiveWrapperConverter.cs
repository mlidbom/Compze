using System;
using System.Collections.Concurrent;
using Compze.Utilities.GenericAbstractions;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.ReflectionCE;
using Newtonsoft.Json;

namespace Compze.Serialization.Newtonsoft.Private.PrimitiveWrappers;

public class EntityIdConverter : JsonConverter
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
                               potentialWrapperType.InHerits(typeof(EntityId<>)));

   public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
   {
      if(value == null)
      {
         writer.WriteNull();
      } else
      {
         serializer.Serialize(writer, value.CastTo<IUntypedEntityId>().UntypedPrimitiveValue);
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
                                                       var wrappedPrimitiveType = wrapperType.GetGenericBaseClass(typeof(EntityId<>))
                                                                                             .GetGenericArguments()[0];
                                                       var constructor = (Func<object, object>)Constructor.Compile.ForType(typeToRead).WithArgumentTypes(wrappedPrimitiveType);
                                                       return new WrappedTypeHelpers(wrappedPrimitiveType, constructor);
                                                    });
                                                 }).Value;

      var primitiveValue = serializer.Deserialize(reader, helpers.WrappedType);
      return primitiveValue != null
                ? helpers.Construct(primitiveValue)
                : null;
   }
}
