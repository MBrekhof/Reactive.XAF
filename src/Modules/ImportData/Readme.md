![](https://img.shields.io/nuget/v/Xpand.XAF.Modules.ImportData.svg?&style=flat) ![](https://img.shields.io/nuget/dt/Xpand.XAF.Modules.ImportData.svg?&style=flat)

[![GitHub issues](https://img.shields.io/github/issues/eXpandFramework/expand/ImportData.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc+label%3AReactive.XAF+label%3AImportData) [![GitHub close issues](https://img.shields.io/github/issues-closed/eXpandFramework/eXpand/ImportData.svg)](https://github.com/eXpandFramework/eXpand/issues?utf8=%E2%9C%93&q=is%3Aissue+is%3Aclosed+sort%3Aupdated-desc+label%3AReactive.XAF+label%3AImportData)
# About

The `ImportData` module imports data from CSV, XLSX, and XLS files into XAF business objects via a modal wizard with automatic column-to-property mapping, configurable import modes, batch processing, and per-row error reporting.

## Details

This is a `platform agnostic` module. It registers an `ImportData` `SingleChoiceAction` on ListViews based on model rules. The action opens a wizard dialog where the user selects a file, reviews column mappings, and executes the import. File parsing uses the DevExpress Spreadsheet Document API (`DevExpress.Document.Processor`), so no external dependencies are needed beyond what XAF already provides.

### Model Configuration

Configure import rules in the Application Model under `ReactiveModules/ImportData/Rules`:

```xml
<Application>
  <ReactiveModules>
    <ImportData>
      <Rules>
        <Rule Caption="Import Customers" ListView="Customer_ListView" DefaultImportMode="Insert" BatchSize="100" IsNewNode="True" />
        <Rule Caption="Update Products" ListView="Product_ListView" DefaultImportMode="Upsert" BatchSize="50" IsNewNode="True" />
      </Rules>
    </ImportData>
  </ReactiveModules>
</Application>
```

Each rule defines:

| Property | Type | Description |
|----------|------|-------------|
| `Caption` | string (required) | Display text in the action dropdown |
| `ListView` | IModelListView (required) | The ListView that activates this import rule |
| `DefaultImportMode` | `Insert` / `Update` / `Upsert` | How objects are resolved during import (default: `Insert`) |
| `BatchSize` | int | Number of rows per `CommitChanges()` call (default: 100) |

### Import Modes

- **Insert** -- Always creates new objects. No key property needed.
- **Update** -- Finds existing objects by `KeyProperty`. Rows without a match are reported as errors.
- **Upsert** -- Finds existing objects by `KeyProperty`; creates new objects when no match is found.

For `Update` and `Upsert` modes, set the `KeyProperty` field in the wizard to the business object property used for matching (e.g., `Code`, `Email`).

### Import Wizard Flow

1. User opens a ListView (e.g., `Customer_ListView`)
2. Clicks `Import Data` action (items come from model rules matching this ListView)
3. Modal dialog opens with an `ImportParameter` DetailView
4. User provides file content (`FileContent` byte array property) and adjusts settings:
   - `SheetName` -- which worksheet to read (for multi-sheet XLSX/XLS files)
   - `HasHeaders` / `HeaderRowIndex` / `DataStartRowIndex` -- header detection
   - `ImportMode` / `KeyProperty` -- object resolution strategy
   - `BatchSize` -- commit frequency
5. The `FieldMaps` collection shows one entry per source column with auto-mapped target properties
6. User reviews/adjusts mappings, sets `DefaultValue` or `Skip` per column
7. User clicks `OK` to execute the import
8. A result dialog shows the summary and any per-row errors
9. The source ListView refreshes automatically

### Automatic Field Mapping

When a file is parsed, the module auto-maps source columns to business object properties using normalized name comparison:

- Case-insensitive
- Underscores, spaces, and hyphens are stripped before comparison

For example, a column named `first_name` maps to a property `FirstName`, and `Order ID` maps to `OrderId`.

Auto-mapped fields have `AutoMapped = true` in the `FieldMaps` collection. You can override any mapping by changing the `TargetProperty` value or set `Skip = true` to exclude a column.

### Type Conversion

Cell values are converted to the target property's CLR type using this priority:

1. **Typed cell value** -- DateTime, Boolean, and Numeric values from the spreadsheet are used directly
2. **Direct assignability** -- if the raw value is already the target type, no conversion needed
3. **Special parsers** -- `Guid.Parse`, `Enum.Parse` (case-insensitive), boolean variants (`yes`/`no`/`1`/`0`/`y`/`n`/`on`/`off`)
4. **OLE Automation dates** -- numeric values targeting `DateTime` are converted via `DateTime.FromOADate`
5. **General conversion** -- `Convert.ChangeType` with `InvariantCulture`
6. **Default values** -- empty cells fall back to the `DefaultValue` string from the field map
7. **Nullable handling** -- `Nullable<T>` types are unwrapped before conversion; empty cells return `null`

### Error Handling

- **Row-level errors** are caught and recorded as `ImportError` entries. The import continues with the next row.
- **Cell conversion failures** record the row index, column name, raw value, target property, and error message.
- **Batch commits** happen every `BatchSize` rows. If a batch fails, previously committed batches are preserved (partial success).
- **File parsing failures** propagate up to the FaultHub for centralized error handling.

### Supported File Formats

| Extension | Format |
|-----------|--------|
| `.xlsx` | Excel 2007+ (Open XML) |
| `.xls` | Excel 97-2003 (Binary) |
| `.csv` | Comma-Separated Values |

### Reactive Pipeline

The module follows the standard `ReactiveModuleBase` pattern. The internal pipeline:

```
ImportDataModule.Setup(manager)
  -> ImportDataService.Connect(manager)
    -> RegisterAction("ImportData", TargetViewType=ListView, SelectionDependency=Independent)
    -> AddItems() from IModelImportDataRules matching the current ListView
    -> MergeIgnored:
      -> ShowImportWizard(): WhenExecuted -> create ImportParameter -> modal DetailView
        -> AcceptAction.WhenExecuted -> ImportExecutionService.Execute()
          -> ShowResult(): display ImportResult in a second modal
          -> Refresh source ListView
```

### NonPersistent Business Objects

All wizard objects are non-persistent (`DomainComponent` + `NonPersistentBaseObject`), so the module works with both XPO and EF Core without any schema changes.

| Class | Purpose |
|-------|---------|
| `ImportParameter` | Main wizard configuration: file, sheet, mode, field maps |
| `ImportFieldMap` | One entry per source column: mapping, default value, skip flag |
| `ImportResult` | Summary: total/success/error/inserted/updated counts, elapsed time |
| `ImportError` | Per-row error detail: row index, column, raw value, message |

---

**Possible future improvements:**

1. Reactive file-change pipeline: observe `FileContent` property changes and auto-parse/auto-map on upload.
2. Mapping template persistence via `IModelImportDataMappingTemplates` in the Application Model.
3. Preview rows before import execution.
4. Async/background import with progress reporting.
5. Any other need you may have.

[Let me know](https://github.com/sponsors/apobekiaris) if you want me to implement them for you.

---

## Installation
1. First you need the nuget package so issue this command to the `VS Nuget package console`

   `Install-Package Xpand.XAF.Modules.ImportData`.

    The above only references the dependencies and nexts steps are mandatory.

2. [Ways to Register a Module](https://documentation.devexpress.com/eXpressAppFramework/118047/Concepts/Application-Solution-Components/Ways-to-Register-a-Module)
or simply add the next call to your module constructor
    ```cs
    RequiredModuleTypes.Add(typeof(Xpand.XAF.Modules.ImportData.ImportDataModule));
    ```

## Versioning
The module is **not bound** to **DevExpress versioning**, which means you can use the latest version with your old DevExpress projects [Read more](https://github.com/eXpandFramework/XAF/tree/master/tools/Xpand.VersionConverter).

The module follows the Nuget [Version Basics](https://docs.microsoft.com/en-us/nuget/reference/package-versioning#version-basics).

## Dependencies
`.NetFramework: net10.0`

|<!-- -->|<!-- -->
|----|----
|**DevExpress.ExpressApp**|**Any**
|**DevExpress.Document.Processor**|**Any**
|Xpand.Extensions.Reactive|4.252.1
 |Xpand.Extensions|4.252.1
 |Xpand.Extensions.XAF|4.252.1
 |[Xpand.XAF.Modules.Reactive](https://github.com/eXpandFramework/Reactive.XAF/tree/master/src/Modules/Reactive)|4.252.1
 |[Fasterflect.Xpand](https://github.com/eXpandFramework/Fasterflect)|2.0.7
 |System.Reactive|6.0.1
 |Lib.Harmony|2.4.2
 |Microsoft.Extensions.Options|10.0.1
 |Microsoft.Extensions.DependencyInjection.Abstractions|10.0.1
 |Microsoft.CodeAnalysis|5.0.0
 |Microsoft.CodeAnalysis.CSharp|5.0.0
 |Microsoft.Extensions.Configuration.Abstractions|10.0.1
 |System.Security.Cryptography.ProtectedData|10.0.1

## Issues-Debugging-Troubleshooting

To `Step in the source code` you need to `enable Source Server support` in your Visual Studio/Tools/Options/Debugging/Enable Source Server Support. See also [How to boost your DevExpress Debugging Experience](https://github.com/eXpandFramework/DevExpress.XAF/wiki/How-to-boost-your-DevExpress-Debugging-Experience#1-index-the-symbols-to-your-custom-devexpresss-installation-location).

If the package is installed in a way that you do not have access to uninstall it, then you can `unload` it with the next call at the constructor of your module.
```cs
Xpand.XAF.Modules.Reactive.ReactiveModuleBase.Unload(typeof(Xpand.XAF.Modules.ImportData.ImportDataModule))
```

### Tests
The module is tested on Azure for each build with these [tests](https://github.com/eXpandFramework/Packages/tree/master/src/Tests/ImportData).
All Tests run as per our [Compatibility Matrix](https://github.com/eXpandFramework/DevExpress.XAF#compatibility-matrix)
