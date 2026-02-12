using System;

namespace Compze.Sql.Common.Abstractions;

[Flags] public enum PoolableConnectionFlags
{
   Defaults = 0,
   MustUseSameConnectionThroughoutATransaction = 1
}