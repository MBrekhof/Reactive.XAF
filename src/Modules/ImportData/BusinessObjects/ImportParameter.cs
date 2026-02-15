using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using Xpand.Extensions.XAF.NonPersistentObjects;

namespace Xpand.XAF.Modules.ImportData.BusinessObjects{
	/// <summary>
	/// Domain component that holds all configuration and state for the import wizard.
	/// </summary>
	/// <remarks>
	/// This non-persistent business object is displayed as a detail view in the import wizard dialog.
	/// It captures user input for file selection, import settings, field mappings, and exposes data
	/// about the file being imported (available sheets, field maps). All changes are kept in memory;
	/// data is only persisted to the database when the import is confirmed and executed.
	/// </remarks>
	[DomainComponent]
	[DefaultProperty(nameof(FileName))]
	[FileAttachment(nameof(File))]
	public class ImportParameter : NonPersistentBaseObject{
		InMemoryFileData _file;
		string _fileName;
		byte[] _fileContent;
		string _sheetName;
		bool _hasHeaders = true;
		int _headerRowIndex;
		int _dataStartRowIndex = 1;
		ImportMode _importMode = ImportMode.Insert;
		string _keyProperty;
		int _batchSize = 100;
		int _maxRecordsToImport;
		readonly BindingList<ImportFieldMap> _fieldMaps = new();
		readonly BindingList<string> _availableSheets = new();

		/// <summary>
		/// The spreadsheet file to import (XLSX, XLS, or CSV format).
		/// </summary>
		/// <remarks>
		/// When the user selects a file via the file picker, this triggers the FileAttachment property's
		/// LoadFromStream method, which populates FileName and FileContent and fires the FileLoaded callback.
		/// </remarks>
		[FileTypeFilter("Spreadsheet Files", 1, "*.xlsx", "*.xls", "*.csv")]
		public InMemoryFileData File{
			get => _file;
			set => SetPropertyValue(nameof(File), ref _file, value);
		}

		/// <summary>
		/// The name of the selected file (populated automatically when file is picked).
		/// </summary>
		[Browsable(false)]
		public string FileName{
			get => _fileName;
			set => SetPropertyValue(nameof(FileName), ref _fileName, value);
		}

		/// <summary>
		/// The raw bytes of the selected file (populated automatically when file is picked).
		/// </summary>
		[Browsable(false)]
		public byte[] FileContent{
			get => _fileContent;
			set => SetPropertyValue(nameof(FileContent), ref _fileContent, value);
		}

		/// <summary>
		/// The name of the worksheet to import from (for multi-sheet XLSX/XLS files).
		/// </summary>
		/// <remarks>
		/// Automatically populated with the first sheet name when a file is loaded.
		/// User can change this to select a different sheet.
		/// </remarks>
		public string SheetName{
			get => _sheetName;
			set => SetPropertyValue(nameof(SheetName), ref _sheetName, value);
		}

		/// <summary>
		/// Whether the spreadsheet contains a header row with column names.
		/// </summary>
		/// <remarks>
		/// If true, the row at HeaderRowIndex is treated as column headers and skipped during data import.
		/// If false, DataStartRowIndex marks the first data row and columns are named "Column0", "Column1", etc.
		/// </remarks>
		[DefaultValue(true)]
		public bool HasHeaders{
			get => _hasHeaders;
			set => SetPropertyValue(nameof(HasHeaders), ref _hasHeaders, value);
		}

		/// <summary>
		/// Zero-based row index of the header row (when HasHeaders is true).
		/// </summary>
		public int HeaderRowIndex{
			get => _headerRowIndex;
			set => SetPropertyValue(nameof(HeaderRowIndex), ref _headerRowIndex, value);
		}

		/// <summary>
		/// Zero-based row index where data rows begin (first row to import as data).
		/// </summary>
		/// <remarks>
		/// For a typical file with headers in row 0, this should be 1 so data starts from row 1.
		/// </remarks>
		[DefaultValue(1)]
		public int DataStartRowIndex{
			get => _dataStartRowIndex;
			set => SetPropertyValue(nameof(DataStartRowIndex), ref _dataStartRowIndex, value);
		}

