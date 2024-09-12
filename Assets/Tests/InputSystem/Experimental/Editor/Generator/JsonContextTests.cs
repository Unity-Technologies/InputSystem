using System;
using NUnit.Framework;
using UnityEngine.InputSystem.Experimental.JSON;

namespace Tests.InputSystem.Experimental.Editor.Generator
{
    // TODO Create Assert/Is extension to support spans in a convenient way
    
    // Simple JSON is "Gamepad.leftStick"
    
    [Category("Experimental")]
    public class JsonContextTests
    {
        public void EnumeratorMoveNext_ShouldReturnFalse_IfEmptyJson()
        {
            var c = new JsonUtility.JsonContext(string.Empty);
            using var e = c.GetEnumerator();
            
            Assert.That(e.MoveNext(), Is.False);
        }
        
        public void EnumeratorMoveNext_ShouldReturnTrue_IfContentIsEmptyJsonObject()
        {
            const string json = "{ }";
            var c = new JsonUtility.JsonContext(json);
            using var e = c.GetEnumerator();
            
            Assert.That(e.MoveNext(), Is.True);
            Assert.That(e.Current.type, Is.EqualTo(JsonUtility.JsonType.Object));
            Assert.That(e.Current.name.ToString(), Is.EqualTo(string.Empty));
            Assert.That(e.Current.value.ToString(), Is.EqualTo(json));
            
            Assert.That(e.MoveNext(), Is.False);
        }
        
        [TestCase("\"Hello\"", "Hello")]
        [TestCase("\"\"", "")]
        public void EnumeratorMoveNext_ShouldReturnTrue_IfValidString(string json, string expected)
        {
            var c = new JsonUtility.JsonContext(json);
            using var e = c.GetEnumerator();
            
            Assert.That(e.MoveNext(), Is.True);
            Assert.That(e.Current.type, Is.EqualTo(JsonUtility.JsonType.String));
            Assert.That(e.Current.name.ToString(), Is.EqualTo(string.Empty));
            Assert.That(e.Current.value.ToString(), Is.EqualTo(expected));
            
            Assert.That(e.MoveNext(), Is.False);
        }
        
        public void EnumeratorMoveNext_ShouldThrow_IfUnterminatedString()
        {
            var c = new JsonUtility.JsonContext("\"");
            using var e = c.GetEnumerator();
            Assert.That(e.MoveNext(), Is.True);
            Assert.Throws<JsonUtility.JsonParseException>(() => e.MoveNext());
        }
        
        // TODO This is really invalid formatting
        [TestCase("\"\"\"")]
        public void EnumeratorMoveNext_ShouldThrow_IfInvalidString(string json)
        {
            var c = new JsonUtility.JsonContext(json);
            using var e = c.GetEnumerator();
            Assert.That(e.MoveNext(), Is.True);
            Assert.Throws<JsonUtility.JsonParseException>(() => e.MoveNext());
        }
        
        [TestCase("42")]
        [TestCase("-1")]
        [TestCase("3.14")]
        [TestCase("1.41e+23")]
        [TestCase("-1.41e+23")]
        [TestCase("2e-23")]
        [TestCase("1e-23")]
        [TestCase("1e-005")]
        public void EnumeratorMoveNext_ShouldReturnTrue_IfValidNumber(string json)
        {
            var c = new JsonUtility.JsonContext(json);
            using var e = c.GetEnumerator();
            
            Assert.That(e.MoveNext(), Is.True);
            Assert.That(e.Current.type, Is.EqualTo(JsonUtility.JsonType.Number));
            Assert.That(e.Current.name.ToString(), Is.EqualTo(string.Empty));
            Assert.That(e.Current.value.ToString(), Is.EqualTo(json));
            
            Assert.That(e.MoveNext(), Is.False);
        }
        
        [TestCase("42a")]
        [TestCase("-a1")]
        [TestCase("-e1")]
        [TestCase("4e")]
        [TestCase(".14")]
        [TestCase("1.4.1")]
        [TestCase("1..2")]
        [TestCase("2ee4")]
        [TestCase("2e++4")]
        [TestCase("e1-2")]
        [TestCase("1-2")]
        [TestCase("--")]
        public void EnumeratorMoveNext_ShouldThrow_IfInvalidNumber(string json)
        {
            var c = new JsonUtility.JsonContext(json);
            using var e = c.GetEnumerator();
            Assert.Throws<JsonUtility.JsonParseException>(() => e.MoveNext());
        }
        
