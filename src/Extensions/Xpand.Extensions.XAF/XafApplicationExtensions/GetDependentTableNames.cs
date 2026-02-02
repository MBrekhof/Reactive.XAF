using System.Collections.Generic;
using System.Data;

namespace Xpand.Extensions.XAF.XafApplicationExtensions {
    public static partial class XafApplicationExtensions {
        public static string[] GetDependentTableNames(this IDbCommand command,string targetTableName) {
            const string sql = @"
        SELECT DISTINCT 
            QUOTENAME(SCHEMA_NAME(t.schema_id)) + '.' + QUOTENAME(t.name)
        FROM sys.foreign_keys fk
        JOIN sys.tables t ON fk.parent_object_id = t.object_id
        WHERE fk.referenced_object_id = OBJECT_ID(@TargetTable)";

            
            command.CommandText = sql;
            command.CommandType = CommandType.Text;

            var param = command.CreateParameter();
            param.ParameterName = "@TargetTable";
            param.Value = targetTableName;
            command.Parameters.Add(param);

            var results = new List<string>();
            using var reader = command.ExecuteReader();
            while (reader.Read()) {
                results.Add(reader.GetString(0));
            }

            return results.ToArray();
        }
    }
}