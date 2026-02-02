using System.Data;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using Fasterflect;
using HarmonyLib;
using Xpand.Extensions.XAF.ObjectSpaceExtensions;

namespace Xpand.Extensions.XAF.XafApplicationExtensions {
    public static partial class XafApplicationExtensions {

        public static void ForceDropTable(this ModuleUpdater moduleUpdater, string tableName) {
            using var dbCommand = ((IObjectSpace)moduleUpdater.GetPropertyValue("ObjectSpace")).CreateCommand();
            dbCommand?.ForceDropTable(tableName);
        }

        public static void ForceDropTable(this IDbCommand command, string tableName) {
            var dependentTableNames = command.GetDependentTableNames(tableName);
            command.DropTables(dependentTableNames.AddToArray(tableName));
        }

        private static void DropTables(this IDbCommand command, params string[] tableNames){
            const string sql = @"
    DECLARE @DependentsSql NVARCHAR(MAX) = N'';

    SELECT @DependentsSql += 'ALTER TABLE ' + 
        QUOTENAME(SCHEMA_NAME(fk.schema_id)) + '.' + QUOTENAME(OBJECT_NAME(fk.parent_object_id)) + 
        ' DROP CONSTRAINT ' + QUOTENAME(fk.name) + ';' + CHAR(13)
    FROM sys.foreign_keys fk
    WHERE referenced_object_id = OBJECT_ID(@ObjName);
    IF LEN(@DependentsSql) > 0
    BEGIN
        EXEC sp_executesql @DependentsSql;
    END
    IF OBJECT_ID(@ObjName) IS NOT NULL
    BEGIN
        DECLARE @DropSql NVARCHAR(MAX) = 'DROP TABLE ' + @ObjName;
        EXEC sp_executesql @DropSql;
    END";

            command.CommandText = sql;
            command.CommandType = CommandType.Text;
            command.Parameters.Clear();

            var param = command.CreateParameter();
            param.ParameterName = "@ObjName";
            command.Parameters.Add(param);

            foreach (var tableName in tableNames) {
                param.Value = tableName;
                command.ExecuteNonQuery();
            }
            
            command.CommandText = sql;
            command.CommandType = CommandType.Text;

            param = command.CreateParameter();
            param.ParameterName = "@TableName";
            command.Parameters.Add(param);

            foreach (var tableName in tableNames)
            {
                param.Value = tableName;
                command.ExecuteNonQuery();
            }
        }
    }
}