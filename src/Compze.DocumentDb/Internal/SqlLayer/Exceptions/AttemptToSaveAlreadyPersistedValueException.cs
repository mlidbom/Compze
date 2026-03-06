namespace Compze.DocumentDb.Internal.SqlLayer.Exceptions;

public class AttemptToSaveAlreadyPersistedValueException(object key, object value) :
   ArgumentException($"Instance of {value.GetType().FullName} with Id: {key} has already been persisted. To update it, load it from a session and modify it rather than attempting to call save");