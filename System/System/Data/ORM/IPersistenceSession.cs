using System;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Composable.Data.ORM
{
    [ContractClass(typeof(PersistenceSessionContract))]
    public interface IPersistenceSession : IDisposable
    {
        IQueryable<T> Query<T>();
        T Get<T>(object id);
        T TryGet<T>(object id);
        void SaveOrUpdate(object instance);
        void Delete(object instance);
    }

    [ContractClassFor(typeof(IPersistenceSession))]
    internal abstract class PersistenceSessionContract : IPersistenceSession
    {
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public IQueryable<T> Query<T>()
        {
            Contract.Ensures(Contract.Result<IQueryable<T>>() != null);
            throw new NotImplementedException();
        }

        public T Get<T>(object id)
        {
            throw new NotImplementedException();
        }

        public T TryGet<T>(object id)
        {
            throw new NotImplementedException();
        }

        public void SaveOrUpdate(object instance)
        {
            Contract.Requires(instance!=null);
            throw new NotImplementedException();
        }

        public void Delete(object instance)
        {
            Contract.Requires(instance != null);
            throw new NotImplementedException();
        }
    }
}