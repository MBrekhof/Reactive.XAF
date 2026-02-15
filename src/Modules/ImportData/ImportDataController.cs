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
			parameter.File = new InMemoryFileData();

			var detailView = Application.CreateDetailView(objectSpace, parameter);
			e.ShowViewParameters.CreatedView = detailView;
			e.ShowViewParameters.TargetWindow = TargetWindow.NewModalWindow;
			e.ShowViewParameters.CreateAllControllers = true;

			var dialogController = Application.CreateController<DialogController>();
			dialogController.SaveOnAccept = false;
			dialogController.AcceptAction.Execute += (s, acceptArgs) => AcceptAction_Execute(acceptArgs, parameter, typeInfo);
			e.ShowViewParameters.Controllers.Add(dialogController);
		}

		void AcceptAction_Execute(SimpleActionExecuteEventArgs e, ImportParameter parameter, DevExpress.ExpressApp.DC.ITypeInfo typeInfo){
			if (parameter.File == null || parameter.File.Size == 0)
				throw new UserFriendlyException("No file selected. Use the file picker to select a file.");

			parameter.FileName = parameter.File.FileName;
			parameter.FileContent = parameter.File.Content;
			SpreadsheetParserService.LoadFileIntoParameter(parameter);
			FieldMappingService.AutoMap(parameter, typeInfo);

			var result = ImportExecutionService.ExecuteSync(Application, parameter, typeInfo);

			var resultOs = Application.CreateObjectSpace(typeof(ImportResult));
			var resultView = Application.CreateDetailView(resultOs, result);
			e.ShowViewParameters.CreatedView = resultView;
			e.ShowViewParameters.TargetWindow = TargetWindow.NewModalWindow;

			View?.ObjectSpace?.Refresh();
		}
	}
}
