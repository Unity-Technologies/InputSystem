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
using System.Collections.Generic;
using SharpYaml.Events;
using SharpYaml.Tokens;
using DocumentEnd = SharpYaml.Events.DocumentEnd;
using DocumentStart = SharpYaml.Events.DocumentStart;

namespace SharpYaml.Model
{
    public class YamlDocument : YamlNode {
        private DocumentStart _documentStart;
        private DocumentEnd _documentEnd;
        private YamlElement _contents;

        public YamlDocument() {
            _documentStart = new DocumentStart(null, new TagDirectiveCollection(), true);
            _documentEnd = new DocumentEnd(true);
        }

        YamlDocument(DocumentStart documentStart, DocumentEnd documentEnd, YamlElement contents, YamlNodeTracker tracker) {
            Tracker = tracker;

            DocumentStart = documentStart;
            DocumentEnd = documentEnd;
            Contents = contents;
        }

        public static YamlDocument Load(EventReader eventReader, YamlNodeTracker tracker = null) {
            var documentStart = eventReader.Allow<DocumentStart>();

            var contents = ReadElement(eventReader, tracker);

            var documentEnd = eventReader.Allow<DocumentEnd>();

            return new YamlDocument(documentStart, documentEnd, contents, tracker);
        }

        public DocumentStart DocumentStart {
            get => _documentStart;
            set {
                var oldValue = _documentStart;

                _documentStart = value;

                if (Tracker != null)
                    Tracker.OnDocumentStartChanged(this, oldValue, value);
            }
        }

        public DocumentEnd DocumentEnd {
            get => _documentEnd;
            set {
                var oldValue = _documentEnd;

                _documentEnd = value;

                if (Tracker != null)
                    Tracker.OnDocumentEndChanged(this, oldValue, value);
            }
        }

        public YamlElement Contents {
            get { return _contents; }
            set {
                var oldValue = _contents;

                _contents = value;

                if (Tracker != null) {
                    value.Tracker = Tracker;
                    Tracker.OnDocumentContentsChanged(this, oldValue, value);
                }
            }
        }

        public override YamlNodeTracker Tracker {
            get { return base.Tracker; }
            internal set {
                if (Tracker == value)
                    return;

                base.Tracker = value;

                if (_contents != null) {
                    _contents.Tracker = value;
                    Tracker.OnDocumentContentsChanged(this, null, _contents);
                }
            }
        }

        public override YamlNode DeepClone(YamlNodeTracker tracker = null) {
            return new YamlDocument(_documentStart, _documentEnd, (YamlElement) Contents?.DeepClone(), tracker);
        }
    }
}