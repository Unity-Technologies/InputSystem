using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SharpYaml.Events;
using System.Runtime.CompilerServices;

namespace SharpYaml.Model {
    public struct ChildIndex {
        public int Index;
        public bool IsKey;

        public ChildIndex(int index, bool isKey) {
            Index = index;
            IsKey = isKey;
        }

        public YamlNode Resolve(YamlNode parent) {
            var stream = parent as YamlStream;
            if (stream != null) {
                if (IsKey)
                    return null;
                if (Index < 0 || Index >= stream.Count)
                    return null;
                return stream[Index];
            }

            var document = parent as YamlDocument;
            if (document != null) {
                if (IsKey || Index != -1)
                    return null;
                return document.Contents;
            }

            var sequence = parent as YamlSequence;
            if (sequence != null) {
                if (IsKey)
                    return null;
                if (Index < 0 || Index >= sequence.Count)
                    return null;
                return sequence[Index];
            }

            var mapping = parent as YamlMapping;
            if (mapping != null) {
                if (Index < 0 || Index >= mapping.Count)
                    return null;
                return IsKey
                    ? mapping[Index].Key
                    : mapping[Index].Value;
            }

            return null;
        }

        public bool Equals(ChildIndex other) {
            return Index == other.Index && IsKey == other.IsKey;
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            return obj is ChildIndex && Equals((ChildIndex)obj);
        }

        public override int GetHashCode() {
            unchecked {
                return (Index * 397) ^ IsKey.GetHashCode();
            }
        }
    }

    public struct Path {
        public YamlNode Root;
        public ChildIndex[] Indices;

        public Path(YamlNode root, ChildIndex[] indices) {
            Root = root;
            Indices = indices;
        }

        public YamlNode Resolve() {
            var node = Root;
            foreach (var index in Indices) {
                if (node == null)
                    return null;

                node = index.Resolve(node);
            }

            return node;
        }

        public bool Equals(Path other) {
            return Equals(Root, other.Root) && Indices.SequenceEqual(other.Indices);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Path && Equals((Path)obj);
        }

        public override int GetHashCode() {
            unchecked {
                var rootHashCode = (Root != null ? Root.GetHashCode() : 0) * 397;
                return Indices == null ? rootHashCode : Indices.Aggregate(rootHashCode, (a, b) => a ^ (b.GetHashCode() * 397));
            }
        }

        public Path Clone() {
            return new Path(Root, Indices.ToArray());
        }
        
        public void Append(ChildIndex childIndex) {
            Array.Resize(ref Indices, Indices.Length + 1);
            Indices[Indices.Length - 1] = childIndex;
        }

        public Path? GetParentPath() {
            if (Indices.Length == 0)
                return null;
            
            return new Path(Root, Indices.Take(Indices.Length - 1).ToArray());
        }
    }
    
    public enum TrackerEventType {
        StreamDocumentAdded,
        StreamDocumentRemoved,
        StreamDocumentChanged,
        DocumentStartChanged,
        DocumentEndChanged,
        DocumentContentsChanged,
        SequenceStartChanged,
        SequenceElementAdded,
        SequenceElementRemoved,
        SequenceElementChanged,
        MappingStartChanged,
        MappingPairAdded,
        MappingPairRemoved,
        MappingPairChanged,
        ScalarPropertiesChanged,
        ScalarValueChanged
    }

    public abstract class TrackerEventArgs : EventArgs {
        public TrackerEventArgs(YamlNode node, IList<Path> parentPaths) {
            Node = node;
            ParentPaths = parentPaths;
        }

        public YamlNode Node { get; }
        public IList<Path> ParentPaths { get; }
        public abstract TrackerEventType EventType { get; }
    }

    public abstract class ChildEventArgs<TParent, TChild> : TrackerEventArgs where TParent : YamlNode {
        public ChildEventArgs(TParent node, IList<Path> parentPaths, TChild child, int index) :
            base(node, parentPaths) {
            Child = child;
            Index = index;
        }

