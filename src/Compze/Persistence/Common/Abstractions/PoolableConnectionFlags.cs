using System;

namespace Compze.Persistence.Common.Abstractions;

[Flags] enum PoolableConnectionFlags
{
   Defaults = 0,
   MustUseSameConnectionThroughoutATransaction = 1
}