using AccountManagement.API.ValidationAttributes.Helpers;
using Compze.Core.Public;

namespace AccountManagement.API.ValidationAttributes;

public sealed class TaggregateIdAttribute : ValidationAttributeBase
{
   protected override bool InternalIsValid(object value) => value is TaggregateId;
}