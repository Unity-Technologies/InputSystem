#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine.InputSystem.Utilities;

////TODO: sync expanded state of SerializedProperties to expanded state of tree (will help preserving expansion in inspector)

////REVIEW: would be great to align all "[device]" parts of binding strings neatly in a column

namespace UnityEngine.InputSystem.Editor
{
    internal abstract class ActionTreeItemBase : TreeViewItem
    {
        public SerializedProperty property { get; }
        public virtual string expectedControlLayout => string.Empty;
        public virtual bool canRename => true;
        public virtual bool serializedDataIncludesChildren => false;
        public abstract GUIStyle colorTagStyle { get; }
        public string name { get; }
        public Guid guid { get; }
        public virtual bool showWarningIcon => false;

        // For some operations (like copy-paste), we want to include information that we have filtered out.
        internal List<ActionTreeItemBase> m_HiddenChildren;
        public bool hasChildrenIncludingHidden => hasChildren || (m_HiddenChildren != null && m_HiddenChildren.Count > 0);
        public IEnumerable<ActionTreeItemBase> hiddenChildren => m_HiddenChildren ?? Enumerable.Empty<ActionTreeItemBase>();
        public IEnumerable<ActionTreeItemBase> childrenIncludingHidden
        {
            get
            {
                if (hasChildren)
                    foreach (var child in children)
                        if (child is ActionTreeItemBase item)
                            yield return item;
                if (m_HiddenChildren != null)
                    foreach (var child in m_HiddenChildren)
                        yield return child;
            }
        }

        // Action data is generally stored in arrays. Action maps are stored in m_ActionMaps arrays in assets,
        // actions are stored in m_Actions arrays on maps and bindings are stored in m_Bindings arrays on maps.
        public SerializedProperty arrayProperty => property.GetArrayPropertyFromElement();

        // Dynamically look up the array index instead of just taking it from `property`.
        // This makes sure whatever insertions or deletions we perform on the serialized data,
        // we get the right array index from an item.
        public int arrayIndex => InputActionSerializationHelpers.GetIndex(arrayProperty, guid);

        protected ActionTreeItemBase(SerializedProperty property)
        {
            this.property = property;

            // Look up name.
            var nameProperty = property.FindPropertyRelative("m_Name");
            Debug.Assert(nameProperty != null, $"Cannot find m_Name property on {property.propertyPath}");
            name = nameProperty.stringValue;

            // Look up ID.
            var idProperty = property.FindPropertyRelative("m_Id");
            Debug.Assert(idProperty != null, $"Cannot find m_Id property on {property.propertyPath}");
            var idPropertyString = idProperty.stringValue;
            if (string.IsNullOrEmpty(idPropertyString))
            {
                // This is somewhat questionable but we can't operate if we don't have IDs on the data used in the tree.
                // Rather than requiring users of the tree to set this up consistently, we assign IDs
                // on the fly, if necessary.
                guid = Guid.NewGuid();
                idPropertyString = guid.ToString();
                idProperty.stringValue = idPropertyString;
                idProperty.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                guid = new Guid(idPropertyString);
            }

            // All our elements (maps, actions, bindings) carry unique IDs. We use their hash
            // codes as item IDs in the tree. This should result in stable item IDs that keep
            // identifying the right item across all reloads and tree mutations.
            id = guid.GetHashCode();
        }

        public virtual void Rename(string newName)
        {
            Debug.Assert(!canRename, "Item is marked as allowing renames yet does not implement Rename()");
        }

        /// <summary>
        /// Delete serialized data for the tree item and its children.
        /// </summary>
        public abstract void DeleteData();

        public abstract bool AcceptsDrop(ActionTreeItemBase item);

        /// <summary>
        /// Get information about where to drop an item of the given type and (optionally) the given index.
        /// </summary>
        public abstract bool GetDropLocation(Type itemType, int? childIndex, ref SerializedProperty array, ref int arrayIndex);

        protected static class Styles
        {
            private static GUIStyle StyleWithBackground(string fileName)
            {
                return new GUIStyle("Label").WithNormalBackground(AssetDatabase.LoadAssetAtPath<Texture2D>($"{InputActionTreeView.SharedResourcesPath}{fileName}.png"));
            }

            public static readonly GUIStyle yellowRect = StyleWithBackground("yellow");
            public static readonly GUIStyle greenRect = StyleWithBackground("green");
            public static readonly GUIStyle blueRect = StyleWithBackground("blue");
            public static readonly GUIStyle pinkRect = StyleWithBackground("pink");
        }
    }

    /// <summary>
    /// Tree view item for an action map.
    /// </summary>
    /// <seealso cref="InputActionMap"/>
    internal class ActionMapTreeItem : ActionTreeItemBase
    {
        public ActionMapTreeItem(SerializedProperty actionMapProperty)
            : base(actionMapProperty)
        {
        }

