# Compze

[Project site](http://compze.net/)

[![Gitter](https://badges.gitter.im/Composable4/Lobby.svg)](https://gitter.im/Composable4/Lobby?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

[Skype Chat](https://join.skype.com/awyeJlk3rVbu)


# Set up development environment
In the root of the project:
* Copy `TestUsingPluggableComponentCombinations.example` to `TestUsingPluggableComponentCombinations`
* Open Compze.Everything.sln in Visual Studio 2022 or Rider.

## Tests
* Preferably you should have administrator access to a SQL database server. By default, Microsoft SQL Server 
  * To change which database servers the tests run against, edit `TestUsingPluggableComponentCombinations` in the project root
* If this connection string is valid you're good to go, otherwise set the environment variable below:
  `Data Source=localhost;Initial Catalog=master;Integrated Security=True;TrustServerCertificate=True;`

>Running the tests will create several databases on your SQL server prefixed: `Compze_DatabasePool_`.  

>If you don't have any Sql server, configure `TestUsingPluggableComponentCombinations` to use only the `Memory` `PersistenceLayer`.
 
### Environment variables you should know about when running the tests

#### Connection strings:
Sets the connection string to use for the database pools that the tests use.

**COMPOSABLE_SQL_SERVER_DATABASE_POOL_MASTER_CONNECTIONSTRING**
**COMPOSABLE_PGSQL_DATABASE_POOL_MASTER_CONNECTIONSTRING**  
**COMPOSABLE_MYSQL_DATABASE_POOL_MASTER_CONNECTIONSTRING**  
**COMPOSABLE_DB2_DATABASE_POOL_MASTER_CONNECTIONSTRING**  
**COMPOSABLE_ORACLE_DATABASE_POOL_MASTER_CONNECTIONSTRING**


### Performance
**COMPOSABLE_MACHINE_SLOWNESS**: 
Lets you adjust the expectations for the performance tests.  
For example: If you set it to 2.0 performance tests are allowed to take 2.0 times as long to complete without failing.