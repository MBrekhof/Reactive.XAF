using System.ComponentModel;
using DevExpress.ExpressApp.DC;
using Xpand.Extensions.XAF.NonPersistentObjects;

namespace Xpand.XAF.Modules.ImportData.BusinessObjects{
	[DomainComponent]
	[DefaultProperty(nameof(SourceColumn))]
	public class ImportFieldMap : NonPersistentBaseObject{
		string _sourceColumn;
		string _targetProperty;
		string _targetPropertyType;
		string _defaultValue;
		string _sampleValue;
		bool _skip;
		bool _autoMapped;

		public string SourceColumn{
			get => _sourceColumn;
			set => SetPropertyValue(nameof(SourceColumn), ref _sourceColumn, value);
		}

		public string TargetProperty{
			get => _targetProperty;
			set => SetPropertyValue(nameof(TargetProperty), ref _targetProperty, value);
		}

		public string TargetPropertyType{
			get => _targetPropertyType;
			set => SetPropertyValue(nameof(TargetPropertyType), ref _targetPropertyType, value);
		}

		public string DefaultValue{
			get => _defaultValue;
			set => SetPropertyValue(nameof(DefaultValue), ref _defaultValue, value);
		}

		public string SampleValue{
			get => _sampleValue;
			set => SetPropertyValue(nameof(SampleValue), ref _sampleValue, value);
		}

		public bool Skip{
			get => _skip;
			set => SetPropertyValue(nameof(Skip), ref _skip, value);
		}

		public bool AutoMapped{
			get => _autoMapped;
			set => SetPropertyValue(nameof(AutoMapped), ref _autoMapped, value);
		}
	}
}
