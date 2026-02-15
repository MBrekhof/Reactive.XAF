using System;
using System.IO;
using DevExpress.Spreadsheet;

namespace Xpand.XAF.Modules.ImportData.Tests{
	internal static class TestDataFactory{

		internal static byte[] CreateXlsx(Action<Workbook> configure){
			using var workbook = new Workbook();
			configure(workbook);
			using var stream = new MemoryStream();
			workbook.SaveDocument(stream, DocumentFormat.Xlsx);
			return stream.ToArray();
		}

		internal static byte[] CreateCsv(Action<Workbook> configure){
			using var workbook = new Workbook();
			configure(workbook);
			using var stream = new MemoryStream();
			workbook.SaveDocument(stream, DocumentFormat.Csv);
			return stream.ToArray();
		}

		internal static byte[] SimpleImportFile(params (string code, string name, int qty, double price, bool active)[] rows){
			return CreateXlsx(wb => {
				var ws = wb.Worksheets[0];
				ws.Cells["A1"].Value = "Code";
				ws.Cells["B1"].Value = "Name";
				ws.Cells["C1"].Value = "Quantity";
				ws.Cells["D1"].Value = "Price";
				ws.Cells["E1"].Value = "IsActive";
				for (var i = 0; i < rows.Length; i++){
					ws.Cells[i + 1, 0].Value = rows[i].code;
					ws.Cells[i + 1, 1].Value = rows[i].name;
					ws.Cells[i + 1, 2].Value = rows[i].qty;
					ws.Cells[i + 1, 3].Value = rows[i].price;
					ws.Cells[i + 1, 4].Value = rows[i].active;
				}
			});
		}

		internal static byte[] MultiSheetFile(){
			return CreateXlsx(wb => {
				wb.Worksheets[0].Name = "Products";
				wb.Worksheets[0].Cells["A1"].Value = "Code";
				wb.Worksheets[0].Cells["B1"].Value = "Name";
				wb.Worksheets[0].Cells["A2"].Value = "P1";
				wb.Worksheets[0].Cells["B2"].Value = "Widget";
				var ws2 = wb.Worksheets.Add("Customers");
				ws2.Cells["A1"].Value = "Id";
				ws2.Cells["B1"].Value = "Email";
				ws2.Cells["A2"].Value = "1";
				ws2.Cells["B2"].Value = "test@example.com";
			});
		}

		internal static byte[] TypeConversionFile(){
			return CreateXlsx(wb => {
				var ws = wb.Worksheets[0];
				ws.Cells["A1"].Value = "Code";
				ws.Cells["B1"].Value = "Name";
				ws.Cells["C1"].Value = "Quantity";
				ws.Cells["D1"].Value = "Price";
				ws.Cells["E1"].Value = "IsActive";
				ws.Cells["F1"].Value = "CreatedDate";
				ws.Cells["G1"].Value = "ExternalId";
				ws.Cells["A2"].Value = "TC1";
				ws.Cells["B2"].Value = "Test Item";
				ws.Cells["C2"].Value = 42;
				ws.Cells["D2"].Value = 19.99;
				ws.Cells["E2"].Value = true;
				ws.Cells["F2"].Value = new DateTime(2025, 6, 15);
				ws.Cells["G2"].Value = "a1b2c3d4-e5f6-7890-abcd-ef1234567890";
			});
		}
	}
}
