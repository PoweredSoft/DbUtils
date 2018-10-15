using System;
using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;
using PoweredSoft.DbUtils.Schema.Core;
using PoweredSoft.DbUtils.Schema.MySql.Models;

namespace PoweredSoft.DbUtils.Schema.MySql
{
    public class DatabaseSchema : IDatabaseSchema
    {
        public List<Table> MySqlTables { get; } = new List<Table>();
        public List<ITable> Tables => MySqlTables.Cast<ITable>().ToList();
        public List<ISequence> Sequences => new List<ISequence>();

        private MySqlConnection _connection;
        public string ConnectionString { get; set; }

        private string DatabaseName => _connection.Database;

        public void LoadSchema()
        {
            // clear 
            MySqlTables.Clear();

            // connect & collect
            using (_connection = new MySqlConnection(ConnectionString))
            {
                _connection.Open();

                GetTables();
                GetColumns();
                GetPrimaryKeys();
                GetForeignKeys();
                GetIndexes();
                GetSequences();
            }
        }

        private int GetMajorVersion()
        {
            var ret = _connection.ServerVersion;
            return int.Parse(ret);
        }

        private void GetSequences()
        {
            // no sequences in MySQL
        }

        protected void GetIndexes()
        {
            var indexSql = MySqlSchemaQueries.FetchIndexes;

            // command.
            using (var command = new MySqlCommand(indexSql, _connection))
            {
                command.Parameters.AddWithValue("@DatabaseName", DatabaseName);
                using (var reader = command.ExecuteReader())
                {
                    var indexModels = new List<MySqlIndexModel>();

                    // read table names.
                    while (reader.Read())
                    {
                        var indexModel = new MySqlIndexModel();
                        indexModel.TableName = reader["TableName"] as string;
                        indexModel.IndexName = reader["IndexName"] as string;
                        indexModel.ColumnName = reader["ColumnName"] as string;
                        indexModel.IsUniqueConstraint = (long) reader["IsUniqueConstraint"] != 0;
                        indexModel.KeyOrdinal = (Byte) (UInt32)reader["KeyOrdinal"];
                        indexModels.Add(indexModel);
                    }

                    var indexes = indexModels
                        .GroupBy(t => new
                        {
                            t.TableName,
                            t.IndexName,
                            t.IsUniqueConstraint
                        })
                        .Select(t =>
                        {
                            var table = MySqlTables.FirstOrDefault(t2 => t2.Name == t.Key.TableName);
                            if (table == null)
                                throw new Exception($"Cannot find table {t.Key.TableName}");

                            var index = new Index();
                            index.MySqlTable = table;

                            index.MySqlColumns = t.OrderBy(t2 => t2.KeyOrdinal).Select(t2 =>
                            {
                                var c = table.MySqlColumns.FirstOrDefault(t3 => t3.Name == t2.ColumnName);
                                if (c == null)
                                    throw new Exception($"Cannot find column {t2.ColumnName} in {table}");
                                return c;
                                ;
                            }).ToList();

                            index.IsUnique = t.Key.IsUniqueConstraint;
                            index.Name = t.Key.IndexName;
                            return index;
                        })
                        .ToList();

                    // add indexes.
                    indexes.ForEach(index2 => { index2.MySqlTable.MySqlIndexes.Add(index2); });
                }
            }
        }

