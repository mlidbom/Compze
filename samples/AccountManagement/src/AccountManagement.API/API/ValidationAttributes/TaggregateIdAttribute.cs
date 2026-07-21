using AccountManagement.API.ValidationAttributes.Helpers;
using Compze.Abstractions;

namespace AccountManagement.API.ValidationAttributes;

sealed class TaggregateIdAttribute : ValidationAttributeBase
{
   protected override bool InternalIsValid(object value) => value is TaggregateId;
}