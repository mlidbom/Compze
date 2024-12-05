using System;

namespace Compze.Persistence.Common.EventStore;

class EventDuplicateKeyException(Exception sqlException) : Exception("""
                                                                     A duplicate key exception occurred while persisting new events. 
                                                                     This is most likely caused by multiple transactions updating the same aggregate and the persistence provider implementation, or database engine, failing to lock appropriately.
                                                                     """,
                                                                     sqlException)
{
   //Todo: Oracle exceptions has property: IsRecoverable. Research what this means and if there is something equivalent for the other providers and how this could be useful to us.
}