        public new TParent Node { get { return (TParent)base.Node; } }
        public TChild Child { get; }
        public int Index { get; }
    }

    public abstract class ChildChangedEventArgs<TParent, TChild> : TrackerEventArgs where TParent : YamlNode {
        public ChildChangedEventArgs(TParent node, IList<Path> parentPaths, TChild oldChild, TChild newChild, int index) :
            base(node, parentPaths) {
            OldChild = oldChild;
            NewChild = newChild;
            Index = index;
        }

        public new TParent Node { get { return (TParent)base.Node; } }
        public TChild OldChild { get; }
        public TChild NewChild { get; }
        public int Index { get; }
    }

    public abstract class PropertyChangedEventArgs<TNode, TProperty> : TrackerEventArgs where TNode : YamlNode {
        public PropertyChangedEventArgs(TNode node, IList<Path> parentPaths, TProperty oldValue, TProperty newValue)
            : base(node, parentPaths) {
            OldValue = oldValue;
            NewValue = newValue;
        }

        public new TNode Node { get { return (TNode)base.Node; } }
        public TProperty OldValue { get; }
        public TProperty NewValue { get; }
    }

    public class StreamDocumentAdded : ChildEventArgs<YamlStream, YamlDocument> {
        public StreamDocumentAdded(YamlStream node, IList<Path> parentPaths, YamlDocument child, int index)
            : base(node, parentPaths, child, index) { }

        public override TrackerEventType EventType {
            get => TrackerEventType.StreamDocumentAdded;
        }
    }

    public class StreamDocumentRemoved : ChildEventArgs<YamlStream, YamlDocument> {
        public StreamDocumentRemoved(YamlStream node, IList<Path> parentPaths, YamlDocument child, int index)
            : base(node, parentPaths, child, index) { }

        public override TrackerEventType EventType {
            get => TrackerEventType.StreamDocumentRemoved;
        }
    }

    public class StreamDocumentChanged : ChildChangedEventArgs<YamlStream, YamlDocument> {
        public StreamDocumentChanged(YamlStream node, IList<Path> parentPaths, YamlDocument oldChild, YamlDocument newChild, int index)
            : base(node, parentPaths, oldChild, newChild, index) { }

        public override TrackerEventType EventType {
            get => TrackerEventType.StreamDocumentChanged;
        }
    }

    public class DocumentStartChanged : PropertyChangedEventArgs<YamlDocument, DocumentStart> {
        public DocumentStartChanged(YamlDocument node, IList<Path> parentPaths, DocumentStart oldValue, DocumentStart newValue)
            : base(node, parentPaths, oldValue, newValue) { }

        public override TrackerEventType EventType {
            get => TrackerEventType.DocumentStartChanged;
        }
    }

    public class DocumentEndChanged : PropertyChangedEventArgs<YamlDocument, DocumentEnd> {
        public DocumentEndChanged(YamlDocument node, IList<Path> parentPaths, DocumentEnd oldValue, DocumentEnd newValue)
            : base(node, parentPaths, oldValue, newValue) { }

        public override TrackerEventType EventType {
            get => TrackerEventType.DocumentEndChanged;
        }
    }

    public class DocumentContentsChanged : PropertyChangedEventArgs<YamlDocument, YamlElement> {
        public DocumentContentsChanged(YamlDocument node, IList<Path> parentPaths, YamlElement oldValue, YamlElement newValue)
            : base(node, parentPaths, oldValue, newValue) { }

        public override TrackerEventType EventType {
            get => TrackerEventType.DocumentContentsChanged;
        }
    }

    public class SequenceStartChanged : PropertyChangedEventArgs<YamlSequence, SequenceStart> {
        public SequenceStartChanged(YamlSequence node, IList<Path> parentPaths, SequenceStart oldValue, SequenceStart newValue)
            : base(node, parentPaths, oldValue, newValue) { }

        public override TrackerEventType EventType {
            get => TrackerEventType.SequenceStartChanged;
        }
    }