        protected void GetForeignKeys()
        {
            var foreignKeysSql = MySqlSchemaQueries.FetchForeignKeys;

            // command.
            using (var command = new MySqlCommand(foreignKeysSql, _connection))
            {
                command.Parameters.AddWithValue("@DatabaseName", DatabaseName);
                using (var reader = command.ExecuteReader())
                {
                    // read table names.
                    while (reader.Read())
                    {
                        var name = reader["FKName"] as string;
                        var deleteCascadeAction = reader["DeleteCascadeAction"] as string;
                        var updateCascadeAction = reader["UpdateCascadeAction"] as string;

                        // fk 
                        var fkTableName = reader["FKTable"] as string;
                        var fkColumnName = reader["FKColumn"] as string;

                        // pk
                        var pkTableName = reader["PKTable"] as string;
                        var pkColumnName = reader["PKColumn"] as string;

                        // fk table
                        var fkTableInfo = MySqlTables.FirstOrDefault(t => t.Name == fkTableName);
                        var pkTableInfo = MySqlTables.FirstOrDefault(t => t.Name == pkTableName);

                        // if not found just skip (prob means it was filtered somewhere in the drain)
                        if (pkTableInfo == null || fkTableInfo == null)
                            continue;

                        // find the fk column.
                        var fkColumn = fkTableInfo.MySqlColumns.FirstOrDefault(t => t.Name.Equals(fkColumnName, StringComparison.InvariantCultureIgnoreCase));
                        if (fkColumn == null)
                            throw new Exception($"Could not find {fkColumnName} in table {fkTableName}");

                        // find the pk column 
                        var pkColumn = pkTableInfo.MySqlColumns.FirstOrDefault(t => t.Name.Equals(pkColumnName, StringComparison.InvariantCultureIgnoreCase));
                        if (pkColumn == null)
                            throw new Exception(
                                $"Could not find {pkColumnName} in table {pkColumnName}");

                        // set foreign key reference
                        var fk = new ForeignKey();
                        fk.MySqlPrimaryKeyColumn = pkColumn;
                        fk.MySqlForeignKeyColumn = fkColumn;
                        fk.DeleteCascadeAction = deleteCascadeAction;
                        fk.UpdateCascadeAction = updateCascadeAction;
                        fk.Name = name;
                        fkTableInfo.MySqlForeignKeys.Add(fk);
                    }
                }
            }
        }

        protected void GetPrimaryKeys()
        {
            var primaryKeySql = MySqlSchemaQueries.FetchPrimaryKeys;

            using (var command = new MySqlCommand(primaryKeySql, _connection))
            {
                command.Parameters.AddWithValue("@DatabaseName", DatabaseName);
                using (var reader = command.ExecuteReader())
                {
                    // read table names.
                    while (reader.Read())
                    {
                        // get the keys.
                        var tableName = reader["TABLE_NAME"] as string;
                        var columnName = reader["COLUMN_NAME"] as string;
                        int ordinalPosition = (int)(uint)reader["ORDINAL_POSITION"];

                        // find the table.
                        var table = MySqlTables.FirstOrDefault(t => t.Name == tableName);
                        if (table == null)
                            continue;

                        // find the column
                        var column = table.MySqlColumns.FirstOrDefault(t => t.Name == columnName);
                        if (column == null)
                            throw new Exception($"could not find {columnName} inside table {tableName}");

                        column.IsPrimaryKey = true;
                        column.PrimaryKeyOrder = ordinalPosition;
                    }
                }
            }
        }

        protected void GetColumns()
        {
            var columnsSql = MySqlSchemaQueries.FetchColumns;

            // command.
            using (var command = new MySqlCommand(columnsSql, _connection))
            {
                command.Parameters.AddWithValue("@DatabaseName", DatabaseName);
                using (var reader = command.ExecuteReader())
                {
                    // table and schema name.
                    string tableName;
                    Table table = null;

                    // read table names.
                    while (reader.Read())
                    {
                        tableName = reader["TABLE_NAME"] as string;

                        // find table.
                        table = MySqlTables.FirstOrDefault(t => t.Name == tableName);
                        if (table == null)
                            continue;

                        // set dynamic stuff.
                        var column = new Column();
                        column.MySqlTable = table;
                        column.IsNullable = (reader["IS_NULLABLE"] as string) == "YES";
                        column.DataType = reader["DATA_TYPE"] as string;
                        column.Name = reader["COLUMN_NAME"] as string;
                        column.IsAutoIncrement = reader["IS_AUTO_INCREMENT"] is DBNull ? false : Convert.ToBoolean(reader["IS_AUTO_INCREMENT"]);



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
                        table.MySqlColumns.Add(column);
                    }
                }
            }
        }

        protected void GetTables()
        {
            var tablesSql = MySqlSchemaQueries.FetchTables;

            // command.
            using (var command = new MySqlCommand(tablesSql, _connection))
            {
                command.Parameters.AddWithValue("@DatabaseName", DatabaseName);
                command.Prepare();

                using (var reader = command.ExecuteReader())
                {
                    // read table names.
                    while (reader.Read())
                    {
                        // create a new table.
                        var t = new Table();
                        t.DatabaseSchema = this;
                        t.Name = reader["TABLE_NAME"] as string;
                        MySqlTables.Add(t);
                    }
                }
            }
        }
    }
}
