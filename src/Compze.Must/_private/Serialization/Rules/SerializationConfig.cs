using System.Linq.Expressions;
using Compze.Must._private.Serialization.Converters;

namespace Compze.Must._private.Serialization.Rules;

/// <summary>The <see cref="ISerializationConfig{TValue}"/> a <see cref="MustExtensions.Must{T}(T, Func{ISerializationConfig{T}, ISerializationConfig{T}}, string)"/> call configures: it collects the assertion chain's own <see cref="SerializationRules"/>.</summary>
class SerializationConfig<TValue> : ISerializationConfig<TValue>
{
   SerializationRules _rules = SerializationRules.None;

   public static SerializationRules RulesFrom(Func<ISerializationConfig<TValue>, ISerializationConfig<TValue>> configure) =>
      ((SerializationConfig<TValue>)configure(new SerializationConfig<TValue>()))._rules;

   public ISerializationConfig<TValue> ExcludeMember<TMember>(Expression<Func<TValue, TMember>> member) =>
      With(_rules.WithExcludedMember(ExcludedMember.Of(member.ExtractFinalMemberInfo())));

   public ISerializationConfig<TValue> ExcludeMember(Type declaringType, string memberName) =>
      With(_rules.WithExcludedMember(new ExcludedMember(declaringType, memberName)));

   public ISerializationConfig<TValue> ExcludeType<TExcluded>() => ExcludeType(typeof(TExcluded));

   public ISerializationConfig<TValue> ExcludeType(Type excluded) => With(_rules.WithTypeRule(new ExcludedTypeJsonConverter(excluded)));

   public ISerializationConfig<TValue> SerializeAs<TReplaced>(Func<TReplaced, object?> replacement) =>
      With(_rules.WithTypeRule(new SerializeAsJsonConverter<TReplaced>(replacement)));

   public ISerializationConfig<TValue> WithoutTypeNames() => With(_rules.WithTypeNamesOmitted());

   SerializationConfig<TValue> With(SerializationRules rules)
   {
      _rules = rules;
      return this;
   }
}
