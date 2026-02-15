using System;
using System.Linq;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using NUnit.Framework;
using Shouldly;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.ImportData.BusinessObjects;
using Xpand.XAF.Modules.ImportData.Services;
using Xpand.XAF.Modules.ImportData.Tests.BOModel;

namespace Xpand.XAF.Modules.ImportData.Tests{
	[TestFixture]
	[NonParallelizable]
	public class ImportExecutionTests : CommonTest{

		ImportParameter CreateParameter(byte[] fileContent, ImportMode mode, string keyProperty, XafApplication application){
			var param = new ImportParameter{
				FileName = "test.xlsx",
				FileContent = fileContent,
				ImportMode = mode,
				KeyProperty = keyProperty,
				BatchSize = 100
			};
			SpreadsheetParserService.LoadFileIntoParameter(param);
			var typeInfo = application.TypesInfo.FindTypeInfo(typeof(ImportTestBO));
			FieldMappingService.AutoMap(param, typeInfo);
			return param;
		}

		ImportResult ExecuteImport(XafApplication application, ImportParameter parameter)
			=> ImportExecutionService.Execute(
				application, parameter, application.TypesInfo.FindTypeInfo(typeof(ImportTestBO)))
				.FirstAsync().Wait();

		XafApplication CreateApplication(){
			ApplicationModulesManager.UseStaticCache = false;
			var application = new TestWinApplication(typeof(ImportDataModule), true, true);
			application.Configure<ImportDataModule>(Platform.Win);
			application.AddModule<ImportDataModule>(typeof(ImportTestBO));
			return application;
		}

		[Test]
		public void Insert_Creates_Objects(){
			using var application = CreateApplication();
			var file = TestDataFactory.SimpleImportFile(
				("A01", "Widget", 10, 5.99, true),
				("A02", "Gadget", 20, 9.99, false));
			var param = CreateParameter(file, ImportMode.Insert, null, application);

			var result = ExecuteImport(application, param);

			result.InsertedCount.ShouldBe(2);
			result.UpdatedCount.ShouldBe(0);
			result.ErrorCount.ShouldBe(0);

			using var os = application.CreateObjectSpace(typeof(ImportTestBO));
			os.GetObjectsQuery<ImportTestBO>().Count().ShouldBe(2);
		}

		[Test]
		public void Insert_Sets_Property_Values_Correctly(){
			using var application = CreateApplication();
			var file = TestDataFactory.SimpleImportFile(("P01", "Premium Widget", 42, 19.99, true));
			var param = CreateParameter(file, ImportMode.Insert, null, application);

			var result = ExecuteImport(application, param);

			result.InsertedCount.ShouldBe(1);

			using var os = application.CreateObjectSpace(typeof(ImportTestBO));
			var obj = os.GetObjectsQuery<ImportTestBO>().First();
			obj.Code.ShouldBe("P01");
			obj.Name.ShouldBe("Premium Widget");
			obj.Quantity.ShouldBe(42);
			obj.Price.ShouldBe(19.99);
			obj.IsActive.ShouldBe(true);
		}

		[Test]
		public void Insert_Empty_File_Returns_Zero_Rows(){
			using var application = CreateApplication();
			var file = TestDataFactory.CreateXlsx(wb => {
				var ws = wb.Worksheets[0];
				ws.Cells["A1"].Value = "Code";
				ws.Cells["B1"].Value = "Name";
				ws.Cells["C1"].Value = "Quantity";
				ws.Cells["D1"].Value = "Price";
				ws.Cells["E1"].Value = "IsActive";
			});
			var param = CreateParameter(file, ImportMode.Insert, null, application);

			var result = ExecuteImport(application, param);

			result.TotalRows.ShouldBe(0);
			result.SuccessCount.ShouldBe(0);
			result.InsertedCount.ShouldBe(0);
			result.ErrorCount.ShouldBe(0);
		}

		[Test]
		public void Update_Modifies_Existing_Objects(){
			using var application = CreateApplication();

			using (var os = application.CreateObjectSpace(typeof(ImportTestBO))){
				var existing = os.CreateObject<ImportTestBO>();
				existing.Code = "UPD1";
				existing.Name = "Old Name";
				existing.Quantity = 1;
				existing.Price = 1.00;
				existing.IsActive = false;
				os.CommitChanges();
			}

			var file = TestDataFactory.SimpleImportFile(("UPD1", "New Name", 50, 25.00, true));
			var param = CreateParameter(file, ImportMode.Update, "Code", application);

			var result = ExecuteImport(application, param);

			result.UpdatedCount.ShouldBe(1);
			result.InsertedCount.ShouldBe(0);
			result.ErrorCount.ShouldBe(0);

			using var verifyOs = application.CreateObjectSpace(typeof(ImportTestBO));
			var obj = verifyOs.GetObjectsQuery<ImportTestBO>().First();
			obj.Name.ShouldBe("New Name");
			obj.Quantity.ShouldBe(50);
			obj.Price.ShouldBe(25.00);
			obj.IsActive.ShouldBe(true);
		}

		[Test]
		public void Update_Records_Error_When_Not_Found(){
			using var application = CreateApplication();
			var file = TestDataFactory.SimpleImportFile(("MISSING", "Ghost", 0, 0, false));
			var param = CreateParameter(file, ImportMode.Update, "Code", application);

			var result = ExecuteImport(application, param);

			result.ErrorCount.ShouldBe(1);
			result.UpdatedCount.ShouldBe(0);
			result.InsertedCount.ShouldBe(0);
			result.Errors[0].Message.ShouldContain("No existing object found");
		}

