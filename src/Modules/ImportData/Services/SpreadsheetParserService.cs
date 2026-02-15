using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DevExpress.Spreadsheet;
using Xpand.XAF.Modules.ImportData.BusinessObjects;

namespace Xpand.XAF.Modules.ImportData.Services{
	internal static class SpreadsheetParserService{

		internal static DocumentFormat DetectFormat(string fileName){
			var ext = Path.GetExtension(fileName)?.ToLowerInvariant();
			return ext switch{
				".xlsx" => DocumentFormat.Xlsx,
				".xls" => DocumentFormat.Xls,
				".csv" => DocumentFormat.Csv,
				_ => DocumentFormat.Xlsx
			};
		}

		internal static void LoadFileIntoParameter(ImportParameter parameter){
			if (parameter.FileContent == null || parameter.FileContent.Length == 0) return;

			using var workbook = new Workbook();
			using var stream = new MemoryStream(parameter.FileContent);
			workbook.LoadDocument(stream, DetectFormat(parameter.FileName ?? "file.xlsx"));

			parameter.AvailableSheets.Clear();
			foreach (var ws in workbook.Worksheets){
				parameter.AvailableSheets.Add(ws.Name);
			}

			if (string.IsNullOrEmpty(parameter.SheetName) && workbook.Worksheets.Count > 0){
				parameter.SheetName = workbook.Worksheets[0].Name;
			}

			LoadFieldMaps(workbook, parameter);
		}

		static void LoadFieldMaps(Workbook workbook, ImportParameter parameter){
			var worksheet = workbook.Worksheets[parameter.SheetName] ?? workbook.Worksheets[0];
			var usedRange = worksheet.GetUsedRange();
			if (usedRange == null) return;

			var headerRow = parameter.HasHeaders ? parameter.HeaderRowIndex : -1;
			var dataRow = parameter.DataStartRowIndex;
			var columnCount = usedRange.ColumnCount;

			parameter.FieldMaps.Clear();
			for (var col = 0; col < columnCount; col++){
				var columnName = headerRow >= 0
					? worksheet.Cells[headerRow, col].Value.TextValue ?? $"Column{col}"
					: $"Column{col}";

				var sampleValue = dataRow <= usedRange.BottomRowIndex
					? GetCellDisplayText(worksheet.Cells[dataRow, col])
					: "";

				var fieldMap = new ImportFieldMap{
					SourceColumn = columnName,
					SampleValue = sampleValue
				};
				parameter.FieldMaps.Add(fieldMap);
			}
		}

		internal static List<Dictionary<string, object>> ParseDataRows(ImportParameter parameter){
			var rows = new List<Dictionary<string, object>>();
			if (parameter.FileContent == null || parameter.FileContent.Length == 0) return rows;

			using var workbook = new Workbook();
			using var stream = new MemoryStream(parameter.FileContent);
			workbook.LoadDocument(stream, DetectFormat(parameter.FileName ?? "file.xlsx"));

			var worksheet = !string.IsNullOrEmpty(parameter.SheetName)
				? workbook.Worksheets[parameter.SheetName] ?? workbook.Worksheets[0]
				: workbook.Worksheets[0];

			var usedRange = worksheet.GetUsedRange();
			if (usedRange == null) return rows;

			var headerRow = parameter.HasHeaders ? parameter.HeaderRowIndex : -1;
			var dataStart = parameter.DataStartRowIndex;
			var columnCount = usedRange.ColumnCount;

			var headers = new string[columnCount];
			for (var col = 0; col < columnCount; col++){
				headers[col] = headerRow >= 0
					? worksheet.Cells[headerRow, col].Value.TextValue ?? $"Column{col}"
					: $"Column{col}";
			}

			for (var row = dataStart; row <= usedRange.BottomRowIndex; row++){
				var rowData = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
				var hasData = false;
				for (var col = 0; col < columnCount; col++){
					var cell = worksheet.Cells[row, col];
					var value = GetTypedCellValue(cell);
					rowData[headers[col]] = value;
					if (value != null) hasData = true;
				}
				if (hasData) rows.Add(rowData);
			}

			return rows;
		}

		static object GetTypedCellValue(Cell cell){
			if (cell.Value.IsEmpty) return null;
			if (cell.Value.IsDateTime) return cell.Value.DateTimeValue;
			if (cell.Value.IsBoolean) return cell.Value.BooleanValue;
			if (cell.Value.IsNumeric) return cell.Value.NumericValue;
			return cell.Value.TextValue;
		}

		static string GetCellDisplayText(Cell cell)
			=> cell.Value.IsEmpty ? "" : cell.Value.TextValue ?? cell.Value.ToString();
	}
}
