using System;
using System.Data;
using System.Threading.Tasks;
using System.Transactions;
using Composable.Contracts;
using Composable.Persistence.Common;
using Composable.SystemCE.LinqCE;
using Composable.SystemCE.ThreadingCE;
using Composable.SystemCE.TransactionsCE;
using IBM.Data.DB2.Core;

namespace Composable.Persistence.DB2
{
    class ComposableDB2Connection : IEnlistmentNotification, IPoolableConnection, IComposableDbConnection<DB2Command>
    {
        DB2Transaction? _db2Transaction;

        IDbConnection IComposableDbConnection.Connection => Connection;
        public DB2Connection Connection { get; }

        public ComposableDB2Connection(string connectionString) => Connection = new DB2Connection(connectionString);

        public void Open()
        {
            Connection.Open();
            EnsureParticipatingInAnyTransaction();
        }

        public async Task OpenAsync()
        {
            await Connection.OpenAsync().NoMarshalling();
            EnsureParticipatingInAnyTransaction();
        }

        internal static ComposableDB2Connection Create(string connString) => new ComposableDB2Connection(connString);

        async Task IPoolableConnection.OpenAsyncFlex(AsyncMode syncOrAsync) =>
            await syncOrAsync.Run(
                                  () => Connection.Open(),
                                  () => Connection.OpenAsync())
                             .NoMarshalling();

        IDbCommand IComposableDbConnection.CreateCommand() => CreateCommand();

        public DB2Command CreateCommand()
        {
            Assert.State.Assert(Connection.IsOpen);
            EnsureParticipatingInAnyTransaction();
            return Connection.CreateCommand().Mutate(@this => @this.Transaction = _db2Transaction);
        }

        public void Dispose()
        {
            Connection.Dispose();
            _db2Transaction?.Dispose();
        }

        public ValueTask DisposeAsync() => Connection.DisposeAsync();

        void IEnlistmentNotification.Commit(Enlistment enlistment)
        {
            _db2Transaction!.Commit();
            DoneWithTransaction(enlistment);
        }

        void IEnlistmentNotification.Rollback(Enlistment enlistment)
        {
            _db2Transaction!.Rollback();
            DoneWithTransaction(enlistment);
        }

        void IEnlistmentNotification.InDoubt(Enlistment enlistment) => DoneWithTransaction(enlistment);

        void IEnlistmentNotification.Prepare(PreparingEnlistment preparingEnlistment) => preparingEnlistment.Prepared();

        void DoneWithTransaction(Enlistment enlistment)
        {
            _db2Transaction?.Dispose();
            _db2Transaction = null;
            _participatingIn = null;
            enlistment.Done();
        }


        Transaction? _participatingIn;
        void EnsureParticipatingInAnyTransaction()
        {
            var ambientTransaction = Transaction.Current;
            if(ambientTransaction != null)
            {
                if(_participatingIn == null)
                {
                    _participatingIn = ambientTransaction;
                    ambientTransaction.EnlistVolatile(this, EnlistmentOptions.EnlistDuringPrepareRequired);
                    _db2Transaction = Connection.BeginTransaction(Transaction.Current.IsolationLevel.ToDataIsolationLevel());
                }
                else if(_participatingIn != ambientTransaction)
                {
                    throw new Exception($"Somehow switched to a new transaction. Original: {_participatingIn.TransactionInformation.LocalIdentifier} new: {ambientTransaction.TransactionInformation.LocalIdentifier}");
                }
            }
        }
    }
}