        [Test]
        public void EnumeratorMoveNext_ShouldReturnTrue_IfContentIsString()
        {
            const string json = @"{ ""a"": ""b"" }";
            var c = new JsonUtility.JsonContext(json);
            using var e = c.GetEnumerator();
            
            Assert.That(e.MoveNext(), Is.True);
            Assert.That(e.Current.type, Is.EqualTo(JsonUtility.JsonType.Object));
            Assert.That(e.Current.name.ToString(), Is.EqualTo(string.Empty));
            Assert.That(e.Current.value.ToString(), Is.EqualTo(json));
            
            Assert.That(e.MoveNext(), Is.True);
            Assert.That(e.Current.type, Is.EqualTo(JsonUtility.JsonType.String));
            Assert.That(e.Current.name.ToString(), Is.EqualTo("a"));
            Assert.That(e.Current.value.ToString(), Is.EqualTo("b"));
            
            Assert.That(e.MoveNext(), Is.False);
        }
        
        [Test]
        public void EnumeratorMoveNext_ShouldReturnTrue_IfContentIsIValue()
        {
            const string json = @"{ ""a"": true, ""b"": false, ""c"": null }";
            var c = new JsonUtility.JsonContext(json);
            using var e = c.GetEnumerator();
            
            Assert.That(e.MoveNext(), Is.True);
            Assert.That(e.Current.type, Is.EqualTo(JsonUtility.JsonType.Object));
            Assert.That(e.Current.name.ToString(), Is.EqualTo(string.Empty));
            Assert.That(e.Current.value.ToString(), Is.EqualTo(json));
            
            Assert.That(e.MoveNext(), Is.True);
            Assert.That(e.Current.type, Is.EqualTo(JsonUtility.JsonType.Value));
            Assert.That(e.Current.name.ToString(), Is.EqualTo("a"));
            Assert.That(e.Current.value.ToString(), Is.EqualTo("true"));
            
            Assert.That(e.MoveNext(), Is.True);
            Assert.That(e.Current.type, Is.EqualTo(JsonUtility.JsonType.Value));
            Assert.That(e.Current.name.ToString(), Is.EqualTo("b"));
            Assert.That(e.Current.value.ToString(), Is.EqualTo("false"));
            
            Assert.That(e.MoveNext(), Is.True);
            Assert.That(e.Current.type, Is.EqualTo(JsonUtility.JsonType.Value));
            Assert.That(e.Current.name.ToString(), Is.EqualTo("c"));
            Assert.That(e.Current.value.ToString(), Is.EqualTo("null"));
            
            Assert.That(e.MoveNext(), Is.False);
        }
        
        [Test]
        public void EnumeratorMoveNext_ShouldReturnTrue_IfContentIsInteger()
        {
            const string json = @"{ ""a"": 1 }";
            var c = new JsonUtility.JsonContext(json);
            using var e = c.GetEnumerator();
            
            Assert.That(e.MoveNext(), Is.True);
            Assert.That(e.Current.type, Is.EqualTo(JsonUtility.JsonType.Object));
            Assert.That(e.Current.name.ToString(), Is.EqualTo(string.Empty));
            Assert.That(e.Current.value.ToString(), Is.EqualTo(json));
            
            Assert.That(e.MoveNext(), Is.True);
            Assert.That(e.Current.type, Is.EqualTo(JsonUtility.JsonType.Number));
            Assert.That(e.Current.name.ToString(), Is.EqualTo("a"));
            Assert.That(e.Current.value.ToString(), Is.EqualTo("1"));
            
            Assert.That(e.MoveNext(), Is.False);
        }
        
        [Test]
        public void EnumeratorMoveNext_ShouldReturnTrue_IfContentIsFloatingPoint()
        {
            const string json = @"{ ""a"": 1.0 }";
            var c = new JsonUtility.JsonContext(json);
            using var e = c.GetEnumerator();
            
            Assert.That(e.MoveNext(), Is.True);
            Assert.That(e.Current.type, Is.EqualTo(JsonUtility.JsonType.Object));
            Assert.That(e.Current.name.ToString(), Is.EqualTo(string.Empty));
            Assert.That(e.Current.value.ToString(), Is.EqualTo(json));
            
            Assert.That(e.MoveNext(), Is.True);
            Assert.That(e.Current.type, Is.EqualTo(JsonUtility.JsonType.Number));
            Assert.That(e.Current.name.ToString(), Is.EqualTo("a"));
            Assert.That(e.Current.value.ToString(), Is.EqualTo("1.0"));
            
            Assert.That(e.MoveNext(), Is.False);
        }

