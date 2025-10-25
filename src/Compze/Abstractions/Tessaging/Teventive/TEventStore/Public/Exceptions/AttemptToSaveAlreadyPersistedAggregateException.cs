using System;
using Compze.Abstractions.Tessaging.Teventive.Public;

namespace Compze.Abstractions.Tessaging.Teventive.TeventStore.Public.Exceptions;

class AttemptToSaveAlreadyPersistedAggregateException(ITeventStored aggregate) :
   InvalidOperationException($"Instance of {aggregate.GetType().FullName} with Id: {aggregate.Id} has already been persisted. To update it, load it from a session and modify it rather than attempting to call save");