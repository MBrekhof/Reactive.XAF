using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using Xpand.XAF.Modules.ImportData.BusinessObjects;

namespace Xpand.XAF.Modules.ImportData.Services{
	internal static class ImportExecutionService{

		internal static ImportResult ExecuteSync(
			XafApplication application, ImportParameter parameter, ITypeInfo typeInfo){
			var sw = Stopwatch.StartNew();
			var result = new ImportResult();
			var rows = SpreadsheetParserService.ParseDataRows(parameter);
			result.TotalRows = rows.Count;

			var activeMaps = parameter.FieldMaps
				.Where(m => !m.Skip && !string.IsNullOrEmpty(m.TargetProperty))
				.ToList();

			var batchSize = parameter.BatchSize > 0 ? parameter.BatchSize : 100;
			var objectSpace = application.CreateObjectSpace(typeInfo.Type);

			try{
				for (var i = 0; i < rows.Count; i++){
					try{
						var row = rows[i];
						var obj = ResolveObject(objectSpace, typeInfo, parameter, row, activeMaps, result, i);
						if (obj == null) continue;

						SetPropertyValues(obj, row, activeMaps, typeInfo, result, i);

						if ((i + 1) % batchSize == 0){
							objectSpace.CommitChanges();
						}
					}
					catch (Exception ex){
						result.Errors.Add(new ImportError{
							RowIndex = i + parameter.DataStartRowIndex,
							Message = ex.Message
						});
					}
				}

				objectSpace.CommitChanges();
			}
			finally{
				sw.Stop();
				result.ElapsedSeconds = Math.Round(sw.Elapsed.TotalSeconds, 2);
				result.SuccessCount = result.InsertedCount + result.UpdatedCount;
				result.ErrorCount = result.Errors.Count;
				result.Summary = $"Imported {result.SuccessCount} of {result.TotalRows} rows " +
				                 $"({result.InsertedCount} inserted, {result.UpdatedCount} updated, " +
				                 $"{result.ErrorCount} errors) in {result.ElapsedSeconds}s";
			}

			return result;
		}

		internal static IObservable<ImportResult> Execute(
			XafApplication application, ImportParameter parameter, ITypeInfo typeInfo)
			=> Observable.Defer(() => Observable.Return(ExecuteSync(application, parameter, typeInfo)));

		static object ResolveObject(IObjectSpace objectSpace, ITypeInfo typeInfo,
			ImportParameter parameter, Dictionary<string, object> row,
			List<ImportFieldMap> activeMaps, ImportResult result, int rowIndex){
			switch (parameter.ImportMode){
				case ImportMode.Insert:
					var created = objectSpace.CreateObject(typeInfo.Type);
					result.InsertedCount++;
					return created;

				case ImportMode.Update:{
					var existing = FindExisting(objectSpace, typeInfo, parameter.KeyProperty, row, activeMaps);
					if (existing != null){
						result.UpdatedCount++;
						return existing;
					}
					result.Errors.Add(new ImportError{
						RowIndex = rowIndex + parameter.DataStartRowIndex,
						Message = $"No existing object found for key '{parameter.KeyProperty}'"
					});
					return null;
				}

				case ImportMode.Upsert:{
					var existing = FindExisting(objectSpace, typeInfo, parameter.KeyProperty, row, activeMaps);
					if (existing != null){
						result.UpdatedCount++;
						return existing;
					}
					var newObj = objectSpace.CreateObject(typeInfo.Type);
					result.InsertedCount++;
					return newObj;
				}

				default:
					return objectSpace.CreateObject(typeInfo.Type);
			}
		}

		static object FindExisting(IObjectSpace objectSpace, ITypeInfo typeInfo,
			string keyProperty, Dictionary<string, object> row, List<ImportFieldMap> activeMaps){
			if (string.IsNullOrEmpty(keyProperty)) return null;

			var map = activeMaps.FirstOrDefault(m =>
				string.Equals(m.TargetProperty, keyProperty, StringComparison.OrdinalIgnoreCase));
			if (map == null) return null;

			row.TryGetValue(map.SourceColumn, out var keyValue);
			if (keyValue == null) return null;

			var memberInfo = typeInfo.FindMember(keyProperty);
			if (memberInfo == null) return null;

			var convertedKey = FieldMappingService.ConvertValue(keyValue, memberInfo.MemberType);
			return objectSpace.FindObject(typeInfo.Type, new BinaryOperator(keyProperty, convertedKey));
		}

		static void SetPropertyValues(object obj, Dictionary<string, object> row,
			List<ImportFieldMap> activeMaps, ITypeInfo typeInfo, ImportResult result, int rowIndex){
			foreach (var map in activeMaps){
				try{
					row.TryGetValue(map.SourceColumn, out var rawValue);
					var member = typeInfo.FindMember(map.TargetProperty);
					if (member == null) continue;

					var converted = FieldMappingService.ConvertValue(rawValue, member.MemberType, map.DefaultValue);
					member.SetValue(obj, converted);
				}
				catch (Exception ex){
					result.Errors.Add(new ImportError{
						RowIndex = rowIndex,
						ColumnName = map.SourceColumn,
						RawValue = row.TryGetValue(map.SourceColumn, out var v) ? v?.ToString() : null,
						TargetProperty = map.TargetProperty,
						Message = ex.Message
					});
				}
			}
		}
	}
}
