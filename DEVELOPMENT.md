# Development Setup Guide

This guide covers how to set up your development environment for working on Compze.

## Prerequisites

- **Visual Studio 2022** or **JetBrains Rider** (recommended IDEs)
- **Administrator access to a SQL database server** (optional, but recommended)
  - Microsoft SQL Server (default)
  - PostgreSQL
  - MySQL

## Initial Setup

### 1. Configure Test Database Combinations

In the root of the project:

1. Copy `TestUsingPluggableComponentCombinations.example` to `TestUsingPluggableComponentCombinations`
2. Edit this file to configure which database servers the tests run against
3. If you don't have any SQL server, configure it to use only the `Memory` `PersistenceLayer`

### 2. Open the Solution

Open `src/Compze.Everything.sln` in Visual Studio 2022 or Rider.

## Database Configuration

### Default Connection String

By default, the tests use this connection string for SQL Server:

```
Data Source=localhost;Initial Catalog=master;Integrated Security=True;TrustServerCertificate=True;
```

If this connection string works for you, you're good to go!

### Custom Connection Strings

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

```powershell
# PowerShell
$env:COMPOSABLE_MACHINE_SLOWNESS = "2.0"
```

```bash
# Bash
export COMPOSABLE_MACHINE_SLOWNESS=2.0
```

### Test Configuration Options

Edit `TestUsingPluggableComponentCombinations` to control:
- Which persistence layers to test (Memory, SQL Server, PostgreSQL, MySQL)
- Which dependency injection containers to test
- Other pluggable component combinations

## Common Issues

### Database Connection Failures

If tests fail with database connection errors:

1. Verify your SQL server is running
2. Check that the connection string is correct
3. Ensure you have appropriate permissions to create databases
4. Consider using the in-memory persistence layer for local development

### Performance Test Failures

If performance tests are failing:

1. Set `COMPOSABLE_MACHINE_SLOWNESS` to a higher value (e.g., `2.0` or `3.0`)
2. Close other resource-intensive applications
3. Check if background processes are affecting performance

### Missing Dependencies

Make sure all NuGet packages are restored:

```powershell
dotnet restore src/Compze.Everything.sln
```

## Project Structure

The solution is organized as follows:

```
src/
├── Compze.Everything.sln           # Main solution file
├── framework/                      # Core framework projects
│   ├── Compze.Abstractions/
│   ├── Compze.EventStore/
│   ├── Compze.DocumentDb/
│   └── ...
├── Samples/                        # Example projects
└── tests/                          # Test projects
```

## Building the Documentation

The documentation website is in the `Website/` directory and uses DocFX.

See `Website/README.md` for instructions on building and running the documentation locally.

## Additional Resources

- [Main README](README.md)
- [Project Website](http://compze.net/)
- [Gitter Chat](https://gitter.im/Composable4/Lobby)
- [Skype Chat](https://join.skype.com/awyeJlk3rVbu)

## Getting Help

If you run into issues:

1. Check this guide first
2. Search existing issues on GitHub
3. Join our [Gitter chat](https://gitter.im/Composable4/Lobby) for community support
4. Open a new issue with details about your problem
