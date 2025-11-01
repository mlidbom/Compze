using AccountManagement.Domain;
using AccountManagement.Domain.Passwords;
using Compze.Core.Public;

namespace AccountManagement.API;

interface IAccountResourceData
{
   AccountId Id { get; }
   Email Email { get; }
   Password Password { get; }
}