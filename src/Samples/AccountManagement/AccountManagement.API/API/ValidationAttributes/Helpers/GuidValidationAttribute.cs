using System;

namespace AccountManagement.API.ValidationAttributes.Helpers;

public abstract class GuidValidationAttribute : ValidationAttributeBase
{
   protected override bool InternalIsValid(object value) => IsValid((Guid)value);
   protected abstract bool IsValid(Guid value);
}