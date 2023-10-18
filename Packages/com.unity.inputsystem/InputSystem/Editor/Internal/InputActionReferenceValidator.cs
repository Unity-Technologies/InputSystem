#if UNITY_EDITOR

using System.Linq;
using UnityEditor;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.Editor
{
    interface IInputActionAssetReferenceValidator
    {
        void OnInvalidate(InputActionAsset asset, InputActionReference reference);
        void OnInvalidReference(InputActionAsset asset, InputActionReference reference);
        void OnDuplicateReference(InputActionAsset asset, InputActionReference reference);
        void OnMissingReference(InputActionAsset asset, InputAction action);
    }

    internal class DefaultInputActionAssetReferenceValidator : IInputActionAssetReferenceValidator
    {
        public void OnInvalidate(InputActionAsset asset, InputActionReference reference)
        {
            reference.Invalidate();
        }

        public void OnInvalidReference(InputActionAsset asset, InputActionReference reference)
        {
            AssetDatabase.RemoveObjectFromAsset(reference);
            Undo.DestroyObjectImmediate(reference); // Enable undo
        }

        public void OnDuplicateReference(InputActionAsset asset, InputActionReference reference)
        {
            OnInvalidReference(asset, reference); // Same action, delete it
        }

        public void OnMissingReference(InputActionAsset asset, InputAction action)
        {
            var reference = InputActionReference.Create(action);
            AssetDatabase.AddObjectToAsset(reference, asset);
        }
    }

    internal class LoggingInputActionAssetReferenceValidator : IInputActionAssetReferenceValidator
    {
        public void OnInvalidate(InputActionAsset asset, InputActionReference reference)
        {
            Debug.Log($"OnInvalidate(asset: {asset}, reference:{reference})");
        }

        public void OnInvalidReference(InputActionAsset asset, InputActionReference reference)
        {
            Debug.Log($"OnInvalidReference(asset: {asset}, reference:{reference})");
        }

        public void OnDuplicateReference(InputActionAsset asset, InputActionReference reference)
        {
            Debug.Log($"OnDuplicateReference(asset: {asset}, reference:{reference})");
        }

        public void OnMissingReference(InputActionAsset asset, InputAction action)
        {
            Debug.Log($"OnMissingReference(asset: {asset}, action:{action})");
        }
    }

    // TODO Known issues:
    //      This doesn't work correctly, doing bulk updates like this works well only up to the point the user
    //      engages with the undo system. Scenario: Reference an action in the Inspector. Delete action in editor,
    //      notice that inspector changes to "Missing (Input Action Reference)" as object is destroyed.
    //      Then if user undo the operation there are two issues:
    //      - Editor is restored to an object not referencing an existing editor object.
    //      - The deletion in editor and inspector re-resolve is considered two different undo steps, they need
    //        to be an atomic step.
    //
    // TODO Instead we likely need to integrate all reference management into each step of the editor.
    internal class InputActionReferenceValidator
    {
        public static void ValidateReferences(InputActionAsset asset)
        {
            if (asset == null)
                return;

            //Debug.Log("ValidateReferences " + asset);

            void RemoveReferenceAtIndex(InputActionReference[] references, ref int count, int index)
            {
                var reference = references[index];
                ArrayHelpers.EraseAtByMovingTail(references, ref count, index);
                AssetDatabase.RemoveObjectFromAsset(reference);
                Undo.DestroyObjectImmediate(reference); // Enable undo
            }

            // Fetch input action references (Note that this will allocate an array)
            var references = InputActionImporter.LoadInputActionReferencesFromAsset(asset).ToArray();

            // Remove dangling references
            var initialCount = references.Length;
            var count = initialCount;
            for (var i = count - 1; i >= 0; --i)
            {
                // TODO Below comparison want work,. maybe ReferenceEquals works or find another way
                // If invalid (no asset, no action ID or not found within this asset) - remove the reference
                /*if (!references[i].m_Asset != asset)
                {
                    Debug.Log("Removing reference with invalid asset reference");
                    RemoveReferenceAtIndex(references, ref count, i);
                    continue;
                }*/

                var referencedAction = asset.FindActionById(references[i].m_ActionId);
                if (referencedAction == null)
                {
                    Debug.Log("Removing invalid or dangling InputActionReference: " + references[i]);
                    RemoveReferenceAtIndex(references, ref count, i);
                }
                else // Reference is associated with an action of this asset as expected
                {
                    // Look for first duplicate reference referencing the same action and eliminate this reference
                    // if duplicates exist (Should not happen, basically corrupt asset). Additional duplicates are
                    // covered since this is evaluated for each element.
                    for (var j = i - 1; j >= 0; --j)
                    {
                        if (ReferenceEquals(references[j].m_Asset, references[i].m_Asset) &&
                            references[j].m_ActionId == references[i].m_ActionId)
                        {
                            Debug.Log("Removing duplicate InputActionReference: " + references[i]);
                            RemoveReferenceAtIndex(references, ref count, i);
                            break;
                        }
                    }
                }
            }

            // Handle added or removed actions
            foreach (var action in asset)
            {
                var referenceIndex = references.IndexOf(r => r.m_ActionId == action.m_Id, 0, count);
                if (referenceIndex >= 0)
                {
                    // Action has exactly one reference as expected, invalidate name if action has changed name
                    var reference = references[referenceIndex];
                    //SerializedObject obj = new SerializedObject(reference);
                    //var assetProperty = obj.FindProperty(nameof(InputActionReference.m_Asset));
                    //var actionIdProperty = obj.FindProperty(nameof(InputActionReference.m_ActionId));
                    //var action = obj.FindProperty(nameof(InputActionReference.m_Ac))
                    reference.Set(action); // Invalidate()
                }
                else
                {
                    // Action is missing a reference so we add it
                    var reference = InputActionReference.Create(action);
                    ArrayHelpers.Append(ref references, reference);
                    AssetDatabase.AddObjectToAsset(reference, asset);
                    Debug.Log("Added missing action reference: " + reference);
                }
            }

            asset.m_References = references;
        }

        // TODO Code below this point is targeted at debugging and should be removed before merge

        [MenuItem("Test/Log AssetPath References")]
        public static void Dump()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(ProjectWideActionsAsset.GetOrCreate()));
            foreach (var asset in assets)
            {
                var reference = asset as InputActionReference;
                if (reference != null)
                    Debug.Log(reference);
            }
        }

        [MenuItem("Test/Log InputActionAsset References")]
        public static void Dump2()
        {
            foreach (var reference in ProjectWideActionsAsset.GetOrCreate().m_References)
            {
                Debug.Log(reference);
            }
        }
    }
}

#endif // #if UNITY_EDITOR
