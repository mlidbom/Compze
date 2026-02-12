using AccountManagement.Domain;
using AccountManagement.Domain.Passwords;

namespace AccountManagement.API;

public interface IAccountResourceData
{
   AccountId Id { get; }
   Email Email { get; }
   Password Password { get; }
}