using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using DevExpress.ExpressApp.DC;
using Xpand.Extensions.XAF.NonPersistentObjects;

namespace Xpand.XAF.Modules.ImportData.BusinessObjects{
	[DomainComponent]
	[DefaultProperty(nameof(FileName))]
	public class ImportParameter : NonPersistentBaseObject{
		string _fileName;
		byte[] _fileContent;
		string _sheetName;
		bool _hasHeaders = true;
		int _headerRowIndex;
		int _dataStartRowIndex = 1;
		ImportMode _importMode = ImportMode.Insert;
		string _keyProperty;
		int _batchSize = 100;
		readonly BindingList<ImportFieldMap> _fieldMaps = new();
		readonly BindingList<string> _availableSheets = new();

		[Editable(false)]
		public string FileName{
			get => _fileName;
			set => SetPropertyValue(nameof(FileName), ref _fileName, value);
		}

		[Browsable(false)]
		[Editable(true)]
		public byte[] FileContent{
			get => _fileContent;
			set => SetPropertyValue(nameof(FileContent), ref _fileContent, value);
		}

		public string SheetName{
			get => _sheetName;
			set => SetPropertyValue(nameof(SheetName), ref _sheetName, value);
		}

		[DefaultValue(true)]
		public bool HasHeaders{
			get => _hasHeaders;
			set => SetPropertyValue(nameof(HasHeaders), ref _hasHeaders, value);
		}

		public int HeaderRowIndex{
			get => _headerRowIndex;
			set => SetPropertyValue(nameof(HeaderRowIndex), ref _headerRowIndex, value);
		}

		[DefaultValue(1)]
		public int DataStartRowIndex{
			get => _dataStartRowIndex;
			set => SetPropertyValue(nameof(DataStartRowIndex), ref _dataStartRowIndex, value);
		}

		public ImportMode ImportMode{
			get => _importMode;
			set => SetPropertyValue(nameof(ImportMode), ref _importMode, value);
		}

		public string KeyProperty{
			get => _keyProperty;
			set => SetPropertyValue(nameof(KeyProperty), ref _keyProperty, value);
		}

		[DefaultValue(100)]
		public int BatchSize{
			get => _batchSize;
			set => SetPropertyValue(nameof(BatchSize), ref _batchSize, value);
		}

		[Browsable(false)]
		internal ITypeInfo TargetTypeInfo{ get; set; }

		public BindingList<ImportFieldMap> FieldMaps => _fieldMaps;

		[Browsable(false)]
		public BindingList<string> AvailableSheets => _availableSheets;
	}
}
