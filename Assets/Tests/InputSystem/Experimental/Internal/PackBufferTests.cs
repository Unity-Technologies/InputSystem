using System;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine.InputSystem.Experimental;

namespace Tests.InputSystem.Experimental
{
    [Category("Experimental")]
    public class PackBufferTests
    {
        [Test]
        public void Test()
        {
            using var bytes = new UnsafeArray<byte>(AllocatorManager.Temp);
            bytes.Resize(32);
            
            unsafe
            {
                var sut = new UnsafePackBuffer(bytes.data, bytes.length);
                Assert.That(sut.MoveNext<int>(), Is.True);
                sut.Write(1);         // 4 bytes
                Assert.That(sut.position, Is.EqualTo(4));
                
                Assert.That(sut.MoveNext<int>(), Is.True);
                sut.Write(2);         // 4 bytes
                Assert.That(sut.position, Is.EqualTo(8));
                
                Assert.That(sut.MoveNext<ushort>(), Is.True);
                sut.Write((ushort)3); // 2 bytes
                Assert.That(sut.position, Is.EqualTo(10));
                
                Assert.That(sut.MoveNext<double>(), Is.True);
                sut.Write(2.0);       // 8 bytes
                Assert.That(sut.position, Is.EqualTo(24));
                
                Assert.That(sut.MoveNext<byte>(), Is.True);
                sut.Write((byte)7);   // 1 byte
                Assert.That(sut.position, Is.EqualTo(25));
                
                Assert.That(sut.MoveNext<float>(), Is.True);
                sut.Write(3.14f);     // 4 bytes
                Assert.That(sut.position, Is.EqualTo(32));

                Assert.That(sut.MoveNext<byte>(), Is.False);
                Assert.Throws<Exception>(() => sut.Write((byte)7));
                
                sut.Reset();
                
                Assert.That(sut.MoveNext<int>(), Is.True);
                Assert.That(sut.Read<int>(), Is.EqualTo(1));
                
                Assert.That(sut.MoveNext<int>(), Is.True);
                Assert.That(sut.Read<int>(), Is.EqualTo(2));
                
                Assert.That(sut.MoveNext<ushort>(), Is.True);
                Assert.That(sut.Read<ushort>(), Is.EqualTo(3));
                
                Assert.That(sut.MoveNext<double>(), Is.True);
                Assert.That(sut.Read<double>(), Is.EqualTo(2.0));
                
                Assert.That(sut.MoveNext<byte>(), Is.True);
                Assert.That(sut.Read<byte>(), Is.EqualTo((byte)7));
                
                Assert.That(sut.MoveNext<float>(), Is.True);
                Assert.That(sut.Read<float>(), Is.EqualTo(3.14f));
                
                Assert.That(sut.MoveNext<byte>(), Is.False);
                Assert.Throws<Exception>(() => sut.Read<byte>());
            }
        }
    }
}