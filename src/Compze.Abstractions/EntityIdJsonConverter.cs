using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Compze.Contracts;
using Compze.Internals.SystemCE.ReflectionCE;

namespace Compze.Abstractions;

///<summary>Has System.Text.Json write an <see cref="EntityId{TPrimitive}"/> as the primitive value it wraps, such as a <see cref="TaggregateId"/><br/>
/// as its <see cref="Guid"/>, and read it back as an id of the same type, both as a value and as the key of a dictionary.<br/>
/// <see cref="EntityId{TPrimitive}"/>, <see cref="EntityId"/>, <see cref="TentityId"/> and <see cref="TaggregateId"/> carry it as their<br/>
/// <see cref="JsonConverterAttribute"/>, so no <see cref="JsonSerializerOptions"/> need register it.</summary>
///<remarks>System.Text.Json reads a <see cref="JsonConverterAttribute"/> off the type it serializes alone, never off a base type, so an id type<br/>
/// of your own carries it too: <c>[JsonConverter(typeof(EntityIdJsonConverter))] public class NoteId(Guid value) : TaggregateId(value);</c><br/>
/// One that doesn't is written as an object holding its <see cref="Infrastructure.ValueWrapper{TValue}.Value"/>.</remarks>
///<remarks>It reads an id through the id type's constructor that takes the primitive value alone. An id type that has no such constructor<br/>
/// fails with an <see cref="Compze.Contracts.Exceptions.ArgumentAssertionFailedException"/> the first time System.Text.Json serializes it.</remarks>
public sealed class EntityIdJsonConverter : JsonConverterFactory
{
   public override bool CanConvert(Type typeToConvert) => PrimitiveOf(typeToConvert) != null;

   public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
   {
      var primitive = PrimitiveOf(typeToConvert)._assert().NotNull();
      Argument.Assert(HasAConstructorTakingOnly(typeToConvert, primitive),
                      () => $"{typeToConvert.GetFullNameCompilable()} has no constructor that takes a {primitive.GetFullNameCompilable()} alone, which {nameof(EntityIdJsonConverter)} reads it back through.");
      return (JsonConverter)Activator.CreateInstance(typeof(Converter<,>).MakeGenericType(typeToConvert, primitive), options)._assert().NotNull();
   }

   ///<summary>The primitive value type of the <see cref="EntityId{TPrimitive}"/> that <paramref name="type"/> is, or null if it is none.</summary>
   static Type? PrimitiveOf(Type type) => type.ClassInheritanceChain()
                                              .FirstOrDefault(it => it.IsGenericType && it.GetGenericTypeDefinition() == typeof(EntityId<>))
                                             ?.GetGenericArguments()[0];

   static bool HasAConstructorTakingOnly(Type idType, Type primitive) =>
      idType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, [primitive]) != null;

   ///<summary>Writes and reads a <typeparamref name="TEntityId"/> as its <typeparamref name="TPrimitive"/>, with the converter that<br/>
   /// System.Text.Json's options have for the <typeparamref name="TPrimitive"/>.</summary>
   sealed class Converter<TEntityId, TPrimitive> : JsonConverter<TEntityId>
      where TEntityId : EntityId<TPrimitive>
      where TPrimitive : IEquatable<TPrimitive>
   {
      readonly Func<TPrimitive, TEntityId> _construct = Constructor.For<TEntityId>.WithArguments<TPrimitive>.Instance;
      readonly JsonConverter<TPrimitive> _primitiveConverter;

      public Converter(JsonSerializerOptions options) => _primitiveConverter = (JsonConverter<TPrimitive>)options.GetConverter(typeof(TPrimitive));

      public override TEntityId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
         _construct(_primitiveConverter.Read(ref reader, typeof(TPrimitive), options)._assert().NotNull());

      public override void Write(Utf8JsonWriter writer, TEntityId value, JsonSerializerOptions options) =>
         _primitiveConverter.Write(writer, value.Value, options);

      public override TEntityId ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
         _construct(_primitiveConverter.ReadAsPropertyName(ref reader, typeof(TPrimitive), options));

      public override void WriteAsPropertyName(Utf8JsonWriter writer, TEntityId value, JsonSerializerOptions options) =>
         _primitiveConverter.WriteAsPropertyName(writer, value.Value, options);
   }
}
