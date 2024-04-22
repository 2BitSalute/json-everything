﻿using System.ComponentModel;
using NUnit.Framework;

namespace Json.Pointer.Tests
{
  public class TypeConverterTests
  {

		[Test]
		public void ConvertFromString()
		{
			var typeConverter = TypeDescriptor.GetConverter(typeof(JsonPointer));
			var pointer = typeConverter.ConvertFromInvariantString("/foo");

			Assert.IsInstanceOf<JsonPointer>(pointer);
			Assert.AreEqual("/foo", pointer!.ToString());
		}

		[Test]
		public void ConvertToString()
		{
			var pointer = JsonPointer.Parse("/foo");
			var typeConverter = TypeDescriptor.GetConverter(typeof(JsonPointer));
			var pointerString = typeConverter.ConvertToInvariantString(pointer);

			Assert.AreEqual("/foo", pointerString);
		}

		[Test]
		public void ConvertFromJsonPointer()
		{
			var pointer = JsonPointer.Parse("/foo");
			var typeConverter = TypeDescriptor.GetConverter(typeof(JsonPointer));
			var pointer2 = typeConverter.ConvertFrom(pointer);

			Assert.IsInstanceOf<JsonPointer>(pointer2);
			Assert.AreNotSame(pointer, pointer2);
			Assert.AreEqual("/foo", pointer2!.ToString());
		}

		[Test]
		public void ConvertToJsonPointer()
		{
			var pointer = JsonPointer.Parse("/foo");
			var typeConverter = TypeDescriptor.GetConverter(typeof(JsonPointer));
			var pointer2 = typeConverter.ConvertTo(pointer, typeof(JsonPointer));

			Assert.IsInstanceOf<JsonPointer>(pointer2);
			Assert.AreNotSame(pointer, pointer2);
			Assert.AreEqual("/foo", pointer2!.ToString());
		}

  }
}