    public class SequenceElementAdded : ChildEventArgs<YamlSequence, YamlElement> {
        public SequenceElementAdded(YamlSequence node, IList<Path> parentPaths, YamlElement child, int index)
            : base(node, parentPaths, child, index) { }

        public override TrackerEventType EventType {
            get => TrackerEventType.SequenceElementAdded;
        }
    }

    public class SequenceElementRemoved : ChildEventArgs<YamlSequence, YamlElement> {
        public SequenceElementRemoved(YamlSequence node, IList<Path> parentPaths, YamlElement child, int index)
            : base(node, parentPaths, child, index) { }

        public override TrackerEventType EventType {
            get => TrackerEventType.SequenceElementRemoved;
        }
    }

    public class SequenceElementChanged : ChildChangedEventArgs<YamlSequence, YamlElement> {
        public SequenceElementChanged(YamlSequence node, IList<Path> parentPaths, YamlElement oldChild, YamlElement newChild, int index)
            : base(node, parentPaths, oldChild, newChild, index) { }

        public override TrackerEventType EventType {
            get => TrackerEventType.SequenceElementChanged;
        }
    }

    public class MappingStartChanged : PropertyChangedEventArgs<YamlMapping, MappingStart> {
        public MappingStartChanged(YamlMapping node, IList<Path> parentPaths, MappingStart oldValue, MappingStart newValue)
            : base(node, parentPaths, oldValue, newValue) { }

        public override TrackerEventType EventType {
            get => TrackerEventType.MappingStartChanged;
        }
    }

    public class MappingPairAdded : ChildEventArgs<YamlMapping, KeyValuePair<YamlElement, YamlElement>> {
        public MappingPairAdded(YamlMapping node, IList<Path> parentPaths, KeyValuePair<YamlElement, YamlElement> child, int index)
            : base(node, parentPaths, child, index) { }

        public override TrackerEventType EventType {
            get => TrackerEventType.MappingPairAdded;
        }
    }

    public class MappingPairRemoved : ChildEventArgs<YamlMapping, KeyValuePair<YamlElement, YamlElement>> {
        public MappingPairRemoved(YamlMapping node, IList<Path> parentPaths, KeyValuePair<YamlElement, YamlElement> child, int index)
            : base(node, parentPaths, child, index) { }

        public override TrackerEventType EventType {
            get => TrackerEventType.MappingPairRemoved;
        }
    }

    public class MappingPairChanged : ChildChangedEventArgs<YamlMapping, KeyValuePair<YamlElement, YamlElement>> {
        public MappingPairChanged(YamlMapping node, IList<Path> parentPaths, KeyValuePair<YamlElement, YamlElement> oldChild, KeyValuePair<YamlElement, YamlElement> newChild, int index)
            : base(node, parentPaths, oldChild, newChild, index) { }

        public override TrackerEventType EventType {
            get => TrackerEventType.MappingPairChanged;
        }
    }

    public class ScalarPropertiesChanged : PropertyChangedEventArgs<YamlValue, Scalar> {
        public ScalarPropertiesChanged(YamlValue node, IList<Path> parentPaths, Scalar oldValue, Scalar newValue) : base(node, parentPaths, oldValue, newValue) { }

        public override TrackerEventType EventType {
            get => TrackerEventType.ScalarPropertiesChanged;
        }
    }

    public class ScalarValueChanged : PropertyChangedEventArgs<YamlValue, string> {
        public ScalarValueChanged(YamlValue node, IList<Path> parentPaths, string oldValue, string newValue) : base(node, parentPaths, oldValue, newValue) { }

        public override TrackerEventType EventType {
            get => TrackerEventType.ScalarValueChanged;
        }
    }

    public class YamlNodeTracker {
        struct ParentAndIndex {
            public YamlNode Parent;
            public ChildIndex Index;

            public ParentAndIndex(YamlNode parent, ChildIndex index) {
                Parent = parent;
                Index = index;
            }

            public bool Equals(ParentAndIndex other) {
                return Equals(Parent, other.Parent) && Index.Equals(other.Index);
            }

