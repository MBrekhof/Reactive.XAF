using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.SystemModule;
using Xpand.XAF.Modules.ImportData.BusinessObjects;
using Xpand.XAF.Modules.ImportData.Services;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.ImportData{
	public class ImportDataController : ViewController<ListView>{
		readonly SingleChoiceAction _importAction;

		public ImportDataController(){
			_importAction = new SingleChoiceAction(this, "ImportData", "RecordEdit"){
				SelectionDependencyType = SelectionDependencyType.Independent,
				ItemType = SingleChoiceActionItemType.ItemIsOperation,
				ImageName = "Action_ImportData"
			};
			_importAction.Execute += ImportAction_Execute;
		}

		protected override void OnViewChanged(){
			base.OnViewChanged();
			PopulateItems();
		}

		void PopulateItems(){
			_importAction.Items.Clear();
			if (View?.Model == null) return;
			var rules = Application.Model.ToReactiveModule<IModelReactiveModulesImportData>()?.ImportData?.Rules;
			if (rules == null) return;
			foreach (var rule in rules.Where(r => r.ListView == View.Model)){
				_importAction.Items.Add(new ChoiceActionItem(rule.Caption, rule));
			}
		}

		void ImportAction_Execute(object sender, SingleChoiceActionExecuteEventArgs e){
			var rule = (IModelImportDataRule)e.SelectedChoiceActionItem.Data;
			var typeInfo = rule.ListView.ModelClass.TypeInfo;

			var objectSpace = Application.CreateObjectSpace(typeof(ImportParameter));
			var parameter = objectSpace.CreateObject<ImportParameter>();
			parameter.ImportMode = rule.DefaultImportMode;
			parameter.BatchSize = rule.BatchSize > 0 ? rule.BatchSize : 100;
			parameter.TargetTypeInfo = typeInfo;

			// Wire up file-loaded callback: when the user picks a file via [FileAttachment],
			// parse it immediately so field mappings show as a preview.
			var file = new InMemoryFileData();
			file.FileLoaded = (fileName, content) => {
				parameter.FileName = fileName;
				parameter.FileContent = content;
				SpreadsheetParserService.LoadFileIntoParameter(parameter);
				FieldMappingService.AutoMap(parameter, typeInfo);
			};
			parameter.File = file;

			var detailView = Application.CreateDetailView(objectSpace, parameter);
			e.ShowViewParameters.CreatedView = detailView;
			e.ShowViewParameters.TargetWindow = TargetWindow.NewModalWindow;
			e.ShowViewParameters.CreateAllControllers = true;

			var dialogController = Application.CreateController<DialogController>();
			dialogController.SaveOnAccept = false;
			dialogController.AcceptAction.Caption = "Import";
			dialogController.AcceptAction.Execute += (s, acceptArgs) => {
				if (parameter.File == null || parameter.File.Size == 0)
					throw new UserFriendlyException("No file selected. Use the file picker to select a file.");

				// Fallback: if FileLoaded callback didn't fire, parse now.
				if (parameter.FileContent == null || parameter.FileContent.Length == 0){
					parameter.FileName = parameter.File.FileName;
					parameter.FileContent = parameter.File.Content;
					SpreadsheetParserService.LoadFileIntoParameter(parameter);
					FieldMappingService.AutoMap(parameter, typeInfo);
				}

				var result = ImportExecutionService.ExecuteSync(Application, parameter, typeInfo);

				// Disable Import button to prevent double-import.
				dialogController.AcceptAction.Enabled.SetItemValue("ImportComplete", false);

				// Refresh the ListView behind the dialog so it shows imported data.
				View?.ObjectSpace?.Refresh();

				// Show result. UserFriendlyException displays a message box.
				// The dialog stays open; user clicks Cancel/X to close.
				throw new UserFriendlyException(result.Summary);
			};
			e.ShowViewParameters.Controllers.Add(dialogController);
		}
	}
}
