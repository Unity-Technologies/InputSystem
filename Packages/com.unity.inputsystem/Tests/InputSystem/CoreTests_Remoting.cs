using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Experimental.Input;
using UnityEngine.Experimental.Input.LowLevel;
using UnityEngine.Experimental.Input.Utilities;
using UnityEngine.Networking.PlayerConnection;

#if !(NET_4_0 || NET_4_6 || NET_STANDARD_2_0)
using UnityEngine.Experimental.Input.Net35Compatibility;
#endif

partial class CoreTests
{
    [Test]
    [Category("Remote")]
    public void Remote_CanConnectTwoInputSystemsOverNetwork()
    {
        // Add some data to the local input system.
        InputSystem.AddDevice("Gamepad");
        InputSystem.RegisterControlLayout(@"{ ""name"" : ""MyGamepad"", ""extend"" : ""Gamepad"" }");
        var localGamepad = (Gamepad)InputSystem.AddDevice("MyGamepad");

        // Now create another input system instance and connect it
        // to our "local" instance.
        // NOTE: This relies on internal APIs. We want remoting as such to be available
        //       entirely from user land but having multiple input systems in the same
        //       application isn't something that we necessarily want to expose (we do
        //       have global state so it can easily lead to surprising results).
        var secondInputRuntime = new InputTestRuntime();
        var secondInputManager = new InputManager();
        secondInputManager.InstallRuntime(secondInputRuntime);
        secondInputManager.InitializeData();

        var local = new InputRemoting(InputSystem.s_Manager);
        var remote = new InputRemoting(secondInputManager);

        // We wire the two directly into each other effectively making function calls
        // our "network transport layer". In a real networking situation, we'd effectively
        // have an RPC-like mechanism sitting in-between.
        local.Subscribe(remote);
        remote.Subscribe(local);

        local.StartSending();

        var remoteGamepadLayout =
            string.Format("{0}0::{1}", InputRemoting.kRemoteLayoutNamespacePrefix, localGamepad.layout);

        // Make sure that our "remote" system now has the data we initially
        // set up on the local system.
        Assert.That(secondInputManager.devices,
            Has.Exactly(1).With.Property("layout").EqualTo(remoteGamepadLayout));
        Assert.That(secondInputManager.devices, Has.Exactly(2).TypeOf<Gamepad>());
        Assert.That(secondInputManager.devices, Has.All.With.Property("remote").True);

        // Send state event to local gamepad.
        InputSystem.QueueStateEvent(localGamepad, new GamepadState {leftTrigger = 0.5f});
        InputSystem.Update();

        // Make second input manager process the events it got.
        // NOTE: This will also switch the system to the state buffers from the second input manager.
        secondInputManager.Update();

        var remoteGamepad = (Gamepad)secondInputManager.devices.First(x => x.layout == remoteGamepadLayout);

        Assert.That(remoteGamepad.leftTrigger.ReadValue(), Is.EqualTo(0.5).Within(0.0000001));

        secondInputRuntime.Dispose();
    }

    [Test]
    [Category("Remote")]
    public void Remote_ChangingDevicesWhileRemoting_WillSendChangesToRemote()
    {
        var secondInputRuntime = new InputTestRuntime();
        var secondInputManager = new InputManager();
        secondInputManager.InstallRuntime(secondInputRuntime);
        secondInputManager.InitializeData();

        var local = new InputRemoting(InputSystem.s_Manager);
        var remote = new InputRemoting(secondInputManager);

        local.Subscribe(remote);
        remote.Subscribe(local);

        local.StartSending();

        // Add device.
        var localGamepad = InputSystem.AddDevice("Gamepad");
        secondInputManager.Update();

        Assert.That(secondInputManager.devices, Has.Count.EqualTo(1));
        var remoteGamepad = secondInputManager.devices[0];
        Assert.That(remoteGamepad, Is.TypeOf<Gamepad>());
        Assert.That(remoteGamepad.remote, Is.True);
        Assert.That(remoteGamepad.layout, Contains.Substring("Gamepad"));

        // Change usage.
        InputSystem.SetUsage(localGamepad, CommonUsages.LeftHand);
        secondInputManager.Update();
        Assert.That(remoteGamepad.usages, Has.Exactly(1).EqualTo(CommonUsages.LeftHand));

        // Bind and disconnect are events so no need to test those.

        // Remove device.
        InputSystem.RemoveDevice(localGamepad);
        secondInputManager.Update();
        Assert.That(secondInputManager.devices, Has.Count.Zero);

        secondInputRuntime.Dispose();
    }

    [Test]
    [Category("Remote")]
    public void Remote_ChangingLayoutsWhileRemoting_WillSendChangesToRemote()
    {
        var secondInputSystem = new InputManager();
        secondInputSystem.InitializeData();

        var local = new InputRemoting(InputSystem.s_Manager);
        var remote = new InputRemoting(secondInputSystem);

        local.Subscribe(remote);
        remote.Subscribe(local);

        local.StartSending();

        const string jsonV1 = @"
            {
                ""name"" : ""MyLayout"",
                ""extend"" : ""Gamepad""
            }
        ";

        // Add layout.
        InputSystem.RegisterControlLayout(jsonV1);

        var layout = secondInputSystem.TryLoadControlLayout(new InternedString("remote0::MyLayout"));
        Assert.That(layout, Is.Not.Null);
        Assert.That(layout.extendsLayout, Is.EqualTo("remote0::Gamepad"));

        const string jsonV2 = @"
            {
                ""name"" : ""MyLayout"",
                ""extend"" : ""Keyboard""
            }
        ";

        // Change layout.
        InputSystem.RegisterControlLayout(jsonV2);

        layout = secondInputSystem.TryLoadControlLayout(new InternedString("remote0::MyLayout"));
        Assert.That(layout.extendsLayout, Is.EqualTo("remote0::Keyboard"));

        // Remove layout.
        InputSystem.RemoveControlLayout("MyLayout");

        Assert.That(secondInputSystem.TryLoadControlLayout(new InternedString("remote0::MyLayout")), Is.Null);
    }

    // If we have more than two players connected, for example, and we add a layout from player A
    // to the system, we don't want to send the layout to player B in turn. I.e. all data mirrored
    // from remotes should stay local.
    [Test]
    [Category("Remote")]
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

    public class RemoteTestObserver : IObserver<InputRemoting.Message>
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
}
