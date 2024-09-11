using System;
using NUnit.Framework;
using UnityEngine.InputSystem.Experimental.JSON;

namespace Tests.InputSystem.Experimental.Editor.Generator
{
    // TODO Create Assert/Is extension to support spans in a convenient way
    
    [Category("Experimental")]
    public class JsonContextTests
    {
        [Test]
        public void EnumeratorMoveNext_ShouldReturnFalse_IfContentIsEmptyJson()
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
            Assert.That(e.Current.value.ToString(), Is.EqualTo("{ \"b\" : 1.0 } }"));
            
            Assert.That(e.MoveNext(), Is.True);
            Assert.That(e.Current.type, Is.EqualTo(JsonUtility.JsonType.Number));
            Assert.That(e.Current.name.ToString(), Is.EqualTo("b"));
            Assert.That(e.Current.value.ToString(), Is.EqualTo("1.0"));
            
            Assert.That(e.MoveNext(), Is.False);
        }
        
        [Test]
        public void EnumeratorMoveNext_ShouldReturnTrue_IfContentIsArray()
        {
            const string json = @"{ ""a"": [ { ""b"" : 1.0 } ] }";
            var c = new JsonUtility.JsonContext(json);
            using var e = c.GetEnumerator();
            
            Assert.That(e.MoveNext(), Is.True);
            Assert.That(e.Current.type, Is.EqualTo(JsonUtility.JsonType.Object));
            Assert.That(e.Current.name.ToString(), Is.EqualTo(string.Empty));
            Assert.That(e.Current.value.ToString(), Is.EqualTo(json));
            
            Assert.That(e.MoveNext(), Is.True);
            Assert.That(e.Current.type, Is.EqualTo(JsonUtility.JsonType.Array));
            Assert.That(e.Current.name.ToString(), Is.EqualTo("a"));
            Assert.That(e.Current.value.ToString(), Is.EqualTo("[ { \"b\" : 1.0 } ]"));
            
            Assert.That(e.MoveNext(), Is.True);
            Assert.That(e.Current.type, Is.EqualTo(JsonUtility.JsonType.Number));
            Assert.That(e.Current.name.ToString(), Is.EqualTo("b"));
            Assert.That(e.Current.value.ToString(), Is.EqualTo("1.0"));
            // TODO Assert array index
            
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
            Assert.Throws<Exception>(() => e.MoveNext()); // "c", "d" instead of "c": "d"
        }
        
        // TODO Test Nested object
        // TODO Test Array
        // TODO Test literals
        // TODO Test invalid syntax
        
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