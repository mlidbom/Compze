namespace Compze.Core.Tessaging.Teventive.TeventStore.Internal.SqlLayer.Abstractions;

public class TeventDuplicateKeyException(Exception sqlException) : Exception("""
                                                                            A duplicate key exception occurred while persisting new tevents. 
                                                                            This is most likely caused by multiple transactions updating the same taggregate and the sql provider implementation, or database engine, failing to lock appropriately.
                                                                            """,
                                                                            sqlException)
{
   //Todo: Oracle exceptions has property: IsRecoverable. Research what this means and if there is something equivalent for the other providers and how this could be useful to us.
}