# Composable

[Project site](http://composabletk.net/)

[![Gitter](https://badges.gitter.im/Composable4/Lobby.svg)](https://gitter.im/Composable4/Lobby?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

[Skype Chat](https://join.skype.com/awyeJlk3rVbu)


# Set up development environment
In the root of the project:
* Copy `TestUsingPluggableComponentCombinations.example` to `TestUsingPluggableComponentCombinations`
* Open Composable.Everything.sln in Visual Studio 2022 or Rider.

## Tests
* You need administrator access to a Sql database server. By default, Microsoft Sql Server 
  * To change which database servers the tests run against, edit `TestUsingPluggableComponentCombinations` in the project root
* If this connection string is valid you're good to go, otherwise set the environment variable below:
  `Data Source=localhost;Initial Catalog=master;Integrated Security=True;TrustServerCertificate=True;`

Note: Running the tests will create a bunch of pool databases on the server. Don't be shocked when you see 10 or 20 databases on your sql :)

### Environment variables you should know about when running the tests

#### Connection strings:
Lets you override the connection string to use for the database pools.

**COMPOSABLE_SQL_SERVER_DATABASE_POOL_MASTER_CONNECTIONSTRING**
**COMPOSABLE_PGSQL_DATABASE_POOL_MASTER_CONNECTIONSTRING**  
**COMPOSABLE_MYSQL_DATABASE_POOL_MASTER_CONNECTIONSTRING**  
**COMPOSABLE_DB2_DATABASE_POOL_MASTER_CONNECTIONSTRING**  
**COMPOSABLE_ORACLE_DATABASE_POOL_MASTER_CONNECTIONSTRING**

###


### Performance
**COMPOSABLE_MACHINE_SLOWNESS**: 
Lets you adjust the expectations for the performance tests.  
For example: If you set it to 2.0 performance tests are allowed to take 2.0 times as long to complete without failing.

**COMPOSABLE_TEMP_DRIVE**:
Lets you move where temp data is stored out of the default system temp folder. 
Among other things the databases in the database pool are stored here.

### Running the sample project
* The connection string AccountManagement in AccountManagement.Server/App.config must be valid.
* The configured user must have full permissions to create tables etc in the database in the connection string.
* Given that just "Start debugging" or "Start without debugging" in visual studio.