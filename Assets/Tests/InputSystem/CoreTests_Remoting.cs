using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Networking.PlayerConnection;
using UnityEngine.Scripting;
using Object = UnityEngine.Object;

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
            Assert.That(remote.remoteManager.devices, Has.Count.EqualTo(2));
            Assert.That(remote.remoteManager.devices, Has.Exactly(1).InstanceOf<Gamepad>().With.Property("layout").EqualTo("Gamepad"));
            Assert.That(remote.remoteManager.devices, Has.Exactly(1).InstanceOf<Keyboard>().With.Property("layout").EqualTo("Keyboard"));
            Assert.That(remote.remoteManager.devices, Has.All.With.Property("remote").True);
        }
    }

    // Here's the rationale for the behavior here:
    // - The idea is that the editor has *all* layouts for *all* platforms.
    // - Also, a given layout should not vary from platform to platform. Same layout, same result is the expectation.
    // - Layout *overrides* and replacements/substitutions should be made available in the editor just as in the player.
    // - ERGO: the editor does not need layouts sent over the wire and can just use the layout information it has.
    // - BUT: this does not work for generated layouts as these are generated on the fly from information available only on the devices.
    // - ERGO: generated layouts need to be sent over the wire.
    // We could support remoting *between* players where this assumption does not hold by remoting *all* layouts but ATM this
    // is not a relevant use case.
    [Test]
    [Category("Remote")]
    public void Remote_OnlyGeneratedLayoutsAreSentToRemotes()
    {
        // Register "normal" layout.
        InputSystem.RegisterLayout(@"
            {
                ""name"" : ""TestLayout_NOT_GENERATED"",
                ""extend"" : ""Gamepad"",
                ""controls"" : [
                    { ""name"" : ""newButton"", ""layout"" : ""Button"" }
                ]
            }
        ");

        // Register generated layout.
        InputSystem.RegisterLayoutBuilder(() =>
        {
            var builder = new InputControlLayout.Builder()
                .WithType<MyDevice>();
            builder.AddControl("MyControl")
                .WithLayout("Button");

            return builder.Build();
        }, "TestLayout_GENERATED");

        using (var remote = new FakeRemote())
        {
            Assert.That(remote.remoteManager.ListControlLayouts(), Has.None.EqualTo("TestLayout_NOT_GENERATED")); // Not remoted.
            Assert.That(remote.remoteManager.ListControlLayouts(), Has.Exactly(1).EqualTo("TestLayout_GENERATED")); // Remoted.

            // Make sure we do not remote "normal" layouts.
            Assert.That(remote.remoteManager.ListControlLayouts(),
                Has.None.Matches((string s) => s.StartsWith("Remote::") && s.EndsWith("Gamepad")));

            // Add a device using the layout builder.
            InputSystem.AddDevice("TestLayout_GENERATED");

            Assert.That(remote.remoteManager.devices, Has.Count.EqualTo(1));
            Assert.That(remote.remoteManager.devices[0].layout, Is.EqualTo("TestLayout_GENERATED"));
            Assert.That(remote.remoteManager.devices[0].remote, Is.True);

            // Register another "normal" layout.
            InputSystem.RegisterLayout(@"
                {
                    ""name"" : ""OtherLayout_NOT_GENERATED"",
                    ""extend"" : ""Gamepad"",
                    ""controls"" : [
                        { ""name"" : ""newButton"", ""layout"" : ""Button"" }
                    ]
                }
            ");

            Assert.That(remote.remoteManager.ListControlLayouts(), Has.None.EqualTo("OtherLayout_NOT_GENERATED")); // Not remoted.

            // Register another generated layout.
            InputSystem.RegisterLayoutBuilder(() =>
            {
                var builder = new InputControlLayout.Builder()
                    .WithType<MyDevice>();
                builder.AddControl("MyControl")
                    .WithLayout("Button");

                return builder.Build();
            }, "OtherLayout_GENERATED");

            Assert.That(remote.remoteManager.ListControlLayouts(), Has.Exactly(1).EqualTo("OtherLayout_GENERATED")); // Remoted.

            // Remove the two layouts we just added. Shouldn't make a difference
            // on the remote.
            InputSystem.RemoveLayout("OtherLayout_GENERATED");
            InputSystem.RemoveLayout("OtherLayout_NOT_GENERATED");

            Assert.That(remote.remoteManager.ListControlLayouts(), Has.None.EqualTo("OtherLayout_NOT_GENERATED")); // Not remoted.
            Assert.That(remote.remoteManager.ListControlLayouts(), Has.Exactly(1).EqualTo("OtherLayout_GENERATED")); // Remoted.
        }
    }

    [Test]
    [Category("Remote")]
    public void Remote_EventsAreSentToRemotes()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();

        using (var remote = new FakeRemote())
        {
            Set(gamepad.leftTrigger, 0.5f, time: 0.1234);

            // Make second input manager process the events it got.
            // NOTE: This will also switch the system to the state buffers from the second input manager.
            remote.remoteManager.Update();

            var remoteGamepad = (Gamepad)remote.remoteManager.devices[0];

            Assert.That(remoteGamepad.leftTrigger.ReadValue(), Is.EqualTo(0.5).Within(0.0000001));
            Assert.That(remoteGamepad.lastUpdateTime, Is.EqualTo(0.1234).Within(0.000001));
        }
    }

    [Test]
    [Category("Remote")]
    public void Remote_RemovingDevice_WillRemoveItFromRemotes()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        using (var remote = new FakeRemote())
        {
            Assert.That(remote.remoteManager.devices.Count, Is.EqualTo(1));

            InputSystem.RemoveDevice(gamepad);

            Assert.That(remote.remoteManager.devices.Count, Is.Zero);
        }
    }

    [Test]
    [Category("Remote")]
    public void Remote_DevicesWithExistingUsage_WillUpdateSendToRemote()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        InputSystem.SetDeviceUsage(gamepad, CommonUsages.LeftHand);
        InputSystem.AddDeviceUsage(gamepad, CommonUsages.RightHand);

        using (var remote = new FakeRemote())
        {
            var remoteGamepad = (Gamepad)remote.remoteManager.devices[0];
            Assert.That(remoteGamepad.usages, Has.Count.EqualTo(2));
            Assert.That(remoteGamepad.usages, Has.Exactly(1).EqualTo(CommonUsages.LeftHand));
            Assert.That(remoteGamepad.usages, Has.Exactly(1).EqualTo(CommonUsages.RightHand));
        }
    }

    [Test]
    [Category("Remote")]
    public void Remote_SettingUsageOnDevice_WillSendChangeToRemotes()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        using (var remote = new FakeRemote())
        {
            var remoteGamepad = (Gamepad)remote.remoteManager.devices[0];
            Assert.That(remoteGamepad.usages, Has.Count.Zero);

            // Can Set
            InputSystem.SetDeviceUsage(gamepad, CommonUsages.LeftHand);
            Assert.That(remoteGamepad.usages, Has.Count.EqualTo(1));
            Assert.That(remoteGamepad.usages, Has.Exactly(1).EqualTo(CommonUsages.LeftHand));

            // Can Replace
            InputSystem.SetDeviceUsage(gamepad, CommonUsages.RightHand);
            Assert.That(remoteGamepad.usages, Has.Count.EqualTo(1));
            Assert.That(remoteGamepad.usages, Has.Exactly(1).EqualTo(CommonUsages.RightHand));

            //Can Clear
            InputSystem.RemoveDeviceUsage(gamepad, CommonUsages.LeftHand);
            InputSystem.RemoveDeviceUsage(gamepad, CommonUsages.RightHand);
            Assert.That(remoteGamepad.usages, Has.Count.Zero);

            //Can Set Multiple
            InputSystem.AddDeviceUsage(gamepad, CommonUsages.LeftHand);
            InputSystem.AddDeviceUsage(gamepad, CommonUsages.RightHand);
            Assert.That(remoteGamepad.usages, Has.Count.EqualTo(2));
            Assert.That(remoteGamepad.usages, Has.Exactly(1).EqualTo(CommonUsages.LeftHand));
            Assert.That(remoteGamepad.usages, Has.Exactly(1).EqualTo(CommonUsages.RightHand));
        }
    }

    [Test]
    [Category("Remote")]
    public void Remote_ResettingDevice_WillSendChangeToRemotes()
    {
        var mouse = InputSystem.AddDevice<Mouse>(); // Use a device with dontReset controls so that a soft reset won't just turn into a hard reset.
        Press(mouse.leftButton);

        using (var remote = new FakeRemote())
        {
            var remoteMouse = (Mouse)remote.remoteManager.devices[0];

            // Process remoted events.
            remote.SwitchToRemoteState();
            remote.remoteManager.Update();

            Assert.That(remoteMouse.leftButton.isPressed, Is.True);

            var remoteMouseWasSoftReset = false;
            var remoteMouseWasHardReset = false;
            remote.remoteManager.onDeviceChange += (device, change) =>
            {
                if (device == remoteMouse && change == InputDeviceChange.SoftReset)
                {
                    Assert.That(remoteMouseWasSoftReset, Is.False);
                    remoteMouseWasSoftReset = true;
                }
                if (device == remoteMouse && change == InputDeviceChange.HardReset)
                {
                    Assert.That(remoteMouseWasHardReset, Is.False);
                    remoteMouseWasHardReset = true;
                }
            };

            remote.SwitchToLocalState();
            InputSystem.ResetDevice(mouse);
            InputSystem.Update();

            remote.SwitchToRemoteState();
            remote.remoteManager.Update();

            Assert.That(remoteMouse.leftButton.isPressed, Is.False);
            Assert.That(remoteMouseWasSoftReset, Is.True);
            Assert.That(remoteMouseWasHardReset, Is.False);

            remoteMouseWasSoftReset = false;

            remote.SwitchToLocalState();
            InputSystem.ResetDevice(mouse, alsoResetDontResetControls: true);
            InputSystem.Update();

            remote.SwitchToRemoteState();
            remote.remoteManager.Update();

            Assert.That(remoteMouseWasSoftReset, Is.False);
            Assert.That(remoteMouseWasHardReset, Is.True);
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

        Object.Destroy(connectionToEditor);
        Object.Destroy(connectionToPlayer);
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
            if (!m_MessageListeners.TryGetValue(messageId, out var msgEvent))
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
            if (m_MessageListeners.TryGetValue(messageId, out var msgEvent))
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

        private readonly Dictionary<Guid, MessageEvent> m_MessageListeners = new Dictionary<Guid, MessageEvent>();
        private readonly ConnectEvent m_ConnectionListeners = new ConnectEvent();
        private readonly ConnectEvent m_DisconnectionListeners = new ConnectEvent();

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

        public InputRemoting local;
        public InputRemoting remote;

        public InputManager localManager => local.manager;
        public InputManager remoteManager => remote.manager;

        public FakeRemote()
        {
            runtime = new InputTestRuntime();
            var manager = new InputManager();
            manager.m_Settings = ScriptableObject.CreateInstance<InputSettings>();
            manager.InitializeData();
            manager.InstallRuntime(runtime);
            manager.ApplySettings();

            local = new InputRemoting(InputSystem.s_Manager);
            remote = new InputRemoting(manager);

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

        public void SwitchToRemoteState()
        {
            InputSystem.s_Manager = remoteManager;
            InputStateBuffers.SwitchTo(remoteManager.m_StateBuffers, remoteManager.defaultUpdateType);
        }

        public void SwitchToLocalState()
        {
            InputSystem.s_Manager = localManager;
            InputStateBuffers.SwitchTo(localManager.m_StateBuffers, localManager.defaultUpdateType);
        }

        public void Dispose()
        {
            if (runtime != null)
            {
                runtime.Dispose();
                runtime = null;
            }
            if (remoteManager != null)
            {
                Object.Destroy(remoteManager.m_Settings);
                remoteManager.Destroy();
            }
        }
    }

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
