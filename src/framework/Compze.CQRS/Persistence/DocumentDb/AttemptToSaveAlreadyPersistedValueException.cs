using System;

namespace Compze.Persistence.DocumentDb;

class AttemptToSaveAlreadyPersistedValueException(object key, object value) :
   Exception($"Instance of {value.GetType().FullName} with Id: {key} has already been persisted. To update it, load it from a session and modify it rather than attempting to call save");
