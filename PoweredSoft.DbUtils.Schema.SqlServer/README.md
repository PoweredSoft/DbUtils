# Getting Started

Install package then.

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