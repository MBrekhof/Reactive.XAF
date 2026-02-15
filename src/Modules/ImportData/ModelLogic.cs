using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.Persistent.Base;
using Xpand.Extensions.XAF.ModelExtensions;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.ImportData{

	public enum ImportMode{
		Insert,
		Update,
		Upsert
	}

	public interface IModelReactiveModulesImportData : IModelReactiveModule{
		IModelImportData ImportData{ get; }
	}

	public interface IModelImportData : IModelNode{
		IModelImportDataRules Rules { get; }
	}

	[DomainLogic(typeof(IModelImportData))]
	public static class ModelImportDataLogic {
		public static IObservable<IModelImportData> ImportData(this IObservable<IModelReactiveModules> source)
			=> source.Select(modules => modules.ImportData());

		public static IModelImportData ImportData(this IModelReactiveModules reactiveModules)
			=> ((IModelReactiveModulesImportData) reactiveModules).ImportData;

		internal static IModelImportData ModelImportData(this IModelApplication modelApplication)
			=> modelApplication.ToReactiveModule<IModelReactiveModulesImportData>().ImportData;
	}

	[ModelNodesGenerator(typeof(ModelImportDataRulesNodesGenerator))]
	public interface IModelImportDataRules:IModelNode,IModelList<IModelImportDataRule> {
	}

	public class ModelImportDataRulesNodesGenerator:ModelNodesGeneratorBase {
		protected override void GenerateNodesCore(ModelNode node) {
		}
	}

	[ModelDisplayName("Rule")]
	public interface IModelImportDataRule:IModelNode {
		[Required][Localizable(true)]
		string Caption { get; set; }
		[Required]
		[DataSourceProperty(nameof(ImportListViews))]
		IModelListView ListView { get; set; }
		ImportMode DefaultImportMode { get; set; }
		[DefaultValue(100)]
		int BatchSize { get; set; }
		[Browsable(false)]
		IModelList<IModelListView> ImportListViews { get; }
	}

	[DomainLogic(typeof(IModelImportDataRule))]
	public class ModelImportDataRuleLogic {
		public static IModelList<IModelListView> Get_ImportListViews(IModelImportDataRule rule)
			=> rule.Application.Views.OfType<IModelListView>().ToCalculatedModelNodeList();

		public static int Get_BatchSize(IModelImportDataRule rule) => 100;
	}
}
