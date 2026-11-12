using System.Reflection;

namespace Compze.Must._private.Serialization;

/// <summary>Which of an object's members Must reads when it serializes the object: all of them, internal and public ones, or public ones.</summary>
class MemberVisibility
{
   /// <summary>Every member: public, internal, protected and private. Failure messages, <see cref="DeepEqualAssertions.DeepEqual{TValue}"/> and <see cref="DeepEqualAssertions.DeepEqualPrivate{TValue}"/> use it.</summary>
   public static readonly MemberVisibility AllMembers = new(_ => true, _ => true);

   /// <summary>Public, internal and protected members. <see cref="DeepEqualAssertions.DeepEqualInternal{TValue}"/> uses it.</summary>
   public static readonly MemberVisibility InternalAndPublicMembers =
      new(property => property.GetMethod is { IsPublic: true } or { IsAssembly: true } or { IsFamilyOrAssembly: true } or { IsFamily: true },
          field => field.IsPublic || field.IsAssembly || field.IsFamilyOrAssembly || field.IsFamily);

   /// <summary>Public members only. <see cref="DeepEqualAssertions.DeepEqualPublic{TValue}"/> uses it.</summary>
   public static readonly MemberVisibility PublicMembers = new(property => property.GetMethod?.IsPublic ?? false, field => field.IsPublic);

   readonly Func<PropertyInfo, bool> _includesProperty;
   readonly Func<FieldInfo, bool> _includesField;

   MemberVisibility(Func<PropertyInfo, bool> includesProperty, Func<FieldInfo, bool> includesField)
   {
      _includesProperty = includesProperty;
      _includesField = includesField;
   }

   public bool Includes(PropertyInfo property) => _includesProperty(property);
   public bool Includes(FieldInfo field) => _includesField(field);
}