        [Test]
        public void EnumeratorMoveNext_ShouldReturnTrue_IfContentIsNestedObject()
        {
            const string json = @"{ ""a"": { ""b"" : 1.0 } }";
            var c = new JsonUtility.JsonContext(json);
            using var e = c.GetEnumerator();
            
            Assert.That(e.MoveNext(), Is.True);
            Assert.That(e.Current.type, Is.EqualTo(JsonUtility.JsonType.Object));
            Assert.That(e.Current.name.ToString(), Is.EqualTo(string.Empty));
            Assert.That(e.Current.value.ToString(), Is.EqualTo(json));
            
            Assert.That(e.MoveNext(), Is.True);
            Assert.That(e.Current.type, Is.EqualTo(JsonUtility.JsonType.Object));
            Assert.That(e.Current.name.ToString(), Is.EqualTo("a"));
            Assert.That(e.Current.value.ToString(), Is.EqualTo("{ \"b\" : 1.0 }"));
            
            Assert.That(e.MoveNext(), Is.True);
            Assert.That(e.Current.type, Is.EqualTo(JsonUtility.JsonType.Number));
            Assert.That(e.Current.name.ToString(), Is.EqualTo("b"));
            Assert.That(e.Current.value.ToString(), Is.EqualTo("1.0"));
            
            Assert.That(e.MoveNext(), Is.False);
        }
        
        [Test]
        public void EnumeratorMoveNext_ShouldReturnTrue_IfContentIsArrayOfNumbers()
        {
            const string json = @"{ ""a"": [ 1, 2 ] }";
            var c = new JsonUtility.JsonContext(json);
            using var e = c.GetEnumerator();
            
            Assert.That(e.MoveNext(), Is.True);
            Assert.That(e.Current.type, Is.EqualTo(JsonUtility.JsonType.Object));
            Assert.That(e.Current.name.ToString(), Is.EqualTo(string.Empty));
            Assert.That(e.Current.value.ToString(), Is.EqualTo(json));
            
            Assert.That(e.MoveNext(), Is.True);
            Assert.That(e.Current.type, Is.EqualTo(JsonUtility.JsonType.Array));
            Assert.That(e.Current.name.ToString(), Is.EqualTo("a"));
            Assert.That(e.Current.value.ToString(), Is.EqualTo("[ 1, 2 ]"));
            
            Assert.That(e.MoveNext(), Is.True);
            Assert.That(e.Current.type, Is.EqualTo(JsonUtility.JsonType.Number));
            Assert.That(e.Current.name.ToString(), Is.EqualTo(""));
            Assert.That(e.Current.value.ToString(), Is.EqualTo("1"));
            Assert.That(e.Current.arrayElementIndex, Is.EqualTo(0));
            
            Assert.That(e.MoveNext(), Is.True);
            Assert.That(e.Current.type, Is.EqualTo(JsonUtility.JsonType.Number));
            Assert.That(e.Current.name.ToString(), Is.EqualTo(""));
            Assert.That(e.Current.value.ToString(), Is.EqualTo("2"));
            Assert.That(e.Current.arrayElementIndex, Is.EqualTo(1));
            
            Assert.That(e.MoveNext(), Is.False);
        }
        