            public override bool Equals(object obj) {
                if (ReferenceEquals(null, obj)) return false;
                return obj is ParentAndIndex && Equals((ParentAndIndex)obj);
            }

            public override int GetHashCode() {
                unchecked {
                    return ((Parent != null ? Parent.GetHashCode() : 0) * 397) ^ Index.GetHashCode();
                }
            }
        }

#if NET35
        Dictionary<YamlNode, ICollection<ParentAndIndex>> parents = new Dictionary<YamlNode, ICollection<ParentAndIndex>>();
#else
        private ConditionalWeakTable<YamlNode, ICollection<ParentAndIndex>> parents = new ConditionalWeakTable<YamlNode, ICollection<ParentAndIndex>>();
#endif

        void AddChild(YamlNode child, YamlNode parent, ChildIndex relationship) {
            var pi = new ParentAndIndex(parent, relationship);

            ICollection<ParentAndIndex> set;
            if (!parents.TryGetValue(child, out set)) {
                set = new[] { pi };
                parents.Add(child, set);
                return;
            }

            var array = set as ParentAndIndex[];
            if (array != null) {
                if (array[0].Equals(pi))
                    return;
                
                var newSet = new HashSet<ParentAndIndex>();
                newSet.Add(array[0]);
                parents.Remove(child);
                set = newSet;
                parents.Add(child, set);
            }

            set.Add(pi);
        }

        void RemoveChild(YamlNode child, YamlNode parent, ChildIndex relationship) {
            ICollection<ParentAndIndex> set;
            if (!parents.TryGetValue(child, out set))
                return;

            if (set is ParentAndIndex[])
                parents.Remove(child);
            else
                set.Remove(new ParentAndIndex(parent, relationship));
        }

        void RemoveChildSubscribers(IList<Path> parentPaths, ChildIndex childIndex) {
            if (subscribers == null)
                return;

            foreach (var parentPath in parentPaths) {
                var childPath = parentPath.Clone();
                childPath.Append(childIndex);

                foreach (var subpath in pathTrie.GetSubpaths(childPath)) {
                    subscribers.Remove(subpath);
                }

                pathTrie.Remove(childPath, true);
            }
        }

        void ShiftChild(YamlNode child, YamlNode parent, ChildIndex relationship, int shift) {
            ICollection<ParentAndIndex> set;
            if (!parents.TryGetValue(child, out set))
                return;

            var newChildIndex = new ChildIndex(relationship.Index + shift, relationship.IsKey);

            if (subscribers != null) {
                var oldPaths = GetPaths(child);

                foreach (var oldPath in oldPaths) {
                    var newPaths = new List<Path>();
                    foreach (var subpath in pathTrie.GetSubpaths(oldPath)) {
                        Dictionary<WeakReference, string> subs;
                        if (!subscribers.TryGetValue(subpath, out subs))
                            continue;

                        var newPath = subpath.Clone();
                        newPath.Indices[oldPath.Indices.Length - 1] = newChildIndex;

                        subscribers.Remove(oldPath);
                        subscribers[newPath] = subs;
                        newPaths.Add(newPath);
                    }

                    pathTrie.Remove(oldPath, true);
                    foreach (var newPath in newPaths)
                        pathTrie.Add(newPath);
                }
            }

            var array = set as ParentAndIndex[];
            if (array != null) {
                array[0] = new ParentAndIndex(parent, newChildIndex);
            } else {
                set.Remove(new ParentAndIndex(parent, relationship));
                set.Add(new ParentAndIndex(parent, newChildIndex));
            }
        }

        public IList<Path> GetPaths(YamlNode child) {
            if (child is YamlStream)
                return new Path[0];

            ICollection<ParentAndIndex> relationships;
            if (!parents.TryGetValue(child, out relationships))
                return new Path[0];

            var result = new List<Path>();

            foreach (var childRelationship in relationships) {
                var prePaths = GetPaths(childRelationship.Parent);

                if (prePaths.Count > 0) {
                    foreach (var prePath in prePaths) {
                        var path = prePath;
                        path.Append(childRelationship.Index);
                        result.Add(path);
                    }
                }
                else
                    result.Add(new Path(childRelationship.Parent, new[] { childRelationship.Index }));
            }

            return result;
        }

