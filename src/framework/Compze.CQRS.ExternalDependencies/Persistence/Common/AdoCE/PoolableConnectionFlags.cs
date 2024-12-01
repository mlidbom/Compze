using System;

namespace Compze.Persistence.Common.AdoCE;

[Flags] enum PoolableConnectionFlags
{
   Defaults = 0,
   MustUseSameConnectionThroughoutATransaction = 1
}