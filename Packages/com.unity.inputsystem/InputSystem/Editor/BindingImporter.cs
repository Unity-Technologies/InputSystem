#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEditor.IMGUI.Controls;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    [CustomEditor(typeof(InputBindingAsset))]
    internal class InputBindingAssetEditor : PropertyDrawer
    {
        [SerializeField] public InputAction action;
        
        //private InputAction m_Action;
        
        /*protected override void OnHeaderGUI()
        {
            base.OnHeaderGUI();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
        }

        public void OnEnable()
        {
            //m_Action = new InputAction(name);
        }

        public void OnDisable()
        {
            //m_Action = null;
        }

        public void OnDestroy()
        {
            
        }
        
#if UNITY_2023_2_OR_NEWER
        [System.Obsolete("CanCacheInspectorGUI has been deprecated and is no longer used.", false)]
#endif
        /*public override bool CanCacheInspectorGUI(SerializedProperty property)
        {
            return false;
        }*/
        
        private class InputActionDrawerViewData
        {
            public InputActionTreeView TreeView;
            public InputControlPickerState ControlPickerState;
        }
        
        private void InitTreeIfNeeded(SerializedProperty property)
        {
            // NOTE: Unlike InputActionEditorWindow, we do not need to protect against the SerializedObject
            //       changing behind our backs by undo/redo here. Being a PropertyDrawer, we will automatically
            //       get recreated by Unity when it touches our serialized data.

            var viewData = GetOrCreateViewData(property);
            var propertyIsClone = IsPropertyAClone(property);

            if (!propertyIsClone && viewData.TreeView != null && viewData.TreeView.serializedObject == property.serializedObject)
                return;

            if (propertyIsClone)
                ResetProperty(property);

            viewData.TreeView = new InputActionTreeView(property.serializedObject)
            {
                onBuildTree = () => BuildTree(property),
                onDoubleClick = item => OnItemDoubleClicked(item, property),
                drawActionPropertiesButton = true,
                title = (GetPropertyTitle(property), property.GetTooltip())
            };
            viewData.TreeView.Reload();
        }

        private void SetNameIfNotSet(SerializedProperty actionProperty)
        {
            var nameProperty = actionProperty.FindPropertyRelative("m_Name");
            if (!string.IsNullOrEmpty(nameProperty.stringValue))
                return;

            // Special case for InputActionProperty where we want to take the name not from
            // the m_Action property embedded in it but rather from the InputActionProperty field
            // itself.
            var name = actionProperty.displayName;
            var parent = actionProperty.GetParentProperty();
            if (parent != null && parent.type == "InputActionProperty")
                name = parent.displayName;

            var suffix = GetSuffixToRemoveFromPropertyDisplayName();
            if (name.EndsWith(suffix))
                name = name.Substring(0, name.Length - suffix.Length);

            // If it's a singleton action, we also need to adjust the InputBinding.action
            // property values in its binding list.
            var singleActionBindings = actionProperty.FindPropertyRelative("m_SingletonActionBindings");
            if (singleActionBindings != null)
            {
                var bindingCount = singleActionBindings.arraySize;
                for (var i = 0; i < bindingCount; ++i)
                {
                    var binding = singleActionBindings.GetArrayElementAtIndex(i);
                    var actionNameProperty = binding.FindPropertyRelative("m_Action");
                    actionNameProperty.stringValue = name;
                }
            }

            nameProperty.stringValue = name;

            actionProperty.serializedObject.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(actionProperty.serializedObject.targetObject);
        }

        private static string GetPropertyTitle(SerializedProperty property)
        {
            var propertyTitleNumeral = string.Empty;
            if (property.GetParentProperty() != null && property.GetParentProperty().isArray)
                propertyTitleNumeral = $" {property.GetIndexOfArrayElement()}";

            var t = property.type.StartsWith("TypedInputAction"); // TODO Dirty quick fix, fix correctly  
            
            if (property.displayName != null &&
                property.displayName.Length > 0 &&
                (property.type == nameof(InputAction) || t || property.type == nameof(InputActionMap)))
            {
                return $"{property.displayName}{propertyTitleNumeral}";
            }

            return property.type == nameof(InputActionMap) ? $"Input Action Map{propertyTitleNumeral}" : $"Input Action{propertyTitleNumeral}";
        }

        private void OnItemDoubleClicked(ActionTreeItemBase item, SerializedProperty property)
        {
            var viewData = GetOrCreateViewData(property);

            // Double-clicking on binding or action item opens property popup.
            PropertiesViewBase propertyView = null;
            if (item is BindingTreeItem)
            {
                if (viewData.ControlPickerState == null)
                    viewData.ControlPickerState = new InputControlPickerState();
                propertyView = new InputBindingPropertiesView(item.property,
                    controlPickerState: viewData.ControlPickerState,
                    expectedControlLayout: item.expectedControlLayout,
                    onChange:
                    change => viewData.TreeView.Reload());
            }
            else if (item is ActionTreeItem)
            {
                propertyView = new InputActionPropertiesView(item.property,
                    onChange: change => viewData.TreeView.Reload());
            }

            if (propertyView != null)
            {
                var rect = new Rect(GUIUtility.GUIToScreenPoint(Event.current.mousePosition), Vector2.zero);
                PropertiesViewPopup.Show(rect, propertyView);
            }
        }
        
        private InputActionDrawerViewData GetOrCreateViewData(SerializedProperty property)
        {
            if (m_PerPropertyViewData == null)
                m_PerPropertyViewData = new Dictionary<string, InputActionDrawerViewData>();

            if (m_PerPropertyViewData.TryGetValue(property.propertyPath, out var data)) return data;

            data = new InputActionDrawerViewData();
            m_PerPropertyViewData.Add(property.propertyPath, data);

            return data;
        }
        
        protected TreeViewItem BuildTree(SerializedProperty property)
        {
            return InputActionTreeView.BuildWithJustBindingsFromAction(property);
        }
        
        protected string GetSuffixToRemoveFromPropertyDisplayName()
        {
            return " Action";
        }
        protected bool IsPropertyAClone(SerializedProperty property)
        {
            // When a new item is added to a collection through the inspector, the default behaviour is
            // to create a clone of the previous item. Here we look at all InputActions that appear before
            // the current one and compare their Ids to determine if we have a clone. We don't look past
            // the current item because Unity will be calling this property drawer for each input action
            // in the collection in turn. If the user just added a new input action, and it's a clone, as
            // we work our way down the list, we'd end up thinking that an existing input action was a clone
            // of the newly added one, instead of the other way around. If we do have a clone, we need to
            // clear out some properties of the InputAction (id, name, and singleton action bindings) and
            // recreate the tree view.

            if (property?.GetParentProperty() == null || property.GetParentProperty().isArray == false)
                return false;

            var array = property.GetArrayPropertyFromElement();
            var index = property.GetIndexOfArrayElement();

            for (var i = 0; i < index; i++)
            {
                if (property.FindPropertyRelative(nameof(InputAction.m_Id))?.stringValue ==
                    array.GetArrayElementAtIndex(i)?.FindPropertyRelative(nameof(InputAction.m_Id))?.stringValue)
                    return true;
            }

            return false;
        }
        
        protected void ResetProperty(SerializedProperty property)
        {
            if (property == null) return;

            property.SetStringValue(nameof(InputAction.m_Id), Guid.NewGuid().ToString());
            property.SetStringValue(nameof(InputAction.m_Name), "Input Action");
            property.FindPropertyRelative(nameof(InputAction.m_SingletonActionBindings))?.ClearArray();
            property.serializedObject?.ApplyModifiedPropertiesWithoutUndo();
        }

        // Unity creates a single instance of a property drawer to draw multiple instances of the property drawer type,
        // so we can't store state in the property drawer for each item. We do need that though, because each InputAction
        // needs to have it's own instance of the InputActionTreeView to correctly draw it's own bindings. So what we do
        // is keep this array around that stores a tree view instance for each unique property path that the property
        // drawer encounters. The tree view will be recreated if we detect that the property being drawn has changed.
        private Dictionary<string, InputActionDrawerViewData> m_PerPropertyViewData;
        
        internal class PropertiesViewPopup : EditorWindow
        {
            public static void Show(Rect btnRect, PropertiesViewBase view)
            {
                var window = CreateInstance<PropertiesViewPopup>();
                window.m_PropertyView = view;
                window.ShowPopup();
                window.ShowAsDropDown(btnRect, new Vector2(300, 350));
            }

            private void OnGUI()
            {
                m_PropertyView.OnGUI();
            }

            private PropertiesViewBase m_PropertyView;
        }
    }
    
    /// <summary>
    /// Custom editor that allows modifying importer settings for a <see cref="BindingImporter"/>.
    /// </summary>
    [CustomEditor(typeof(BindingImporter))]
    internal class BindingImporterEditor : ScriptedImporterEditor
    {
        private void DrawInspector(SerializedObject obj)
        {
            EditorGUILayout.LabelField("Hello");
            //var property = obj.FindProperty("m_Bindings");
            
        }
        
        #if true
        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();
            
            // ScriptedImporterEditor in 2019.2 now requires explicitly updating the SerializedObject
            // like in other types of editors.
            EditorGUI.BeginChangeCheck();
            serializedObject.UpdateIfRequiredOrScript();
            DrawInspector(serializedObject);
            serializedObject.ApplyModifiedProperties();
            EditorGUI.EndChangeCheck();
        
            ApplyRevertGUI(); // Note: required when using UpdateIfRequiredOrScript and ApplyModifiedProperties above.    
        }
        #else
        public override VisualElement CreateInspectorGUI()
        {
            // See: https://docs.unity3d.com/Manual/UIE-HowTo-CreateCustomInspector.html for guidance
            VisualElement editor = new VisualElement();
            editor.Add(new Label("This is a custom Inspector"));
            return editor;
        }
        #endif
    }
    
    // TODO If its a scriptable object we do not really need importer?

    /// <summary>
    /// Scripted importer for InputBindingAsset assets.22
    /// </summary>
    [ScriptedImporter(version: kVersion, ext: kExtension)]
    internal sealed class BindingImporter : ScriptedImporter
    {
        private const int kVersion = 1;
        private const string kExtension = "binding";
        private const string kDefaultAssetFilename = "New Binding";
        private const string kIcon = "Packages/com.unity.inputsystem/InputSystem/Editor/Icons/InputAction.png";
        private const string kDefaultContent = "{}";

        public static string GetFileName(string path)
        {
            return path + "." + kExtension;
        }

        public static Texture2D GetIcon()
        {
            return (Texture2D)EditorGUIUtility.Load(kIcon);
        }
        
        public override void OnImportAsset(AssetImportContext ctx)
        {
            if (ctx == null)
                throw new ArgumentNullException(nameof(ctx));

            try
            {
                ImportBindingAsset(ctx);
            }
            catch (Exception e)
            {
                ctx.LogImportError($"Failed to import asset '{ctx.assetPath}': ({e})");
                Debug.LogException(e);
            }
        }

        private static void ImportBindingAsset(AssetImportContext ctx)
        {
            var text = File.ReadAllText(ctx.assetPath);
            
            var thumbnail = GetIcon();
            var asset = ScriptableObject.CreateInstance<InputBindingAsset>();
            asset.name = Path.GetFileNameWithoutExtension(ctx.assetPath);
            //asset.m_Bindings = Array.Empty<InputBinding>();
            ctx.AddObjectToAsset("<root>", asset, thumbnail);
            ctx.SetMainObject(asset);
            
            /*if (asset.m_Bindings == null) 
                return;
            
            // Make sure all bindings have GUIDs.
            for (var i = 0; i < asset.m_Bindings.Length; ++i)
            {
                // TODO Utilize asset GUID?!
                var bindingId = asset.m_Bindings[i].m_Id;
                if (string.IsNullOrEmpty(bindingId))
                    asset.m_Bindings[i].GenerateId();
            }*/
        }
        
        [MenuItem("Assets/Create/Input Binding/Empty (Default)")]
        public static void CreateBindingAsset()
        {
            ProjectWindowUtil.CreateAssetWithContent(
                filename: $"{kDefaultAssetFilename}.{kExtension}",
                content: kDefaultContent, 
                icon: (Texture2D)EditorGUIUtility.Load(kIcon));
        }
    }
}

#endif