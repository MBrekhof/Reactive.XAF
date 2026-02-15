using System;
using NUnit.Framework;
using Shouldly;
using Xpand.XAF.Modules.ImportData.Services;

namespace Xpand.XAF.Modules.ImportData.Tests{
	[TestFixture]
	public class FieldMappingServiceTests{

		[TestCase("42", typeof(int), 42)]
		[TestCase("3.14", typeof(double), 3.14)]
		[TestCase("-7", typeof(int), -7)]
		public void Primitive_conversions(object rawValue, Type targetType, object expected){
			var result = FieldMappingService.ConvertValue(rawValue, targetType);
			result.ShouldBe(expected);
		}

		[Test]
		public void String_passthrough(){
			var result = FieldMappingService.ConvertValue("hello", typeof(string));
			result.ShouldBe("hello");
		}

		[Test]
		public void Guid_parsing(){
			var guid = Guid.NewGuid();
			var result = FieldMappingService.ConvertValue(guid.ToString(), typeof(Guid));
			result.ShouldBe(guid);
		}

		[TestCase("Insert", ImportMode.Insert)]
		[TestCase("update", ImportMode.Update)]
		[TestCase("UPSERT", ImportMode.Upsert)]
		public void Enum_parsing_case_insensitive(string rawValue, ImportMode expected){
			var result = FieldMappingService.ConvertValue(rawValue, typeof(ImportMode));
			result.ShouldBe(expected);
		}

		[TestCase("yes", true)]
		[TestCase("no", false)]
		[TestCase("1", true)]
		[TestCase("0", false)]
		[TestCase("y", true)]
		[TestCase("n", false)]
		[TestCase("on", true)]
		[TestCase("off", false)]
		[TestCase("true", true)]
		[TestCase("false", false)]
		[TestCase("True", true)]
		[TestCase("False", false)]
		public void Bool_variants(string rawValue, bool expected){
			var result = FieldMappingService.ConvertValue(rawValue, typeof(bool));
			result.ShouldBe(expected);
		}

		[Test]
		public void OLE_date_double_to_DateTime(){
			var oleDate = 45457.5;
			var expected = DateTime.FromOADate(oleDate);
			var result = FieldMappingService.ConvertValue(oleDate, typeof(DateTime));
			result.ShouldBe(expected);
		}

		[Test]
		public void Null_for_nullable_int_returns_null(){
			var result = FieldMappingService.ConvertValue(null, typeof(int?));
			result.ShouldBeNull();
		}

		[Test]
		public void Null_for_reference_type_returns_null(){
			var result = FieldMappingService.ConvertValue(null, typeof(string));
			result.ShouldBeNull();
		}

		[Test]
		public void Null_for_value_type_returns_default(){
			var result = FieldMappingService.ConvertValue(null, typeof(int));
			result.ShouldBe(0);

			var resultBool = FieldMappingService.ConvertValue(null, typeof(bool));
			resultBool.ShouldBe(false);
		}

		[Test]
		public void Whitespace_for_nullable_returns_null(){
			var result = FieldMappingService.ConvertValue("   ", typeof(int?));
			result.ShouldBeNull();
		}

		[Test]
		public void DefaultValue_fallback_when_raw_is_null(){
			var result = FieldMappingService.ConvertValue(null, typeof(int), "99");
			result.ShouldBe(99);
		}

		[Test]
		public void DefaultValue_fallback_when_raw_is_whitespace(){
			var result = FieldMappingService.ConvertValue("  ", typeof(int), "55");
			result.ShouldBe(55);
		}

		[Test]
		public void Same_type_passthrough(){
			var date = new DateTime(2025, 6, 15, 10, 30, 0);
			var result = FieldMappingService.ConvertValue(date, typeof(DateTime));
			result.ShouldBe(date);
		}

		[Test]
		public void Invalid_conversion_throws(){
			Should.Throw<FormatException>(() =>
				FieldMappingService.ConvertValue("not-a-number", typeof(int)));
		}

		[Test]
		public void Invalid_boolean_throws(){
			Should.Throw<FormatException>(() =>
				FieldMappingService.ConvertValue("maybe", typeof(bool)));
		}
	}
}
