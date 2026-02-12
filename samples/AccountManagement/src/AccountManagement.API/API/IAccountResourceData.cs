using AccountManagement.Domain;
using AccountManagement.Domain.Passwords;

namespace AccountManagement.API;

interface IAccountResourceData
{
   AccountId Id { get; }
   Email Email { get; }
   Password Password { get; }
}