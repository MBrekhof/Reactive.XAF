using System;
using System.Runtime.CompilerServices;
using Xpand.Extensions.Tracing;
using Xpand.Extensions.Reactive.Utility;

namespace Xpand.XAF.Modules.ImportData{
	public static class ImportDataService{
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
