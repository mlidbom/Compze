using System;
using Compze.Core.Tessaging.Teventive.Public;

namespace Compze.Core.Tessaging.Teventive.TeventStore.Public.Exceptions;

class AttemptToSaveAlreadyPersistedAggregateException(ITaggregate taggregate) :
   InvalidOperationException($"Instance of {taggregate.GetType().FullName} with Id: {taggregate.Id} has already been persisted. To update it, load it from a session and modify it rather than attempting to call save");