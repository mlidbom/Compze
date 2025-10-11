using System;

namespace Compze.Sql.Common.Abstractions;

[Flags] enum PoolableConnectionFlags
{
   Defaults = 0,
   MustUseSameConnectionThroughoutATransaction = 1
}