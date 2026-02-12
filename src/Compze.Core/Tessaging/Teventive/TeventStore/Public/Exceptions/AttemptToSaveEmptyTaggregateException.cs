using System;

namespace Compze.Core.Tessaging.Teventive.TeventStore.Public.Exceptions;

public class AttemptToSaveEmptyAggregateException(object value) : Exception($"Attempting to save an: {value.GetType().FullName} that Version=0 and no history to persist.");