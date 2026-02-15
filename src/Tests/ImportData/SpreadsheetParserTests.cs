using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.Spreadsheet;
using NUnit.Framework;
using Shouldly;
using Xpand.XAF.Modules.ImportData.BusinessObjects;
using Xpand.XAF.Modules.ImportData.Services;

namespace Xpand.XAF.Modules.ImportData.Tests{
	[TestFixture]
	public class SpreadsheetParserTests{

		[Test]
		public void DetectFormat_returns_correct_format_for_known_and_unknown_extensions(){
			SpreadsheetParserService.DetectFormat("file.xlsx").ShouldBe(DocumentFormat.Xlsx);
			SpreadsheetParserService.DetectFormat("file.xls").ShouldBe(DocumentFormat.Xls);
			SpreadsheetParserService.DetectFormat("file.csv").ShouldBe(DocumentFormat.Csv);
			SpreadsheetParserService.DetectFormat("file.json").ShouldBe(DocumentFormat.Xlsx);
		}

		[Test]
		public void DetectFormat_is_case_insensitive(){
			SpreadsheetParserService.DetectFormat("file.XLSX").ShouldBe(DocumentFormat.Xlsx);
			SpreadsheetParserService.DetectFormat("file.CSV").ShouldBe(DocumentFormat.Csv);
			SpreadsheetParserService.DetectFormat("file.Xls").ShouldBe(DocumentFormat.Xls);
		}

		[Test]
		public void LoadFileIntoParameter_populates_AvailableSheets_and_auto_selects_first(){
			var parameter = new ImportParameter{
				FileName = "test.xlsx",
				FileContent = TestDataFactory.MultiSheetFile()
			};

			SpreadsheetParserService.LoadFileIntoParameter(parameter);

			parameter.AvailableSheets.Count.ShouldBe(2);
			parameter.AvailableSheets.ShouldContain("Products");
			parameter.AvailableSheets.ShouldContain("Customers");
			parameter.SheetName.ShouldBe("Products");
		}

		[Test]
		public void LoadFileIntoParameter_preserves_explicit_SheetName(){
			var parameter = new ImportParameter{
				FileName = "test.xlsx",
				FileContent = TestDataFactory.MultiSheetFile(),
				SheetName = "Customers"
			};

			SpreadsheetParserService.LoadFileIntoParameter(parameter);

			parameter.SheetName.ShouldBe("Customers");
		}

		[Test]
		public void LoadFileIntoParameter_creates_FieldMaps_from_headers_with_sample_values(){
			var parameter = new ImportParameter{
				FileName = "test.xlsx",
				FileContent = TestDataFactory.SimpleImportFile(("P1", "Widget", 10, 9.99, true))
			};

			SpreadsheetParserService.LoadFileIntoParameter(parameter);

			parameter.FieldMaps.Count.ShouldBe(5);
			parameter.FieldMaps[0].SourceColumn.ShouldBe("Code");
			parameter.FieldMaps[1].SourceColumn.ShouldBe("Name");
			parameter.FieldMaps[2].SourceColumn.ShouldBe("Quantity");
			parameter.FieldMaps[3].SourceColumn.ShouldBe("Price");
			parameter.FieldMaps[4].SourceColumn.ShouldBe("IsActive");
			parameter.FieldMaps[0].SampleValue.ShouldNotBeNullOrEmpty();
		}

		[Test]
		public void LoadFileIntoParameter_noop_when_FileContent_is_null(){
			var parameter = new ImportParameter{
				FileName = "test.xlsx",
				FileContent = null
			};

			SpreadsheetParserService.LoadFileIntoParameter(parameter);

			parameter.AvailableSheets.Count.ShouldBe(0);
			parameter.FieldMaps.Count.ShouldBe(0);
		}

		[Test]
		public void LoadFileIntoParameter_uses_ColumnN_when_HasHeaders_is_false(){
			var parameter = new ImportParameter{
				FileName = "test.xlsx",
				FileContent = TestDataFactory.SimpleImportFile(("P1", "Widget", 10, 9.99, true)),
				HasHeaders = false,
				DataStartRowIndex = 0
			};

			SpreadsheetParserService.LoadFileIntoParameter(parameter);

			parameter.FieldMaps.Count.ShouldBeGreaterThan(0);
			parameter.FieldMaps[0].SourceColumn.ShouldBe("Column0");
			parameter.FieldMaps[1].SourceColumn.ShouldBe("Column1");
		}