        public override GUIStyle colorTagStyle => Styles.yellowRect;
        public SerializedProperty bindingsArrayProperty => property.FindPropertyRelative("m_Bindings");
        public SerializedProperty actionsArrayProperty => property.FindPropertyRelative("m_Actions");
        public override bool serializedDataIncludesChildren => true;

        public override void Rename(string newName)
        {
            InputActionSerializationHelpers.RenameActionMap(property, newName);
        }

        public override void DeleteData()
        {
            var assetObject = property.serializedObject;
            if (!(assetObject.targetObject is InputActionAsset))
                throw new InvalidOperationException(
                    $"Action map must be part of InputActionAsset but is in {assetObject.targetObject} instead");

            InputActionSerializationHelpers.DeleteActionMap(assetObject, guid);
        }

        public override bool AcceptsDrop(ActionTreeItemBase item)
        {
            return item is ActionTreeItem;
        }

        public override bool GetDropLocation(Type itemType, int? childIndex, ref SerializedProperty array, ref int arrayIndex)
        {
            // Drop actions into action array.
            if (itemType == typeof(ActionTreeItem))
            {
                array = actionsArrayProperty;
                arrayIndex = childIndex ?? -1;
                return true;
            }

            // For action maps in assets, drop other action maps next to them.
            if (itemType == typeof(ActionMapTreeItem) && property.serializedObject.targetObject is InputActionAsset)
            {
                array = property.GetArrayPropertyFromElement();
                arrayIndex = this.arrayIndex + 1;
                return true;
            }

            ////REVIEW: would be nice to be able to replace the entire contents of a map in the inspector by dropping in another map

            return false;
        }

        public static ActionMapTreeItem AddTo(TreeViewItem parent, SerializedProperty actionMapProperty)
        {
            var item = new ActionMapTreeItem(actionMapProperty);

            item.depth = parent.depth + 1;
            item.displayName = item.name;
            parent.AddChild(item);

            return item;
        }

        public void AddActionsTo(TreeViewItem parent)
        {
            AddActionsTo(parent, addBindings: false);
        }

        public void AddActionsAndBindingsTo(TreeViewItem parent)
        {
            AddActionsTo(parent, addBindings: true);
        }

        private void AddActionsTo(TreeViewItem parent, bool addBindings)
        {
            var actionsArrayProperty = this.actionsArrayProperty;
            Debug.Assert(actionsArrayProperty != null, $"Cannot find m_Actions in {property}");

            for (var i = 0; i < actionsArrayProperty.arraySize; i++)
            {
                var actionProperty = actionsArrayProperty.GetArrayElementAtIndex(i);
                var actionItem = ActionTreeItem.AddTo(parent, property, actionProperty);

                if (addBindings)
                    actionItem.AddBindingsTo(actionItem);
            }
        }

        public static void AddActionMapsFromAssetTo(TreeViewItem parent, SerializedObject assetObject)
        {
            var actionMapsArrayProperty = assetObject.FindProperty("m_ActionMaps");
            Debug.Assert(actionMapsArrayProperty != null, $"Cannot find m_ActionMaps in {assetObject}");
            Debug.Assert(actionMapsArrayProperty.isArray, $"m_ActionMaps in {assetObject} is not an array");

            var mapCount = actionMapsArrayProperty.arraySize;
            for (var i = 0; i < mapCount; ++i)
            {
                var mapProperty = actionMapsArrayProperty.GetArrayElementAtIndex(i);
                AddTo(parent, mapProperty);
            }
        }
    }

    /// <summary>
    /// Tree view item for an action.
    /// </summary>
    /// <see cref="InputAction"/>
    internal class ActionTreeItem : ActionTreeItemBase
    {
        public ActionTreeItem(SerializedProperty actionMapProperty, SerializedProperty actionProperty)
            : base(actionProperty)
        {
            this.actionMapProperty = actionMapProperty;
        }

        public SerializedProperty actionMapProperty { get; }
        public override GUIStyle colorTagStyle => Styles.greenRect;
        public bool isSingletonAction => actionMapProperty == null;

        public override string expectedControlLayout
        {
            get
            {
                var expectedControlType = property.FindPropertyRelative("m_ExpectedControlType").stringValue;
                if (!string.IsNullOrEmpty(expectedControlType))
                    return expectedControlType;

                var type = property.FindPropertyRelative("m_Type").intValue;
                if (type == (int)InputActionType.Button)
                    return "Button";

                return null;
            }
        }

        public SerializedProperty bindingsArrayProperty => isSingletonAction
        ? property.FindPropertyRelative("m_SingletonActionBindings")
            : actionMapProperty.FindPropertyRelative("m_Bindings");

        // If we're a singleton action (no associated action map property), we include all our bindings in the
        // serialized data.
        public override bool serializedDataIncludesChildren => actionMapProperty == null;

