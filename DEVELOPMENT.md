# Development Setup Guide

## Prerequisites

- **Visual Studio 2022**, **JetBrains Rider** or **Visual Studio Code**, 

### Optional: External Database Servers
If you want to test against real database servers (recommended for comprehensive testing):
- Microsoft SQL Server
- PostgreSQL  
- MySQL

## Initial Setup

### 1. Configure Test Database Combinations

In the root of the project:

1. **Optional**: Copy `TestUsingPluggableComponentCombinations.example` to `TestUsingPluggableComponentCombinations`
   - If you don't create this file, it will be automatically created from the example during the first build
   - **By default, tests run using SQLite in-memory** with no required configuration.
2. Edit this file to configure which database servers the tests run against
3. To test against external databases, uncomment the desired combinations in the config file

### 2. Open the Solution

Open `src/Compze.AllProjects.slnx` in Visual Studio 2022 or Rider.


### Optional: External Database Servers

If you want to test against external database servers, you may need to configure connection strings.

#### Default Connection String for SQL Server

By default, if SQL Server tests are enabled, they use this connection string:

```
Data Source=localhost;Initial Catalog=master;Integrated Security=True;TrustServerCertificate=True;
```

If this connection string works for you, you're good to go!

#### Custom Connection Strings

If you need to use different connection strings, set these environment variables:

#### SQL Server
```
COMPOSABLE_SQL_SERVER_DATABASE_POOL_MASTER_CONNECTIONSTRING
```

#### PostgreSQL
```
COMPOSABLE_PGSQL_DATABASE_POOL_MASTER_CONNECTIONSTRING
```

#### MySQL
```
COMPOSABLE_MYSQL_DATABASE_POOL_MASTER_CONNECTIONSTRING
```

### Test Databases

⚠️ **Important:** Running the tests will create several databases on your SQL server with the prefix: `Compze_DatabasePool_`

These databases are used by the test database pool and will be automatically managed by the test infrastructure.

## Running Tests

### Performance Tuning

If tests are failing due to performance expectations on your machine, you can adjust the performance threshold using the `COMPOSABLE_MACHINE_SLOWNESS` environment variable.

#### COMPOSABLE_MACHINE_SLOWNESS

This environment variable lets you adjust the expectations for the performance tests.

**Example:** If you set it to `2.0`, performance tests are allowed to take 2.0 times as long to complete without failing.


### Test Configuration Options

Edit `TestUsingPluggableComponentCombinations` to control:
- Which persistence layers to test (SQLite in-memory by default, can enable SQL Server, PostgreSQL, MySQL, file-based SQLite)
- Which dependency injection containers to test (Microsoft DI, Autofac)
- Other pluggable component combinations

## Common Issues

### Quick Start - Just Want to Code?
1. Clone the repository
2. Open the solution
3. Run tests
4. Start coding!

### Database Connection Failures

If tests fail with database connection errors
1. Verify your SQL server is running
2. Check that the connection string is correct
3. Ensure you have appropriate permissions to create databases
4. **Quick fix:** Switch back to SQLite in-memory by editing `TestUsingPluggableComponentCombinations` to only enable `SqliteMemory:Microsoft`

### Performance Test Failures

If performance tests are failing:

1. Set `COMPOSABLE_MACHINE_SLOWNESS` to a higher value (e.g., `2.0` or `3.0`)
2. Close other resource-intensive applications
3. Check if background processes are affecting performance


## Contributing Documentation

The documentation website is in the `Website/` directory and uses DocFX.

See `Website/README.md` for instructions on building and running the documentation locally.

## Additional Resources

- [Main README](README.md)
- [Project Website](http://compze.net/)
