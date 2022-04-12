using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

partial class CoreTests
{
    [UnityTest]
    [Category("API")]
    public IEnumerator API_CanReadPenInputThroughPenAPI()
    {
        Assert.That(Input.penEventCount, Is.Zero);
        Assert.That(() => Input.GetPenEvent(0), Throws.InstanceOf<ArgumentOutOfRangeException>());
        Assert.That(Input.GetLastPenContactEvent().position, Is.EqualTo(default(Vector2)));
        Assert.That(Input.GetLastPenContactEvent().tilt, Is.EqualTo(default(Vector2)));
        Assert.That(Input.GetLastPenContactEvent().penStatus, Is.EqualTo(default(PenStatus)));
        Assert.That(Input.GetLastPenContactEvent().twist, Is.EqualTo(default(float)));
        Assert.That(Input.GetLastPenContactEvent().pressure, Is.EqualTo(default(float)));
        Assert.That(Input.GetLastPenContactEvent().contactType, Is.EqualTo(default(PenEventType)));
        Assert.That(Input.GetLastPenContactEvent().deltaPos, Is.EqualTo(default(Vector2)));

        var pen1 = InputSystem.AddDevice<Pen>();

        Assert.That(Input.penEventCount, Is.Zero);
        Assert.That(() => Input.GetPenEvent(0), Throws.InstanceOf<ArgumentOutOfRangeException>());
        Assert.That(Input.GetLastPenContactEvent().position, Is.EqualTo(default(Vector2)));
        Assert.That(Input.GetLastPenContactEvent().tilt, Is.EqualTo(default(Vector2)));
        Assert.That(Input.GetLastPenContactEvent().penStatus, Is.EqualTo(default(PenStatus)));
        Assert.That(Input.GetLastPenContactEvent().twist, Is.EqualTo(default(float)));
        Assert.That(Input.GetLastPenContactEvent().pressure, Is.EqualTo(default(float)));
        Assert.That(Input.GetLastPenContactEvent().contactType, Is.EqualTo(default(PenEventType)));
        Assert.That(Input.GetLastPenContactEvent().deltaPos, Is.EqualTo(default(Vector2)));

        InputSystem.QueueStateEvent(pen1, new PenState
        {
            position = new Vector2(123, 234),
            delta = new Vector2(0.234f, 0.345f),
            tilt = new Vector2(0.345f, 0.456f),
            pressure = 0.567f,
            twist = 0.678f,
        }.WithButton(PenButton.Tip));
        yield return null;

        Assert.That(Input.penEventCount, Is.EqualTo(1));
        Assert.That(Input.GetPenEvent(0).position, Is.EqualTo(new Vector2(123f, 234f)));
        Assert.That(Input.GetPenEvent(0).deltaPos, Is.EqualTo(new Vector2(0.234f, 0.345f)));
        Assert.That(Input.GetPenEvent(0).tilt, Is.EqualTo(new Vector2(0.345f, 0.456f)));
        Assert.That(Input.GetPenEvent(0).pressure, Is.EqualTo(0.567f));
        Assert.That(Input.GetPenEvent(0).twist, Is.EqualTo(0.678f));
        Assert.That(Input.GetPenEvent(0).contactType, Is.EqualTo(PenEventType.NoContact)); // Weird, but that's how it works in the current native impl
        Assert.That(Input.GetPenEvent(0).penStatus, Is.EqualTo(PenStatus.Contact));
        Assert.That(() => Input.GetPenEvent(1), Throws.InstanceOf<ArgumentOutOfRangeException>());
        Assert.That(Input.GetLastPenContactEvent().position, Is.EqualTo(new Vector2(123f, 234f)));
        Assert.That(Input.GetLastPenContactEvent().deltaPos, Is.EqualTo(new Vector2(0.234f, 0.345f)));
        Assert.That(Input.GetLastPenContactEvent().tilt, Is.EqualTo(new Vector2(0.345f, 0.456f)));
        Assert.That(Input.GetLastPenContactEvent().pressure, Is.EqualTo(0.567f));
        Assert.That(Input.GetLastPenContactEvent().twist, Is.EqualTo(0.678f));
        Assert.That(Input.GetLastPenContactEvent().contactType, Is.EqualTo(PenEventType.PenDown));
        Assert.That(Input.GetLastPenContactEvent().penStatus, Is.EqualTo(PenStatus.Contact));

        yield return null;

        Assert.That(Input.penEventCount, Is.Zero);
        Assert.That(() => Input.GetPenEvent(0), Throws.InstanceOf<ArgumentOutOfRangeException>());
        Assert.That(Input.GetLastPenContactEvent().position, Is.EqualTo(new Vector2(123f, 234f)));
        Assert.That(Input.GetLastPenContactEvent().deltaPos, Is.EqualTo(new Vector2(0.234f, 0.345f)));
        Assert.That(Input.GetLastPenContactEvent().tilt, Is.EqualTo(new Vector2(0.345f, 0.456f)));
        Assert.That(Input.GetLastPenContactEvent().pressure, Is.EqualTo(0.567f));
        Assert.That(Input.GetLastPenContactEvent().twist, Is.EqualTo(0.678f));
        Assert.That(Input.GetLastPenContactEvent().contactType, Is.EqualTo(PenEventType.PenDown));
        Assert.That(Input.GetLastPenContactEvent().penStatus, Is.EqualTo(PenStatus.Contact));

        InputSystem.QueueStateEvent(pen1, new PenState
        {
            position = new Vector2(234f, 345f),
            delta = new Vector2(0.345f, 0.456f),
            tilt = new Vector2(0.456f, 0.567f),
            pressure = 0.678f,
            twist = 0.789f,
        }.WithButton(PenButton.Tip));
        InputSystem.QueueStateEvent(pen1, new PenState
        {
            position = new Vector2(111f, 222f),
            delta = new Vector2(0.333f, 0.444f),
            tilt = new Vector2(0.555f, 0.666f),
            pressure = 0.777f,
            twist = 0.888f,
        }.WithButton(PenButton.Tip));
        yield return null;

        Assert.That(Input.penEventCount, Is.EqualTo(1));
        Assert.That(Input.GetPenEvent(0).position, Is.EqualTo(new Vector2(123f, 234f)));
        Assert.That(Input.GetPenEvent(0).deltaPos, Is.EqualTo(new Vector2(0.234f, 0.345f)));
        Assert.That(Input.GetPenEvent(0).tilt, Is.EqualTo(new Vector2(0.345f, 0.456f)));
        Assert.That(Input.GetPenEvent(0).pressure, Is.EqualTo(0.567f));
        Assert.That(Input.GetPenEvent(0).twist, Is.EqualTo(0.678f));
        Assert.That(Input.GetPenEvent(0).contactType, Is.EqualTo(PenEventType.NoContact)); // Weird, but that's how it works in the current native impl
        Assert.That(Input.GetPenEvent(0).penStatus, Is.EqualTo(PenStatus.Contact));
        Assert.That(() => Input.GetPenEvent(1), Throws.InstanceOf<ArgumentOutOfRangeException>());
        Assert.That(Input.GetLastPenContactEvent().position, Is.EqualTo(new Vector2(123f, 234f)));
        Assert.That(Input.GetLastPenContactEvent().deltaPos, Is.EqualTo(new Vector2(0.234f, 0.345f)));
        Assert.That(Input.GetLastPenContactEvent().tilt, Is.EqualTo(new Vector2(0.345f, 0.456f)));
        Assert.That(Input.GetLastPenContactEvent().pressure, Is.EqualTo(0.567f));
        Assert.That(Input.GetLastPenContactEvent().twist, Is.EqualTo(0.678f));
        Assert.That(Input.GetLastPenContactEvent().contactType, Is.EqualTo(PenEventType.PenDown));
        Assert.That(Input.GetLastPenContactEvent().penStatus, Is.EqualTo(PenStatus.Contact));

        Assert.Fail();

        var pen2 = InputSystem.AddDevice<Pen>();
    }
}