        public override void Rename(string newName)
        {
            InputActionSerializationHelpers.RenameAction(property, actionMapProperty, newName);
        }

        public override void DeleteData()
        {
            InputActionSerializationHelpers.DeleteActionAndBindings(actionMapProperty, guid);
        }

        public override bool AcceptsDrop(ActionTreeItemBase item)
        {
            return item is BindingTreeItem && !(item is PartOfCompositeBindingTreeItem);
        }

        public override bool GetDropLocation(Type itemType, int? childIndex, ref SerializedProperty array, ref int arrayIndex)
        {
            // Drop bindings into binding array.
            if (typeof(BindingTreeItem).IsAssignableFrom(itemType))
            {
                array = bindingsArrayProperty;

                // Indexing by tree items is relative to each action but indexing in
                // binding array is global for all actions in a map. Adjust index accordingly.
                // NOTE: Bindings for any one action need not be stored contiguously in the binding array
                //       so we can't just add something to the index of the first binding to the action.
                arrayIndex =
                    InputActionSerializationHelpers.ConvertBindingIndexOnActionToBindingIndexInArray(
                        array, name, childIndex ?? -1);

                return true;
            }

            // Drop other actions next to us.
            if (itemType == typeof(ActionTreeItem))
            {
                array = arrayProperty;
                arrayIndex = this.arrayIndex + 1;
                return true;
            }

            return false;
        }

        public static ActionTreeItem AddTo(TreeViewItem parent, SerializedProperty actionMapProperty, SerializedProperty actionProperty)
        {
            var item = new ActionTreeItem(actionMapProperty, actionProperty);

            item.depth = parent.depth + 1;
            item.displayName = item.name;
            parent.AddChild(item);

            return item;
        }

        /// <summary>
        /// Add items for the bindings of just this action to the given parent tree item.
        /// </summary>
        public void AddBindingsTo(TreeViewItem parent)
        {
            var isSingleton = actionMapProperty == null;
            var bindingsArrayProperty = isSingleton
                ? property.FindPropertyRelative("m_SingletonActionBindings")
                : actionMapProperty.FindPropertyRelative("m_Bindings");

            var bindingsCountInMap = bindingsArrayProperty.arraySize;
            var currentComposite = (CompositeBindingTreeItem)null;
            for (var i = 0; i < bindingsCountInMap; ++i)
            {
                var bindingProperty = bindingsArrayProperty.GetArrayElementAtIndex(i);

                // Skip if binding is not for action.
                var actionProperty = bindingProperty.FindPropertyRelative("m_Action");
                Debug.Assert(actionProperty != null, $"Could not find m_Action in {bindingProperty}");
                if (!actionProperty.stringValue.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                    continue;

                // See what kind of binding we have.
                var flagsProperty = bindingProperty.FindPropertyRelative("m_Flags");
                Debug.Assert(actionProperty != null, $"Could not find m_Flags in {bindingProperty}");
                var flags = (InputBinding.Flags)flagsProperty.intValue;
                if ((flags & InputBinding.Flags.PartOfComposite) != 0 && currentComposite != null)
                {
                    // Composite part binding.
                    PartOfCompositeBindingTreeItem.AddTo(currentComposite, bindingProperty);
                }
                else if ((flags & InputBinding.Flags.Composite) != 0)
                {
                    // Composite binding.
                    currentComposite = CompositeBindingTreeItem.AddTo(parent, bindingProperty);
                }
                else
                {
                    // "Normal" binding.
                    BindingTreeItem.AddTo(parent, bindingProperty);
                    currentComposite = null;
                }
            }
        }
    }

    /// <summary>
    /// Tree view item for a binding.
    /// </summary>
    /// <seealso cref="InputBinding"/>
    internal class BindingTreeItem : ActionTreeItemBase
    {
        public BindingTreeItem(SerializedProperty bindingProperty)
            : base(bindingProperty)
        {
            path = property.FindPropertyRelative("m_Path").stringValue;
            groups = property.FindPropertyRelative("m_Groups").stringValue;
            action = property.FindPropertyRelative("m_Action").stringValue;
        }

        public string path { get; }
        public string groups { get; }
        public string action { get; }
        public override bool showWarningIcon => InputSystem.ShouldDrawWarningIconForBinding(path);

        public override bool canRename => false;
        public override GUIStyle colorTagStyle => Styles.blueRect;

        public string displayPath =>
            !string.IsNullOrEmpty(path) ? InputControlPath.ToHumanReadableString(path) : "<No Binding>";

        private ActionTreeItem actionItem
        {
            get
            {
                // Find the action we're under.
                for (var node = parent; node != null; node = node.parent)
                    if (node is ActionTreeItem item)
                        return item;
                return null;
            }
        }

