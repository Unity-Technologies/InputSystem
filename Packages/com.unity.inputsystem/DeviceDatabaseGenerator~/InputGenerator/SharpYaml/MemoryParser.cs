using System.Collections;
using System.Collections.Generic;
using SharpYaml.Events;

namespace SharpYaml
{
    public class MemoryParser : IParser
    {
        private readonly IEnumerator<ParsingEvent> parsingEvents;

        public MemoryParser(IEnumerable<ParsingEvent> parsingEvents)
        {
            this.parsingEvents = parsingEvents.GetEnumerator();
        }

        public ParsingEvent Current { get { return parsingEvents.Current; } }

        public bool MoveNext()
        {
            return parsingEvents.MoveNext();
        }
    }
}