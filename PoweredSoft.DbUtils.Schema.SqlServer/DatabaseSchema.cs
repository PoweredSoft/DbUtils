using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using PoweredSoft.DbUtils.Schema.Core;
using PoweredSoft.DbUtils.Schema.SqlServer.Models;

namespace PoweredSoft.DbUtils.Schema.SqlServer
{
    public class DatabaseSchema : IDatabaseSchema
    {
        public List<Table> SqlServerTables { get; set; } = new List<Table>();
        public List<ITable> Tables => SqlServerTables.Cast<ITable>().ToList();
        private SqlConnection _connection;

        public string ConnectionString { get; set; }

        public void LoadSchema()
        {
            // clear tables.
            SqlServerTables.Clear();

            // connect.
            using (_connection = new SqlConnection(ConnectionString))
            {
                _connection.Open();

                GetTables();
                GetColumns();
                GetPrimaryKeys();
                GetForeignKeys();
                GetIndexes();
            }
        }

        protected void GetIndexes()
        {
            var indexSql = SqlServerShemaQueries.FetchIndexes;

            // command.
            using (var command = new SqlCommand(indexSql, _connection))
            using (var reader = command.ExecuteReader())
            {
                var indexModels = new List<SqlServerIndexModel>();

                // read table names.
                while (reader.Read())
                {
                    var indexModel = new SqlServerIndexModel();
                    indexModel.TableSchemaName = reader["TableSchemaName"] as string;
                    indexModel.TableName = reader["TableName"] as string;
                    indexModel.IndexName = reader["IndexName"] as string;
                    indexModel.ColumnName = reader["ColumnName"] as string;
                    indexModel.IsIncludedColumn = (bool) reader["IsIncludedColumn"];
                    indexModel.IsUniqueConstraint = (bool) reader["IsUniqueConstraint"];
                    indexModel.HasFilter = (bool) reader["HasFilter"];
                    indexModel.FilterDefinition = reader["FilterDefinition"] as string;
                    indexModel.IsDescendingKey = (bool)reader["IsDescendingKey"];
                    indexModel.KeyOrdinal = (Byte)reader["KeyOrdinal"];
                    indexModels.Add(indexModel);
                }

                var indexes = indexModels
                    .GroupBy(t => new
                    {
                        t.TableSchemaName,
                        t.TableName,
                        t.IndexName,
                        t.IsUniqueConstraint,
                        t.HasFilter,
                        t.FilterDefinition
                    })
                    .Select(t =>
                    {
                        var table = SqlServerTables.FirstOrDefault(t2 => t2.Schema == t.Key.TableSchemaName && t2.Name == t.Key.TableName);
                        if (table == null)
                            throw new Exception($"Cannot find table {t.Key.TableSchemaName}.{t.Key.TableName}");

                        var index = new Index();
                        index.SqlServerTable = table;

                        index.SqlServerColumns = t.Where(t2 => !t2.IsIncludedColumn).OrderBy(t2 => t2.KeyOrdinal).Select(t2 =>
                        {
                            var c = table.SqlServerColumns.FirstOrDefault(t3 => t3.Name == t2.ColumnName);
                            if (c == null)
                                throw new Exception($"Cannot find column {t2.ColumnName} in {table}");
                            return c;
;                        }).ToList();

                        index.SqlServerIncludedColumns = t.Where(t2 => t2.IsIncludedColumn).OrderBy(t2 => t2.KeyOrdinal).Select(t2 =>
                        {
                            var c = table.SqlServerColumns.FirstOrDefault(t3 => t3.Name == t2.ColumnName);
                            if (c == null)
                                throw new Exception($"Cannot find column {t2.ColumnName} in {table}");
                            return c;
                        }).ToList();

                        index.IsUnique = t.Key.IsUniqueConstraint;
                        index.FilterDefinition = t.Key.FilterDefinition;
                        index.Name = t.Key.IndexName;
                        return index;
                    })
                    .ToList();

                // add indexes.
                indexes.ForEach(index2 =>
                {
                    index2.SqlServerTable.SqlServerIndexes.Add(index2);
                });
            }
        }

