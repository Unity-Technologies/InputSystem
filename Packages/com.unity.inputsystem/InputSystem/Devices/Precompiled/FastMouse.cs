//------------------------------------------------------------------------------
// <auto-generated>
//     This code was auto-generated by com.unity.inputsystem:InputLayoutCodeGenerator
//     version 1.0.0.0
//     from "Mouse" layout
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem
{
    internal partial class FastMouse : Mouse
    {
        public const string metadata = "AutoWindowSpace;Vector2;Button;Axis;Digital;Integer;Mouse;Pointer";
        public FastMouse()
        {
            this.Setup(25, 10, 2)
                .WithName("Mouse")
                .WithDisplayName("Mouse")
                .WithChildren(0, 13)
                .WithLayout(new InternedString("Mouse"))
                .WithFormat(new FourCC(1297044819))
                .WithSizeInBits(344)
                .Finish();

            var kVector2Layout = new InternedString("Vector2");
            var kButtonLayout = new InternedString("Button");
            var kAxisLayout = new InternedString("Axis");
            var kDigitalLayout = new InternedString("Digital");
            var kIntegerLayout = new InternedString("Integer");

            // /Mouse/position
            var ctrlMouseposition = new UnityEngine.InputSystem.Controls.Vector2Control();
            ctrlMouseposition.Setup()
                .At(this, 0)
                .WithParent(this)
                .WithChildren(13, 2)
                .WithName("position")
                .WithDisplayName("Position")
                .WithLayout(kVector2Layout)
                .WithUsages(0, 1)
                .WithStateBlock(new InputStateBlock
                {
                    format = new FourCC(1447379762),
                    byteOffset = 0,
                    bitOffset = 0,
                    sizeInBits = 64
                })
                #if UNITY_EDITOR
                .WithProcessor<InputProcessor<UnityEngine.Vector2>, UnityEngine.Vector2>(new UnityEngine.InputSystem.Processors.EditorWindowSpaceProcessor())
                #endif
                .Finish();

            // /Mouse/delta
            var ctrlMousedelta = new UnityEngine.InputSystem.Controls.Vector2Control();
            ctrlMousedelta.Setup()
                .At(this, 1)
                .WithParent(this)
                .WithChildren(15, 2)
                .WithName("delta")
                .WithDisplayName("Delta")
                .WithLayout(kVector2Layout)
                .WithUsages(1, 1)
                .WithStateBlock(new InputStateBlock
                {
                    format = new FourCC(1447379762),
                    byteOffset = 8,
                    bitOffset = 0,
                    sizeInBits = 64
                })
                .Finish();

            // /Mouse/scroll
            var ctrlMousescroll = new UnityEngine.InputSystem.Controls.Vector2Control();
            ctrlMousescroll.Setup()
                .At(this, 2)
                .WithParent(this)
                .WithChildren(17, 6)
                .WithName("scroll")
                .WithDisplayName("Scroll")
                .WithLayout(kVector2Layout)
                .WithStateBlock(new InputStateBlock
                {
                    format = new FourCC(1447379762),
                    byteOffset = 16,
                    bitOffset = 0,
                    sizeInBits = 64
                })
                .Finish();

            // /Mouse/press
            var ctrlMousepress = new UnityEngine.InputSystem.Controls.ButtonControl();
            ctrlMousepress.Setup()
                .At(this, 3)
                .WithParent(this)
                .WithName("press")
                .WithDisplayName("Press")
                .WithLayout(kButtonLayout)
                .IsSynthetic(true)
                .WithStateBlock(new InputStateBlock
                {
                    format = new FourCC(1112101920),
                    byteOffset = 24,
                    bitOffset = 0,
                    sizeInBits = 1
                })
                .WithMinAndMax(0, 1)
                .Finish();

            // /Mouse/leftButton
            var ctrlMouseleftButton = new UnityEngine.InputSystem.Controls.ButtonControl();
            ctrlMouseleftButton.Setup()
                .At(this, 4)
                .WithParent(this)
                .WithName("leftButton")
                .WithDisplayName("Left Button")
                .WithShortDisplayName("LMB")
                .WithLayout(kButtonLayout)
                .WithUsages(4, 1)
                .WithStateBlock(new InputStateBlock
                {
                    format = new FourCC(1112101920),
                    byteOffset = 24,
                    bitOffset = 0,
                    sizeInBits = 1
                })
                .WithMinAndMax(0, 1)
                .Finish();

            // /Mouse/rightButton
            var ctrlMouserightButton = new UnityEngine.InputSystem.Controls.ButtonControl();
            ctrlMouserightButton.Setup()
                .At(this, 5)
                .WithParent(this)
                .WithName("rightButton")
                .WithDisplayName("Right Button")
                .WithShortDisplayName("RMB")
                .WithLayout(kButtonLayout)
                .WithUsages(5, 1)
                .WithStateBlock(new InputStateBlock
                {
                    format = new FourCC(1112101920),
                    byteOffset = 24,
                    bitOffset = 1,
                    sizeInBits = 1
                })
                .WithMinAndMax(0, 1)
                .Finish();

            // /Mouse/middleButton
            var ctrlMousemiddleButton = new UnityEngine.InputSystem.Controls.ButtonControl();
            ctrlMousemiddleButton.Setup()
                .At(this, 6)
                .WithParent(this)
                .WithName("middleButton")
                .WithDisplayName("Middle Button")
                .WithShortDisplayName("MMB")
                .WithLayout(kButtonLayout)
                .WithStateBlock(new InputStateBlock
                {
                    format = new FourCC(1112101920),
                    byteOffset = 24,
                    bitOffset = 2,
                    sizeInBits = 1
                })
                .WithMinAndMax(0, 1)
                .Finish();

            // /Mouse/forwardButton
            var ctrlMouseforwardButton = new UnityEngine.InputSystem.Controls.ButtonControl();
            ctrlMouseforwardButton.Setup()
                .At(this, 7)
                .WithParent(this)
                .WithName("forwardButton")
                .WithDisplayName("Forward")
                .WithLayout(kButtonLayout)
                .WithUsages(6, 1)
                .WithStateBlock(new InputStateBlock
                {
                    format = new FourCC(1112101920),
                    byteOffset = 24,
                    bitOffset = 3,
                    sizeInBits = 1
                })
                .WithMinAndMax(0, 1)
                .Finish();

            // /Mouse/backButton
            var ctrlMousebackButton = new UnityEngine.InputSystem.Controls.ButtonControl();
            ctrlMousebackButton.Setup()
                .At(this, 8)
                .WithParent(this)
                .WithName("backButton")
                .WithDisplayName("Back")
                .WithLayout(kButtonLayout)
                .WithUsages(7, 1)
                .WithStateBlock(new InputStateBlock
                {
                    format = new FourCC(1112101920),
                    byteOffset = 24,
                    bitOffset = 4,
                    sizeInBits = 1
                })
                .WithMinAndMax(0, 1)
                .Finish();

            // /Mouse/pressure
            var ctrlMousepressure = new UnityEngine.InputSystem.Controls.AxisControl();
            ctrlMousepressure.Setup()
                .At(this, 9)
                .WithParent(this)
                .WithName("pressure")
                .WithDisplayName("Pressure")
                .WithLayout(kAxisLayout)
                .WithUsages(8, 1)
                .WithStateBlock(new InputStateBlock
                {
                    format = new FourCC(1179407392),
                    byteOffset = 30,
                    bitOffset = 0,
                    sizeInBits = 32
                })
                .WithDefaultState(1)
                .Finish();

            // /Mouse/radius
            var ctrlMouseradius = new UnityEngine.InputSystem.Controls.Vector2Control();
            ctrlMouseradius.Setup()
                .At(this, 10)
                .WithParent(this)
                .WithChildren(23, 2)
                .WithName("radius")
                .WithDisplayName("Radius")
                .WithLayout(kVector2Layout)
                .WithUsages(9, 1)
                .WithStateBlock(new InputStateBlock
                {
                    format = new FourCC(1447379762),
                    byteOffset = 34,
                    bitOffset = 0,
                    sizeInBits = 64
                })
                .Finish();

            // /Mouse/pointerId
            var ctrlMousepointerId = new UnityEngine.InputSystem.Controls.IntegerControl();
            ctrlMousepointerId.Setup()
                .At(this, 11)
                .WithParent(this)
                .WithName("pointerId")
                .WithDisplayName("pointerId")
                .WithLayout(kDigitalLayout)
                .WithStateBlock(new InputStateBlock
                {
                    format = new FourCC(1112101920),
                    byteOffset = 42,
                    bitOffset = 0,
                    sizeInBits = 1
                })
                .Finish();

            // /Mouse/clickCount
            var ctrlMouseclickCount = new UnityEngine.InputSystem.Controls.IntegerControl();
            ctrlMouseclickCount.Setup()
                .At(this, 12)
                .WithParent(this)
                .WithName("clickCount")
                .WithDisplayName("Click Count")
                .WithLayout(kIntegerLayout)
                .IsSynthetic(true)
                .WithStateBlock(new InputStateBlock
                {
                    format = new FourCC(1431521364),
                    byteOffset = 28,
                    bitOffset = 0,
                    sizeInBits = 16
                })
                .Finish();

            // /Mouse/position/x
            var ctrlMousepositionx = new UnityEngine.InputSystem.Controls.AxisControl();
            ctrlMousepositionx.Setup()
                .At(this, 13)
                .WithParent(ctrlMouseposition)
                .WithName("x")
                .WithDisplayName("Position X")
                .WithShortDisplayName("Position X")
                .WithLayout(kAxisLayout)
                .WithStateBlock(new InputStateBlock
                {
                    format = new FourCC(1179407392),
                    byteOffset = 0,
                    bitOffset = 0,
                    sizeInBits = 32
                })
                .Finish();

            // /Mouse/position/y
            var ctrlMousepositiony = new UnityEngine.InputSystem.Controls.AxisControl();
            ctrlMousepositiony.Setup()
                .At(this, 14)
                .WithParent(ctrlMouseposition)
                .WithName("y")
                .WithDisplayName("Position Y")
                .WithShortDisplayName("Position Y")
                .WithLayout(kAxisLayout)
                .WithStateBlock(new InputStateBlock
                {
                    format = new FourCC(1179407392),
                    byteOffset = 4,
                    bitOffset = 0,
                    sizeInBits = 32
                })
                .Finish();

            // /Mouse/delta/x
            var ctrlMousedeltax = new UnityEngine.InputSystem.Controls.AxisControl();
            ctrlMousedeltax.Setup()
                .At(this, 15)
                .WithParent(ctrlMousedelta)
                .WithName("x")
                .WithDisplayName("Delta X")
                .WithShortDisplayName("Delta X")
                .WithLayout(kAxisLayout)
                .WithStateBlock(new InputStateBlock
                {
                    format = new FourCC(1179407392),
                    byteOffset = 8,
                    bitOffset = 0,
                    sizeInBits = 32
                })
                .Finish();

            // /Mouse/delta/y
            var ctrlMousedeltay = new UnityEngine.InputSystem.Controls.AxisControl();
            ctrlMousedeltay.Setup()
                .At(this, 16)
                .WithParent(ctrlMousedelta)
                .WithName("y")
                .WithDisplayName("Delta Y")
                .WithShortDisplayName("Delta Y")
                .WithLayout(kAxisLayout)
                .WithStateBlock(new InputStateBlock
                {
                    format = new FourCC(1179407392),
                    byteOffset = 12,
                    bitOffset = 0,
                    sizeInBits = 32
                })
                .Finish();

            // /Mouse/scroll/x
            var ctrlMousescrollx = new UnityEngine.InputSystem.Controls.AxisControl();
            ctrlMousescrollx.Setup()
                .At(this, 17)
                .WithParent(ctrlMousescroll)
                .WithName("x")
                .WithDisplayName("Scroll Left/Right")
                .WithShortDisplayName("Scroll Left/Right")
                .WithLayout(kAxisLayout)
                .WithUsages(2, 1)
                .WithAliases(0, 1)
                .WithStateBlock(new InputStateBlock
                {
                    format = new FourCC(1179407392),
                    byteOffset = 16,
                    bitOffset = 0,
                    sizeInBits = 32
                })
                .Finish();

            // /Mouse/scroll/y
            var ctrlMousescrolly = new UnityEngine.InputSystem.Controls.AxisControl();
            ctrlMousescrolly.Setup()
                .At(this, 18)
                .WithParent(ctrlMousescroll)
                .WithName("y")
                .WithDisplayName("Scroll Up/Down")
                .WithShortDisplayName("Scroll Wheel")
                .WithLayout(kAxisLayout)
                .WithUsages(3, 1)
                .WithAliases(1, 1)
                .WithStateBlock(new InputStateBlock
                {
                    format = new FourCC(1179407392),
                    byteOffset = 20,
                    bitOffset = 0,
                    sizeInBits = 32
                })
                .Finish();

            // /Mouse/scroll/up
            var ctrlMousescrollup = new UnityEngine.InputSystem.Controls.ButtonControl { clamp = UnityEngine.InputSystem.Controls.AxisControl.Clamp.BeforeNormalize, clampMax = 1f };
            ctrlMousescrollup.Setup()
                .At(this, 19)
                .WithParent(ctrlMousescroll)
                .WithName("up")
                .WithDisplayName("Scroll Up")
                .WithShortDisplayName("Scroll Up")
                .WithLayout(kButtonLayout)
                .IsSynthetic(true)
                .WithStateBlock(new InputStateBlock
                {
                    format = new FourCC(1179407392),
                    byteOffset = 20,
                    bitOffset = 0,
                    sizeInBits = 32
                })
                .WithMinAndMax(0, 1)
                .Finish();

            // /Mouse/scroll/down
            var ctrlMousescrolldown = new UnityEngine.InputSystem.Controls.ButtonControl { clamp = UnityEngine.InputSystem.Controls.AxisControl.Clamp.BeforeNormalize, clampMin = -1f, invert = true };
            ctrlMousescrolldown.Setup()
                .At(this, 20)
                .WithParent(ctrlMousescroll)
                .WithName("down")
                .WithDisplayName("Scroll Down")
                .WithShortDisplayName("Scroll Down")
                .WithLayout(kButtonLayout)
                .IsSynthetic(true)
                .WithStateBlock(new InputStateBlock
                {
                    format = new FourCC(1179407392),
                    byteOffset = 20,
                    bitOffset = 0,
                    sizeInBits = 32
                })
                .WithMinAndMax(0, 1)
                .Finish();

            // /Mouse/scroll/left
            var ctrlMousescrollleft = new UnityEngine.InputSystem.Controls.ButtonControl { clamp = UnityEngine.InputSystem.Controls.AxisControl.Clamp.BeforeNormalize, clampMin = -1f, invert = true };
            ctrlMousescrollleft.Setup()
                .At(this, 21)
                .WithParent(ctrlMousescroll)
                .WithName("left")
                .WithDisplayName("Scroll Left")
                .WithShortDisplayName("Scroll Left")
                .WithLayout(kButtonLayout)
                .IsSynthetic(true)
                .WithStateBlock(new InputStateBlock
                {
                    format = new FourCC(1179407392),
                    byteOffset = 16,
                    bitOffset = 0,
                    sizeInBits = 32
                })
                .WithMinAndMax(0, 1)
                .Finish();

            // /Mouse/scroll/right
            var ctrlMousescrollright = new UnityEngine.InputSystem.Controls.ButtonControl { clamp = UnityEngine.InputSystem.Controls.AxisControl.Clamp.BeforeNormalize, clampMax = 1f };
            ctrlMousescrollright.Setup()
                .At(this, 22)
                .WithParent(ctrlMousescroll)
                .WithName("right")
                .WithDisplayName("Scroll Right")
                .WithShortDisplayName("Scroll Right")
                .WithLayout(kButtonLayout)
                .IsSynthetic(true)
                .WithStateBlock(new InputStateBlock
                {
                    format = new FourCC(1179407392),
                    byteOffset = 16,
                    bitOffset = 0,
                    sizeInBits = 32
                })
                .WithMinAndMax(0, 1)
                .Finish();

            // /Mouse/radius/x
            var ctrlMouseradiusx = new UnityEngine.InputSystem.Controls.AxisControl();
            ctrlMouseradiusx.Setup()
                .At(this, 23)
                .WithParent(ctrlMouseradius)
                .WithName("x")
                .WithDisplayName("Radius X")
                .WithShortDisplayName("Radius X")
                .WithLayout(kAxisLayout)
                .WithStateBlock(new InputStateBlock
                {
                    format = new FourCC(1179407392),
                    byteOffset = 34,
                    bitOffset = 0,
                    sizeInBits = 32
                })
                .Finish();

            // /Mouse/radius/y
            var ctrlMouseradiusy = new UnityEngine.InputSystem.Controls.AxisControl();
            ctrlMouseradiusy.Setup()
                .At(this, 24)
                .WithParent(ctrlMouseradius)
                .WithName("y")
                .WithDisplayName("Radius Y")
                .WithShortDisplayName("Radius Y")
                .WithLayout(kAxisLayout)
                .WithStateBlock(new InputStateBlock
                {
                    format = new FourCC(1179407392),
                    byteOffset = 38,
                    bitOffset = 0,
                    sizeInBits = 32
                })
                .Finish();

            // Usages.
            m_UsagesForEachControl[0] = new InternedString("Point");
            m_UsageToControl[0] = ctrlMouseposition;
            m_UsagesForEachControl[1] = new InternedString("Secondary2DMotion");
            m_UsageToControl[1] = ctrlMousedelta;
            m_UsagesForEachControl[2] = new InternedString("ScrollHorizontal");
            m_UsageToControl[2] = ctrlMousescrollx;
            m_UsagesForEachControl[3] = new InternedString("ScrollVertical");
            m_UsageToControl[3] = ctrlMousescrolly;
            m_UsagesForEachControl[4] = new InternedString("PrimaryAction");
            m_UsageToControl[4] = ctrlMouseleftButton;
            m_UsagesForEachControl[5] = new InternedString("SecondaryAction");
            m_UsageToControl[5] = ctrlMouserightButton;
            m_UsagesForEachControl[6] = new InternedString("Forward");
            m_UsageToControl[6] = ctrlMouseforwardButton;
            m_UsagesForEachControl[7] = new InternedString("Back");
            m_UsageToControl[7] = ctrlMousebackButton;
            m_UsagesForEachControl[8] = new InternedString("Pressure");
            m_UsageToControl[8] = ctrlMousepressure;
            m_UsagesForEachControl[9] = new InternedString("Radius");
            m_UsageToControl[9] = ctrlMouseradius;

            // Aliases.
            m_AliasesForEachControl[0] = new InternedString("horizontal");
            m_AliasesForEachControl[1] = new InternedString("vertical");

            // Control getters/arrays.
            this.scroll = ctrlMousescroll;
            this.leftButton = ctrlMouseleftButton;
            this.middleButton = ctrlMousemiddleButton;
            this.rightButton = ctrlMouserightButton;
            this.backButton = ctrlMousebackButton;
            this.forwardButton = ctrlMouseforwardButton;
            this.clickCount = ctrlMouseclickCount;
            this.position = ctrlMouseposition;
            this.delta = ctrlMousedelta;
            this.radius = ctrlMouseradius;
            this.pressure = ctrlMousepressure;
            this.press = ctrlMousepress;
            ctrlMouseposition.x = ctrlMousepositionx;
            ctrlMouseposition.y = ctrlMousepositiony;
            ctrlMousedelta.x = ctrlMousedeltax;
            ctrlMousedelta.y = ctrlMousedeltay;
            ctrlMousescroll.x = ctrlMousescrollx;
            ctrlMousescroll.y = ctrlMousescrolly;
            ctrlMouseradius.x = ctrlMouseradiusx;
            ctrlMouseradius.y = ctrlMouseradiusy;
        }
    }
}