		[Test]
		public void ParseDataRows_returns_typed_values(){
			var parameter = new ImportParameter{
				FileName = "test.xlsx",
				FileContent = TestDataFactory.TypeConversionFile()
			};

			var rows = SpreadsheetParserService.ParseDataRows(parameter);

			rows.Count.ShouldBe(1);
			var row = rows[0];
			row["Code"].ShouldBeOfType<string>();
			row["Name"].ShouldBeOfType<string>();
			row["Quantity"].ShouldBeOfType<double>();
			row["Price"].ShouldBeOfType<double>();
			row["IsActive"].ShouldBeOfType<bool>();
			row["CreatedDate"].ShouldBeOfType<DateTime>();
			row["ExternalId"].ShouldBeOfType<string>();

			((double)row["Quantity"]).ShouldBe(42);
			((double)row["Price"]).ShouldBe(19.99);
			((bool)row["IsActive"]).ShouldBe(true);
		}

		[Test]
		public void ParseDataRows_skips_empty_rows(){
			var fileContent = TestDataFactory.CreateXlsx(wb => {
				var ws = wb.Worksheets[0];
				ws.Cells["A1"].Value = "Name";
				ws.Cells["B1"].Value = "Value";
				ws.Cells["A2"].Value = "Row1";
				ws.Cells["B2"].Value = 10;
				// row 3 is left entirely empty
				ws.Cells["A4"].Value = "Row3";
				ws.Cells["B4"].Value = 30;
			});

			var parameter = new ImportParameter{
				FileName = "test.xlsx",
				FileContent = fileContent
			};

			var rows = SpreadsheetParserService.ParseDataRows(parameter);

			rows.Count.ShouldBe(2);
			rows[0]["Name"].ShouldBe("Row1");
			rows[1]["Name"].ShouldBe("Row3");
		}

		[Test]
		public void ParseDataRows_returns_empty_for_null_FileContent(){
			var parameter = new ImportParameter{
				FileName = "test.xlsx",
				FileContent = null
			};

			var rows = SpreadsheetParserService.ParseDataRows(parameter);

			rows.ShouldBeEmpty();
		}

		[Test]
		public void ParseDataRows_respects_SheetName_selection(){
			var parameter = new ImportParameter{
				FileName = "test.xlsx",
				FileContent = TestDataFactory.MultiSheetFile(),
				SheetName = "Customers"
			};

			var rows = SpreadsheetParserService.ParseDataRows(parameter);

			rows.Count.ShouldBe(1);
			rows[0].ShouldContainKey("Id");
			rows[0].ShouldContainKey("Email");
			rows[0]["Email"].ShouldBe("test@example.com");
		}

		[Test]
		public void ParseDataRows_works_with_CSV_format(){
			var csvContent = TestDataFactory.CreateCsv(wb => {
				var ws = wb.Worksheets[0];
				ws.Cells["A1"].Value = "Name";
				ws.Cells["B1"].Value = "Age";
				ws.Cells["A2"].Value = "Alice";
				ws.Cells["B2"].Value = 30;
				ws.Cells["A3"].Value = "Bob";
				ws.Cells["B3"].Value = 25;
			});

			var parameter = new ImportParameter{
				FileName = "data.csv",
				FileContent = csvContent
			};

			var rows = SpreadsheetParserService.ParseDataRows(parameter);

			rows.Count.ShouldBe(2);
			rows[0]["Name"].ShouldBe("Alice");
			rows[1]["Name"].ShouldBe("Bob");
		}

		[Test]
		public void ParseDataRows_uses_case_insensitive_keys(){
			var parameter = new ImportParameter{
				FileName = "test.xlsx",
				FileContent = TestDataFactory.SimpleImportFile(("P1", "Widget", 5, 1.50, true))
			};

			var rows = SpreadsheetParserService.ParseDataRows(parameter);

			rows.Count.ShouldBe(1);
			rows[0]["code"].ShouldBe("P1");
			rows[0]["CODE"].ShouldBe("P1");
			rows[0]["Code"].ShouldBe("P1");
		}
	}
}
