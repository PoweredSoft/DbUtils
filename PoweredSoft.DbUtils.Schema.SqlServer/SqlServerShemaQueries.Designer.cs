﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace PoweredSoft.DbUtils.Schema.SqlServer {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "15.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class SqlServerShemaQueries {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal SqlServerShemaQueries() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("PoweredSoft.DbUtils.Schema.SqlServer.SqlServerShemaQueries", typeof(SqlServerShemaQueries).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SELECT 
        ///	C.*,
        ///	COLUMNPROPERTY(object_id(QUOTENAME(T.TABLE_SCHEMA) + &apos;.&apos; + QUOTENAME(T.TABLE_NAME)), COLUMN_NAME, &apos;IsIdentity&apos;) IS_IDENTITY
        ///FROM
        ///	INFORMATION_SCHEMA.COLUMNS C
        ///INNER JOIN	
        ///	INFORMATION_SCHEMA.TABLES T
        ///ON
        ///	T.TABLE_NAME = C.TABLE_NAME
        ///	AND
        ///	T.TABLE_SCHEMA = C.TABLE_SCHEMA
        ///WHERE 
        ///	T.TABLE_TYPE = &apos;BASE TABLE&apos;
        ///ORDER BY
        ///	C.TABLE_SCHEMA, 
        ///	C.TABLE_NAME, 
        ///	C.ORDINAL_POSITION.
        /// </summary>
        internal static string FetchColumns {
            get {
                return ResourceManager.GetString("FetchColumns", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SELECT
        ///	FK.name AS FKName,
        ///	OBJECT_SCHEMA_NAME(FKCOL.object_id) AS FKSchema,
        ///	OBJECT_NAME(FKCOL.object_id) AS FKTable,
        ///	FKCOL.name AS FKColumn,
        ///	OBJECT_SCHEMA_NAME(PKCOL.object_id) AS PKSchema,
        ///	OBJECT_NAME(PKCOL.object_id) AS PKTable,
        ///	PKCOL.name AS PKColumn,
        ///	FK.delete_referential_action_desc As DeleteCascadeAction,
        ///	FK.update_referential_action_desc As UpdateCascadeAction
        ///FROM sys.foreign_keys FK
        ///INNER JOIN sys.foreign_key_columns FKC ON FKC.constraint_object_id = FK.object_id
        ///INNER JOIN sys. [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string FetchForeignKeys {
            get {
                return ResourceManager.GetString("FetchForeignKeys", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SELECT 
        ///     s.name as TableSchemaName,
        ///     t.[name] As TableName,
        ///     ind.name as IndexName,
        ///     col.name  As ColumnName,
        ///	 ic.is_included_column As IsIncludedColumn,
        ///	 ind.is_unique_constraint As IsUniqueConstraint,
        ///	 ind.has_filter As HasFilter,
        ///	 ind.filter_definition As FilterDefinition,
        ///	 ic.is_descending_key As IsDescendingKey,
        ///	 ic.key_ordinal As KeyOrdinal
        ///FROM 
        ///     sys.indexes ind 
        ///INNER JOIN 
        ///     sys.index_columns ic ON  ind.object_id = ic.object_id and ind.index_id = ic.index_ [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string FetchIndexes {
            get {
                return ResourceManager.GetString("FetchIndexes", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SELECT
        ///	TC.CONSTRAINT_SCHEMA,
        ///	TC.CONSTRAINT_NAME,
        ///	TC.TABLE_SCHEMA,
        ///	TC.TABLE_NAME,
        ///	KCU.COLUMN_NAME,
        ///	KCU.ORDINAL_POSITION
        ///
        ///FROM
        ///	INFORMATION_SCHEMA.TABLE_CONSTRAINTS TC
        ///INNER JOIN
        ///	INFORMATION_SCHEMA.KEY_COLUMN_USAGE KCU
        ///ON
        ///	KCU.CONSTRAINT_SCHEMA = TC.CONSTRAINT_SCHEMA AND KCU.CONSTRAINT_NAME = TC.CONSTRAINT_NAME
        ///WHERE
        ///	TC.CONSTRAINT_TYPE= &apos;PRIMARY KEY&apos;
        ///ORDER BY
        ///	TC.TABLE_SCHEMA,
        ///	TC.TABLE_NAME,
        ///	KCU.ORDINAL_POSITION.
        /// </summary>
        internal static string FetchPrimaryKeys {
            get {
                return ResourceManager.GetString("FetchPrimaryKeys", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SELECT * FROM INFORMATION_SCHEMA.SEQUENCES.
        /// </summary>
        internal static string FetchSequences {
            get {
                return ResourceManager.GetString("FetchSequences", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SELECT 
        ///	*
        ///FROM
        ///	INFORMATION_SCHEMA.TABLES T
        ///WHERE 
        ///	T.TABLE_TYPE = &apos;Base Table&apos;
        ///ORDER BY 
        ///                  T.TABLE_SCHEMA, T.TABLE_NAME.
        /// </summary>
        internal static string FetchTables {
            get {
                return ResourceManager.GetString("FetchTables", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SELECT SERVERPROPERTY (&apos;productversion&apos;).
        /// </summary>
        internal static string FetchVersion {
            get {
                return ResourceManager.GetString("FetchVersion", resourceCulture);
            }
        }
    }
}
