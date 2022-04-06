using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem
{
    partial class InputActionRebindingExtensions
    {
        /// <summary>
        /// Return the current value of the given parameter as found on the processors, interactions, or composites
        /// of the action's current bindings.
        /// </summary>
        /// <param name="action">Action on whose bindings to look for the value of the given parameter.</param>
        /// <param name="name">Name of the parameter to get the value of. Case-insensitive. This can either be just the name of the
        /// parameter (like <c>"duration"</c> or expressed as <c>nameof(TapInteraction.duration)</c>) or can be prefixed with the
        /// type of object to get the parameter value from. For example, <c>"tap:duration"</c> will specifically get the <c>"duration"</c>
        /// parameter from the object registered as <c>"tap"</c> (which will usually be <see cref="Interactions.TapInteraction"/>).</param>
        /// <param name="bindingMask">Optional mask that determines on which bindings to look for objects with parameters. If used, only
        /// bindings that match (see <see cref="InputBinding.Matches"/>) the given mask will be taken into account.</param>
        /// <returns>The current value of the given parameter or <c>null</c> if the parameter could not be found.</returns>
        /// <remarks>
        /// Parameters are found on interactions (<see cref="IInputInteraction"/>), processors (<see cref="InputProcessor"/>), and
        /// composites (see <see cref="InputBindingComposite"/>) that are applied to bindings. For example, the following binding
        /// adds a <c>Hold</c> interaction with a custom <c>duration</c> parameter on top of binding to the gamepad's A button:
        ///
        /// <example>
        /// <code>
        /// new InputBinding
        /// {
        ///     path = "&lt;Gamepad&gt;/buttonSouth",
        ///     interactions = "hold(duration=0.6)"
        /// };
        /// </code>
        /// </example>
        ///
        /// In the editor UI, parameters are set graphically from the properties sections in the right-most pane
        /// in the action editor when an action or a binding is selected.
        ///
        /// When the binding above is applied to an action, the <c>duration</c> parameter from the <c>Hold</c> interaction can be
        /// queried like so:
        ///
        /// <example>
        /// <code>
        /// action.GetParameterValue("duration") // Returns 0.6
        /// </code>
        /// </example>
        ///
        /// Note that if there are multiple objects on the action that use the same parameter name, the value of the <em>first</em> parameter
        /// that is encountered is returned. Also note that this method will create GC heap garbage.
        ///
        /// The type of object to query the parameter from can be include in the <paramref name="name"/> parameter. For example, if
        /// an action has both a <see cref="Interactions.TapInteraction"/> and a <see cref="Interactions.HoldInteraction"/> on it, the
        /// <c>duration</c> parameter can be queried independently like so:
        ///
        /// <example>
        /// <code>
        /// // Query "duration" from "hold":
        /// action.GetParameterValue("hold:duration");
        ///
        /// // Query "duration" from "tap":
        /// action.GetParameterValue("tap:duration");
        /// </code>
        /// </example>
        ///
        /// The names used here to identify the object holding the parameter are the same used by <see cref="InputSystem.RegisterInteraction"/>,
        /// <see cref="InputSystem.RegisterBindingComposite"/>, and <see cref="InputSystem.RegisterProcessor"/>.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c> -or- <paramref name="name"/> is <c>null</c></exception>
        /// <seealso cref="ApplyParameterOverride(InputActionMap,string,PrimitiveValue,InputBinding)"/>
        /// <seealso cref="ApplyBindingOverride(InputAction,string,string,string)"/>
        /// <seealso cref="Editor.InputParameterEditor"/>
        public static PrimitiveValue? GetParameterValue(this InputAction action, string name, InputBinding bindingMask = default)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            return action.GetParameterValue(new ParameterOverride(name, bindingMask));
        }

        private static PrimitiveValue? GetParameterValue(this InputAction action, ParameterOverride parameterOverride)
        {
            parameterOverride.bindingMask.action = action.name;

            var actionMap = action.GetOrCreateActionMap();
            actionMap.ResolveBindingsIfNecessary();
            foreach (var parameter in new ParameterEnumerable(actionMap.m_State, parameterOverride, actionMap.m_MapIndexInState))
            {
                var value = parameter.field.GetValue(parameter.instance);
                return PrimitiveValue.FromObject(value);
            }

            return null;
        }

        /// <summary>
        /// Return the current value of the given parameter as found on the processors, interactions, or composites
        /// of the action's current bindings.
        /// </summary>
        /// <param name="action">Action on whose bindings to look for the value of the given parameter.</param>
        /// <param name="name">Name of the parameter to get the value of. Case-insensitive. This can either be just the name of the
        /// parameter (like <c>"duration"</c> or expressed as <c>nameof(TapInteraction.duration)</c>) or can be prefixed with the
        /// type of object to get the parameter value from. For example, <c>"tap:duration"</c> will specifically get the <c>"duration"</c>
        /// parameter from the object registered as <c>"tap"</c> (which will usually be <see cref="Interactions.TapInteraction"/>).</param>
        /// <param name="bindingIndex">Index of the binding in <paramref name="action"/>'s <see cref="InputAction.bindings"/>
        /// to look for processors, interactions, and composites on.</param>
        /// <returns>The current value of the given parameter or <c>null</c> if the parameter not could be found.</returns>
        /// <remarks>
        /// This method is a variation of <see cref="ApplyParameterOverride(InputActionMap,string,PrimitiveValue,InputBinding)"/>
        /// to specifically target a single binding by index. Otherwise, the method is identical in functionality.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c> -or- <paramref name="name"/> is <c>null</c></exception>
        public static PrimitiveValue? GetParameterValue(this InputAction action, string name, int bindingIndex)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));
            if (bindingIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(bindingIndex));

            var indexOnMap = action.BindingIndexOnActionToBindingIndexOnMap(bindingIndex);
            var bindingMask = new InputBinding { id = action.GetOrCreateActionMap().bindings[indexOnMap].id };

            return action.GetParameterValue(name, bindingMask);
        }

        /// <summary>
        /// Return the current value of the given parameter as found on the processors, interactions, or composites
        /// of the action's current bindings.
        /// </summary>
        /// <param name="action">Action on whose bindings to look for the value of the given parameter.</param>
        /// <param name="expr">An expression such as <c>(TapInteraction x) => x.duration</c> that determines the
        /// name and type of the parameter being looked for.</param>
        /// <param name="bindingMask">Optional mask that determines on which bindings to look for objects with parameters. If used, only
        /// bindings that match (see <see cref="InputBinding.Matches"/>) the given mask will be taken into account.</param>
        /// <returns>The current value of the given parameter or <c>null</c> if the parameter not could be found.</returns>
        /// <remarks>
        /// This method is a variation of <see cref="ApplyParameterOverride(InputActionMap,string,PrimitiveValue,InputBinding)"/>
        /// that encapsulates a reference to the name of the parameter and the type of object it is found on in a way that is
        /// type-safe and does not involve strings.
        ///
        /// <example>
        /// <code>
        /// // Get the "duration" parameter from a TapInteraction.
        /// // This is equivalent to calling GetParameterValue("tap:duration")
        /// // but will return a float? instead of a PrimitiveValue?.
        /// action.GetParameterValue((TapInteraction x) => x.duration)
        /// </code>
        /// </example>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c> -or- <paramref name="expr"/> is <c>null</c></exception>
        /// <seealso cref="ApplyParameterOverride{TObject,TValue}(InputAction,Expression{Func{TObject,TValue}},TValue,InputBinding)"/>
        public static unsafe TValue? GetParameterValue<TObject, TValue>(this InputAction action, Expression<Func<TObject, TValue>> expr, InputBinding bindingMask = default)
            where TValue : struct
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            if (expr == null)
                throw new ArgumentNullException(nameof(expr));

            var parameterOverride = ExtractParameterOverride(expr, bindingMask);
            var value = action.GetParameterValue(parameterOverride);

            if (value == null)
                return null;

            // Type is guaranteed to match but just in case,
            // make extra sure with a check here.
            if (Type.GetTypeCode(typeof(TValue)) == value.Value.type)
            {
                // Can't just cast here so use UnsafeUtility to work around that.
                var v = value.Value;
                var result = default(TValue);
                UnsafeUtility.MemCpy(UnsafeUtility.AddressOf(ref result),
                    v.valuePtr,
                    UnsafeUtility.SizeOf<TValue>());
                return result;
            }

            // Shouldn't get here but just in case, do a conversion using C#'s Convert
            // machinery as a fallback.
            return (TValue)Convert.ChangeType(value.Value.ToObject(), typeof(TValue));
        }

        /// <summary>
        /// Set the value of the given parameter on the <see cref="InputBindingComposite"/>, <see cref="IInputInteraction"/>,
        /// and <see cref="InputProcessor"/> objects found on the <see cref="InputAction.bindings"/> of <paramref name="action"/>.
        /// </summary>
        /// <param name="action">An action on whose <see cref="InputAction.bindings"/> to look for objects to set
        /// the parameter value on.</param>
        /// <param name="expr">An expression such as <c>(TapInteraction x) => x.duration</c> that determines the
        /// name and type of the parameter whose value to set.</param>
        /// <param name="value">New value to assign to the parameter.</param>
        /// <param name="bindingMask">Optional mask that determines on which bindings to look for objects with parameters. If used, only
        /// bindings that match (see <see cref="InputBinding.Matches"/>) the given mask will have the override applied to them.</param>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c> -or- <paramref name="expr"/> is <c>null</c>
        /// or empty.</exception>
        /// <remarks>
        /// This method is a variation of <see cref="ApplyParameterOverride(InputAction,string,PrimitiveValue,InputBinding)"/>
        /// that encapsulates a reference to the name of the parameter and the type of object it is found on in a way that is
        /// type-safe and does not involve strings.
        ///
        /// <example>
        /// <code>
        /// // Override the "duration" parameter from a TapInteraction.
        /// // This is equivalent to calling ApplyParameterOverride("tap:duration", 0.4f).
        /// action.ApplyParameterOverride((TapInteraction x) => x.duration, 0.4f);
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="GetParameterValue{TObject,TValue}(InputAction,Expression{Func{TObject,TValue}},InputBinding)"/>
        public static void ApplyParameterOverride<TObject, TValue>(this InputAction action, Expression<Func<TObject, TValue>> expr, TValue value,
            InputBinding bindingMask = default)
            where TValue : struct
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            if (expr == null)
                throw new ArgumentNullException(nameof(expr));

            var actionMap = action.GetOrCreateActionMap();
            actionMap.ResolveBindingsIfNecessary();
            bindingMask.action = action.name;

            var parameterOverride = ExtractParameterOverride(expr, bindingMask, PrimitiveValue.From(value));

            ApplyParameterOverride(actionMap.m_State, actionMap.m_MapIndexInState,
                ref actionMap.m_ParameterOverrides, ref actionMap.m_ParameterOverridesCount,
                parameterOverride);
        }

        /// <summary>
        /// Set the value of the given parameter on the <see cref="InputBindingComposite"/>, <see cref="IInputInteraction"/>,
        /// and <see cref="InputProcessor"/> objects found on the <see cref="InputActionMap.bindings"/> of <paramref name="actionMap"/>.
        /// </summary>
        /// <param name="actionMap">An action on whose <see cref="InputActionMap.bindings"/> to look for objects to set
        /// the parameter value on.</param>
        /// <param name="expr">An expression such as <c>(TapInteraction x) => x.duration</c> that determines the
        /// name and type of the parameter whose value to set.</param>
        /// <param name="value">New value to assign to the parameter.</param>
        /// <param name="bindingMask">Optional mask that determines on which bindings to look for objects with parameters. If used, only
        /// bindings that match (see <see cref="InputBinding.Matches"/>) the given mask will have the override applied to them.</param>
        /// <exception cref="ArgumentNullException"><paramref name="actionMap"/> is <c>null</c> -or- <paramref name="expr"/> is <c>null</c>
        /// or empty.</exception>
        /// <remarks>
        /// This method is a variation of <see cref="ApplyParameterOverride(InputActionMap,string,PrimitiveValue,InputBinding)"/>
        /// that encapsulates a reference to the name of the parameter and the type of object it is found on in a way that is
        /// type-safe and does not involve strings.
        ///
        /// <example>
        /// <code>
        /// // Override the "duration" parameter from a TapInteraction.
        /// // This is equivalent to calling mApplyParameterOverride("tap:duration", 0.4f).
        /// actionMap.ApplyParameterOverride((TapInteraction x) => x.duration, 0.4f);
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="GetParameterValue{TObject,TValue}(InputAction,Expression{Func{TObject,TValue}},InputBinding)"/>
        public static void ApplyParameterOverride<TObject, TValue>(this InputActionMap actionMap, Expression<Func<TObject, TValue>> expr, TValue value,
            InputBinding bindingMask = default)
            where TValue : struct
        {
            if (actionMap == null)
                throw new ArgumentNullException(nameof(actionMap));
            if (expr == null)
                throw new ArgumentNullException(nameof(expr));

            actionMap.ResolveBindingsIfNecessary();

            var parameterOverride = ExtractParameterOverride(expr, bindingMask, PrimitiveValue.From(value));

            ApplyParameterOverride(actionMap.m_State, actionMap.m_MapIndexInState,
                ref actionMap.m_ParameterOverrides, ref actionMap.m_ParameterOverridesCount,
                parameterOverride);
        }

        /// <summary>
        /// Set the value of the given parameter on the <see cref="InputBindingComposite"/>, <see cref="IInputInteraction"/>,
        /// and <see cref="InputProcessor"/> objects found on the <see cref="InputActionMap.bindings"/> of the <see cref="InputActionAsset.actionMaps"/>
        /// in <paramref name="asset"/>.
        /// </summary>
        /// <param name="asset">An asset on whose <see cref="InputActionMap.bindings"/> to look for objects to set
        /// the parameter value on.</param>
        /// <param name="expr">An expression such as <c>(TapInteraction x) => x.duration</c> that determines the
        /// name and type of the parameter whose value to set.</param>
        /// <param name="value">New value to assign to the parameter.</param>
        /// <param name="bindingMask">Optional mask that determines on which bindings to look for objects with parameters. If used, only
        /// bindings that match (see <see cref="InputBinding.Matches"/>) the given mask will have the override applied to them.</param>
        /// <exception cref="ArgumentNullException"><paramref name="asset"/> is <c>null</c> -or- <paramref name="expr"/> is <c>null</c>
        /// or empty.</exception>
        /// <remarks>
        /// This method is a variation of <see cref="ApplyParameterOverride(InputActionAsset,string,PrimitiveValue,InputBinding)"/>
        /// that encapsulates a reference to the name of the parameter and the type of object it is found on in a way that is
        /// type-safe and does not involve strings.
        ///
        /// <example>
        /// <code>
        /// // Override the "duration" parameter from a TapInteraction.
        /// // This is equivalent to calling mApplyParameterOverride("tap:duration", 0.4f).
        /// asset.ApplyParameterOverride((TapInteraction x) => x.duration, 0.4f);
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="GetParameterValue{TObject,TValue}(InputAction,Expression{Func{TObject,TValue}},InputBinding)"/>
        public static void ApplyParameterOverride<TObject, TValue>(this InputActionAsset asset, Expression<Func<TObject, TValue>> expr, TValue value,
            InputBinding bindingMask = default)
            where TValue : struct
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));
            if (expr == null)
                throw new ArgumentNullException(nameof(expr));

            asset.ResolveBindingsIfNecessary();

            var parameterOverride = ExtractParameterOverride(expr, bindingMask, PrimitiveValue.From(value));

            ApplyParameterOverride(asset.m_SharedStateForAllMaps, -1,
                ref asset.m_ParameterOverrides, ref asset.m_ParameterOverridesCount,
                parameterOverride);
        }

        private static ParameterOverride ExtractParameterOverride<TObject, TValue>(Expression<Func<TObject, TValue>> expr,
            InputBinding bindingMask = default, PrimitiveValue value = default)
        {
            if (!(expr is LambdaExpression lambda))
                throw new ArgumentException($"Expression must be a LambdaExpression but was a {expr.GetType().Name} instead", nameof(expr));

            if (!(lambda.Body is MemberExpression body))
            {
                // If the field type in the lambda doesn't match the TValue type being used,
                // but there is a coercion, the compiler will automatically insert a Convert(x.name, TValue)
                // expression.
                if (lambda.Body is UnaryExpression unary && unary.NodeType == ExpressionType.Convert && unary.Operand is MemberExpression b)
                {
                    body = b;
                }
                else
                {
                    throw new ArgumentException(
                        $"Body in LambdaExpression must be a MemberExpression (x.name) but was a {expr.GetType().Name} instead",
                        nameof(expr));
                }
            }

            string objectRegistrationName;
            if (typeof(InputProcessor).IsAssignableFrom(typeof(TObject)))
                objectRegistrationName = InputProcessor.s_Processors.FindNameForType(typeof(TObject));
            else if (typeof(IInputInteraction).IsAssignableFrom(typeof(TObject)))
                objectRegistrationName = InputInteraction.s_Interactions.FindNameForType(typeof(TObject));
            else if (typeof(InputBindingComposite).IsAssignableFrom(typeof(TObject)))
                objectRegistrationName = InputBindingComposite.s_Composites.FindNameForType(typeof(TObject));
            else
                throw new ArgumentException(
                    $"Given type must be an InputProcessor, IInputInteraction, or InputBindingComposite (was {typeof(TObject).Name})",
                    nameof(TObject));

            return new ParameterOverride(objectRegistrationName, body.Member.Name, bindingMask, value);
        }

        /// <summary>
        /// Set the value of the given parameter on the <see cref="InputBindingComposite"/>, <see cref="IInputInteraction"/>,
        /// and <see cref="InputProcessor"/> objects found on the <see cref="InputActionMap.bindings"/> of <paramref name="actionMap"/>.
        /// </summary>
        /// <param name="actionMap">An action map on whose <see cref="InputActionMap.bindings"/> to look for objects to set
        /// the parameter value on.</param>
        /// <param name="name">Name of the parameter to get the value of. Case-insensitive. This can either be just the name of the
        /// parameter (like <c>"duration"</c> or expressed as <c>nameof(TapInteraction.duration)</c>) or can be prefixed with the
        /// type of object to get the parameter value from. For example, <c>"tap:duration"</c> will specifically get the <c>"duration"</c>
        /// parameter from the object registered as <c>"tap"</c> (which will usually be <see cref="Interactions.TapInteraction"/>).</param>
        /// <param name="value">New value to assign to the parameter.</param>
        /// <param name="bindingMask">A binding mask that determines which of <paramref name="actionMap"/>'s <see cref="InputActionMap.bindings"/>
        /// to apply the override to. By default this is empty which leads to the override to be applied to all bindings in the map.</param>
        /// <remarks>
        /// This method both directly applies the new value and also stores the override internally.
        ///
        /// If an override for the same parameter <paramref name="name"/> and with the same <paramref name="bindingMask"/> already exists,
        /// its value is simply updated. No new override will be created.
        ///
        /// You can use this method to set parameters (public fields) on composites, interactions, and processors that are created
        /// from bindings.
        ///
        /// <example>
        /// <code>
        /// // Create an action map with two actions.
        /// var map = new InputActionMap();
        /// var action1 = map.AddAction("action1");
        /// var action2 = map.AddAction("action2");
        ///
        /// // Add a binding to each action to which  a "ClampProcessor" is applied.
        /// // This processor has two parameters:
        /// // - "min" (float)
        /// // - "max" (float)
        /// action1.AddBinding("&gt;Gamepad&gt;/rightTrigger", processors: "clamp(min=0.2,max=0.8)");
        /// action2.AddBinding("&gt;Gamepad&gt;/leftTrigger", processors: "clamp(min=0.2,max=0.8)");
        ///
        /// // Apply parameter overrides to set the values differently.
        /// // This will apply the setting to *both* the bindings on action1 *and* action2.
        /// map.ApplyParameterOverride("min", 0.3f);
        /// map.ApplyParameterOverride("max", 0.9f);
        /// </code>
        /// </example>
        ///
        /// An override can optionally be directed at a specific type of object.
        ///
        /// <example>
        /// <code>
        /// map.ApplyParameterOverride("clamp:min", 0.3f);
        /// map.ApplyParameterOverride("clamp:max", 0.9f);
        /// </code>
        /// </example>
        ///
        /// By default, the parameter override will apply to all bindings in the map. To limit the override
        /// to specific bindings, you can supply a <paramref name="bindingMask"/>.
        ///
        /// <example>
        /// <code>
        /// // Apply a parameter override only to action1.
        /// map.ApplyBindingOverride("clamp:min", 0.25f, new InputBinding { action = action1.name });
        ///
        /// // Apply a parameter override only to a specific binding path.
        /// map.ApplyBindingOverride("clamp:min", 0.4f, new InputBinding { path = "&lt;Gamepad&gt;/leftTrigger" });
        /// </code>
        /// </example>
        ///
        /// If multiple overrides exist for the same parameter, an attempt is made to choose the override that is most specific.
        /// Say, that you apply an override for <c>"duration"</c> on an entire <see cref="InputActionAsset"/> using
        /// <see cref="ApplyParameterOverride(InputActionAsset,String,PrimitiveValue,InputBinding)"/>. But then you also apply
        /// an override to just an individual <see cref="InputAction"/> inside the asset. In this case, the <c>"duration"</c>
        /// override for just that action will be applied to bindings of that action and the override inside the asset will
        /// be applied to bindings of all other actions. Note that if multiple overrides exist that could all be considered
        /// equally valid, the behavior is undecided.
        ///
        /// Note that parameter overrides stay in place on the map. Like binding overrides, however, they are not
        /// automatically persisted and thus need to be reapplied when actions are loaded from assets. This will, however, be applied
        /// automatically to bindings added to the action in the future as well as whenever bindings are resolved to controls.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="actionMap"/> is <c>null</c> -or- <paramref name="name"/> is <c>null</c>
        /// or empty.</exception>
        /// <seealso cref="GetParameterValue(InputAction,string,InputBinding)"/>
        public static void ApplyParameterOverride(this InputActionMap actionMap, string name, PrimitiveValue value, InputBinding bindingMask = default)
        {
            if (actionMap == null)
                throw new ArgumentNullException(nameof(actionMap));
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            actionMap.ResolveBindingsIfNecessary();

            ApplyParameterOverride(actionMap.m_State, actionMap.m_MapIndexInState,
                ref actionMap.m_ParameterOverrides, ref actionMap.m_ParameterOverridesCount,
                new ParameterOverride(name, bindingMask, value));
        }

        /// <summary>
        /// Set the value of the given parameter on the <see cref="InputBindingComposite"/>, <see cref="IInputInteraction"/>,
        /// and <see cref="InputProcessor"/> objects found on the <see cref="InputActionMap.bindings"/> of each of the <see cref="InputActionAsset.actionMaps"/>
        /// in <paramref name="asset"/>.
        /// </summary>
        /// <param name="asset">An <c>.inputactions</c> asset on whose <see cref="InputActionMap.bindings"/> to look for objects to set
        /// the parameter value on.</param>
        /// <param name="name">Name of the parameter to get the value of. Case-insensitive. This can either be just the name of the
        /// parameter (like <c>"duration"</c> or expressed as <c>nameof(TapInteraction.duration)</c>) or can be prefixed with the
        /// type of object to get the parameter value from. For example, <c>"tap:duration"</c> will specifically get the <c>"duration"</c>
        /// parameter from the object registered as <c>"tap"</c> (which will usually be <see cref="Interactions.TapInteraction"/>).</param>
        /// <param name="value">New value to assign to the parameter.</param>
        /// <param name="bindingMask">A binding mask that determines which of the <see cref="InputActionMap.bindings"/>
        /// to apply the override to. By default this is empty which leads to the override to be applied to all bindings in the asset.</param>
        /// <remarks>
        /// This method both directly applies the new value and also stores the override internally.
        ///
        /// If an override for the same parameter <paramref name="name"/> and with the same <paramref name="bindingMask"/> already exists,
        /// its value is simply updated. No new override will be created.
        ///
        /// You can use this method to set parameters (public fields) on composites, interactions, and processors that are created
        /// from bindings.
        ///
        /// <example>
        /// <code>
        /// // Create an asset with one action map and two actions.
        /// var asset = ScriptableObject.CreateInstance&lt;InputActionAsset&gt;();
        /// var map = asset.AddActionMap("map");
        /// var action1 = map.AddAction("action1");
        /// var action2 = map.AddAction("action2");
        ///
        /// // Add a binding to each action to which  a "ClampProcessor" is applied.
        /// // This processor has two parameters:
        /// // - "min" (float)
        /// // - "max" (float)
        /// action1.AddBinding("&gt;Gamepad&gt;/rightTrigger", processors: "clamp(min=0.2,max=0.8)");
        /// action2.AddBinding("&gt;Gamepad&gt;/leftTrigger", processors: "clamp(min=0.2,max=0.8)");
        ///
        /// // Apply parameter overrides to set the values differently.
        /// // This will apply the setting to *both* the bindings on action1 *and* action2.
        /// asset.ApplyParameterOverride("min", 0.3f);
        /// asset.ApplyParameterOverride("max", 0.9f);
        /// </code>
        /// </example>
        ///
        /// An override can optionally be directed at a specific type of object.
        ///
        /// <example>
        /// <code>
        /// asset.ApplyParameterOverride("clamp:min", 0.3f);
        /// asset.ApplyParameterOverride("clamp:max", 0.9f);
        /// </code>
        /// </example>
        ///
        /// By default, the parameter override will apply to all bindings in the asset. To limit the override
        /// to specific bindings, you can supply a <paramref name="bindingMask"/>.
        ///
        /// <example>
        /// <code>
        /// // Apply a parameter override only to action1.
        /// asset.ApplyBindingOverride("clamp:min", 0.25f, new InputBinding { action = action1.name });
        ///
        /// // Apply a parameter override only to a specific binding path.
        /// asset.ApplyBindingOverride("clamp:min", 0.4f, new InputBinding { path = "&lt;Gamepad&gt;/leftTrigger" });
        /// </code>
        /// </example>
        ///
        /// If multiple overrides exist for the same parameter, an attempt is made to choose the override that is most specific.
        /// Say, that you apply an override for <c>"duration"</c> on an entire <see cref="InputActionAsset"/> using
        /// <see cref="ApplyParameterOverride(InputActionAsset,String,PrimitiveValue,InputBinding)"/>. But then you also apply
        /// an override to just an individual <see cref="InputAction"/> inside the asset. In this case, the <c>"duration"</c>
        /// override for just that action will be applied to bindings of that action and the override inside the asset will
        /// be applied to bindings of all other actions. Note that if multiple overrides exist that could all be considered
        /// equally valid, the behavior is undecided.
        ///
        /// Note that parameter overrides stay in place on the map. Like binding overrides, however, they are not
        /// automatically persisted and thus need to be reapplied when actions are loaded from assets. This will, however, be applied
        /// automatically to bindings added to the action in the future as well as whenever bindings are resolved to controls.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="asset"/> is <c>null</c> -or- <paramref name="name"/> is <c>null</c>
        /// or empty.</exception>
        /// <seealso cref="GetParameterValue(InputAction,string,InputBinding)"/>
        public static void ApplyParameterOverride(this InputActionAsset asset, string name, PrimitiveValue value, InputBinding bindingMask = default)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            asset.ResolveBindingsIfNecessary();

            ApplyParameterOverride(asset.m_SharedStateForAllMaps, -1,
                ref asset.m_ParameterOverrides, ref asset.m_ParameterOverridesCount,
                new ParameterOverride(name, bindingMask, value));
        }

        /// <summary>
        /// Set the value of the given parameter on the <see cref="InputBindingComposite"/>, <see cref="IInputInteraction"/>,
        /// and <see cref="InputProcessor"/> objects found on the <see cref="InputAction.bindings"/> of <paramref name="action"/>.
        /// </summary>
        /// <param name="action">An action on whose <see cref="InputAction.bindings"/> to look for objects to set
        /// the parameter value on.</param>
        /// <param name="name">Name of the parameter to get the value of. Case-insensitive. This can either be just the name of the
        /// parameter (like <c>"duration"</c> or expressed as <c>nameof(TapInteraction.duration)</c>) or can be prefixed with the
        /// type of object to get the parameter value from. For example, <c>"tap:duration"</c> will specifically get the <c>"duration"</c>
        /// parameter from the object registered as <c>"tap"</c> (which will usually be <see cref="Interactions.TapInteraction"/>).</param>
        /// <param name="value">New value to assign to the parameter.</param>
        /// <param name="bindingMask">A binding mask that determines which of <paramref name="action"/>'s <see cref="InputAction.bindings"/>
        /// to apply the override to. By default this is empty which leads to the override to be applied to all bindings of the action.</param>
        /// <remarks>
        /// This method both directly applies the new value and also stores the override internally.
        ///
        /// If an override for the same parameter <paramref name="name"/> on the same <paramref name="action"/> and with the same
        /// <paramref name="bindingMask"/> already exists, its value is simply updated. No new override will be created.
        ///
        /// You can use this method to set parameters (public fields) on composites, interactions, and processors that are created
        /// from bindings.
        ///
        /// <example>
        /// <code>
        /// // Create an action with a binding that has a "ClampProcessor" applied to it.
        /// // This processor has two parameters:
        /// // - "min" (float)
        /// // - "max" (float)
        /// var action = new InputAction(binding: "&gt;Gamepad&gt;/rightTrigger", processors: "clamp(min=0.2,max=0.8)");
        ///
        /// // Apply parameter overrides to set the values differently.
        /// action.ApplyParameterOverride("min", 0.3f);
        /// action.ApplyParameterOverride("max", 0.9f);
        /// </code>
        /// </example>
        ///
        /// An override can optionally be directed at a specific type of object.
        ///
        /// <example>
        /// <code>
        /// // Create an action with both a "tap" and a "hold" interaction. Both have a
        /// // "duration" parameter.
        /// var action = new InputAction(binding: "&lt;Gamepad&gt;/buttonSouth", interactions: "tap;hold");
        ///
        /// // Apply parameter overrides individually to the two.
        /// action.ApplyParameterOverride("tap:duration", 0.6f);
        /// action.ApplyParameterOverride("hold:duration", 4f);
        /// </code>
        /// </example>
        ///
        /// By default, the parameter override will apply to all bindings on the action. To limit the override
        /// to specific bindings, you can supply a <paramref name="bindingMask"/>.
        ///
        /// <example>
        /// <code>
        /// // Create a "look" style action with a mouse and a gamepad binding.
        /// var lookAction = new InputAction();
        /// lookAction.AddBinding("&lt;Mouse&gt;/delta", processors: "scaleVector2", groups: "Mouse");
        /// lookAction.AddBinding("&lt;Gamepad&gt;/rightStick", processors: "scaleVector2", groups: "Gamepad");
        ///
        /// // Override scaling of the mouse delta individually.
        /// lookAction.ApplyBindingOverride("scaleVector2:x", 0.25f, InputBinding.MaskByGroup("Mouse"));
        /// lookAction.ApplyBindingOverride("scaleVector2:y", 0.25f, InputBinding.MaskByGroup("Mouse"));
        ///
        /// // Can also do that by path.
        /// lookAction.ApplyBindingOverride("scaleVector2:x", 0.25f, new InputBinding("&lt;Mouse&gt;/delta"));
        /// lookAction.ApplyBindingOverride("scaleVector2:y", 0.25f, new InputBinding("&lt;Mouse&gt;/delta"));
        /// </code>
        /// </example>
        ///
        /// Note that parameter overrides stay in place on the action. Like binding overrides, however, they are not
        /// automatically persisted and thus need to be reapplied when actions are loaded from assets. This will, however, be applied
        /// automatically to bindings added to the action in the future as well as whenever bindings for the action are resolved.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c> -or- <paramref name="name"/> is <c>null</c>
        /// or empty.</exception>
        /// <seealso cref="GetParameterValue(InputAction,string,InputBinding)"/>
        public static void ApplyParameterOverride(this InputAction action, string name, PrimitiveValue value, InputBinding bindingMask = default)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            var actionMap = action.GetOrCreateActionMap();
            actionMap.ResolveBindingsIfNecessary();
            bindingMask.action = action.name;

            ApplyParameterOverride(actionMap.m_State, actionMap.m_MapIndexInState,
                ref actionMap.m_ParameterOverrides, ref actionMap.m_ParameterOverridesCount,
                new ParameterOverride(name, bindingMask, value));
        }

        /// <summary>
        /// Set the value of the given parameter on the <see cref="InputBindingComposite"/>, <see cref="IInputInteraction"/>,
        /// and <see cref="InputProcessor"/> objects found on the <see cref="InputAction.bindings"/> of <paramref name="action"/>.
        /// </summary>
        /// <param name="action">An action on whose <see cref="InputAction.bindings"/> to look for objects to set
        /// the parameter value on.</param>
        /// <param name="name">Name of the parameter to get the value of. Case-insensitive. This can either be just the name of the
        /// parameter (like <c>"duration"</c> or expressed as <c>nameof(TapInteraction.duration)</c>) or can be prefixed with the
        /// type of object to get the parameter value from. For example, <c>"tap:duration"</c> will specifically get the <c>"duration"</c>
        /// parameter from the object registered as <c>"tap"</c> (which will usually be <see cref="Interactions.TapInteraction"/>).</param>
        /// <param name="value">New value to assign to the parameter.</param>
        /// <param name="bindingIndex">Index of the binding in <see cref="InputAction.bindings"/> of <paramref name="action"/> to which
        /// to restrict the parameter override to.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="bindingIndex"/> is negative or equal or greater than the number of
        /// <see cref="InputAction.bindings"/> of <paramref name="action"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c> -or- <paramref name="name"/> is <c>null</c>
        /// or empty.</exception>
        /// <remarks>
        /// This method is a variation of <see cref="ApplyParameterOverride(InputActionMap,string,PrimitiveValue,InputBinding)"/> which
        /// allows specifying a binding by index. It otherwise behaves identically to that method.
        /// </remarks>
        public static void ApplyParameterOverride(this InputAction action, string name, PrimitiveValue value, int bindingIndex)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));
            if (bindingIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(bindingIndex));

            var indexOnMap = action.BindingIndexOnActionToBindingIndexOnMap(bindingIndex);
            var bindingMask = new InputBinding { id = action.GetOrCreateActionMap().bindings[indexOnMap].id };

            action.ApplyParameterOverride(name, value, bindingMask);
        }

        private static void ApplyParameterOverride(InputActionState state, int mapIndex,
            ref ParameterOverride[] parameterOverrides, ref int parameterOverridesCount, ParameterOverride parameterOverride)
        {
            // Update the parameter overrides on the map or asset.
            var haveExistingOverride = false;
            if (parameterOverrides != null)
            {
                // Try to find existing override.
                for (var i = 0; i < parameterOverridesCount; ++i)
                {
                    ref var p = ref parameterOverrides[i];
                    if (string.Equals(p.objectRegistrationName, parameterOverride.objectRegistrationName, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(p.parameter, parameterOverride.parameter, StringComparison.OrdinalIgnoreCase) &&
                        p.bindingMask == parameterOverride.bindingMask)
                    {
                        haveExistingOverride = true;
                        // Update value on existing override.
                        p = parameterOverride;
                        break;
                    }
                }
            }
            if (!haveExistingOverride)
            {
                // Add new override.
                ArrayHelpers.AppendWithCapacity(ref parameterOverrides, ref parameterOverridesCount, parameterOverride);
            }

            // Set value on all current processor and/or interaction instances that use the parameter.
            foreach (var parameter in new ParameterEnumerable(state, parameterOverride, mapIndex))
            {
                // We cannot just blindly apply the parameter here as the override we have set may be less
                // specific than an override we already have applied. So instead, we look up the most specific
                // override and set that.
                var actionMap = state.GetActionMap(parameter.bindingIndex);
                ref var binding = ref state.GetBinding(parameter.bindingIndex);
                var overrideToApply = ParameterOverride.Find(actionMap, ref binding, parameterOverride.parameter,
                    parameterOverride.objectRegistrationName);
                if (overrideToApply.HasValue)
                {
                    var fieldTypeCode = Type.GetTypeCode(parameter.field.FieldType);
                    parameter.field.SetValue(parameter.instance, overrideToApply.Value.value.ConvertTo(fieldTypeCode).ToObject());
                }
            }
        }

        internal struct Parameter
        {
            public object instance;
            public FieldInfo field;
            public int bindingIndex;
        }

        // Finds all instances of a parameter in one or more actions.
        private struct ParameterEnumerable : IEnumerable<Parameter>
        {
            private InputActionState m_State;
            private ParameterOverride m_Parameter;
            private int m_MapIndex;

            public ParameterEnumerable(InputActionState state, ParameterOverride parameter, int mapIndex = -1)
            {
                m_State = state;
                m_Parameter = parameter;
                m_MapIndex = mapIndex;
            }

            public ParameterEnumerator GetEnumerator()
            {
                return new ParameterEnumerator(m_State, m_Parameter, m_MapIndex);
            }

            IEnumerator<Parameter> IEnumerable<Parameter>.GetEnumerator()
            {
                return GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private struct ParameterEnumerator : IEnumerator<Parameter>
        {
            private InputActionState m_State;
            private int m_MapIndex;
            private int m_BindingCurrentIndex;
            private int m_BindingEndIndex;
            private int m_InteractionCurrentIndex;
            private int m_InteractionEndIndex;
            private int m_ProcessorCurrentIndex;
            private int m_ProcessorEndIndex;

            private InputBinding m_BindingMask;
            private Type m_ObjectType;
            private string m_ParameterName;
            private bool m_MayBeInteraction;
            private bool m_MayBeProcessor;
            private bool m_MayBeComposite;
            private bool m_CurrentBindingIsComposite;
            private object m_CurrentObject;
            private FieldInfo m_CurrentParameter;

            public ParameterEnumerator(InputActionState state, ParameterOverride parameter, int mapIndex = -1)
                : this()
            {
                m_State = state;
                m_ParameterName = parameter.parameter;
                m_MapIndex = mapIndex;
                m_ObjectType = parameter.objectType;
                m_MayBeComposite = m_ObjectType == null || typeof(InputBindingComposite).IsAssignableFrom(m_ObjectType);
                m_MayBeProcessor = m_ObjectType == null || typeof(InputProcessor).IsAssignableFrom(m_ObjectType);
                m_MayBeInteraction = m_ObjectType == null || typeof(IInputInteraction).IsAssignableFrom(m_ObjectType);
                m_BindingMask = parameter.bindingMask;
                Reset();
            }

            private bool MoveToNextBinding()
            {
                // Find a binding that matches our mask.
                while (true)
                {
                    ++m_BindingCurrentIndex;
                    if (m_BindingCurrentIndex >= m_BindingEndIndex)
                        return false; // Reached the end.

                    ref var binding = ref m_State.GetBinding(m_BindingCurrentIndex);
                    ref var bindingState = ref m_State.GetBindingState(m_BindingCurrentIndex);

                    // Skip any binding that has no associated objects with parameters.
                    if (bindingState.processorCount == 0 && bindingState.interactionCount == 0 && !binding.isComposite)
                        continue;

                    // If we're only looking for composites, skip any binding that isn't one.
                    if (m_MayBeComposite && !m_MayBeProcessor && !m_MayBeInteraction && !binding.isComposite)
                        continue;

                    // If we're only looking for processors, skip any that hasn't got any.
                    if (m_MayBeProcessor && !m_MayBeComposite && !m_MayBeInteraction && bindingState.processorCount == 0)
                        continue;

                    // If we're only looking for interactions, skip any that hasn't got any.
                    if (m_MayBeInteraction && !m_MayBeComposite && !m_MayBeProcessor && bindingState.interactionCount == 0)
                        continue;

                    if (m_BindingMask.Matches(ref binding))
                    {
                        if (m_MayBeComposite)
                            m_CurrentBindingIsComposite = binding.isComposite;

                        // Reset interaction and processor count.
                        m_ProcessorCurrentIndex = bindingState.processorStartIndex - 1; // Minus one to account for first MoveNext().
                        m_ProcessorEndIndex = bindingState.processorStartIndex + bindingState.processorCount;
                        m_InteractionCurrentIndex = bindingState.interactionStartIndex - 1; // Minus one to account for first MoveNext().
                        m_InteractionEndIndex = bindingState.interactionStartIndex + bindingState.interactionCount;

                        return true;
                    }
                }
            }

            private bool MoveToNextInteraction()
            {
                while (m_InteractionCurrentIndex < m_InteractionEndIndex)
                {
                    ++m_InteractionCurrentIndex;
                    if (m_InteractionCurrentIndex == m_InteractionEndIndex)
                        break;
                    var interaction = m_State.interactions[m_InteractionCurrentIndex];
                    if (FindParameter(interaction))
                        return true;
                }
                return false;
            }

            private bool MoveToNextProcessor()
            {
                while (m_ProcessorCurrentIndex < m_ProcessorEndIndex)
                {
                    ++m_ProcessorCurrentIndex;
                    if (m_ProcessorCurrentIndex == m_ProcessorEndIndex)
                        break;
                    var processor = m_State.processors[m_ProcessorCurrentIndex];
                    if (FindParameter(processor))
                        return true;
                }
                return false;
            }

            private bool FindParameter(object instance)
            {
                if (m_ObjectType != null && !m_ObjectType.IsInstanceOfType(instance))
                    return false;

                var field = instance.GetType().GetField(m_ParameterName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                if (field == null)
                    return false;

                m_CurrentParameter = field;
                m_CurrentObject = instance;

                return true;
            }

            public bool MoveNext()
            {
                while (true)
                {
                    if (m_MayBeInteraction && MoveToNextInteraction())
                        return true;

                    if (m_MayBeProcessor && MoveToNextProcessor())
                        return true;

                    if (!MoveToNextBinding())
                        return false;

                    if (m_MayBeComposite && m_CurrentBindingIsComposite)
                    {
                        var compositeIndex = m_State.GetBindingState(m_BindingCurrentIndex).compositeOrCompositeBindingIndex;
                        var composite = m_State.composites[compositeIndex];
                        if (FindParameter(composite))
                            return true;
                    }
                }
            }

            public unsafe void Reset()
            {
                m_CurrentObject = default;
                m_CurrentParameter = default;
                m_InteractionCurrentIndex = default;
                m_InteractionEndIndex = default;
                m_ProcessorCurrentIndex = default;
                m_ProcessorEndIndex = default;
                m_CurrentBindingIsComposite = default;
                if (m_MapIndex < 0)
                {
                    m_BindingCurrentIndex = -1; // Account for first MoveNext().
                    m_BindingEndIndex = m_State.totalBindingCount;
                }
                else
                {
                    m_BindingCurrentIndex = m_State.mapIndices[m_MapIndex].bindingStartIndex - 1; // Account for first MoveNext().
                    m_BindingEndIndex = m_State.mapIndices[m_MapIndex].bindingStartIndex + m_State.mapIndices[m_MapIndex].bindingCount;
                }
            }

            public Parameter Current => new Parameter
            {
                instance = m_CurrentObject,
                field = m_CurrentParameter,
                bindingIndex = m_BindingCurrentIndex,
            };

            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }
        }

        internal struct ParameterOverride
        {
            public string objectRegistrationName; // Optional. Such as "hold" or "scale".
            public string parameter;
            public InputBinding bindingMask;
            public PrimitiveValue value;

            public Type objectType =>
                InputProcessor.s_Processors.LookupTypeRegistration(objectRegistrationName)
                ?? InputInteraction.s_Interactions.LookupTypeRegistration(objectRegistrationName)
                ?? InputBindingComposite.s_Composites.LookupTypeRegistration(objectRegistrationName);

            public ParameterOverride(string parameterName, InputBinding bindingMask, PrimitiveValue value = default)
            {
                var colonIndex = parameterName.IndexOf(':');
                if (colonIndex < 0)
                {
                    objectRegistrationName = null;
                    parameter = parameterName;
                }
                else
                {
                    objectRegistrationName = parameterName.Substring(0, colonIndex);
                    parameter = parameterName.Substring(colonIndex + 1);
                }
                this.bindingMask = bindingMask;
                this.value = value;
            }

            public ParameterOverride(string objectRegistrationName, string parameterName, InputBinding bindingMask, PrimitiveValue value = default)
            {
                this.objectRegistrationName = objectRegistrationName;
                this.parameter = parameterName;
                this.bindingMask = bindingMask;
                this.value = value;
            }

            // Find the *most specific* override to apply to the given parameter.
            public static ParameterOverride? Find(InputActionMap actionMap, ref InputBinding binding, string parameterName, string objectRegistrationName)
            {
                // Look at level of map.
                var overrideOnMap = Find(actionMap.m_ParameterOverrides, actionMap.m_ParameterOverridesCount, ref binding, parameterName,
                    objectRegistrationName);

                // Look at level of asset (if present).
                var asset = actionMap.asset;
                var overrideOnAsset = asset != null
                    ? Find(asset.m_ParameterOverrides, asset.m_ParameterOverridesCount, ref binding, parameterName,
                    objectRegistrationName)
                    : null;

                return PickMoreSpecificOne(overrideOnMap, overrideOnAsset);
            }

            private static ParameterOverride? Find(ParameterOverride[] overrides, int overrideCount,
                ref InputBinding binding, string parameterName, string objectRegistrationName)
            {
                ParameterOverride? result = null;
                for (var i = 0; i < overrideCount; ++i)
                {
                    ref var current = ref overrides[i];

                    if (!string.Equals(parameterName, current.parameter, StringComparison.OrdinalIgnoreCase))
                        continue; // Different parameter name.

                    if (!current.bindingMask.Matches(binding))
                        continue;

                    if (current.objectRegistrationName != null && !string.Equals(current.objectRegistrationName, objectRegistrationName,
                        StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (result == null)
                    {
                        // First match.
                        result = current;
                    }
                    else
                    {
                        // Already have a match. See which one is more specific.
                        result = PickMoreSpecificOne(result, current);
                    }
                }
                return result;
            }

            private static ParameterOverride? PickMoreSpecificOne(ParameterOverride? first, ParameterOverride? second)
            {
                if (first == null)
                    return second;
                if (second == null)
                    return first;

                // Having an objectRegistrationName always wins vs not having one.
                if (first.Value.objectRegistrationName != null && second.Value.objectRegistrationName == null)
                    return first;
                if (second.Value.objectRegistrationName != null && first.Value.objectRegistrationName == null)
                    return second;

                // Targeting a specific path always wins vs not doing so.
                if (first.Value.bindingMask.effectivePath != null && second.Value.bindingMask.effectivePath == null)
                    return first;
                if (second.Value.bindingMask.effectivePath != null && first.Value.bindingMask.effectivePath == null)
                    return second;

                // Targeting a specific actions always wins vs not doing so.
                if (first.Value.bindingMask.action != null && second.Value.bindingMask.action == null)
                    return first;
                if (second.Value.bindingMask.action != null && first.Value.bindingMask.action == null)
                    return second;

                // Undecided. First wins by default.
                return first;
            }
        }
    }
}