        [Test]
        public void EnumeratorMoveNext_ShouldReturnTrue_IfContentIsArrayOfStrings()
        {
            const string json = @"{ ""a"": [ ""b"", ""c"" ] }";
            var c = new JsonUtility.JsonContext(json);
            using var e = c.GetEnumerator();
            
            Assert.That(e.MoveNext(), Is.True);
            Assert.That(e.Current.type, Is.EqualTo(JsonUtility.JsonType.Object));
            Assert.That(e.Current.name.ToString(), Is.EqualTo(string.Empty));
            Assert.That(e.Current.value.ToString(), Is.EqualTo(json));
            
            Assert.That(e.MoveNext(), Is.True);
            Assert.That(e.Current.type, Is.EqualTo(JsonUtility.JsonType.Array));
            Assert.That(e.Current.name.ToString(), Is.EqualTo("a"));
            Assert.That(e.Current.value.ToString(), Is.EqualTo("[ \"b\", \"c\" ]"));
            
            Assert.That(e.MoveNext(), Is.True);
            Assert.That(e.Current.type, Is.EqualTo(JsonUtility.JsonType.String));
            Assert.That(e.Current.name.ToString(), Is.EqualTo(""));
            Assert.That(e.Current.value.ToString(), Is.EqualTo("b"));
            Assert.That(e.Current.arrayElementIndex, Is.EqualTo(0));
            
            Assert.That(e.MoveNext(), Is.True);
            Assert.That(e.Current.type, Is.EqualTo(JsonUtility.JsonType.String));
            Assert.That(e.Current.name.ToString(), Is.EqualTo(""));
            Assert.That(e.Current.value.ToString(), Is.EqualTo("c"));
            Assert.That(e.Current.arrayElementIndex, Is.EqualTo(1));
            
            Assert.That(e.MoveNext(), Is.False);
        }
        
        // TODO Array of literals
        
        [Test]
        public void EnumeratorMoveNext_ShouldReturnTrue_IfContentIsArrayOfObjects()
        {
            const string json = @"{ ""a"": [ { ""b"" : 1 }, { ""c"" : 2 } ] }";
            var c = new JsonUtility.JsonContext(json);
            using var e = c.GetEnumerator();
            
            Assert.That(e.MoveNext(), Is.True);
            Assert.That(e.Current.type, Is.EqualTo(JsonUtility.JsonType.Object));
            Assert.That(e.Current.name.ToString(), Is.EqualTo(string.Empty));
            Assert.That(e.Current.value.ToString(), Is.EqualTo(json));
            
            Assert.That(e.MoveNext(), Is.True);
            Assert.That(e.Current.type, Is.EqualTo(JsonUtility.JsonType.Array));
            Assert.That(e.Current.name.ToString(), Is.EqualTo("a"));
            Assert.That(e.Current.value.ToString(), Is.EqualTo("[ { \"b\" : 1 }, { \"c\" : 2 } ]"));
            
            Assert.That(e.MoveNext(), Is.True);
            Assert.That(e.Current.type, Is.EqualTo(JsonUtility.JsonType.Object));
            Assert.That(e.Current.name.ToString(), Is.EqualTo(""));
            Assert.That(e.Current.value.ToString(), Is.EqualTo("{ \"b\" : 1 }"));
            Assert.That(e.Current.arrayElementIndex, Is.EqualTo(0));
            
            Assert.That(e.MoveNext(), Is.True);
            Assert.That(e.Current.type, Is.EqualTo(JsonUtility.JsonType.Number));
            Assert.That(e.Current.name.ToString(), Is.EqualTo("b"));
            Assert.That(e.Current.value.ToString(), Is.EqualTo("1"));
            Assert.That(e.Current.arrayElementIndex, Is.EqualTo(0));
            
            Assert.That(e.MoveNext(), Is.True);
            Assert.That(e.Current.type, Is.EqualTo(JsonUtility.JsonType.Object));
            Assert.That(e.Current.name.ToString(), Is.EqualTo(""));
            Assert.That(e.Current.value.ToString(), Is.EqualTo("{ \"c\" : 2 }"));
            Assert.That(e.Current.arrayElementIndex, Is.EqualTo(1));
            
            Assert.That(e.MoveNext(), Is.True);
            Assert.That(e.Current.type, Is.EqualTo(JsonUtility.JsonType.Number));
            Assert.That(e.Current.name.ToString(), Is.EqualTo("c"));
            Assert.That(e.Current.value.ToString(), Is.EqualTo("2"));
            Assert.That(e.Current.arrayElementIndex, Is.EqualTo(0));
            
            Assert.That(e.MoveNext(), Is.False);
        }
        