        internal void OnStreamAddDocument(YamlStream sender, YamlDocument newDocument, int index, ICollection<YamlDocument> nextChildren) {
            var paths = GetPaths(sender);
            AddChild(newDocument, sender, new ChildIndex(index, false));

            if (nextChildren != null) {
                // Need to iterate children in reverse so that the subscribers don't get mixed up as each index is incremented.
                var nextIndex = index + nextChildren.Count - 1;
                foreach (var nextChild in nextChildren.Reverse())
                    ShiftChild(nextChild, sender, new ChildIndex(nextIndex--, false), 1);
            }
            
            OnTrackerEvent(new StreamDocumentAdded(sender, paths, newDocument, index));
        }

        internal void OnStreamRemoveDocument(YamlStream sender, YamlDocument removedDocument, int index, IEnumerable<YamlDocument> nextChildren) {
            var paths = GetPaths(sender);
            RemoveChild(removedDocument, sender, new ChildIndex(index, false));

            OnTrackerEvent(new StreamDocumentRemoved(sender, paths, removedDocument, index));

            RemoveChildSubscribers(paths, new ChildIndex(index, false));
            
            if (nextChildren != null) {
                var nextIndex = index + 1;
                foreach (var nextChild in nextChildren)
                    ShiftChild(nextChild, sender, new ChildIndex(nextIndex++, false), -1);
            }
        }

        internal void OnStreamDocumentChanged(YamlStream sender, int index, YamlDocument oldDocument, YamlDocument newDocument) {
            if (oldDocument == newDocument)
                return;

            var paths = GetPaths(sender);

            var r = new ChildIndex(index, false);
            RemoveChild(oldDocument, sender, r);
            AddChild(newDocument, sender, r);

            OnTrackerEvent(new StreamDocumentChanged(sender, paths, oldDocument, newDocument, index));
        }

        internal void OnDocumentStartChanged(YamlDocument sender, DocumentStart oldValue, DocumentStart newValue) {
            if (oldValue == newValue)
                return;

            OnTrackerEvent(new DocumentStartChanged(sender, GetPaths(sender), oldValue, newValue));
        }

        internal void OnDocumentEndChanged(YamlDocument sender, DocumentEnd oldValue, DocumentEnd newValue) {
            if (oldValue == newValue)
                return;

            OnTrackerEvent(new DocumentEndChanged(sender, GetPaths(sender), oldValue, newValue));
        }

        internal void OnDocumentContentsChanged(YamlDocument sender, YamlElement oldValue, YamlElement newValue) {
            if (oldValue == newValue)
                return;

            var paths = GetPaths(sender);

            if (oldValue != null)
                RemoveChild(oldValue, sender, new ChildIndex(-1, false));

            if (newValue != null)
                AddChild(newValue, sender, new ChildIndex(-1, false));

            OnTrackerEvent(new DocumentContentsChanged(sender, paths, oldValue, newValue));
        }

        internal void OnSequenceStartChanged(YamlSequence sender, SequenceStart oldValue, SequenceStart newValue) {
            if (oldValue == newValue)
                return;

            OnTrackerEvent(new SequenceStartChanged(sender, GetPaths(sender), oldValue, newValue));
        }

        internal void OnSequenceAddElement(YamlSequence sender, YamlElement newElement, int index, ICollection<YamlElement> nextChildren) {
            var paths = GetPaths(sender);
            AddChild(newElement, sender, new ChildIndex(index, false));

            if (nextChildren != null) {
                // Need to iterate children in reverse so that the subscribers don't get mixed up as each index is incremented.
                var nextIndex = index + nextChildren.Count - 1;
                foreach (var nextChild in nextChildren.Reverse()) 
                    ShiftChild(nextChild, sender, new ChildIndex(nextIndex--, false), 1);
            }
            
            OnTrackerEvent(new SequenceElementAdded(sender, paths, newElement, index));
        }

