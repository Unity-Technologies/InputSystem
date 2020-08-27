using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Networking.PlayerConnection;
using UnityEngine.Scripting;

////TODO: have to decide what to do if a layout is removed

partial class CoreTests
{
    [Test]
    [Category("Remote")]
    public void Remote_ExistingDevicesAreSentToRemotes_WhenStartingToSend()
    {
        InputSystem.AddDevice<Gamepad>();
        InputSystem.AddDevice<Keyboard>();

        using (var remote = new FakeRemote())
        {
            Assert.That(remote.manager.devices, Has.Count.EqualTo(2));
            Assert.That(remote.manager.devices, Has.Exactly(1).InstanceOf<Gamepad>().With.Property("layout").EqualTo("Gamepad"));
            Assert.That(remote.manager.devices, Has.Exactly(1).InstanceOf<Keyboard>().With.Property("layout").EqualTo("Keyboard"));
            Assert.That(remote.manager.devices, Has.All.With.Property("remote").True);
        }
    }

    [Test]
    [Category("Remote")]
#if UNITY_ANDROID && !UNITY_EDITOR
    [Ignore("Case 1254567")]
#endif
    public void Remote_EventsAreSentToRemotes()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        using (var remote = new FakeRemote())
        {
            InputSystem.QueueStateEvent(gamepad, new GamepadState {leftTrigger = 0.5f}, 0.1234);
            InputSystem.Update();

            // Make second input manager process the events it got.
            // NOTE: This will also switch the system to the state buffers from the second input manager.
            remote.manager.Update();

            var remoteGamepad = (Gamepad)remote.manager.devices[0];

            Assert.That(remoteGamepad.leftTrigger.ReadValue(), Is.EqualTo(0.5).Within(0.0000001));
            Assert.That(remoteGamepad.lastUpdateTime, Is.EqualTo(0.1234).Within(0.000001));
        }
    }

    [Test]
    [Category("Remote")]
    public void Remote_AddingNewControlLayout_WillSendLayoutToRemotes()
    {
        using (var remote = new FakeRemote())
        {
            InputSystem.RegisterLayout(@"{ ""name"" : ""MyGamepad"", ""extend"" : ""Gamepad"" }");
            InputSystem.AddDevice("MyGamepad");

            var layouts = remote.manager.ListControlLayouts().ToList();

            Assert.That(layouts, Has.Exactly(1).EqualTo("Remote::MyGamepad"));
            Assert.That(remote.manager.devices, Has.Exactly(1).With.Property("layout").EqualTo("Remote::MyGamepad").And.TypeOf<Gamepad>());
            Assert.That(remote.manager.TryLoadControlLayout(new InternedString("Remote::MyGamepad")),
                Is.Not.Null.And.With.Property("baseLayouts").EquivalentTo(new[] {new InternedString("Gamepad")}));
        }
    }

    [Test]
    [Category("Remote")]
    public void Remote_AddingNewOverrideLayout_WillSendLayoutToRemotes()
    {
        const string overrideJson = @"
            {
                ""name"" : ""MyOverride"",
                ""extend"" : ""Gamepad"",
                ""commonUsages"" : [ ""LeftHand"", ""RightHand"" ]
            }
        ";

        using (var remote = new FakeRemote())
        {
            InputSystem.RegisterLayoutOverride(overrideJson);

            InputSystem.AddDevice("Gamepad");

            Assert.That(remote.manager.devices, Has.Count.EqualTo(1));
            Assert.That(remote.manager.devices, Has.All.With.Property("remote").True);

            var layout = remote.manager.TryLoadControlLayout(new InternedString("Gamepad"));

            Assert.That(layout.appliedOverrides, Is.EquivalentTo(new[] {new InternedString("Remote::MyOverride")}));
            Assert.That(layout.commonUsages.Count, Is.EqualTo(2));
            Assert.That(layout.commonUsages, Has.Exactly(1).EqualTo(new InternedString("LeftHand")));
            Assert.That(layout.commonUsages, Has.Exactly(1).EqualTo(new InternedString("RightHand")));
        }
    }

    [Test]
    [Category("Remote")]
    [Ignore("TODO")] //// TODO: Extend field of derived layout needs to be fixed by receiver to use namespace of base layout
    public void Remote_AddingNewOverrideLayoutExtendingCustomLayout_WillSendLayoutToRemotes()
    {
        const string json = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""Gamepad"",
                ""controls"" : [
                    {
                        ""name"" : ""MyControl"",
                        ""layout"" : ""Button""
                    }
                ]
            }
        ";

        const string overrideJson = @"
            {
                ""name"" : ""MyOverride"",
                ""extend"" : ""MyDevice"",
                ""commonUsages"" : [ ""LeftHand"", ""RightHand"" ]
            }
        ";

        using (var remote = new FakeRemote())
        {
            InputSystem.RegisterLayout(json);
            InputSystem.RegisterLayoutOverride(overrideJson);

            InputSystem.AddDevice("MyDevice");

            var layouts = remote.manager.ListControlLayouts().ToList();
            Assert.That(layouts, Has.Exactly(1).EqualTo("Remote::MyDevice"));

            Assert.That(remote.manager.devices, Has.Count.EqualTo(1));
            Assert.That(remote.manager.devices, Has.All.With.Property("layout").EqualTo("Remote::MyDevice"));
            Assert.That(remote.manager.devices, Has.All.With.Property("remote").True);

            var layout = remote.manager.TryLoadControlLayout(new InternedString("Remote::MyDevice"));

            Assert.That(layout.appliedOverrides, Is.EquivalentTo(new[] {new InternedString("Remote::MyOverride")}));
            Assert.That(layout.commonUsages.Count, Is.EqualTo(2));
            Assert.That(layout.commonUsages, Has.Exactly(1).EqualTo(new InternedString("LeftHand")));
            Assert.That(layout.commonUsages, Has.Exactly(1).EqualTo(new InternedString("RightHand")));
        }
    }

    [Test]
    [Category("Remote")]
    public void Remote_ConnectingWithExistingGeneratedLayout_WillSendLayoutToRemotes()
    {
        InputSystem.RegisterLayoutBuilder(() =>
        {
            var builder = new InputControlLayout.Builder()
                .WithType<MyDevice>();
            builder.AddControl("MyControl")
                .WithLayout("Button");

            return builder.Build();
        },
            "MyCustomLayout");
        InputSystem.AddDevice("MyCustomLayout");

        using (var remote = new FakeRemote())
        {
            var layouts = remote.manager.ListControlLayouts().ToList();
            Assert.That(layouts, Has.Exactly(1).EqualTo("Remote::MyCustomLayout"));

            Assert.That(remote.manager.devices, Has.Count.EqualTo(1));
            Assert.That(remote.manager.devices, Has.All.With.Property("layout").EqualTo("Remote::MyCustomLayout"));
            Assert.That(remote.manager.devices, Has.All.With.Property("remote").True);
        }
    }

    [Test]
    [Category("Remote")]
    public void Remote_AddingNewGeneratedLayout_WillSendLayoutToRemotes()
    {
        using (var remote = new FakeRemote())
        {
            InputSystem.RegisterLayoutBuilder(() =>
            {
                var builder = new InputControlLayout.Builder()
                    .WithType<MyDevice>();
                builder.AddControl("MyControl")
                    .WithLayout("Button");

                return builder.Build();
            },
                "MyCustomLayout");
            InputSystem.AddDevice("MyCustomLayout");

            var layouts = remote.manager.ListControlLayouts().ToList();
            Assert.That(layouts, Has.Exactly(1).EqualTo("Remote::MyCustomLayout"));

            Assert.That(remote.manager.devices, Has.Count.EqualTo(1));
            Assert.That(remote.manager.devices, Has.All.With.Property("layout").EqualTo("Remote::MyCustomLayout"));
            Assert.That(remote.manager.devices, Has.All.With.Property("remote").True);
        }
    }

    [Test]
    [Category("Remote")]
    [Ignore("TODO")] //// TODO: Extend field of derived layout needs to be fixed by receiver to use namespace of base layout
    public void Remote_AddingNewLayoutExtendingCustomLayout_WillSendLayoutToRemotes()
    {
        const string baseLayout = @"
            {
                ""name"" : ""BaseLayout"",
                ""controls"" : [
                    {
                        ""name"" : ""stick"",
                        ""layout"" : ""Stick"",
                        ""usage"" : ""BaseUsage""
                    },
                    {
                        ""name"" : ""axis"",
                        ""layout"" : ""Axis"",
                        ""usage"" : ""BaseUsage""
                    }
                ]
            }
        ";

        const string derivedLayout = @"
            {
                ""name"" : ""MyDevice"",
                ""extend"" : ""BaseLayout"",
                ""controls"" : [
                    {
                        ""name"" : ""stick"",
                        ""usage"" : ""DerivedUsage""
                    },
                    {
                        ""name"" : ""button"",
                        ""layout"" : ""Button"",
                        ""usage"" : ""DerivedUsage""
                    }
                ]
            }
        ";

        using (var remote = new FakeRemote())
        {
            InputSystem.RegisterLayout(baseLayout);
            InputSystem.RegisterLayout(derivedLayout);
            InputSystem.AddDevice("MyDevice");

            var layouts = remote.manager.ListControlLayouts().ToList();
            Assert.That(layouts, Has.Exactly(1).EqualTo("Remote::MyDevice"));

            Assert.That(remote.manager.devices, Has.Count.EqualTo(1));
            Assert.That(remote.manager.devices, Has.All.With.Property("layout").EqualTo("Remote::MyDevice"));
            Assert.That(remote.manager.devices, Has.All.With.Property("remote").True);

            var layout = remote.manager.TryLoadControlLayout(new InternedString("Remote::MyDevice"));

            Assert.That(layout["stick"].usages.Count, Is.EqualTo(1));
            Assert.That(layout["stick"].usages, Has.Exactly(1).EqualTo(new InternedString("DerivedUsage")));
            Assert.That(layout["axis"].usages.Count, Is.EqualTo(1));
            Assert.That(layout["axis"].usages, Has.Exactly(1).EqualTo(new InternedString("BaseUsage")));
            Assert.That(layout["button"].usages.Count, Is.EqualTo(1));
            Assert.That(layout["button"].usages, Has.Exactly(1).EqualTo(new InternedString("DerivedUsage")));
        }
    }

    [Test]
    [Category("Remote")]
    [Ignore("TODO")] //// TODO: Layout field of controls item needs to be fixed by receiver to use namespace of control layout
    public void Remote_AddingNewLayoutWithCustomControlLayout_WillSendLayoutToRemotes()
    {
        const string controlJson = @"
            {
                ""name"" : ""MyControl"",
                ""extend"" : ""Vector2""
            }
        ";
        const string deviceJson = @"
            {
                ""name"" : ""MyDevice"",
                ""controls"" : [
                    {
                        ""name"" : ""myThing"",
                        ""layout"" : ""MyControl"",
                        ""usage"" : ""LeftStick""
                    }
                ]
            }
        ";

        using (var remote = new FakeRemote())
        {
            InputSystem.RegisterLayout(controlJson);
            InputSystem.RegisterLayout(deviceJson);
            InputSystem.AddDevice("MyDevice");

            var layouts = remote.manager.ListControlLayouts().ToList();
            Assert.That(layouts, Has.Exactly(1).EqualTo("Remote::MyDevice"));

            Assert.That(remote.manager.devices, Has.Count.EqualTo(1));
            Assert.That(remote.manager.devices, Has.All.With.Property("layout").EqualTo("Remote::MyDevice"));
            Assert.That(remote.manager.devices, Has.All.With.Property("remote").True);
        }
    }

    [Test]
    [Category("Remote")]
    public void Remote_RemovingLayout_WillRemoveItFromRemotes()
    {
        const string json = @"
            {
                ""name"" : ""MyGamepad"",
                ""extend"" : ""Gamepad""
            }
        ";

        using (var remote = new FakeRemote())
        {
            InputSystem.RegisterLayout(json);

            var layouts = remote.manager.ListControlLayouts().ToList();
            Assert.That(layouts, Has.Exactly(1).EqualTo("Remote::MyGamepad"));

            InputSystem.RemoveLayout("MyGamepad");

            layouts = remote.manager.ListControlLayouts().ToList();
            Assert.That(layouts, Has.Exactly(0).EqualTo("Remote::MyGamepad"));
        }
    }

    [Test]
    [Category("Remote")]
    public void Remote_RemovingDevice_WillRemoveItFromRemotes()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        using (var remote = new FakeRemote())
        {
            Assert.That(remote.manager.devices.Count, Is.EqualTo(1));

            InputSystem.RemoveDevice(gamepad);

            Assert.That(remote.manager.devices.Count, Is.Zero);
        }
    }

    [Test]
    [Category("Remote")]
    public void Remote_SettingUsageOnDevice_WillSendChangeToRemotes()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        using (var remote = new FakeRemote())
        {
            var remoteGamepad = (Gamepad)remote.manager.devices[0];
            Assert.That(remoteGamepad.usages, Has.Count.Zero);

            InputSystem.SetDeviceUsage(gamepad, CommonUsages.LeftHand);

            Assert.That(remoteGamepad.usages, Has.Exactly(1).EqualTo(CommonUsages.LeftHand));
        }
    }

    [Test]
    [Category("Remote")]
    public void Remote_CanConnectInputSystemsOverEditorPlayerConnection()
    {
#if UNITY_EDITOR
        // In the editor, RemoteInputPlayerConnection is a scriptable singleton. Creating multiple instances of it
        // will cause an error messages - but will work nevertheless, so we expect those errors to let us run the test.
        // We call RemoteInputPlayerConnection.instance once to make sure that we an instance is created, and we get
        // a deterministic number of two errors.
        var instance = RemoteInputPlayerConnection.instance;
        UnityEngine.TestTools.LogAssert.Expect(LogType.Error, "ScriptableSingleton already exists. Did you query the singleton in a constructor?");
        UnityEngine.TestTools.LogAssert.Expect(LogType.Error, "ScriptableSingleton already exists. Did you query the singleton in a constructor?");
#endif
        var connectionToEditor = ScriptableObject.CreateInstance<RemoteInputPlayerConnection>();
        var connectionToPlayer = ScriptableObject.CreateInstance<RemoteInputPlayerConnection>();

        connectionToEditor.name = "ConnectionToEditor";
        connectionToPlayer.name = "ConnectionToPlayer";

        var fakeEditorConnection = new FakePlayerConnection {playerId = 0};
        var fakePlayerConnection = new FakePlayerConnection {playerId = 1};

        fakeEditorConnection.otherEnd = fakePlayerConnection;
        fakePlayerConnection.otherEnd = fakeEditorConnection;

        var observerEditor = new RemoteTestObserver();
        var observerPlayer = new RemoteTestObserver();

        // In the Unity API, "PlayerConnection" is the connection to the editor
        // and "EditorConnection" is the connection to the player. Seems counter-intuitive.
        connectionToEditor.Bind(fakePlayerConnection, true);
        connectionToPlayer.Bind(fakeEditorConnection, true);

        // Bind a local remote on the player side.
        var local = new InputRemoting(InputSystem.s_Manager);
        local.Subscribe(connectionToEditor);

        connectionToEditor.Subscribe(local);
        connectionToPlayer.Subscribe(observerEditor);
        connectionToEditor.Subscribe(observerPlayer);

        fakeEditorConnection.Send(RemoteInputPlayerConnection.kStartSendingMsg, null);

        var device = InputSystem.AddDevice<Gamepad>();
        InputSystem.QueueStateEvent(device, new GamepadState());
        InputSystem.Update();
        InputSystem.RemoveDevice(device);

        fakeEditorConnection.Send(RemoteInputPlayerConnection.kStopSendingMsg, null);

        // We should not obseve any messages for these, as we stopped sending!
        device = InputSystem.AddDevice<Gamepad>();
        InputSystem.QueueStateEvent(device, new GamepadState());
        InputSystem.Update();
        InputSystem.RemoveDevice(device);

        fakeEditorConnection.DisconnectAll();

        ////TODO: make sure that we also get the connection sequence right and send our initial layouts and devices
        Assert.That(observerEditor.messages, Has.Count.EqualTo(5));
        Assert.That(observerEditor.messages[0].type, Is.EqualTo(InputRemoting.MessageType.Connect));
        Assert.That(observerEditor.messages[1].type, Is.EqualTo(InputRemoting.MessageType.NewDevice));
        Assert.That(observerEditor.messages[2].type, Is.EqualTo(InputRemoting.MessageType.NewEvents));
        Assert.That(observerEditor.messages[3].type, Is.EqualTo(InputRemoting.MessageType.RemoveDevice));
        Assert.That(observerEditor.messages[4].type, Is.EqualTo(InputRemoting.MessageType.Disconnect));

        Assert.That(observerPlayer.messages, Has.Count.EqualTo(3));
        Assert.That(observerPlayer.messages[0].type, Is.EqualTo(InputRemoting.MessageType.Connect));
        Assert.That(observerPlayer.messages[1].type, Is.EqualTo(InputRemoting.MessageType.StartSending));
        Assert.That(observerPlayer.messages[2].type, Is.EqualTo(InputRemoting.MessageType.StopSending));

        ScriptableObject.Destroy(connectionToEditor);
        ScriptableObject.Destroy(connectionToPlayer);
    }

    // If we have more than two players connected, for example, and we add a layout from player A
    // to the system, we don't want to send the layout to player B in turn. I.e. all data mirrored
    // from remotes should stay local.
    [Test]
    [Category("Remote")]
    [Ignore("TODO")]
    public void TODO_Remote_WithMultipleRemotesConnected_DoesNotDuplicateDataFromOneRemoteToOtherRemotes()
    {
        Assert.Fail();
    }

    // PlayerConnection isn't connected in the editor and EditorConnection isn't connected
    // in players so we can't really test actual transport in just the application itself.
    // This will act as an IEditorPlayerConnection that immediately makes the FakePlayerConnection
    // on the other end receive messages.
    private class FakePlayerConnection : IEditorPlayerConnection
    {
        public int playerId;

        // The fake connection acting as the socket on the opposite end of us.
        public FakePlayerConnection otherEnd;

        public void Register(Guid messageId, UnityAction<MessageEventArgs> callback)
        {
            MessageEvent msgEvent;
            if (!m_MessageListeners.TryGetValue(messageId, out msgEvent))
            {
                msgEvent = new MessageEvent();
                m_MessageListeners[messageId] = msgEvent;
            }

            msgEvent.AddListener(callback);
        }

        public void Unregister(Guid messageId, UnityAction<MessageEventArgs> callback)
        {
            m_MessageListeners[messageId].RemoveListener(callback);
        }

        public void DisconnectAll()
        {
            m_DisconnectionListeners.Invoke(playerId);
            m_MessageListeners.Clear();
            m_ConnectionListeners.RemoveAllListeners();
            m_DisconnectionListeners.RemoveAllListeners();
        }

        public void RegisterConnection(UnityAction<int> callback)
        {
            m_ConnectionListeners.AddListener(callback);
        }

        public void RegisterDisconnection(UnityAction<int> callback)
        {
            m_DisconnectionListeners.AddListener(callback);
        }

        public void UnregisterConnection(UnityAction<int> callback)
        {
            m_ConnectionListeners.RemoveListener(callback);
        }

        public void UnregisterDisconnection(UnityAction<int> callback)
        {
            m_DisconnectionListeners.RemoveListener(callback);
        }

        public void Receive(Guid messageId, byte[] data)
        {
            MessageEvent msgEvent;
            if (m_MessageListeners.TryGetValue(messageId, out msgEvent))
                msgEvent.Invoke(new MessageEventArgs {playerId = playerId, data = data});
        }

        public void Send(Guid messageId, byte[] data)
        {
            otherEnd.Receive(messageId, data);
        }

        public bool TrySend(Guid messageId, byte[] data)
        {
            Send(messageId, data);
            return true;
        }

        private Dictionary<Guid, MessageEvent> m_MessageListeners = new Dictionary<Guid, MessageEvent>();
        private ConnectEvent m_ConnectionListeners = new ConnectEvent();
        private ConnectEvent m_DisconnectionListeners = new ConnectEvent();

        private class MessageEvent : UnityEvent<MessageEventArgs>
        {
        }

        private class ConnectEvent : UnityEvent<int>
        {
        }
    }

    private class RemoteTestObserver : IObserver<InputRemoting.Message>
    {
        public List<InputRemoting.Message> messages = new List<InputRemoting.Message>();

        public void OnNext(InputRemoting.Message msg)
        {
            messages.Add(msg);
        }

        public void OnError(Exception error)
        {
        }

        public void OnCompleted()
        {
        }
    }

    private class GlobalsInstallerObserver : IObserver<InputRemoting.Message>
    {
        private readonly InputManager m_Manager;

        public GlobalsInstallerObserver(InputManager manager)
        {
            m_Manager = manager;
        }

        public void OnNext(InputRemoting.Message msg)
        {
            m_Manager.InstallGlobals();
        }

        public void OnError(Exception error)
        {
        }

        public void OnCompleted()
        {
        }
    }

    private class FakeRemote : IDisposable
    {
        public InputTestRuntime runtime;
        public InputManager manager;

        public InputRemoting local;
        public InputRemoting remote;

        private static readonly InternedString s_LayoutNamespace = new InternedString("Remote");

        public FakeRemote()
        {
            runtime = new InputTestRuntime();
            manager = new InputManager();
            manager.m_Settings = ScriptableObject.CreateInstance<InputSettings>();
            manager.InstallRuntime(runtime);
            manager.InitializeData();
            manager.ApplySettings();

            local = new InputRemoting(InputSystem.s_Manager);
            remote = new InputRemoting(manager)
            {
                layoutNamespaceBuilder = BuildLayoutNamespace,
            };

            var remoteInstaller = new GlobalsInstallerObserver(manager);
            var localInstaller = new GlobalsInstallerObserver(InputSystem.s_Manager);

            // The installers will ensure the globals environment is prepared right before
            // the receiver processes the message. There are some static fields, such as
            // the layouts collection, that needs to be set to that InputManager's version.
            // After processing, the environment will be reverted back to the local manager
            // to keep it the default.
            local.Subscribe(remoteInstaller);
            local.Subscribe(remote);
            local.Subscribe(localInstaller);
            remote.Subscribe(localInstaller);
            remote.Subscribe(local);

            local.StartSending();
        }

        static InternedString BuildLayoutNamespace(int senderId)
        {
            return s_LayoutNamespace;
        }

        ~FakeRemote()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (runtime != null)
            {
                // During tear down, restore the globals of this local InputManager
                // since that is the expected default for all tests.
                InputSystem.s_Manager?.InstallGlobals();

                runtime.Dispose();
                runtime = null;
            }
        }
    }

    [Preserve]
    private class MyDevice : InputDevice
    {
        public ButtonControl myControl { get; private set; }

        protected override void FinishSetup()
        {
            base.FinishSetup();

            myControl = GetChildControl<ButtonControl>(nameof(myControl));
        }
    }
}
