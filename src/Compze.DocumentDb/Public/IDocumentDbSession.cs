namespace Compze.DocumentDb.Public;

//refactor: break up and probably remove this monolithic interface.
public interface IDocumentDbSession : IDocumentDbBulkReader, IDocumentDbUpdater;