        protected void GetForeignKeys()
        {
            var foreignKeysSql = SqlServerShemaQueries.FetchForeignKeys;

            // command.
            using (var command = new SqlCommand(foreignKeysSql, _connection))
            using (var reader = command.ExecuteReader())
            {
                // read table names.
                while (reader.Read())
                {
                    var name = reader["FKName"] as string;
                    var deleteCascadeAction = reader["DeleteCascadeAction"] as string;
                    var updateCascadeAction = reader["UpdateCascadeAction"] as string;

                    // fk 
                    var fkSchemaName = reader["FKSchema"] as string;
                    var fkTableName = reader["FKTable"] as string;
                    var fkColumnName = reader["FKColumn"] as string;

                    // pk
                    var pkSchemaName = reader["PKSchema"] as string;
                    var pkTableName = reader["PKTable"] as string;
                    var pkColumnName = reader["PKColumn"] as string;

                    // fk table
                    var fkTableInfo = SqlServerTables.FirstOrDefault(t => t.Schema == fkSchemaName && t.Name == fkTableName);
                    var pkTableInfo = SqlServerTables.FirstOrDefault(t => t.Schema == pkSchemaName && t.Name == pkTableName);

                    // if not found just skip (prob means it was filtered somewhere in the drain)
                    if (pkTableInfo == null || fkTableInfo == null)
                        continue;

                    // find the fk column.
                    var fkColumn = fkTableInfo.SqlServerColumns.FirstOrDefault(t => t.Name == fkColumnName);
                    if (fkColumn == null)
                        throw new Exception($"Could not find {fkColumnName} in table {fkSchemaName}.{fkTableName}");

                    // find the pk column 
                    var pkColumn = pkTableInfo.SqlServerColumns.FirstOrDefault(t => t.Name == pkColumnName);
                    if (pkColumn == null)
                        throw new Exception($"Could not find {pkColumnName} in table {pkSchemaName}.{pkColumnName}");

                    // set foreign key reference
                    var fk = new ForeignKey();
                    fk.SqlServerPrimaryKeyColumn = pkColumn;
                    fk.SqlServerForeignKeyColumn = fkColumn;
                    fk.DeleteCascadeAction = deleteCascadeAction;
                    fk.UpdateCascadeAction = updateCascadeAction;
                    fk.Name = name;
                    fkTableInfo.SqlServerForeignKeys.Add(fk);
                }
            }
        }

        protected void GetPrimaryKeys()
        {
            var primaryKeySql = SqlServerShemaQueries.FetchPrimaryKeys;

            using (var command = new SqlCommand(primaryKeySql, _connection))
            using (var reader = command.ExecuteReader())
            {
                // read table names.
                while (reader.Read())
                {
                    // get the keys.
                    var tableSchema = reader["TABLE_SCHEMA"] as string;
                    var tableName = reader["TABLE_NAME"] as string;
                    var columnName = reader["COLUMN_NAME"] as string;

                    // find the table.
                    var table = SqlServerTables.FirstOrDefault(t => t.Schema == tableSchema && t.Name == tableName);
                    if (table == null)
                        continue;

                    // find the column
                    var column = table.SqlServerColumns.FirstOrDefault(t => t.Name == columnName);
                    if (column == null)
                        throw new Exception($"could not find {columnName} inside table {tableSchema}.{tableName}");

                    column.IsPrimaryKey = true;
                }
            }
        }

        protected void GetColumns()
        {
            var columnsSql = SqlServerShemaQueries.FetchColumns;

            // command.
            using (var command = new SqlCommand(columnsSql, _connection))
            using (var reader = command.ExecuteReader())
            {
                // table and schema name.
                string tableName;
                string schemaName;
                Table table = null;

                // read table names.
                while (reader.Read())
                {
                    // set schema and table name.
                    schemaName = reader["TABLE_SCHEMA"] as string;
                    tableName = reader["TABLE_NAME"] as string;

                    // find table.
                    table = SqlServerTables.FirstOrDefault(t => t.Schema == schemaName && t.Name == tableName);
                    if (table == null)
                        continue;

                    // set dynamic stuff.
                    var column = new Column();
                    column.SqlServerTable = table;
                    column.IsNullable = (reader["IS_NULLABLE"] as string) == "YES";
                    column.DataType = reader["DATA_TYPE"] as string;
                    column.Name = reader["COLUMN_NAME"] as string;
                    column.IsAutoIncrement = reader["IS_IDENTITY"] is DBNull ? false : Convert.ToBoolean(reader["IS_IDENTITY"]);

               

                    // default.
                    if (false == (reader["COLUMN_DEFAULT"] is DBNull))
                        column.DefaultValue = reader["COLUMN_DEFAULT"] as string;

                    // max char size.
                    if (false == (reader["CHARACTER_MAXIMUM_LENGTH"] is DBNull))
                        column.CharacterMaximumLength = reader["CHARACTER_MAXIMUM_LENGTH"] as int?;

                    // numeric precision stuff.
                    if (false == (reader["NUMERIC_PRECISION"] is DBNull))
                        column.NumericPrecision = Convert.ToByte(reader["NUMERIC_PRECISION"]);

                    if (false == (reader["NUMERIC_SCALE"] is DBNull))
                        column.NumericScale = Convert.ToByte(reader["NUMERIC_SCALE"]);

                    if (false == (reader["DATETIME_PRECISION"] is DBNull))
                        column.DateTimePrecision = Convert.ToByte(reader["DATETIME_PRECISION"]);

                    // add the column
                    table.SqlServerColumns.Add(column);
                }
            }
        }

        protected void GetTables()
        {
            var tablesSql = SqlServerShemaQueries.FetchTables;

            // command.
            using (var command = new SqlCommand(tablesSql, _connection))
            using (var reader = command.ExecuteReader())
            {
                // read table names.
                while (reader.Read())
                {
                    // create a new table.
                    var t = new Table();
                    t.Schema = reader["TABLE_SCHEMA"] as string;
                    t.Name = reader["TABLE_NAME"] as string;
                    SqlServerTables.Add(t);
                }
            }
        }
    }
}
