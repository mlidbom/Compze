using System;
using AccountManagement.API.ValidationAttributes.Helpers;

namespace AccountManagement.API.ValidationAttributes;

public sealed class EntityIdAttribute : GuidValidationAttribute
{
   protected override bool IsValid(Guid value) => value != Guid.Empty;
}