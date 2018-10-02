using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Utilities;
using UnityEngine.Networking.PlayerConnection;

#if !(NET_4_0 || NET_4_6 || NET_STANDARD_2_0 || UNITY_WSA)
using UnityEngine.Experimental.Input.Net35Compatibility;
#endif

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
            Assert.That(remote.manager.devices, Has.Exactly(1).TypeOf<Gamepad>().With.Property("layout").EqualTo("Gamepad"));
            Assert.That(remote.manager.devices, Has.Exactly(1).TypeOf<Keyboard>().With.Property("layout").EqualTo("Keyboard"));
            Assert.That(remote.manager.devices, Has.All.With.Property("remote").True);
        }
    }

    [Test]
    [Category("Remote")]
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

            var layouts = new List<string>();
            remote.manager.ListControlLayouts(layouts);

            Assert.That(layouts, Has.Exactly(1).EqualTo("MyGamepad"));
            Assert.That(remote.manager.devices, Has.Exactly(1).With.Property("layout").EqualTo("MyGamepad").And.TypeOf<Gamepad>());
            Assert.That(remote.manager.TryLoadControlLayout(new InternedString("MyGamepad")),
                Is.Not.Null.And.With.Property("baseLayouts").EquivalentTo(new[] {new InternedString("Gamepad")}));
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
        var gamepad = InputSystem.AddDevice("Gamepad");
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
        var connectionToEditor = ScriptableObject.CreateInstance<RemoteInputPlayerConnection>();
        var connectionToPlayer = ScriptableObject.CreateInstance<RemoteInputPlayerConnection>();

        connectionToEditor.name = "ConnectionToEditor";
        connectionToPlayer.name = "ConnectionToPlayer";

        var fakeEditorConnection = new FakePlayerConnection {playerId = 0};
        var fakePlayerConnection = new FakePlayerConnection {playerId = 1};

        fakeEditorConnection.otherEnd = fakePlayerConnection;
        fakePlayerConnection.otherEnd = fakeEditorConnection;

        var observer = new RemoteTestObserver();

        // In the Unity API, "PlayerConnection" is the connection to the editor
        // and "EditorConnection" is the connection to the player. Seems counter-intuitive.
        connectionToEditor.Bind(fakePlayerConnection, true);
        connectionToPlayer.Bind(fakeEditorConnection, true);

        // Bind a local remote on the player side.
        var local = new InputRemoting(InputSystem.s_Manager);
        local.Subscribe(connectionToEditor);
        local.StartSending();

        connectionToPlayer.Subscribe(observer);

        var device = InputSystem.AddDevice("Gamepad");
        InputSystem.QueueStateEvent(device, new GamepadState());
        InputSystem.Update();
        InputSystem.RemoveDevice(device);

        ////TODO: make sure that we also get the connection sequence right and send our initial layouts and devices
        Assert.That(observer.messages, Has.Count.EqualTo(4));
        Assert.That(observer.messages[0].type, Is.EqualTo(InputRemoting.MessageType.Connect));
        Assert.That(observer.messages[1].type, Is.EqualTo(InputRemoting.MessageType.NewDevice));
        Assert.That(observer.messages[2].type, Is.EqualTo(InputRemoting.MessageType.NewEvents));
        Assert.That(observer.messages[3].type, Is.EqualTo(InputRemoting.MessageType.RemoveDevice));

        ////TODO: test disconnection

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

    private class FakeRemote : IDisposable
    {
        public InputTestRuntime runtime;
        public InputManager manager;

        public InputRemoting local;
        public InputRemoting remote;

        public FakeRemote()
        {
            runtime = new InputTestRuntime();
            manager = new InputManager();
            manager.InstallRuntime(runtime);
            manager.InitializeData();

            local = new InputRemoting(InputSystem.s_Manager);
            remote = new InputRemoting(manager);

            local.Subscribe(remote);
            remote.Subscribe(local);

            local.StartSending();
        }

        ~FakeRemote()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (runtime != null)
            {
                runtime.Dispose();
                runtime = null;
            }
        }
    }
}
