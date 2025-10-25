namespace Compze.Abstractions.DocumentDb.Public;

//refactor: break up and probably remove this monolithic interface.
public interface IDocumentDbSession : IDocumentDbBulkReader, IDocumentDbUpdater;