		[Test]
		public void Upsert_Updates_Existing_And_Creates_New(){
			using var application = CreateApplication();

			using (var os = application.CreateObjectSpace(typeof(ImportTestBO))){
				var existing = os.CreateObject<ImportTestBO>();
				existing.Code = "EX1";
				existing.Name = "Existing";
				existing.Quantity = 5;
				existing.Price = 10.00;
				existing.IsActive = true;
				os.CommitChanges();
			}

			var file = TestDataFactory.SimpleImportFile(
				("EX1", "Updated Existing", 10, 20.00, true),
				("NEW1", "Brand New", 1, 5.00, false));
			var param = CreateParameter(file, ImportMode.Upsert, "Code", application);

			var result = ExecuteImport(application, param);

			result.UpdatedCount.ShouldBe(1);
			result.InsertedCount.ShouldBe(1);
			result.ErrorCount.ShouldBe(0);

			using var verifyOs = application.CreateObjectSpace(typeof(ImportTestBO));
			verifyOs.GetObjectsQuery<ImportTestBO>().Count().ShouldBe(2);

			var updated = verifyOs.GetObjectsQuery<ImportTestBO>().First(o => o.Code == "EX1");
			updated.Name.ShouldBe("Updated Existing");

			var created = verifyOs.GetObjectsQuery<ImportTestBO>().First(o => o.Code == "NEW1");
			created.Name.ShouldBe("Brand New");
		}

		[Test]
		public void Conversion_Error_Does_Not_Stop_Import(){
			using var application = CreateApplication();
			var file = TestDataFactory.CreateXlsx(wb => {
				var ws = wb.Worksheets[0];
				ws.Cells["A1"].Value = "Code";
				ws.Cells["B1"].Value = "Name";
				ws.Cells["C1"].Value = "Quantity";
				ws.Cells["D1"].Value = "Price";
				ws.Cells["E1"].Value = "IsActive";
				// Row 1: invalid Quantity
				ws.Cells["A2"].Value = "ERR1";
				ws.Cells["B2"].Value = "Bad Qty";
				ws.Cells["C2"].Value = "not-a-number";
				ws.Cells["D2"].Value = 1.00;
				ws.Cells["E2"].Value = false;
				// Row 2: valid
				ws.Cells["A3"].Value = "OK1";
				ws.Cells["B3"].Value = "Good Item";
				ws.Cells["C3"].Value = 10;
				ws.Cells["D3"].Value = 5.00;
				ws.Cells["E3"].Value = true;
			});
			var param = CreateParameter(file, ImportMode.Insert, null, application);

			var result = ExecuteImport(application, param);

			result.InsertedCount.ShouldBe(2);
			result.Errors.Count.ShouldBeGreaterThan(0);
			result.Errors.ShouldContain(e => e.TargetProperty == "Quantity");

			using var os = application.CreateObjectSpace(typeof(ImportTestBO));
			os.GetObjectsQuery<ImportTestBO>().Count().ShouldBe(2);
		}

		[Test]
		public void Skip_Flag_Excludes_Column_From_Import(){
			using var application = CreateApplication();
			var file = TestDataFactory.SimpleImportFile(("SK1", "Skip Test", 99, 15.00, true));
			var param = CreateParameter(file, ImportMode.Insert, null, application);

			var quantityMap = param.FieldMaps.First(m => m.TargetProperty == "Quantity");
			quantityMap.Skip = true;

			var result = ExecuteImport(application, param);

			result.InsertedCount.ShouldBe(1);

			using var os = application.CreateObjectSpace(typeof(ImportTestBO));
			var obj = os.GetObjectsQuery<ImportTestBO>().First();
			obj.Code.ShouldBe("SK1");
			obj.Name.ShouldBe("Skip Test");
			obj.Quantity.ShouldBe(0);
			obj.Price.ShouldBe(15.00);
		}

		[Test]
		public void Result_Summary_Is_Formatted_Correctly(){
			using var application = CreateApplication();
			var file = TestDataFactory.SimpleImportFile(("S01", "Summary Test", 1, 1.00, true));
			var param = CreateParameter(file, ImportMode.Insert, null, application);

			var result = ExecuteImport(application, param);

			result.Summary.ShouldContain("1 inserted");
			result.Summary.ShouldContain("0 updated");
			result.Summary.ShouldContain("0 errors");
			result.ElapsedSeconds.ShouldBeGreaterThanOrEqualTo(0);
		}

		[Test]
		public void Import_Handles_All_Property_Types(){
			using var application = CreateApplication();
			var file = TestDataFactory.TypeConversionFile();
			var param = CreateParameter(file, ImportMode.Insert, null, application);

			var result = ExecuteImport(application, param);

			result.ErrorCount.ShouldBe(0);
			result.InsertedCount.ShouldBe(1);

			using var os = application.CreateObjectSpace(typeof(ImportTestBO));
			var obj = os.GetObjectsQuery<ImportTestBO>().First();
			obj.Code.ShouldBe("TC1");
			obj.Name.ShouldBe("Test Item");
			obj.Quantity.ShouldBe(42);
			obj.Price.ShouldBe(19.99);
			obj.IsActive.ShouldBe(true);
			obj.CreatedDate.ShouldBe(new DateTime(2025, 6, 15));
			obj.ExternalId.ShouldBe(Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"));
		}
	}
}
