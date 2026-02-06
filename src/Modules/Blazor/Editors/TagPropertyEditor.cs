using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Components;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Blazor.Editors.Adapters;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using Microsoft.AspNetCore.Components;
using Xpand.XAF.Modules.Reactive.Services;
using EditorAliases = Xpand.Extensions.XAF.Attributes.EditorAliases;

namespace Xpand.XAF.Modules.Blazor.Editors {
    [PropertyEditor(typeof(object), EditorAliases.Tag,false)]
    public class TagPropertyEditor(Type objectType, IModelMemberViewItem model)
	    : ComponentPropertyEditor(objectType, model) {
	    class DxTagBoxToStringAdapter(DxTagBoxModel<DataItem<object>, object> componentModel)
		    : DxTagBoxAdapter<DataItem<object>, object>(componentModel) {
		    public override object GetValue() => base.GetValue().ToString()?.Split($"{CheckedListBoxItemsDisplayTextProvider.MultiTextSeparator} ");
		    // public override void SetValue(object value) {
			// 	string str = (string)value;
			// 	object[] values = !string.IsNullOrEmpty(str) ? str.Split(CheckedListBoxItemsDisplayTextProvider.MultiTextSeparator).Select(str => str.Trim()).ToArray() : null;
			// 	base.SetValue(values);
			// }
		}
		
		protected override IComponentAdapter CreateComponentAdapter() {
			var componentModel = new DxTagBoxModel<DataItem<object>, object>();
            var dataSourcePropertyAttribute = MemberInfo.FindAttribute<DataSourcePropertyAttribute>();
            IEnumerable<DataItem<object>> dataItems = Array.Empty<DataItem<object>>();
            if (dataSourcePropertyAttribute != null) {
                var datasource = ((IEnumerable) MemberInfo.Owner
                    .FindMember(dataSourcePropertyAttribute.DataSourceProperty).GetValue(CurrentObject)).Cast<object>();
                dataItems = datasource.Select(o => new DataItem<object>(o,$"{o}"));
                if (datasource is DynamicCollection dynamicCollection) {
                    dynamicCollection.WhenObjects()
                        // .TakeUntil(o => !IsDisposed)
                        // .Select(o => new DataItem<object>(o, $"{o}")).ToEnumerable();
                    .Do(o => componentModel.Data=componentModel.Data.Concat(new List<DataItem<object>>(){new(o,$"{o}")}.ToArray()),() => {})
                    .Subscribe();
                }
            }
            
			if(CurrentObject is ICheckedListBoxItemsProvider itemsProvider) {
                dataItems = dataItems.Concat(itemsProvider.GetCheckedListBoxItems(PropertyName).Select(item => new DataItem<object>(item.Key, item.Value)));
            }
			
			componentModel.Data = dataItems;
			componentModel.ValueFieldName = nameof(DataItem<object>.Value);
			componentModel.TextFieldName = nameof(DataItem<object>.Text);
			return new DxTagBoxToStringAdapter(componentModel);
		}
		protected override RenderFragment CreateViewComponentCore(object dataContext) 
            => DisplayTextRenderer.Create(new DisplayTextModel {DisplayText = GetPropertyDisplayValue(dataContext)});

		public override string GetPropertyDisplayValue(object dataContext) 
            => CheckedListBoxItemsDisplayTextProvider.GetDisplayText((string)this.GetPropertyValue(dataContext), PropertyName, dataContext as ICheckedListBoxItemsProvider);
    }
}