        internal void OnSequenceRemoveElement(YamlSequence sender, YamlElement removedElement, int index, IEnumerable<YamlElement> nextChildren) {
            var paths = GetPaths(sender);
            RemoveChild(removedElement, sender, new ChildIndex(index, false));
            
            OnTrackerEvent(new SequenceElementRemoved(sender, paths, removedElement, index));
            
            RemoveChildSubscribers(paths, new ChildIndex(index, false));

            if (nextChildren != null) {
                var nextIndex = index + 1;
                foreach (var nextChild in nextChildren)
                    ShiftChild(nextChild, sender, new ChildIndex(nextIndex++, false), -1);
            }
        }

        internal void OnSequenceElementChanged(YamlSequence sender, int index, YamlElement oldElement, YamlElement newElement) {
            if (newElement == oldElement)
                return;

            var paths = GetPaths(sender);

            var r = new ChildIndex(index, false);
            RemoveChild(oldElement, sender, r);
            RemoveChild(newElement, sender, r);
            OnTrackerEvent(new SequenceElementChanged(sender, paths, oldElement, newElement, index));
        }

        internal void OnMappingStartChanged(YamlMapping sender, MappingStart oldValue, MappingStart newValue) {
            if (oldValue == newValue)
                return;

            OnTrackerEvent(new MappingStartChanged(sender, GetPaths(sender), oldValue, newValue));
        }

        internal void OnMappingAddPair(YamlMapping sender, KeyValuePair<YamlElement, YamlElement> newPair, int index, ICollection<KeyValuePair<YamlElement, YamlElement>> nextChildren) {
            var paths = GetPaths(sender);

            AddChild(newPair.Key, sender, new ChildIndex(index, true));
            AddChild(newPair.Value, sender, new ChildIndex(index, false));

            if (nextChildren != null) {
                // Need to iterate children in reverse so that the subscribers don't get mixed up as each index is incremented.
                var nextIndex = index + nextChildren.Count - 1;
                foreach (var nextChild in nextChildren.Reverse()) {
                    ShiftChild(nextChild.Key, sender, new ChildIndex(nextIndex, true), 1);
                    ShiftChild(nextChild.Value, sender, new ChildIndex(nextIndex--, false), 1);
                }
            }

            OnTrackerEvent(new MappingPairAdded(sender, paths, newPair, index));
        }

        internal void OnMappingRemovePair(YamlMapping sender, KeyValuePair<YamlElement, YamlElement> removedPair, int index, IEnumerable<KeyValuePair<YamlElement, YamlElement>> nextChildren) {
            var paths = GetPaths(sender);

            RemoveChild(removedPair.Key, sender, new ChildIndex(index, true));
            RemoveChild(removedPair.Value, sender, new ChildIndex(index, false));
            
            OnTrackerEvent(new MappingPairRemoved(sender, paths, removedPair, index));

            RemoveChildSubscribers(paths, new ChildIndex(index, true));
            RemoveChildSubscribers(paths, new ChildIndex(index, false));

            if (nextChildren != null) {
                var nextIndex = index + 1;
                foreach (var nextChild in nextChildren) {
                    ShiftChild(nextChild.Key, sender, new ChildIndex(nextIndex, true), -1);
                    ShiftChild(nextChild.Value, sender, new ChildIndex(nextIndex++, false), -1);
                }
            }
        }

        internal void OnMappingPairChanged(YamlMapping sender, int index, KeyValuePair<YamlElement, YamlElement> oldPair, KeyValuePair<YamlElement, YamlElement> newPair) {
            var paths = GetPaths(sender);

            if (oldPair.Key != newPair.Key) {
                var r = new ChildIndex(index, true);
                RemoveChild(oldPair.Key, sender, r);
                AddChild(newPair.Key, sender, r);
            }

            if (oldPair.Value != newPair.Value) {
                var r = new ChildIndex(index, false);
                RemoveChild(oldPair.Value, sender, r);
                AddChild(newPair.Value, sender, r);
            }

            OnTrackerEvent(new MappingPairChanged(sender, paths, oldPair, newPair, index));
        }

