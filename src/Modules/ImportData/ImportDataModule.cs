using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.SystemModule;

using Xpand.Extensions.Reactive.Conditional;
using Xpand.XAF.Modules.Reactive;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.ImportData{

	public sealed class ImportDataModule : ReactiveModuleBase{
		public static ReactiveTraceSource TraceSource{ get; set; }
		static ImportDataModule()
			=> TraceSource=new ReactiveTraceSource(nameof(ImportDataModule));

		public ImportDataModule(){
			RequiredModuleTypes.Add(typeof(SystemModule));
			RequiredModuleTypes.Add(typeof(ReactiveModule));
		}

		public override void Setup(ApplicationModulesManager moduleManager){
			base.Setup(moduleManager);
			moduleManager.Connect()
				.TakeUntilDisposed(this)
				.Subscribe();
		}

		public override void ExtendModelInterfaces(ModelInterfaceExtenders extenders){
			base.ExtendModelInterfaces(extenders);
			extenders.Add<IModelReactiveModules,IModelReactiveModulesImportData>();
		}
	}
}
