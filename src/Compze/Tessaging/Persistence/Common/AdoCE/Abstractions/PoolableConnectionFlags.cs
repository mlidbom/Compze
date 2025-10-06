using System;

namespace Compze.Persistence.Common.AdoCE.Abstractions;

[Flags] enum PoolableConnectionFlags
{
   Defaults = 0,
   MustUseSameConnectionThroughoutATransaction = 1
}