		/// <summary>
		/// The import strategy to use: Insert (new objects only), Update (existing only), or Upsert (new or update).
		/// </summary>
		/// <remarks>
		/// <list type="bullet">
		/// <item><description>Insert: Creates new objects for every row. KeyProperty is ignored.</description></item>
		/// <item><description>Update: Only updates existing objects found by KeyProperty. Rows with no match are reported as errors.</description></item>
		/// <item><description>Upsert: Updates existing objects by KeyProperty, or creates new objects if not found.</description></item>
		/// </list>
		/// </remarks>
		[ImmediatePostData]
		public ImportMode ImportMode{
			get => _importMode;
			set => SetPropertyValue(nameof(ImportMode), ref _importMode, value);
		}

		/// <summary>
		/// The business object property name used as the lookup key for Update and Upsert modes.
		/// </summary>
		/// <remarks>
		/// Populated from the target properties of active (non-skipped) field mappings.
		/// Select the property that uniquely identifies existing records for matching.
		/// Ignored if ImportMode is Insert.
		/// </remarks>
		[DataSourceProperty(nameof(AvailableKeyProperties))]
		public string KeyProperty{
			get => _keyProperty;
			set => SetPropertyValue(nameof(KeyProperty), ref _keyProperty, value);
		}

		/// <summary>
		/// Available target properties that can be used as the key for Update/Upsert lookups.
		/// </summary>
		/// <remarks>
		/// Auto-populated from the active (non-skipped) field mappings when a file is loaded.
		/// </remarks>
		[Browsable(false)]
		public BindingList<string> AvailableKeyProperties{ get; } = new();

		/// <summary>
		/// Refreshes AvailableKeyProperties from the current active field mappings.
		/// </summary>
		internal void RefreshAvailableKeyProperties(){
			AvailableKeyProperties.Clear();
			foreach (var prop in FieldMaps
				.Where(m => !m.Skip && !string.IsNullOrEmpty(m.TargetProperty))
				.Select(m => m.TargetProperty)
				.Distinct()){
				AvailableKeyProperties.Add(prop);
			}
		}

		/// <summary>
		/// Number of rows to process before committing changes to the database.
		/// </summary>
		/// <remarks>
		/// Higher values improve performance but use more memory. Lower values reduce memory usage but increase
		/// database round-trips. Default is 100. If a batch fails, previously committed batches are preserved (partial success).
		/// </remarks>
		[DefaultValue(100)]
		public int BatchSize{
			get => _batchSize;
			set => SetPropertyValue(nameof(BatchSize), ref _batchSize, value);
		}

		/// <summary>
		/// Maximum number of data rows to import from the file (0 = no limit, import all rows).
		/// </summary>
		/// <remarks>
		/// Use this to limit the import scope, for example to test with the first 100 rows before importing
		/// all 10,000 rows. Set to 0 to import all rows in the file.
		/// </remarks>
		[DefaultValue(0)]
		public int MaxRecordsToImport{
			get => _maxRecordsToImport;
			set => SetPropertyValue(nameof(MaxRecordsToImport), ref _maxRecordsToImport, value);
		}

		/// <summary>
		/// Internal reference to the TypeInfo of the target business object class.
		/// </summary>
		[Browsable(false)]
		internal ITypeInfo TargetTypeInfo{ get; set; }

		/// <summary>
		/// Collection of field mappings between spreadsheet columns and business object properties.
		/// </summary>
		/// <remarks>
		/// One entry per column in the spreadsheet. Each mapping can be auto-mapped, manually overridden,
		/// or skipped. Users can also set a DefaultValue or mark the column as "Skip" to exclude it from import.
		/// </remarks>
		public BindingList<ImportFieldMap> FieldMaps => _fieldMaps;

		/// <summary>
		/// List of sheet names available in the workbook (auto-populated when file is loaded).
		/// </summary>
		[Browsable(false)]
		public BindingList<string> AvailableSheets => _availableSheets;
	}
}
