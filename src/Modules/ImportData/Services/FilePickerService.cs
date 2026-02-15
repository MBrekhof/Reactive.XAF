using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text.Json;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using Fasterflect;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.ImportData.BusinessObjects;

namespace Xpand.XAF.Modules.ImportData.Services{
	internal static class FilePickerService{
		const string WinFilter = "Spreadsheet Files|*.xlsx;*.xls;*.csv|Excel (*.xlsx)|*.xlsx|CSV (*.csv)|*.csv|All Files|*.*";
		const string BlazorAccept = ".xlsx,.xls,.csv";

		internal static IObservable<ImportParameter> PickFile(
			XafApplication application, ImportParameter parameter, ITypeInfo typeInfo)
			=> application.GetPlatform() == Platform.Win
				? PickFileWin(parameter, typeInfo)
				: PickFileBlazor(application, parameter, typeInfo);

		static IObservable<ImportParameter> PickFileWin(ImportParameter parameter, ITypeInfo typeInfo)
			=> Observable.Defer(() => {
				var dialogType = AppDomain.CurrentDomain.GetAssemblyType("System.Windows.Forms.OpenFileDialog");
				if (dialogType == null) return Observable.Empty<ImportParameter>();

				using var dialog = (IDisposable)Activator.CreateInstance(dialogType);
				dialog.SetPropertyValue("Filter", WinFilter);
				dialog.SetPropertyValue("Title", "Select Import File");

				var result = (int)dialog.CallMethod("ShowDialog");
				if (result != 1) return Observable.Empty<ImportParameter>();

				var filePath = (string)dialog.GetPropertyValue("FileName");
				parameter.FileName = Path.GetFileName(filePath);
				parameter.FileContent = File.ReadAllBytes(filePath);
				SpreadsheetParserService.LoadFileIntoParameter(parameter);
				FieldMappingService.AutoMap(parameter, typeInfo);
				return parameter.Observe();
			});

		static IObservable<ImportParameter> PickFileBlazor(
			XafApplication application, ImportParameter parameter, ITypeInfo typeInfo)
			=> Observable.FromAsync(async () => {
				var jsRuntimeType = AppDomain.CurrentDomain.GetAssemblyType(
					"Microsoft.JSInterop.IJSRuntime");
				if (jsRuntimeType == null) return null;

				var serviceProvider = application.GetPropertyValue("ServiceProvider");
				var jsRuntime = serviceProvider?.CallMethod("GetService", jsRuntimeType);
				if (jsRuntime == null) return null;

				var js = "(function(){" +
					"return new Promise((resolve)=>{" +
					"const input=document.createElement('input');" +
					"input.type='file';" +
					"input.accept='" + BlazorAccept + "';" +
					"input.style.display='none';" +
					"input.addEventListener('change',async()=>{" +
					"if(!input.files.length){resolve(null);return;}" +
					"const file=input.files[0];" +
					"const buf=await file.arrayBuffer();" +
					"const bytes=Array.from(new Uint8Array(buf));" +
					"const b64=btoa(bytes.reduce((s,b)=>s+String.fromCharCode(b),''));" +
					"resolve(JSON.stringify({name:file.name,data:b64}));" +
					"document.body.removeChild(input);" +
					"});" +
					"input.addEventListener('cancel',()=>{" +
					"resolve(null);" +
					"document.body.removeChild(input);" +
					"});" +
					"document.body.appendChild(input);" +
					"input.click();" +
					"});" +
					"})()";

				var extensionsType = AppDomain.CurrentDomain.GetAssemblyType(
					"Microsoft.JSInterop.JSRuntimeExtensions");
				var invokeMethod = extensionsType?.GetMethods()
					.FirstOrDefault(m => m.Name == "InvokeAsync"
						&& m.IsGenericMethodDefinition
						&& m.GetParameters().Length == 3);
				var generic = invokeMethod?.MakeGenericMethod(typeof(string));
				if (generic == null) return null;

				var valueTask = generic.Invoke(null, new object[]{
					jsRuntime, "eval", new object[]{ js }
				});
				var task = (System.Threading.Tasks.Task<string>)valueTask
					.CallMethod("AsTask");
				var jsonResult = await task;

				if (string.IsNullOrEmpty(jsonResult)) return null;

				using var doc = JsonDocument.Parse(jsonResult);
				var fileName = doc.RootElement.GetProperty("name").GetString();
				var base64 = doc.RootElement.GetProperty("data").GetString();

				parameter.FileName = fileName;
				parameter.FileContent = Convert.FromBase64String(base64);
				SpreadsheetParserService.LoadFileIntoParameter(parameter);
				FieldMappingService.AutoMap(parameter, typeInfo);
				return parameter;
			}).Where(p => p != null);
	}
}
