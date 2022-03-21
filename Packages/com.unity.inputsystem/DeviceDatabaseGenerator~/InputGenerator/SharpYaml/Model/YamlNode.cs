// Copyright (c) SharpYaml - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SharpYaml.Events;
using SharpYaml.Serialization;
using DocumentStart = SharpYaml.Events.DocumentStart;
using Scalar = SharpYaml.Events.Scalar;
using StreamStart = SharpYaml.Events.StreamStart;

namespace SharpYaml.Model {
     public abstract class YamlNode {
        public virtual YamlNodeTracker Tracker { get; internal set; }

        protected static YamlElement ReadElement(EventReader eventReader, YamlNodeTracker tracker = null) {
            if (eventReader.Accept<MappingStart>())
                return YamlMapping.Load(eventReader, tracker);

            if (eventReader.Accept<SequenceStart>())
                return YamlSequence.Load(eventReader, tracker);

            if (eventReader.Accept<Scalar>())
                return YamlValue.Load(eventReader, tracker);

            return null;
        }

        public IEnumerable<ParsingEvent> EnumerateEvents() {
            return new YamlNodeEventEnumerator(this);
        }

        public void WriteTo(TextWriter writer, bool suppressDocumentTags = false) {
            WriteTo(new Emitter(writer), suppressDocumentTags);
        }

        public void WriteTo(IEmitter emitter, bool suppressDocumentTags = false) {
            var events = EnumerateEvents().ToList();

            // Emitter will throw an exception if we attempt to use it without
            // starting StremStart and DocumentStart events.
            if (!(events[0] is StreamStart))
                events.Insert(0, new StreamStart());

            if (!(events[1] is DocumentStart))
                events.Insert(1, new DocumentStart());

            foreach (var evnt in events) {
                if (suppressDocumentTags) {
                    var document = evnt as DocumentStart;
                    if (document != null && document.Tags != null) {
                        document.Tags.Clear();
                    }
                }

                emitter.Emit(evnt);
            }
        }

        public override string ToString() {
            var sb = new StringBuilder();
            WriteTo(new StringWriter(sb), true);
            return sb.ToString().Trim();
        }

        public T ToObject<T>(SerializerSettings settings = null)
        {
            return (T) ToObject(typeof(T), settings);
        }

        public object ToObject(Type type, SerializerSettings settings = null)
        {
            var s = new Serializer(settings);

            var context = new SerializerContext(s, null) { Reader = new EventReader(new MemoryParser(EnumerateEvents())) };
            return context.ReadYaml(null, type);
        }

        class MemoryEmitter : IEmitter {
            public List<ParsingEvent> Events = new List<ParsingEvent>();

            public void Emit(ParsingEvent evnt) {
                Events.Add(evnt);
            }
        }

        public static YamlElement FromObject(object value, SerializerSettings settings = null, Type expectedType = null) {
            var s = new Serializer(settings);

            var emitter = new MemoryEmitter();
            var context = new SerializerContext(s, null) { Writer = new WriterEventEmitter(emitter) };
            context.WriteYaml(value, expectedType);

            return ReadElement(new EventReader(new MemoryParser(emitter.Events)));
        }

        public abstract YamlNode DeepClone(YamlNodeTracker tracker = null);
    }
}
