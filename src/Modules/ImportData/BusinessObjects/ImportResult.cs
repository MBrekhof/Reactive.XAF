using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using DevExpress.ExpressApp.DC;
using Xpand.Extensions.XAF.NonPersistentObjects;

namespace Xpand.XAF.Modules.ImportData.BusinessObjects{
	[DomainComponent]
	[DefaultProperty(nameof(Summary))]
	public class ImportResult : NonPersistentBaseObject{
		int _totalRows;
		int _successCount;
		int _errorCount;
		int _insertedCount;
		int _updatedCount;
		double _elapsedSeconds;
		string _summary;
		readonly BindingList<ImportError> _errors = new();

		public int TotalRows{
			get => _totalRows;
			set => SetPropertyValue(nameof(TotalRows), ref _totalRows, value);
		}

		public int SuccessCount{
			get => _successCount;
			set => SetPropertyValue(nameof(SuccessCount), ref _successCount, value);
		}

		public int ErrorCount{
			get => _errorCount;
			set => SetPropertyValue(nameof(ErrorCount), ref _errorCount, value);
		}

		public int InsertedCount{
			get => _insertedCount;
			set => SetPropertyValue(nameof(InsertedCount), ref _insertedCount, value);
		}

		public int UpdatedCount{
			get => _updatedCount;
			set => SetPropertyValue(nameof(UpdatedCount), ref _updatedCount, value);
		}

		public double ElapsedSeconds{
			get => _elapsedSeconds;
			set => SetPropertyValue(nameof(ElapsedSeconds), ref _elapsedSeconds, value);
		}

		[Editable(false)]
		public string Summary{
			get => _summary;
			set => SetPropertyValue(nameof(Summary), ref _summary, value);
		}

		public BindingList<ImportError> Errors => _errors;
	}
}
