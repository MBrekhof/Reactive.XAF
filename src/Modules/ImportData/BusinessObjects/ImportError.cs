using System.ComponentModel;
using DevExpress.ExpressApp.DC;
using Xpand.Extensions.XAF.NonPersistentObjects;

namespace Xpand.XAF.Modules.ImportData.BusinessObjects{
	[DomainComponent]
	[DefaultProperty(nameof(Message))]
	public class ImportError : NonPersistentBaseObject{
		int _rowIndex;
		string _columnName;
		string _rawValue;
		string _targetProperty;
		string _message;

		public int RowIndex{
			get => _rowIndex;
			set => SetPropertyValue(nameof(RowIndex), ref _rowIndex, value);
		}

		public string ColumnName{
			get => _columnName;
			set => SetPropertyValue(nameof(ColumnName), ref _columnName, value);
		}

		public string RawValue{
			get => _rawValue;
			set => SetPropertyValue(nameof(RawValue), ref _rawValue, value);
		}

		public string TargetProperty{
			get => _targetProperty;
			set => SetPropertyValue(nameof(TargetProperty), ref _targetProperty, value);
		}

		public string Message{
			get => _message;
			set => SetPropertyValue(nameof(Message), ref _message, value);
		}
	}
}