        internal void OnValueScalarPropertiesChanged(YamlValue sender, Scalar oldValue, Scalar newValue) {
            if (oldValue == newValue)
                return;

            OnTrackerEvent(new ScalarPropertiesChanged(sender, GetPaths(sender), oldValue, newValue));
        }

        internal void OnValueScalarChanged(YamlValue sender, string oldValue, string newValue) {
            if (oldValue == newValue)
                return;

            OnTrackerEvent(new ScalarValueChanged(sender, GetPaths(sender), oldValue, newValue));
        }

        void OnTrackerEvent(TrackerEventArgs eventArgs) {
            if (TrackerEvent != null)
                TrackerEvent(this, eventArgs);

            if (subscribers != null) {
                InvokeSubscribers(null, eventArgs);

                var allPaths = new HashSet<Path>();

                foreach (var path in eventArgs.ParentPaths) {
                    allPaths.Add(path);

                    Path? parentPath = path;
                    do {
                        allPaths.Add(parentPath.Value);
                        parentPath = parentPath.Value.GetParentPath();
                    } while (parentPath != null);
                }

                foreach (var path in allPaths) {
                    InvokeSubscribers(path, eventArgs);
                }
            }
        }

        void InvokeSubscribers(Path? path, TrackerEventArgs eventArgs) {
            Dictionary<WeakReference, string> dict;

            if (path.HasValue) {
                if (!subscribers.TryGetValue(path.Value, out dict))
                    return;
            }
            else dict = noFilterSubscribers;

            foreach (var pair in dict) {
                if (!pair.Key.IsAlive)
                    continue;

                var target = pair.Key.Target;

                var method = target.GetType().GetMethod(pair.Value);
                method.Invoke(target, new object[] { eventArgs });
            }
        }

        public event EventHandler<TrackerEventArgs> TrackerEvent;

        private Dictionary<Path, Dictionary<WeakReference, string>> subscribers;
        private Dictionary<WeakReference, string> noFilterSubscribers;
        private PathTrie pathTrie;

        void CompactSubscribers() {
            foreach (var path in subscribers.Keys.ToArray()) {
                var dict = subscribers[path];

                foreach (var key in dict.Keys.ToArray()) {
                    if (!key.IsAlive)
                        dict.Remove(key);
                }

                if (dict.Count == 0) {
                    subscribers.Remove(path);
                    pathTrie.Remove(path, false);
                }
            }

            foreach (var key in noFilterSubscribers.Keys.ToArray()) {
                if (!key.IsAlive)
                    noFilterSubscribers.Remove(key);
            }
        }

        public void Subscribe(object subscriber, Path? filterPath, string methodName) {
            if (subscribers == null) {
                subscribers = new Dictionary<Path, Dictionary<WeakReference, string>>();
                noFilterSubscribers = new Dictionary<WeakReference, string>();
                pathTrie = new PathTrie();
            }

            CompactSubscribers();

            Dictionary<WeakReference, string> dict;
            if (filterPath.HasValue) {
                if (!subscribers.TryGetValue(filterPath.Value, out dict)) {
                    dict = new Dictionary<WeakReference, string>();
                    subscribers[filterPath.Value] = dict;
                    pathTrie.Add(filterPath.Value);
                }
            }
            else
                dict = noFilterSubscribers;

            var reference = new WeakReference(subscriber);

            if (dict.ContainsKey(reference))
                throw new Exception("Object already subscribed.");

            dict[reference] = methodName;
        }

        public void Unsubscribe(object subscriber) {
            foreach (var pair in subscribers) {
                pair.Value.Remove(new WeakReference(subscriber));
            }
        }
    }
}
