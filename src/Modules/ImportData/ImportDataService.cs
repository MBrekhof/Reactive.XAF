using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.SystemModule;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.Tracing;
using Xpand.Extensions.XAF.ActionExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.XAF.Modules.ImportData.BusinessObjects;
using Xpand.XAF.Modules.ImportData.Services;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;

namespace Xpand.XAF.Modules.ImportData{
	public static class ImportDataService{
		static IScheduler Scheduler => ReactiveModuleBase.Scheduler;

		public static SingleChoiceAction ImportData(this (ImportDataModule, Frame frame) tuple)
			=> tuple.frame.Action(nameof(ImportData)).As<SingleChoiceAction>();

		public static SimpleAction BrowseFile(this (ImportDataModule, Frame frame) tuple)
			=> tuple.frame.Action(nameof(BrowseFile)).As<SimpleAction>();

		internal static IObservable<Unit> Connect(this ApplicationModulesManager manager)
			=> manager.RegisterAction()
				.AddItems(action => action.AddItems().ToUnit(), Scheduler)
				.MergeIgnored(action => action.ShowImportWizard())
				.ToUnit()
				.Merge(manager.RegisterBrowseAction().ToUnit());

		static IObservable<SingleChoiceAction> RegisterAction(this ApplicationModulesManager manager)
			=> manager.RegisterViewSingleChoiceAction(nameof(ImportData), action => action.ConfigureAction());

		static void ConfigureAction(this SingleChoiceAction action){
			action.SelectionDependencyType = SelectionDependencyType.Independent;
			action.ItemType = SingleChoiceActionItemType.ItemIsOperation;
			action.TargetViewType = ViewType.ListView;
			action.ImageName = "Action_ImportData";
		}

		static IObservable<ChoiceActionItem> AddItems(this SingleChoiceAction action)
			=> action.Model.Application.ModelImportData().Rules
				.Where(rule => action.View()?.Model == rule.ListView)
				.Select(rule => new ChoiceActionItem(rule.Caption, rule))
				.ToNowObservable()
				.Do(item => action.Items.Add(item))
				.TraceImportData(item => item.Caption);

		static IObservable<SimpleAction> RegisterBrowseAction(this ApplicationModulesManager manager)
			=> manager.RegisterViewSimpleAction(nameof(BrowseFile), action => {
				action.TargetObjectType = typeof(ImportParameter);
				action.TargetViewType = ViewType.DetailView;
				action.ImageName = "Action_Open";
				action.Caption = "Browse...";
				action.Category = "Edit";
			}).MergeIgnored(action => action.WhenExecuted(e => {
				var parameter = (ImportParameter)e.Action.View().CurrentObject;
				var application = e.Action.Application;
				var typeInfo = parameter.TargetTypeInfo;
				return FilePickerService.PickFile(application, parameter, typeInfo)
					.ObserveOn(Scheduler)
					.Do(_ => e.Action.View()?.Refresh())
					.ToUnit();
			}).ToUnit());

		static IObservable<Unit> ShowImportWizard(this SingleChoiceAction action)
			=> action.WhenExecuted(e => {
				var rule = (IModelImportDataRule) e.SelectedChoiceActionItem.Data;
				var application = e.Action.Application;
				var listViewFrame = e.Action.Controller.Frame;
				var objectType = rule.ListView.ModelClass.TypeInfo.Type;
				var typeInfo = rule.ListView.ModelClass.TypeInfo;

				var objectSpace = application.CreateObjectSpace(typeof(ImportParameter));
				var parameter = objectSpace.CreateObject<ImportParameter>();
				parameter.ImportMode = rule.DefaultImportMode;
				parameter.BatchSize = rule.BatchSize > 0 ? rule.BatchSize : 100;
				parameter.TargetTypeInfo = typeInfo;

				var detailView = application.CreateDetailView(objectSpace, parameter);
				e.ShowViewParameters.CreatedView = detailView;
				e.ShowViewParameters.TargetWindow = TargetWindow.NewModalWindow;
				e.ShowViewParameters.CreateAllControllers = true;

				var dialogController = application.CreateController<DialogController>();
				dialogController.SaveOnAccept = false;
				e.ShowViewParameters.Controllers.Add(dialogController);

				return dialogController.AcceptAction.WhenExecuted(_ => {
					if (parameter.FileContent == null || parameter.FileContent.Length == 0)
						throw new UserFriendlyException("No file selected. Use the Browse button to select a file.");
					return ImportExecutionService.Execute(application, parameter, typeInfo)
						.ObserveOn(Scheduler)
						.SelectMany(result => ShowResult(application, result, e.ShowViewParameters))
						.Do(_ => {
							listViewFrame.View?.ObjectSpace?.Refresh();
						});
				}).ToUnit();
			}).ToUnit();

		static IObservable<Unit> ShowResult(XafApplication application, ImportResult result, ShowViewParameters parentParameters){
			var objectSpace = application.CreateObjectSpace(typeof(ImportResult));
			var detailView = application.CreateDetailView(objectSpace, result);
			return detailView.Observe().Do(_ => {
				parentParameters.CreatedView = detailView;
				parentParameters.TargetWindow = TargetWindow.NewModalWindow;
			}).ToUnit();
		}

		internal static IObservable<TSource> TraceImportData<TSource>(this IObservable<TSource> source,
			Func<TSource, string> messageFactory = null, string name = null, Action<ITraceEvent> traceAction = null,
			Func<Exception, string> errorMessageFactory = null,
			ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.OnNextOrOnError,
			Func<string> allMessageFactory = null,
			[CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "",
			[CallerLineNumber] int sourceLineNumber = 0)
			=> source.Trace(name, ImportDataModule.TraceSource, messageFactory, errorMessageFactory, traceAction,
				traceStrategy, allMessageFactory, memberName, sourceFilePath, sourceLineNumber);
	}
}
