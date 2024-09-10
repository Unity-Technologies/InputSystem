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
            var c = new JsonUtility.JsonContext("{}");
            using var e = c.GetEnumerator();
            
            Assert.That(e.MoveNext(), Is.False);
        }
        
        [Test]
        public void EnumeratorMoveNext_ShouldReturnTrue_IfContentIsString()
        {
            var c = new JsonUtility.JsonContext(@"{ ""a"": ""b"" }");
            using var e = c.GetEnumerator();
            
            Assert.That(e.MoveNext(), Is.True);
            Assert.That(e.Current.Type, Is.EqualTo(JsonUtility.JsonType.String));
            Assert.That(e.Current.name.ToString(), Is.EqualTo("a"));
            Assert.That(e.Current.value.ToString(), Is.EqualTo("b"));
            
            Assert.That(e.MoveNext(), Is.False);
        }
        
        [Test]
        public void EnumeratorMoveNext_ShouldReturnTrue_IfContentIsInteger()
        {
            var c = new JsonUtility.JsonContext(@"{ ""a"": 1 }");
            using var e = c.GetEnumerator();
            
            Assert.That(e.MoveNext(), Is.True);
            Assert.That(e.Current.Type, Is.EqualTo(JsonUtility.JsonType.Number));
            Assert.That(e.Current.name.ToString(), Is.EqualTo("a"));
            Assert.That(e.Current.value.ToString(), Is.EqualTo("1"));
            
            Assert.That(e.MoveNext(), Is.False);
        }
        
        [Test]
        public void EnumeratorMoveNext_ShouldReturnTrue_IfContentIsFloatingPoint()
        {
            var c = new JsonUtility.JsonContext(@"{ ""a"": 1.0 }");
            using var e = c.GetEnumerator();
            
            Assert.That(e.MoveNext(), Is.True);
            Assert.That(e.Current.Type, Is.EqualTo(JsonUtility.JsonType.Number));
            Assert.That(e.Current.name.ToString(), Is.EqualTo("a"));
            Assert.That(e.Current.value.ToString(), Is.EqualTo("1.0"));
            
            Assert.That(e.MoveNext(), Is.False);
        }

        [Test]
        public void EnumeratorMoveNext_ShouldReturnTrue_IfContentIsNestedObject()
        {
            var c = new JsonUtility.JsonContext(@"{ ""a"": { ""b"" : 1.0 } }");
            using var e = c.GetEnumerator();
            Assert.That(e.MoveNext(), Is.True);
            Assert.That(e.Current.Type, Is.EqualTo(JsonUtility.JsonType.Object));
            Assert.That(e.Current.name.ToString(), Is.EqualTo("a"));
            Assert.That(e.Current.value.ToString(), Is.EqualTo("{ \"b\" : 1.0 } }"));
        }
        
        [Test]
        public void EnumeratorMoveNext_ShouldReturnTrue_IfContentIsMultipleStrings()
        {
            var c = new JsonUtility.JsonContext(@"{ ""a"": ""b"", ""c"": ""d"" }");
            using var e = c.GetEnumerator();
            
            Assert.That(e.MoveNext(), Is.True);
            Assert.That(e.Current.Type, Is.EqualTo(JsonUtility.JsonType.String));
            Assert.That(e.Current.name.ToString(), Is.EqualTo("a"));
            Assert.That(e.Current.value.ToString(), Is.EqualTo("b"));
            
            Assert.That(e.MoveNext(), Is.True);
            Assert.That(e.Current.Type, Is.EqualTo(JsonUtility.JsonType.String));
            Assert.That(e.Current.name.ToString(), Is.EqualTo("c"));
            Assert.That(e.Current.value.ToString(), Is.EqualTo("d"));
            
            Assert.That(e.MoveNext(), Is.False);
        }
        
        [Test]
        public void EnumeratorMoveNext_ShouldReturnTrue_IfContentIsMultipleIntegers()
        {
            var c = new JsonUtility.JsonContext(@"{ ""a"": 1, ""b"": 2 }");
            using var e = c.GetEnumerator();
            
            Assert.That(e.MoveNext(), Is.True);
            Assert.That(e.Current.Type, Is.EqualTo(JsonUtility.JsonType.Number));
            Assert.That(e.Current.name.ToString(), Is.EqualTo("a"));
            Assert.That(e.Current.value.ToString(), Is.EqualTo("1"));
            
            Assert.That(e.MoveNext(), Is.True);
            Assert.That(e.Current.Type, Is.EqualTo(JsonUtility.JsonType.Number));
            Assert.That(e.Current.name.ToString(), Is.EqualTo("b"));
            Assert.That(e.Current.value.ToString(), Is.EqualTo("2"));
            
            Assert.That(e.MoveNext(), Is.False);
        }
        
        [Test]
        public void EnumeratorMoveNext_ShouldThrow_IfValueSeparatorInsteadOfNameSeparator()
        {
            var c = new JsonUtility.JsonContext(@"{ ""a"": ""b"", ""c"", ""d"" }");
            using var e = c.GetEnumerator();
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
            Assert.That(e.Current.Type, Is.EqualTo(JsonUtility.JsonType.String));
        }

        [Test]
        public void JsonTry()
        {
            
        }
    }
}