        [Test]
        public void EnumeratorMoveNext_ShouldReturnTrue_IfContentIsMultipleStrings()
        {
            const string json = @"{ ""a"": ""b"", ""c"": ""d"" }";
            var c = new JsonUtility.JsonContext(json);
            using var e = c.GetEnumerator();
            
            Assert.That(e.MoveNext(), Is.True);
            Assert.That(e.Current.type, Is.EqualTo(JsonUtility.JsonType.Object));
            Assert.That(e.Current.name.ToString(), Is.EqualTo(string.Empty));
            Assert.That(e.Current.value.ToString(), Is.EqualTo(json));
            
            Assert.That(e.MoveNext(), Is.True);
            Assert.That(e.Current.type, Is.EqualTo(JsonUtility.JsonType.String));
            Assert.That(e.Current.name.ToString(), Is.EqualTo("a"));
            Assert.That(e.Current.value.ToString(), Is.EqualTo("b"));
            
            Assert.That(e.MoveNext(), Is.True);
            Assert.That(e.Current.type, Is.EqualTo(JsonUtility.JsonType.String));
            Assert.That(e.Current.name.ToString(), Is.EqualTo("c"));
            Assert.That(e.Current.value.ToString(), Is.EqualTo("d"));
            
            Assert.That(e.MoveNext(), Is.False);
        }
        
        [Test]
        public void EnumeratorMoveNext_ShouldReturnTrue_IfContentIsMultipleIntegers()
        {
            const string json = @"{ ""a"": 1, ""b"": 2 }";
            var c = new JsonUtility.JsonContext(json);
            using var e = c.GetEnumerator();
            
            Assert.That(e.MoveNext(), Is.True);
            Assert.That(e.Current.type, Is.EqualTo(JsonUtility.JsonType.Object));
            Assert.That(e.Current.name.ToString(), Is.EqualTo(string.Empty));
            Assert.That(e.Current.value.ToString(), Is.EqualTo(json));
            
            Assert.That(e.MoveNext(), Is.True);
            Assert.That(e.Current.type, Is.EqualTo(JsonUtility.JsonType.Number));
            Assert.That(e.Current.name.ToString(), Is.EqualTo("a"));
            Assert.That(e.Current.value.ToString(), Is.EqualTo("1"));
            
            Assert.That(e.MoveNext(), Is.True);
            Assert.That(e.Current.type, Is.EqualTo(JsonUtility.JsonType.Number));
            Assert.That(e.Current.name.ToString(), Is.EqualTo("b"));
            Assert.That(e.Current.value.ToString(), Is.EqualTo("2"));
            
            Assert.That(e.MoveNext(), Is.False);
        }
        
        [Test]
        public void EnumeratorMoveNext_ShouldThrow_IfValueSeparatorInsteadOfNameSeparator()
        {
            const string json = @"{ ""a"": ""b"", ""c"", ""d"" }";
            var c = new JsonUtility.JsonContext(json);
            using var e = c.GetEnumerator();
            Assert.That(e.MoveNext(), Is.True);
            Assert.That(e.MoveNext(), Is.True);
            Assert.Throws<JsonUtility.JsonParseException>(() => e.MoveNext()); 
        }
        
        [Test]
        public void EnumeratorMoveNext_ShouldThrow_IfNumberContainsMultipleFloatingPoints()
        {
            const string json = @"{ ""a"": 1.2.3 }";
            var c = new JsonUtility.JsonContext(json);
            using var e = c.GetEnumerator();
            Assert.That(e.MoveNext(), Is.True);
            Assert.Throws<JsonUtility.JsonParseException>(() => e.MoveNext()); 
        }
        
        // TODO Test invalid syntax
        // TODO Test exponential part e and E
        // TODO Test fractional part only, e.g. .1f
        // TODO Test negative numbers
        // TODO Test root array
        // TODO Test only values
        
        [Test]
        public void JsonExample()
        {
            // See: https://json.org/example.html
            var c = new JsonUtility.JsonContext("{\"menu\": {\n  \"id\": \"file\",\n  \"value\": \"File\",\n  \"popup\": {\n    \"menuitem\": [\n      {\"value\": \"New\", \"onclick\": \"CreateNewDoc()\"},\n      {\"value\": \"Open\", \"onclick\": \"OpenDoc()\"},\n      {\"value\": \"Close\", \"onclick\": \"CloseDoc()\"}\n    ]\n  }\n}}");
            using var e = c.GetEnumerator();
            Assert.That(e.MoveNext(), Is.True);
            Assert.That(e.Current.type, Is.EqualTo(JsonUtility.JsonType.String));
        }

        [Test]
        public void JsonTry()
        {
            
        }
    }
}