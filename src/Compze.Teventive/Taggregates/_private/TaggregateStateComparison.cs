using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using Compze.Contracts;
using Compze.Threading;
using Compze.Teventive.Taggregates.BaseClasses._private;

namespace Compze.Teventive.Taggregates._private;

///<summary>The differences between the states of two instances of one taggregate: an original and a copy made through a snapshot.<br/>
/// The state is the value of every field of the taggregate's own classes, and of every object reached from them, as<br/>
/// <see cref="SnapshotRoundTrip"/> describes.</summary>
sealed class TaggregateStateComparison
{
   readonly List<string> _differences = [];

   ///<summary>The pairs whose comparison has begun and not ended. A pair met again inside its own comparison, through a reference back,<br/>
   /// such as a component's to its taggregate, is taken for equal there: whatever differs in it is found where its comparison began.</summary>
   readonly HashSet<ComparedPair> _beingCompared;

   TaggregateStateComparison(HashSet<ComparedPair> beingCompared) => _beingCompared = beingCompared;

   ///<summary>Each difference between <paramref name="original"/> and <paramref name="copy"/>, as a line naming the path from the<br/>
   /// taggregate to the field or item, and its value in each.</summary>
   internal static IReadOnlyList<string> DifferencesBetween(ITaggregate original, ITaggregate copy)
   {
      var comparison = new TaggregateStateComparison([]);
      comparison.Compare(original, copy, original.GetType().Name);
      return comparison._differences;
   }

   void Compare(object? original, object? copy, string path)
   {
      if(ReferenceEquals(original, copy)) return;
      if(original is null || copy is null || original.GetType() != copy.GetType())
      {
         Differ(path, original, copy);
         return;
      }

      if(IsComparedByItsOwnEquality(original.GetType()))
      {
         if(!original.Equals(copy)) Differ(path, original, copy);
         return;
      }

      if(original is Delegate) return; //Delegates aren't state.

      if(original is IEntityCollectionState originalEntities)
      {
         Compare(originalEntities.EntitiesWithTheirIdsInCreationOrder, ((IEntityCollectionState)copy).EntitiesWithTheirIdsInCreationOrder, path);
         return;
      }

      if(IsCreatedOnFirstRead(original.GetType()))
      {
         Compare(ValueOf(original), ValueOf(copy), path);
         return;
      }

      var pair = new ComparedPair(original, copy);
      if(!_beingCompared.Add(pair)) return;

      if(original is IEnumerable originalItems) CompareCollections(originalItems, (IEnumerable)copy, path);
      else CompareFields(original, copy, path);

      _beingCompared.Remove(pair);
   }

   void CompareFields(object original, object copy, string path)
   {
      foreach(var field in StateFieldsOf(original.GetType()))
         Compare(field.GetValue(original), field.GetValue(copy), $"{path}.{NameOf(field)}");
   }

   void CompareCollections(IEnumerable original, IEnumerable copy, string path)
   {
      var originalItems = original.Cast<object?>().ToList();
      var copiedItems = copy.Cast<object?>().ToList();
      if(originalItems.Count != copiedItems.Count)
      {
         _differences.Add($"{path}: {originalItems.Count} items in the original, {copiedItems.Count} in the copy");
         return;
      }

      if(IsUnordered(original.GetType())) CompareInAnyOrder(originalItems, copiedItems, path);
      else
         for(var index = 0; index < originalItems.Count; index++)
            Compare(originalItems[index], copiedItems[index], $"{path}[{index}]");
   }

   void CompareInAnyOrder(List<object?> originalItems, List<object?> copiedItems, string path)
   {
      var unmatched = new List<object?>(copiedItems);
      foreach(var originalItem in originalItems)
      {
         var match = unmatched.FindIndex(copiedItem => AreEqual(originalItem, copiedItem));
         if(match < 0) _differences.Add($"{path}: the original holds {Describe(originalItem)}, which the copy doesn't");
         else unmatched.RemoveAt(match);
      }
   }

   ///<summary>Whether <paramref name="original"/> and <paramref name="copy"/> hold the same state, found by a comparison of their own that<br/>
   /// adds no difference to this one: to match the items of a set or dictionary.</summary>
   bool AreEqual(object? original, object? copy)
   {
      var trial = new TaggregateStateComparison(_beingCompared);
      trial.Compare(original, copy, "");
      return trial._differences.Count == 0;
   }

