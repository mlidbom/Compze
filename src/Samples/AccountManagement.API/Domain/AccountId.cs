using System;
using Compze.Core.Public;

namespace AccountManagement.Domain;

///<summary>Let's not make every id the same type, type safety and being able to find usages is very useful indeed.</summary>
public class AccountId(Guid id) : TaggregateId(id)
{
   public AccountId(): this(Guid.NewGuid()){}
}