using System;
using Compze.Core.Public;

namespace Compze.Sql.Common.DbPool;

public class DbPoolId : EntityId
{
   public DbPoolId(Guid id) : base(id) {}
   public DbPoolId() : base(Guid.NewGuid()) {}
}
