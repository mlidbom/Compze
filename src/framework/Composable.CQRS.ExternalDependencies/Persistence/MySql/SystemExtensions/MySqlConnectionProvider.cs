using System;
using System.Data;
using MySql.Data.MySqlClient;
using System.Transactions;

namespace Composable.Persistence.MySql.SystemExtensions
{
    class MySqlConnectionProvider : IMySqlConnectionProvider
    {
        public string ConnectionString { get; }
        public MySqlConnectionProvider(string connectionString) => ConnectionString = connectionString;

        public MySqlConnection OpenConnection()
        {
            var transactionInformationDistributedIdentifierBefore = Transaction.Current?.TransactionInformation.DistributedIdentifier;
            var connection = GetConnectionFromPool();

            if(transactionInformationDistributedIdentifierBefore != null && transactionInformationDistributedIdentifierBefore.Value == Guid.Empty)
            {
                if(Transaction.Current!.TransactionInformation.DistributedIdentifier != Guid.Empty)
                {
                    throw new Exception("Opening connection escalated transaction to distributed. For now this is disallowed");
                }
            }
            return connection;
        }

        //Urgent: Since the MySql connection pooling is way slow we should do something about that here. Something like using Task to keep a pool of open connections on hand.
        MySqlConnection GetConnectionFromPool()
        {
            var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            return connection;
        }
    }

    static class MySqlDataReaderExtensions
    {
        public static void ForEachSuccessfulRead(this MySqlDataReader @this, Action<MySqlDataReader> forEach)
        {
            while(@this.Read()) forEach(@this);
        }
    }

    static class MySqlCommandParameterExtensions
    {
        public static MySqlCommand AddParameter(this MySqlCommand @this, string name, int value) => AddParameter(@this, name, MySqlDbType.Int32, value);
        public static MySqlCommand AddParameter(this MySqlCommand @this, string name, Guid value) => AddParameter(@this, name, MySqlDbType.Guid, value);
        public static MySqlCommand AddDateTime2Parameter(this MySqlCommand @this, string name, DateTime value) => AddParameter(@this, name, MySqlDbType.DateTime, value);
        public static MySqlCommand AddVarcharParameter(this MySqlCommand @this, string name, int length, string value) => AddParameter(@this, name, MySqlDbType.VarString, value, length);

        public static MySqlCommand AddParameter(this MySqlCommand @this, MySqlParameter parameter)
        {
            @this.Parameters.Add(parameter);
            return @this;
        }

        static MySqlCommand AddParameter(MySqlCommand @this, string name, MySqlDbType type, object value, int length) => @this.AddParameter(new MySqlParameter(name, type, length) {Value = value});

        public static MySqlCommand AddParameter(this MySqlCommand @this, string name, MySqlDbType type, object value) => @this.AddParameter(new MySqlParameter(name, type) {Value = value});

        public static MySqlCommand AddNullableParameter(this MySqlCommand @this, string name, MySqlDbType type, object? value) => @this.AddParameter(Nullable(new MySqlParameter(name, type) {Value = value}));

        static MySqlParameter Nullable(MySqlParameter @this)
        {
            @this.IsNullable = true;
            @this.Direction = ParameterDirection.Input;
            if(@this.Value == null)
            {
                @this.Value = DBNull.Value;
            }
            return @this;
        }
    }

    interface IMySqlConnectionProvider
    {
        MySqlConnection OpenConnection();
        string ConnectionString { get; }
    }
}
