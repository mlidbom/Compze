namespace Compze.Teventive.TeventStore.Abstractions.Public.Exceptions;

public class AttemptToSaveEmptyTaggregateException(object value) : Exception($"Attempting to save an: {value.GetType().FullName} that Version=0 and no history to persist.");