   void Differ(string path, object? original, object? copy) => _differences.Add($"{path}: {Describe(original)} in the original, {Describe(copy)} in the copy");

   ///<summary>The fields of <paramref name="type"/> and its base classes, up to the first of Compze's, whose fields aren't state.</summary>
   static IEnumerable<FieldInfo> StateFieldsOf(Type type)
   {
      for(var current = type; current != null && !IsCompzesMachinery(current); current = current.BaseType)
         foreach(var field in current.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            yield return field;
   }

   ///<summary>Whether <paramref name="type"/> is one of Compze.Teventive's, such as the base classes of taggregates and of their parts:<br/>
   /// what their fields hold is how tevents are published and applied, not the consumer's state.</summary>
   static bool IsCompzesMachinery(Type type) => type.Assembly == typeof(TaggregateStateComparison).Assembly;

   ///<summary>A field's name as the code declares it: a property's, for the field the compiler generates behind it.</summary>
   static string NameOf(FieldInfo field) => field.Name.StartsWithOrdinal("<") ? field.Name[1..field.Name.IndexOfOrdinal('>')] : field.Name;

   ///<summary>Whether <paramref name="type"/> is a value created on its first read, a <see cref="LazyCE{TValue}"/> or a <see cref="Lazy{T}"/>:<br/>
   /// its state is its value, which the comparison reads in both instances, creating it where no read has yet. Whether a read has<br/>
   /// created it is no state of the taggregate's, and a value derived from the state that comes out different is a real difference.</summary>
   static bool IsCreatedOnFirstRead(Type type) => type.IsGenericType && ValuesCreatedOnFirstRead.Contains(type.GetGenericTypeDefinition());

   static readonly HashSet<Type> ValuesCreatedOnFirstRead = [typeof(LazyCE<>), typeof(Lazy<>)];

   static object? ValueOf(object createdOnFirstRead) =>
      createdOnFirstRead.GetType().GetProperty(nameof(Lazy<object>.Value))._assert().NotNull().GetValue(createdOnFirstRead);

   static bool IsComparedByItsOwnEquality(Type type) => type.IsPrimitive || type.IsEnum || SimpleValueTypes.Contains(type) || typeof(MemberInfo).IsAssignableFrom(type);

   ///<summary>.NET's simple values, apart from primitives and enums, whose own equality compares the whole of their value.</summary>
   static readonly HashSet<Type> SimpleValueTypes = [typeof(string), typeof(decimal), typeof(DateTime), typeof(DateTimeOffset), typeof(TimeSpan), typeof(DateOnly), typeof(TimeOnly), typeof(Guid), typeof(Uri)];

   static bool IsUnordered(Type collectionType) =>
      typeof(IDictionary).IsAssignableFrom(collectionType)
   || collectionType.GetInterfaces().Any(it => it.IsGenericType && UnorderedCollections.Contains(it.GetGenericTypeDefinition()));

   ///<summary>The collections whose items have no order: sets, and dictionaries, whose items are their entries.</summary>
   static readonly HashSet<Type> UnorderedCollections = [typeof(ISet<>), typeof(IReadOnlySet<>), typeof(IDictionary<,>), typeof(IReadOnlyDictionary<,>)];

   static string Describe(object? value) => value switch
   {
      null => "null",
      string text => $"\"{text}\"",
      _ when value.GetType().IsValueType || IsComparedByItsOwnEquality(value.GetType()) => value.ToString() ?? "",
      _ => $"a {value.GetType().Name}"
   };

   ///<summary>An original object and the copy's object compared with it, equal to another such pair only when it is of the very same objects.</summary>
   readonly struct ComparedPair(object original, object copy) : IEquatable<ComparedPair>
   {
      readonly object _original = original;
      readonly object _copy = copy;

      public bool Equals(ComparedPair other) => ReferenceEquals(_original, other._original) && ReferenceEquals(_copy, other._copy);
      public override bool Equals(object? other) => other is ComparedPair pair && Equals(pair);
      public override int GetHashCode() => HashCode.Combine(RuntimeHelpers.GetHashCode(_original), RuntimeHelpers.GetHashCode(_copy));
   }
}
