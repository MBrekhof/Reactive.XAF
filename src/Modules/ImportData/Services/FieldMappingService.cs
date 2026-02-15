using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DevExpress.ExpressApp.DC;
using Xpand.XAF.Modules.ImportData.BusinessObjects;

namespace Xpand.XAF.Modules.ImportData.Services{
	internal static class FieldMappingService{

		internal static void AutoMap(ImportParameter parameter, ITypeInfo typeInfo){
			var members = typeInfo.Members
				.Where(m => m.IsPublic && !m.IsReadOnly && !m.IsKey)
				.ToList();

			foreach (var fieldMap in parameter.FieldMaps){
				if (fieldMap.Skip) continue;

				var normalized = Normalize(fieldMap.SourceColumn);
				var member = members.FirstOrDefault(m => Normalize(m.Name) == normalized);
				if (member != null){
					fieldMap.TargetProperty = member.Name;
					fieldMap.TargetPropertyType = member.MemberType.Name;
					fieldMap.AutoMapped = true;
				}
			}
		}

		static string Normalize(string name)
			=> name?.Replace("_", "").Replace(" ", "").Replace("-", "").ToLowerInvariant() ?? "";

		internal static object ConvertValue(object rawValue, Type targetType, string defaultValue = null){
			var underlyingType = Nullable.GetUnderlyingType(targetType);
			var isNullable = underlyingType != null;
			var effectiveType = underlyingType ?? targetType;

			if (rawValue == null || (rawValue is string s && string.IsNullOrWhiteSpace(s))){
				if (!string.IsNullOrEmpty(defaultValue)){
					return ConvertValue(defaultValue, targetType);
				}
				if (isNullable || !effectiveType.IsValueType) return null;
				return Activator.CreateInstance(effectiveType);
			}

			if (effectiveType.IsInstanceOfType(rawValue)) return rawValue;

			var text = rawValue.ToString();

			if (effectiveType == typeof(Guid)) return Guid.Parse(text);

			if (effectiveType.IsEnum) return Enum.Parse(effectiveType, text, true);

			if (effectiveType == typeof(bool)) return ParseBool(text);

			if (effectiveType == typeof(DateTime) && rawValue is double numericDate)
				return DateTime.FromOADate(numericDate);

			return Convert.ChangeType(rawValue, effectiveType, CultureInfo.InvariantCulture);
		}

		static bool ParseBool(string text){
			if (bool.TryParse(text, out var result)) return result;
			return text.ToLowerInvariant() switch{
				"1" or "yes" or "y" or "true" or "on" => true,
				"0" or "no" or "n" or "false" or "off" => false,
				_ => throw new FormatException($"Cannot convert '{text}' to Boolean")
			};
		}

		internal static IEnumerable<string> GetMappableProperties(ITypeInfo typeInfo)
			=> typeInfo.Members
				.Where(m => m.IsPublic && !m.IsReadOnly)
				.Select(m => m.Name);
	}
}
