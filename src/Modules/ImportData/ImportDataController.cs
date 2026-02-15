using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.SystemModule;
using Xpand.XAF.Modules.ImportData.BusinessObjects;
using Xpand.XAF.Modules.ImportData.Services;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.ImportData{
	/// <summary>
	/// ListView controller that provides the "Import Data" action for importing spreadsheet data into business objects.
	/// </summary>
	/// <remarks>
	/// This controller automatically populates an "Import Data" action on ListViews that match configured import rules
	/// in the application model. When the action is executed, it opens a modal dialog where users can:
	/// <list type="bullet">
	/// <item><description>Select a spreadsheet file (XLSX, XLS, or CSV)</description></item>
	/// <item><description>Configure import settings (headers, data start row, import mode, batch size, record limit)</description></item>
	/// <item><description>Review and adjust field mappings between columns and business object properties</description></item>
	/// <item><description>Execute the import and view results (success count, errors, elapsed time)</description></item>
	/// </list>
	/// </remarks>
	public class ImportDataController : ViewController<ListView>{
		readonly SingleChoiceAction _importAction;

		/// <summary>
		/// Initializes the ImportDataController and sets up the "Import Data" action.
		/// </summary>
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

		/// <summary>
		/// Populates the "Import Data" action with choices from configured import rules.
		/// </summary>
		void PopulateItems(){
			_importAction.Items.Clear();
			if (View?.Model == null) return;
			var rules = Application.Model.ToReactiveModule<IModelReactiveModulesImportData>()?.ImportData?.Rules;
			if (rules == null) return;
			foreach (var rule in rules.Where(r => r.ListView == View.Model)){
				_importAction.Items.Add(new ChoiceActionItem(rule.Caption, rule));
			}
		}

		/// <summary>
		/// Executes when the user selects an import rule from the "Import Data" action dropdown.
		/// </summary>
		/// <remarks>
		/// <para>Orchestrates the import wizard: opens dialog, handles file loading, runs import,
		/// shows result via toast notification, and closes the dialog automatically.</para>
		/// </remarks>
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
				parameter.RefreshAvailableKeyProperties();
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
					parameter.RefreshAvailableKeyProperties();
				}

				// Execute the import
				var importResult = ImportExecutionService.ExecuteSync(Application, parameter, typeInfo);

				// Refresh the ListView behind the dialog so it shows imported data.
				View?.ObjectSpace?.Refresh();

				// Show result as a toast notification (cross-platform, persists after dialog closes).
				Application.ShowViewStrategy.ShowMessage(new MessageOptions{
					Message = importResult.Summary,
					Type = InformationType.Success,
					Duration = 10000
				});

				// Handler returns normally → DialogController closes the dialog automatically.
			};
			e.ShowViewParameters.Controllers.Add(dialogController);
		}
	}
}
