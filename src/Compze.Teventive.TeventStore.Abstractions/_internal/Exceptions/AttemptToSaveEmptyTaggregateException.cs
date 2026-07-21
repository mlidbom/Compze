namespace Compze.Teventive.TeventStore.Abstractions._internal.Exceptions;

class AttemptToSaveEmptyTaggregateException(object value) : Exception($"Attempting to save an: {value.GetType().FullName} that Version=0 and no history to persist.");