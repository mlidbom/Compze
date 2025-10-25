using System;
using Compze.Abstractions.Tessaging.Teventive.Public;

namespace Compze.Abstractions.Tessaging.Teventive.TeventStore.Public.Exceptions;

class AttemptToSaveAlreadyPersistedAggregateException(ITeventStored taggregate) :
   InvalidOperationException($"Instance of {taggregate.GetType().FullName} with Id: {taggregate.Id} has already been persisted. To update it, load it from a session and modify it rather than attempting to call save");