        public override string expectedControlLayout
        {
            get
            {
                var currentActionItem = actionItem;
                return currentActionItem != null ? currentActionItem.expectedControlLayout : string.Empty;
            }
        }

        public override void DeleteData()
        {
            var currentActionItem = actionItem;
            Debug.Assert(currentActionItem != null, "BindingTreeItem should always have a parent action");
            var bindingsArrayProperty = currentActionItem.bindingsArrayProperty;
            InputActionSerializationHelpers.DeleteBinding(bindingsArrayProperty, guid);
        }

        public override bool AcceptsDrop(ActionTreeItemBase item)
        {
            return false;
        }

        public override bool GetDropLocation(Type itemType, int? childIndex, ref SerializedProperty array, ref int arrayIndex)
        {
            // Drop bindings next to us.
            if (typeof(BindingTreeItem).IsAssignableFrom(itemType))
            {
                array = arrayProperty;
                arrayIndex = this.arrayIndex + 1;
                return true;
            }

            return false;
        }

        public static BindingTreeItem AddTo(TreeViewItem parent, SerializedProperty bindingProperty)
        {
            var item = new BindingTreeItem(bindingProperty);

            item.depth = parent.depth + 1;
            item.displayName = item.displayPath;
            parent.AddChild(item);

            return item;
        }
    }

    /// <summary>
    /// Tree view item for a composite binding.
    /// </summary>
    /// <seealso cref="InputBinding.isComposite"/>
    internal class CompositeBindingTreeItem : BindingTreeItem
    {
        public CompositeBindingTreeItem(SerializedProperty bindingProperty)
            : base(bindingProperty)
        {
        }

        public override GUIStyle colorTagStyle => Styles.blueRect;
        public override bool canRename => true;

        public string compositeName => NameAndParameters.ParseName(path);

        public override void Rename(string newName)
        {
            InputActionSerializationHelpers.RenameComposite(property, newName);
        }

        public override bool AcceptsDrop(ActionTreeItemBase item)
        {
            return item is PartOfCompositeBindingTreeItem;
        }

        public override bool GetDropLocation(Type itemType, int? childIndex, ref SerializedProperty array, ref int arrayIndex)
        {
            // Drop part binding into composite.
            if (itemType == typeof(PartOfCompositeBindingTreeItem))
            {
                array = arrayProperty;

                // Adjust child index by index of composite item itself.
                arrayIndex = childIndex != null
                    ? this.arrayIndex + 1 + childIndex.Value // Dropping at #0 should put as our index plus one.
                    : this.arrayIndex + 1 + InputActionSerializationHelpers.GetCompositePartCount(array, this.arrayIndex);

                return true;
            }

            // Drop other bindings next to us.
            if (typeof(BindingTreeItem).IsAssignableFrom(itemType))
            {
                array = arrayProperty;
                arrayIndex = this.arrayIndex + 1 +
                    InputActionSerializationHelpers.GetCompositePartCount(array, this.arrayIndex);
                return true;
            }

            return false;
        }

        public new static CompositeBindingTreeItem AddTo(TreeViewItem parent, SerializedProperty bindingProperty)
        {
            var item = new CompositeBindingTreeItem(bindingProperty);

            item.depth = parent.depth + 1;
            item.displayName = !string.IsNullOrEmpty(item.name)
                ? item.name
                : ObjectNames.NicifyVariableName(NameAndParameters.ParseName(item.path));

            parent.AddChild(item);

            return item;
        }
    }

    /// <summary>
    /// Tree view item for bindings that are parts of composites.
    /// </summary>
    /// <see cref="InputBinding.isPartOfComposite"/>
    internal class PartOfCompositeBindingTreeItem : BindingTreeItem
    {
        public PartOfCompositeBindingTreeItem(SerializedProperty bindingProperty)
            : base(bindingProperty)
        {
        }

        public override GUIStyle colorTagStyle => Styles.pinkRect;
        public override bool canRename => false;

        public override string expectedControlLayout
        {
            get
            {
                if (m_ExpectedControlLayout == null)
                {
                    var partName = name;
                    var compositeName = ((CompositeBindingTreeItem)parent).compositeName;
                    var layoutName = InputBindingComposite.GetExpectedControlLayoutName(compositeName, partName);
                    m_ExpectedControlLayout = layoutName ?? "";
                }

                return m_ExpectedControlLayout;
            }
        }

        private string m_ExpectedControlLayout;

        public new static PartOfCompositeBindingTreeItem AddTo(TreeViewItem parent, SerializedProperty bindingProperty)
        {
            var item = new PartOfCompositeBindingTreeItem(bindingProperty);

            item.depth = parent.depth + 1;
            item.displayName = $"{ObjectNames.NicifyVariableName(item.name)}: {item.displayPath}";
            parent.AddChild(item);

            return item;
        }
    }
}
#endif // UNITY_EDITOR
