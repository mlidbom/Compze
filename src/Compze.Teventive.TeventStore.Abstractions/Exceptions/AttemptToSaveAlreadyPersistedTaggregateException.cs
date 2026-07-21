namespace Compze.Teventive.TeventStore.Abstractions.Exceptions;

public class AttemptToSaveAlreadyPersistedTaggregateException(ITaggregate taggregate) :
   InvalidOperationException($"Instance of {taggregate.GetType().FullName} with Id: {taggregate.Id} has already been persisted. To update it, load it from a session and modify it rather than attempting to call save");