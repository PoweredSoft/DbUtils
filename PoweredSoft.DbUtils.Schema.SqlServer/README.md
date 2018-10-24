# Getting Started

## Download
Full Version | NuGet | NuGet Install
------------ | :-------------: | :-------------:
PoweredSoft.DbUtils.Schema.SqlServer | <a href="https://www.nuget.org/packages/PoweredSoft.DbUtils.Schema.SqlServer/" target="_blank">[![NuGet](https://img.shields.io/nuget/v/PoweredSoft.DbUtils.Schema.SqlServer.svg?style=flat-square&label=nuget)](https://www.nuget.org/packages/PoweredSoft.DynamicLinq/)</a> | ```PM> Install-Package PoweredSoft.DbUtils.Schema.SqlServer```

## How to use

```csharp
var schema = new DatabaseSchema();
schema.ConnectionString = "your connection string";
schema.LoadSchema();

schema.Tables.ForEach(table => 
{
	var tableName = table.Name;
	var schemaName = table.GetTableSchema();
});
```
