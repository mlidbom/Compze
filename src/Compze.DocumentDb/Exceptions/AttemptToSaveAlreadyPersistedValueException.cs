namespace Compze.DocumentDb.Exceptions;

///<summary>Thrown by the save operations of <see cref="Public.IDocumentDbUpdater"/> and <see cref="Public.IDocumentDbSession"/> when a document<br/>
/// with the same key has already been persisted. Updates happen by loading the document from a session and modifying it, never by saving again.</summary>
public class AttemptToSaveAlreadyPersistedValueException(object key, object value) :
   ArgumentException($"Instance of {value.GetType().FullName} with Id: {key} has already been persisted. To update it, load it from a session and modify it rather than attempting to call save");
