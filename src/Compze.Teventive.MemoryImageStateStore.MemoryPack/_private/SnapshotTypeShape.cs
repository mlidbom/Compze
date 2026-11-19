using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using MemoryPack;

namespace Compze.Teventive.MemoryImageStateStore.MemoryPack._private;

///<summary>The shape of a snapshot type, as far as it decides what MemoryPack writes: its instance fields, and those of each type<br/>
/// reachable through them, with their names, types and order. Its fingerprint tells a cache written for another shape of the type<br/>
/// from one written for this one.</summary>
///<remarks>Fields, not MemoryPack's own rules of what it writes, so the shape is never narrower than what MemoryPack reads: a property<br/>
/// is caught by its backing field, and a change MemoryPack ignores costs one start that reads every file, never wrong data.<br/>
/// Recursion stops at the base class library's types, by name, except that a generic one's type arguments are described, so a<br/>
/// collection's elements are. A type met again is named, not described again, which also ends a cycle.</remarks>
sealed class SnapshotTypeShape
{
   readonly StringBuilder _description = new();
   readonly HashSet<Type> _described = [];

   SnapshotTypeShape() {}

   ///<summary>A fingerprint of <paramref name="snapshotType"/>'s shape: the same in every run for the same shape, and another for any<br/>
   /// change to it.</summary>
   internal static string FingerprintOf(Type snapshotType)
   {
      var shape = new SnapshotTypeShape();
      shape.Describe(snapshotType);
      return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(shape._description.ToString())));
   }

   void Describe(Type type)
   {
      if(type.IsEnum) DescribeEnum(type);
      else if(type.IsArray) DescribeArray(type);
      else if(IsOfTheBaseClassLibrary(type)) DescribeByName(type);
      else if(!_described.Add(type)) _description.Append('@').Append(NameOf(type));
      else DescribeFields(type);
   }

   void DescribeEnum(Type type)
   {
      _description.Append("enum ").Append(NameOf(type)).Append(':').Append(Enum.GetUnderlyingType(type).Name).Append('{');
      var values = Enum.GetValuesAsUnderlyingType(type);
      var names = Enum.GetNames(type);
      for(var index = 0; index < names.Length; index++) _description.Append(names[index]).Append('=').Append(values.GetValue(index)).Append(';');
      _description.Append('}');
   }

   void DescribeArray(Type type)
   {
      _description.Append("array").Append(type.GetArrayRank()).Append('<');
      Describe(type.GetElementType()!);
      _description.Append('>');
   }

   ///<summary>A type of the base class library, by its name, and a generic one with its type arguments described.</summary>
   void DescribeByName(Type type)
   {
      _description.Append(type.IsGenericType ? type.GetGenericTypeDefinition().FullName : type.FullName);
      DescribeTypeArguments(type);
   }

   void DescribeFields(Type type)
   {
      _description.Append(type.IsGenericType ? type.GetGenericTypeDefinition().FullName : type.FullName);
      DescribeTypeArguments(type);
      _description.Append('{');
      foreach(var field in InstanceFieldsOf(type))
      {
         _description.Append(field.Name).Append(':');
         Describe(field.FieldType);
         _description.Append(';');
      }

      foreach(var union in type.GetCustomAttributes<MemoryPackUnionAttribute>().OrderBy(it => it.Tag))
      {
         _description.Append('|').Append(union.Tag).Append('=');
         Describe(union.Type);
      }

      _description.Append('}');
   }

   void DescribeTypeArguments(Type type)
   {
      if(!type.IsGenericType) return;
      _description.Append('<');
      foreach(var argument in type.GetGenericArguments())
      {
         Describe(argument);
         _description.Append(',');
      }

      _description.Append('>');
   }

   ///<summary>The instance fields of <paramref name="type"/>, public or not, its base classes' first, each class's in the order it<br/>
   /// declares them, which is MemoryPack's order too.</summary>
   static IEnumerable<FieldInfo> InstanceFieldsOf(Type type) =>
      ClassesFromTheBaseDown(type).SelectMany(it => it.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                                                      .OrderBy(field => field.MetadataToken));

   static IEnumerable<Type> ClassesFromTheBaseDown(Type type) =>
      type.BaseType is {} baseType && !IsOfTheBaseClassLibrary(baseType) ? [..ClassesFromTheBaseDown(baseType), type] : [type];

   ///<summary>A name for <paramref name="type"/> that holds no assembly version: <see cref="Type.FullName"/> of a closed generic does.</summary>
   static string NameOf(Type type) =>
      type.IsGenericType
         ? $"{type.GetGenericTypeDefinition().FullName}[{string.Join(",", type.GetGenericArguments().Select(NameOf))}]"
         : type.FullName ?? type.Name;

   static bool IsOfTheBaseClassLibrary(Type type) => type.Namespace is "System" || type.Namespace?.StartsWith("System.", StringComparison.Ordinal) == true;
}
