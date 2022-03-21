
[assembly: InputInlineDeviceDatabase(@"

# This is built-in device database of InputSystem

# ---------------------------------------------------------------------------------------------------------------------
# Control Type definitions
# ---------------------------------------------------------------------------------------------------------------------

controlTypes:
- guid: 8f04fea6-2574-43ac-aa63-8610a1f84b05
  name:                   Button
  displayName:            Button
  nativeVisible:          true
  sampleTypeName:         InputButtonControlSample
  sampleTypeNameAlias:    bool
  stateTypeName:          InputButtonControlState
  ingressFunctionName:    InputButtonControlIngress
  frameBeginFunctionName: InputButtonControlFrameBegin
  defaultRecordingMode:   AllMerged
  virtualControls:
  - {guid: 95e1c019-a12f-417a-8ba2-94d545163420, virtualControlRelativeIndex: 1, name: As, type: AxisOneWay}

- guid: c3b1ba47-72ad-4d8d-b4a8-d25682ca23a1
  name:                   AxisOneWay
  displayName:            Axis One Way [0,1]
  nativeVisible:          true
  sampleTypeName:         InputAxisOneWayControlSample
  sampleTypeNameAlias:    float
  stateTypeName:          InputAxisOneWayControlState
  ingressFunctionName:    InputAxisOneWayControlIngress
  frameBeginFunctionName: InputAxisOneWayFrameBegin
  virtualControls:
  - {guid: b4741c7b-12d4-4a25-ad53-0846b79fd79f, virtualControlRelativeIndex: 1, name: As, type: Button}

- guid: 65901491-ebe2-4d45-82e9-90e761569b5c
  name:                   AxisTwoWay
  displayName:            Axis Two Way [-1,1]
  nativeVisible:          true
  sampleTypeName:         InputAxisTwoWayControlSample
  sampleTypeNameAlias:    float
  stateTypeName:          InputAxisTwoWayControlState
  ingressFunctionName:    InputAxisTwoWayControlIngress
  frameBeginFunctionName: InputAxisTwoWayFrameBegin
  virtualControls:
  - {guid: b446cd7f-5bb0-42d5-a05f-2b243c85f540, virtualControlRelativeIndex: 1, name: Positive, type: AxisOneWay}
  - {guid: 55925fd1-5cd9-4704-aeed-bf6420d932f9, virtualControlRelativeIndex: 2, name: Negative, type: AxisOneWay}
  - {guid: d8c9e8d6-9fca-4177-a288-29d4eefd893d, virtualControlRelativeIndex: 3, name: Positive, type: Button}
  - {guid: 54273787-6c9e-467b-828e-526c75809e3e, virtualControlRelativeIndex: 4, name: Negative, type: Button}

- guid: 9b1a0d0d-5c6f-461a-81f2-b65936d75fa8
  name:                   DeltaAxisTwoWay
  displayName:            Delta Axis Two Way [-1,1] per actuation
  nativeVisible:          true
  sampleTypeName:         InputDeltaAxisTwoWayControlSample
  sampleTypeNameAlias:    float
  stateTypeName:          InputDeltaAxisTwoWayControlState
  ingressFunctionName:    InputDeltaAxisTwoWayControlIngress
  frameBeginFunctionName: InputDeltaAxisTwoWayFrameBegin
  virtualControls:
  - {guid: d1d77302-7730-41a4-ab81-df9fab74d13e, virtualControlRelativeIndex: 1, name: Positive, type: Button}
  - {guid: 3090be15-f02a-445d-bd09-c2c96d69e6de, virtualControlRelativeIndex: 2, name: Negative, type: Button}

- guid: 28c92f87-d0ea-419b-aa2d-a5264085ed32
  name:                   Stick
  displayName:            Stick 2D [-1,1]
  nativeVisible:          true
  sampleTypeName:         InputStickControlSample
  stateTypeName:          InputStickControlState
  ingressFunctionName:    InputStickControlIngress
  frameBeginFunctionName: InputStickFrameBegin
  virtualControls:
  - {guid: 4db57d98-770a-4a4b-9af7-5924aa548bd6, virtualControlRelativeIndex:  1, name: Vertical,   type: AxisTwoWay}
  - {guid: a110d8b2-8f94-4883-9ac4-9104e8532630, virtualControlRelativeIndex:  2, name: Horizontal, type: AxisTwoWay}
  - {guid: 6ea9f8a4-b306-458b-a83c-3c6623b90c52, virtualControlRelativeIndex:  3, name: Left,       type: AxisOneWay}
  - {guid: 74bbd17c-82c6-46af-a596-4e10a265a3f1, virtualControlRelativeIndex:  4, name: Up,         type: AxisOneWay}
  - {guid: dadbccb6-d898-4eb7-a571-c8c82602fb96, virtualControlRelativeIndex:  5, name: Right,      type: AxisOneWay}
  - {guid: 98d38ae3-9642-4977-b371-d0f6575f4598, virtualControlRelativeIndex:  6, name: Down,       type: AxisOneWay}
  - {guid: 76ab8b08-0b08-4507-a335-90d651b9b6ce, virtualControlRelativeIndex:  7, name: Left,       type: Button}
  - {guid: a0f744d7-ee02-46c5-97fa-717d0afdd8e7, virtualControlRelativeIndex:  8, name: Up,         type: Button}
  - {guid: 0fa7a8d6-b30b-4b66-bc43-8b40efcdaded, virtualControlRelativeIndex:  9, name: Right,      type: Button}
  - {guid: a6241bcc-e21e-4e6c-a27f-016c469fe3bc, virtualControlRelativeIndex: 10, name: Down,       type: Button}

- guid: ceef0569-675e-4da5-8608-f9bd070acfe3
  name:                   DeltaVector2D
  displayName:            Delta Vector 2D [-1,1] per actuation
  nativeVisible:          true
  sampleTypeName:         InputDeltaVector2DControlSample
  stateTypeName:          InputDeltaVector2DControlState
  ingressFunctionName:    InputDeltaVector2DControlIngress
  frameBeginFunctionName: InputDeltaVector2DFrameBegin
  virtualControls:
  - {guid: 7bb65089-9163-4d14-af8f-628e76258ddc, virtualControlRelativeIndex: 1, name: Vertical,   type: DeltaAxisTwoWay}
  - {guid: cf820d92-75be-47e3-95b0-021a1f821fdb, virtualControlRelativeIndex: 2, name: Horizontal, type: DeltaAxisTwoWay}
  - {guid: a9b1787a-7a12-410a-b55c-c949f600cd4f, virtualControlRelativeIndex: 3, name: Left,       type: Button}
  - {guid: 83499b44-3d05-4cc4-a2a4-479082a6d3da, virtualControlRelativeIndex: 4, name: Up,         type: Button}
  - {guid: f7c3673d-7ad4-4af7-9f13-9213e74c2fd0, virtualControlRelativeIndex: 5, name: Right,      type: Button}
  - {guid: 79bcc020-c6cf-4953-9e93-afd26f146ea3, virtualControlRelativeIndex: 6, name: Down,       type: Button}

- guid: 083946bc-0936-4032-9d0e-a9d0af671ce6
  name:                   Position2D
  displayName:            Absolute 2D Vector normalized to [0,1] with surface index
  nativeVisible:          true
  sampleTypeName:         uint8_t # TODO fix me
  stateTypeName:          uint8_t # TODO fix me
  ingressFunctionName:    _TodoIngress
  frameBeginFunctionName: _TodoFrameBegin

# ---------------------------------------------------------------------------------------------------------------------
# Device traits definitions
# ---------------------------------------------------------------------------------------------------------------------

deviceTraits:

# ---------------------------------------------------------
# Explicitly Pollable Device
# ---------------------------------------------------------

- guid: e19258e7-3729-4ba3-823e-3b3d33b15a09
  name: ExplicitlyPollableDevice
  displayName: Explicitly Pollable Device
  nativeVisible: true
  methods:
  - name: Poll
    implementation: ManagedOrNative

# ---------------------------------------------------------
# Keyboard trait
# ---------------------------------------------------------

- guid: 2df15f1b-02e7-438e-9b42-4ef40e15a4cf
  name: Keyboard
  displayName: Keyboard
  nativeVisible: true
  controls:
  - {guid: e46aaeca-d88f-4768-a1f7-a59ade21a31e, name: Escape,         type: Button}
  - {guid: 76e77ef5-560b-44d6-9080-ef357460ba11, name: Space,          type: Button}
  - {guid: dbc1d2ec-d4b3-4a0e-bd5f-ee6aa33e1ccb, name: Enter,          type: Button}
  - {guid: 4d4500a0-b628-4a99-8057-a97e72dc74c3, name: Tab,            type: Button}
  - {guid: 3e9e565f-15d1-4ca3-ab89-718c4c53f27e, name: Backquote,      type: Button}
  - {guid: f2de5b4a-9c27-42fd-8127-db6998883d4d, name: Quote,          type: Button}
  - {guid: ade26773-f1ae-428c-88ca-1f29c6fbfc20, name: Semicolon,      type: Button}
  - {guid: 6892c71c-c1b5-4e53-9a1d-1a85a2c83879, name: Comma,          type: Button}
  - {guid: ebd66b08-0b42-4ae4-9546-b5614042dcca, name: Period,         type: Button}
  - {guid: 6738108e-822c-45d9-87a4-05918c9238e4, name: Slash,          type: Button}
  - {guid: a6ee216f-38e1-4c44-b12e-e5494649258f, name: Backslash,      type: Button}
  - {guid: 136f28f8-aadb-4180-b7ea-cda62417b63b, name: LeftBracket,    type: Button}
  - {guid: 06d56359-9cbe-4ab9-a6d4-e668fddf49ba, name: RightBracket,   type: Button}
  - {guid: 7c0494a7-d488-4849-b843-ca441952ed35, name: Minus,          type: Button}
  - {guid: ac4f344f-ef7b-4f52-bb52-d4b358a2b500, name: Equals,         type: Button}
  - {guid: 780014b9-22ac-43b6-9b7c-4aafa5765862, name: UpArrow,        type: Button}
  - {guid: bf4a0f67-cf56-4870-8daa-94e8bd8de366, name: DownArrow,      type: Button}
  - {guid: 4767225e-90de-417a-b9a8-2483207ceb2d, name: LeftArrow,      type: Button}
  - {guid: 5cbf7c37-89fc-405a-ac85-7bddd1062e43, name: RightArrow,     type: Button}
  - {guid: 107e6187-c8e8-410e-a0c4-0c8f260aa2d6, name: A,              type: Button}
  - {guid: 76859f9f-b692-45b5-aa4c-45ec1632aff0, name: B,              type: Button}
  - {guid: 4ae7c0c7-b113-4bd6-86ea-7ef0bea6d262, name: C,              type: Button}
  - {guid: 4535a3cd-71cc-4d55-92bc-706cdd0713d6, name: D,              type: Button}
  - {guid: 398a6f90-41e5-49b9-87aa-3a9e4ec09920, name: E,              type: Button}
  - {guid: 6b277edf-0d98-47aa-affa-0cd0ef4d2cf3, name: F,              type: Button}
  - {guid: eaf85285-3ef9-48ce-9b69-50b71302a02d, name: G,              type: Button}
  - {guid: ac5b066e-5f18-444f-9f3f-2d587d27c7eb, name: H,              type: Button}
  - {guid: 5f9f3b53-634e-4e9c-a49e-1d350c6529e3, name: I,              type: Button}
  - {guid: 33d340dc-dbb2-447d-8054-f4564a2f1374, name: J,              type: Button}
  - {guid: 23a37e38-c426-4c84-82e6-b9188080c701, name: K,              type: Button}
  - {guid: a7ca2423-3b78-4206-8b53-b2ebec22098b, name: L,              type: Button}
  - {guid: a2310970-6e61-4f53-bcae-284be3057a07, name: M,              type: Button}
  - {guid: 344a54aa-9f31-4385-be85-360d24012f97, name: N,              type: Button}
  - {guid: 6ea2ca67-de8f-4a67-a428-6d636e6f8b52, name: O,              type: Button}
  - {guid: 0c5fb7be-5d45-4148-b6b3-218416739585, name: P,              type: Button}
  - {guid: 697d80d4-c58c-4b49-93b0-3400f41a1066, name: Q,              type: Button}
  - {guid: 8fda505f-36f4-4c3f-b72b-a402ba2e51a7, name: R,              type: Button}
  - {guid: 510ceb86-03b1-470a-a47c-f1137615080d, name: S,              type: Button}
  - {guid: 93aed622-b028-491d-bc83-62a513da4bcc, name: T,              type: Button}
  - {guid: 0b67dbdd-db54-4f47-a870-66b745d54201, name: U,              type: Button}
  - {guid: 93d7805d-dad6-4c3d-ba0c-c74c4683ded4, name: V,              type: Button}
  - {guid: b9dbafff-fe24-4fa5-8fed-2048d8b74e0e, name: W,              type: Button}
  - {guid: b2fa9681-efe3-497d-8027-d569f77a59a5, name: X,              type: Button}
  - {guid: 8a41fb7f-6d93-4b50-bc22-6e4181751eb3, name: Y,              type: Button}
  - {guid: c438a131-54f4-4c78-a633-edd709f57b4c, name: Z,              type: Button}
  - {guid: e49b2009-3d58-459b-abd1-406631c8809c, name: Digit1,         type: Button}
  - {guid: fc464ad0-1c7b-431f-9b11-8af3e9f5844c, name: Digit2,         type: Button}
  - {guid: 97370b2b-25e3-4542-a11b-3a86111b975a, name: Digit3,         type: Button}
  - {guid: 71f845fe-193d-432f-a166-7b1da7e5d7e9, name: Digit4,         type: Button}
  - {guid: 1654edff-3178-460a-acee-1ca2b2cca8cf, name: Digit5,         type: Button}
  - {guid: 253d2bc5-6ba1-4d14-8b94-0b34ba2072ca, name: Digit6,         type: Button}
  - {guid: 48d87497-500e-4298-b046-6e3382ae60a1, name: Digit7,         type: Button}
  - {guid: a9e310b1-5c70-4aca-b7e1-6c2a8c648329, name: Digit8,         type: Button}
  - {guid: bdf1c681-f8eb-49ba-9d11-5a494eca9010, name: Digit9,         type: Button}
  - {guid: e0d526c1-6e5d-4d5a-a177-cf71a47426cd, name: Digit0,         type: Button}
  - {guid: d3b30b39-a1cd-4863-90cd-668d1cfe0843, name: LeftShift,      type: Button}
  - {guid: a28a4f92-3e42-4e9c-bf7c-0ea9f6e7904b, name: RightShift,     type: Button}
  - {guid: 2c3bd5bb-c597-4d35-8494-2b4537a5b924, name: Shift,          type: Button}
  - {guid: 801af042-6a0c-4a03-bf14-a5c4b932052f, name: LeftAlt,        type: Button}
  - {guid: 3f9aea80-7bd7-4548-b081-dcc2aedb4d5e, name: RightAlt,       type: Button}
  - {guid: 5f5b3327-c806-4221-b4cf-ca6f6f676fd0, name: Alt,            type: Button}
  - {guid: 7acace1e-5ac4-44b8-9266-37fb8ecba0e7, name: LeftCtrl,       type: Button}
  - {guid: e79971f0-70b1-4d77-a973-2dc25de6c929, name: RightCtrl,      type: Button}
  - {guid: 1c91ed24-0cb9-468a-a61c-3d6af2b08124, name: Ctrl,           type: Button}
  - {guid: 2df985c7-329a-47d6-ab40-bdf7faef38cf, name: LeftMeta,       type: Button}
  - {guid: b85ca017-2d15-4b5d-9145-a0083ee3d084, name: RightMeta,      type: Button}
  - {guid: 07a7ff32-8d8a-4470-9724-6df43ce3ed0e, name: ContextMenu,    type: Button}
  - {guid: 9c29a928-8cda-47f8-909b-b480f575237a, name: Backspace,      type: Button}
  - {guid: 6d42dbfa-2a0f-41e7-84f3-5546a475151d, name: PageDown,       type: Button}
  - {guid: b84ee25a-ff4f-4d0b-a657-9bc6eef4be62, name: PageUp,         type: Button}
  - {guid: a206ce6a-caea-4d50-9706-48ab993d030d, name: Home,           type: Button}
  - {guid: 387e8b85-b0b3-41e4-8e26-65d590b5e25a, name: End,            type: Button}
  - {guid: 68cce7f8-43a7-40b1-b09b-0b2cbaddd003, name: Insert,         type: Button}
  - {guid: 9f76e3ea-d6c4-4824-a4b6-78551de4950f, name: Delete,         type: Button}
  - {guid: 51d4f372-3c5a-4f88-ba7c-7162df740856, name: CapsLock,       type: Button}
  - {guid: 369cc009-20be-4379-851a-9facacffec17, name: NumLock,        type: Button}
  - {guid: 97e9c1b4-d741-4211-a01d-e1d6809db7df, name: PrintScreen,    type: Button}
  - {guid: bd57a8c4-abf2-4385-8582-e22a52b20af0, name: ScrollLock,     type: Button}
  - {guid: 73e70608-ea25-401d-9643-575864528b4f, name: Pause,          type: Button}
  - {guid: 2101a53b-9bc5-4153-8e0d-7f6d225b215a, name: NumpadEnter,    type: Button}
  - {guid: cd347e31-0889-4955-919a-2e5fef648c2a, name: NumpadDivide,   type: Button}
  - {guid: a998e36f-c635-48e4-bd13-136b1a000530, name: NumpadMultiply, type: Button}
  - {guid: 976f56bb-200c-4fb7-b321-198b01c15341, name: NumpadPlus,     type: Button}
  - {guid: 26ca0c51-365a-4c3c-be65-6c934a5c5c00, name: NumpadMinus,    type: Button}
  - {guid: b7c6b441-8f5f-4897-a824-9e67e9eb8b2b, name: NumpadPeriod,   type: Button}
  - {guid: e0267bbe-fa39-4a50-b306-9e64ce3f62ff, name: NumpadEquals,   type: Button}
  - {guid: 039fa931-f52a-49a1-83ce-a4f55b098f47, name: Numpad1,        type: Button}
  - {guid: 91b69c68-b29d-4ae3-970d-a2196717beeb, name: Numpad2,        type: Button}
  - {guid: ce6c63db-9cff-457e-9c62-4606c72d6ea4, name: Numpad3,        type: Button}
  - {guid: 0d27a2d7-e1f3-4e2d-9b39-236cee1058d3, name: Numpad4,        type: Button}
  - {guid: 8f0defbd-9f05-4ea0-a22c-3deb7619415e, name: Numpad5,        type: Button}
  - {guid: f3aed98c-8a82-4394-8297-dfa176b80c13, name: Numpad6,        type: Button}
  - {guid: 72f0cfbf-03a1-4b1e-bda9-e487a4e0de4d, name: Numpad7,        type: Button}
  - {guid: c29b5521-af15-42fc-a89d-4ca1ab68113d, name: Numpad8,        type: Button}
  - {guid: c0e4c6ef-3dc7-4c0c-a99a-ed3817df3503, name: Numpad9,        type: Button}
  - {guid: ffa73583-0d76-4ae4-9dc2-1455d9f56f37, name: Numpad0,        type: Button}
  - {guid: 0b548306-eacb-450e-a0a7-b125201369cd, name: F1,             type: Button}
  - {guid: a59c6f4d-82f4-4b5a-a339-96e71f07e98e, name: F2,             type: Button}
  - {guid: 829476e2-5311-480a-a255-1138275719b3, name: F3,             type: Button}
  - {guid: b9af86b6-07cd-43b9-9261-e39eb2f3c765, name: F4,             type: Button}
  - {guid: 2000e589-7d83-49f5-a3c8-fd12a68c45b0, name: F5,             type: Button}
  - {guid: 5de62548-4d78-4007-b663-73ab29cc9fa7, name: F6,             type: Button}
  - {guid: 19dea2ba-3f3d-4242-a321-909666c81c1e, name: F7,             type: Button}
  - {guid: 1aaa1073-ef10-46e7-995f-bce07e96beec, name: F8,             type: Button}
  - {guid: 376765ed-19c6-4bfb-9516-e66e820af1fc, name: F9,             type: Button}
  - {guid: 3932ca2f-5323-4227-8dff-e2c37b37c01a, name: F10,            type: Button}
  - {guid: dae50a43-91ce-41fd-9cca-3f252129bc38, name: F11,            type: Button}
  - {guid: d686e00a-1744-42d4-ae2d-0f0f9c560106, name: F12,            type: Button}
  - {guid: 34c365f9-7b2f-4e3f-b2ea-bff3f37c49ef, name: OEM1,           type: Button}
  - {guid: 1e744838-b9db-42d2-b291-3793b8f2c80f, name: OEM2,           type: Button}
  - {guid: cbd83108-e849-41e9-bfd7-e59198baf297, name: OEM3,           type: Button}
  - {guid: 3f4b3526-f452-4e77-ba27-b6bf962b3f50, name: OEM4,           type: Button}
  - {guid: ea2ad267-1f2f-45ce-9860-831120ecfe92, name: OEM5,           type: Button}


# ---------------------------------------------------------
# Pointer trait
# ---------------------------------------------------------

# TODO current pointer implementation in the system mixes some touch/pen values, while something like system cursor wouldn't have them

- guid: 71e744dd-8f39-4ca0-9e97-37448f4747de
  name: Pointer
  displayName: Pointer
  nativeVisible: true
  controls:
  - {guid: 3191bb63-ee1d-4acc-8b0a-fc44d94d0dc4, name: Position, type: Position2D}

# ---------------------------------------------------------
# Mouse trait
# ---------------------------------------------------------

- guid: 30b0bb07-7151-4627-91d1-aeade5a2f584
  name: Mouse
  displayName: Mouse
  nativeVisible: true
  controls:
  - {guid: c34fba4f-cfbf-4b9a-bd23-de27058c98c1, name: Motion,        type: DeltaVector2D}
  - {guid: 4ad982ec-c1df-4238-8fdb-65f9ded49aa0, name: Scroll,        type: DeltaVector2D}
  - {guid: 26d67b22-74bc-472e-813e-a60eb6d2ecb9, name: Left,          type: Button}
  - {guid: 3eb2df2e-23bb-4008-ade7-84ab3e5f6c12, name: Middle,        type: Button}
  - {guid: d34cafef-49fd-4be7-9e7d-0cf0f1aa0a96, name: Right,         type: Button}
  - {guid: 0cf849de-60cc-4235-9431-229bc1e2ec1b, name: Back,          type: Button}
  - {guid: 22826ae4-6d6f-466c-8a34-e202400dc10a, name: Forward,       type: Button}

# ---------------------------------------------------------
# Gamepad trait
# ---------------------------------------------------------

- guid: 9f98ae93-373a-41f2-ac6a-d9f163587a2c
  name: Gamepad
  displayName: Gamepad
  nativeVisible: true
  controls:
  - {guid: f4fc798e-e04d-4219-88b8-16985584ebfd, name: West,          type: Button}
  - {guid: ce67230f-b58c-42e1-8714-12c03480a490, name: North,         type: Button}
  - {guid: f2cc1334-2091-4da8-9e7d-da7ceafc1f74, name: East,          type: Button}
  - {guid: 55a4e6fc-ef1d-4b76-9ae8-8f932690a6c3, name: South,         type: Button}
  - {guid: dbac5f74-fc02-4a3b-ba5f-877fe65b3492, name: Left,          type: Stick}
  - {guid: 8d5b9aa9-3ee1-4bdb-8d1b-c4d7d1098c0f, name: Right,         type: Stick}
  - {guid: 43b55dde-0823-4174-858b-d89325cd939d, name: LeftStick,     type: Button}
  - {guid: 5810d5b1-7729-4f03-8f49-f7393b10f530, name: RightStick,    type: Button}
  - {guid: 2b9111ce-c92f-4cc1-8cfb-30ccaa8b0939, name: DPad,          type: Stick}
  - {guid: 2e439345-b13b-437a-b2f3-82e75b406297, name: LeftShoulder,  type: Button}
  - {guid: 1f621503-5d1d-4558-9e7a-88d07b17c97f, name: RightShoulder, type: Button}
  - {guid: 45637d1d-db53-4272-b629-97c0240e6b39, name: LeftTrigger,   type: AxisOneWay}
  - {guid: 42ca579d-fc21-4b75-8841-3a1bebf6d3e9, name: RightTrigger,  type: AxisOneWay}

# ---------------------------------------------------------
# DualSense trait
# ---------------------------------------------------------

# TODO:
# DualShock3: Select+Start+Playstation, 4 player LED
# DualShock4: Options+Share+Playstation, player LED??? + Touchpad
# DualSense: Options+Share+Playstation+Mic, 5 player LED ??? + Touchpad

- guid: 25677573-5d95-48e5-865c-8b5a1b25d31a
  name: DualSense
  displayName: DualSense
  nativeVisible: true
  controls:
  - {guid: eeaef587-85c4-451b-95a5-15ae76b4c772, name: Options,     type: Button}
  - {guid: faa4ef66-5b6f-416d-b5a1-6e53aeea9670, name: Share,       type: Button}
  - {guid: c73bf2c4-be4e-4660-8db4-8ff1bb11d036, name: Playstation, type: Button}
  - {guid: f81a43b3-9f7b-4c39-ae2d-6513647b497d, name: Mic,         type: Button}
  methods:
  - name: SetLED
    implementation: ManagedOrNative
    args:
    - {name: playerIndex, type: int}
  - name: SetColor
    implementation: ManagedOrNative
    args:
    - {name: r, type: float}
    - {name: g, type: float}
    - {name: b, type: float}
    - {name: a, type: float}

# ---------------------------------------------------------
# Generic controls trait
# ---------------------------------------------------------

- guid: d56f16d5-edbe-4920-bcaf-af124ee4368e
  name: GenericControls
  displayName: Generic Controls
  nativeVisible: true
  controlsAreOptional: true # meaning some or all controls may be abscent
  controls:
  - {guid: 8d8a3ff3-5e63-4f21-ac8e-4ca8719e3bbd, name: Generic0,        type: Button}
  - {guid: dade7d14-921b-4abf-9157-6c404701dc6f, name: Generic1,        type: Button}
  - {guid: c3be02d6-b185-4f76-b568-1e717e6e3f31, name: Generic2,        type: Button}
  - {guid: 3b7d4721-d1ad-43fd-9027-a0dca892dc3f, name: Generic3,        type: Button}
  - {guid: 0ae6e9a5-44c1-4613-ab60-61e904bda703, name: Generic4,        type: Button}
  - {guid: 885cf1ed-4c87-43eb-aa09-249a7989a67a, name: Generic5,        type: Button}
  - {guid: 987ddc6a-114f-42bc-a055-d3d5822aebcd, name: Generic6,        type: Button}
  - {guid: 61df095b-c283-4548-a7b3-978bd64d71da, name: Generic7,        type: Button}
  - {guid: ab508085-43da-43df-8810-2d11c49ad7c8, name: Generic8,        type: Button}
  - {guid: 5b6fccdc-3324-4925-9bab-5614b24228af, name: Generic9,        type: Button}
  - {guid: 1f0f8341-3e5d-4dbe-84ea-7fd398553809, name: Generic10,       type: Button}
  - {guid: e9c33388-ffde-4f3f-a511-23f094b1045a, name: Generic11,       type: Button}
  - {guid: 466bc33b-ee67-485f-aa48-83f284299651, name: Generic12,       type: Button}
  - {guid: aa83c00c-7d67-46c1-b5e1-f5d0ead812f8, name: Generic13,       type: Button}
  - {guid: 1a9e36e1-45a5-4d10-940c-a147ef385f3d, name: Generic14,       type: Button}
  - {guid: 7d38df3d-7adf-4e84-a184-49a52e5359ee, name: Generic15,       type: Button}
  - {guid: f132db85-4683-471a-93de-43989b35c9de, name: Generic0,        type: AxisOneWay}
  - {guid: fe34ed77-28d4-48f0-9d16-3f723863a7e4, name: Generic1,        type: AxisOneWay}
  - {guid: 221022a9-8a19-4795-b910-5b774704d0eb, name: Generic2,        type: AxisOneWay}
  - {guid: fd314b1f-70e2-4956-a248-361a9b77dcd3, name: Generic3,        type: AxisOneWay}
  - {guid: f8956845-181b-493f-bad7-287adbfd3e3f, name: Generic4,        type: AxisOneWay}
  - {guid: dcaafd94-ff62-4933-9d70-4ae96b4c6cca, name: Generic5,        type: AxisOneWay}
  - {guid: 2e0920f8-87f8-4c94-91a7-f057ed3a42e0, name: Generic6,        type: AxisOneWay}
  - {guid: 45ec2af2-5289-4db3-86b2-8e8d2885921b, name: Generic7,        type: AxisOneWay}
  - {guid: 5b4bacd5-e9bd-48f0-8c8e-900d8c6d03ab, name: Generic8,        type: AxisOneWay}
  - {guid: 4c586fb5-794e-4896-855f-747686a1ceb6, name: Generic9,        type: AxisOneWay}
  - {guid: 62783670-da82-4fc7-b7a5-226745609cb8, name: Generic10,       type: AxisOneWay}
  - {guid: d414d88a-3952-4ed0-ba15-157d57015f22, name: Generic11,       type: AxisOneWay}
  - {guid: 382176b2-782a-4876-b241-2cbbdc098a3d, name: Generic12,       type: AxisOneWay}
  - {guid: 5182c358-656f-40bf-a5ae-7d44b1828a6c, name: Generic13,       type: AxisOneWay}
  - {guid: d80d15f4-ad8b-43f2-bfc2-2eb431465ee4, name: Generic14,       type: AxisOneWay}
  - {guid: 28c778dc-f85d-4c10-905b-3b81154a0465, name: Generic15,       type: AxisOneWay}
  - {guid: 4ff12b6d-6d22-4322-97da-bf56ff446f39, name: Generic0,        type: AxisTwoWay}
  - {guid: b5d041d4-fe06-4ef5-9aca-6cfd91312a18, name: Generic1,        type: AxisTwoWay}
  - {guid: c0e21f93-d678-4829-88a4-1e55c920b23e, name: Generic2,        type: AxisTwoWay}
  - {guid: 0092260f-7ccf-4c28-be04-eaff77c4a02b, name: Generic3,        type: AxisTwoWay}
  - {guid: c103c7b0-c7ab-4bfe-adb1-1178e899584f, name: Generic4,        type: AxisTwoWay}
  - {guid: 38a1f5a0-935b-49f7-8db2-86cbe4ef773a, name: Generic5,        type: AxisTwoWay}
  - {guid: 1f4db9de-a28d-4d77-ab4b-0a0f462ac128, name: Generic6,        type: AxisTwoWay}
  - {guid: 0b4c999f-62f8-479f-acd9-eb0cc6214b8e, name: Generic7,        type: AxisTwoWay}
  - {guid: ea93557b-5114-4b9d-aa4c-7eb47d612191, name: Generic8,        type: AxisTwoWay}
  - {guid: ea76d1d2-8069-451f-9d50-81c7780ed511, name: Generic9,        type: AxisTwoWay}
  - {guid: e0d2f7a9-6fed-4a17-a722-ac1342ee275d, name: Generic10,       type: AxisTwoWay}
  - {guid: f2d0c1b5-31b0-48e5-802f-688c01e28d58, name: Generic11,       type: AxisTwoWay}
  - {guid: 2a89ef06-3828-4de3-9815-943e2142c3d3, name: Generic12,       type: AxisTwoWay}
  - {guid: 0f8af853-4349-4634-93bd-414517014eb4, name: Generic13,       type: AxisTwoWay}
  - {guid: 6bdcf7b4-838d-4923-bfb5-bebb90d85ded, name: Generic14,       type: AxisTwoWay}
  - {guid: efd92bc0-a740-414a-acce-c32af25fe9e3, name: Generic15,       type: AxisTwoWay}

# ---------------------------------------------------------------------------------------------------------------------
# Devices definitions
# ---------------------------------------------------------------------------------------------------------------------

# ---------------------------------------------------------
# Keyboard on Windows
# ---------------------------------------------------------

devices:

- guid: 8d37e884-458e-4b1d-805f-95425987e9d1
  name: KeyboardWindows
  displayName: Keyboard (Windows)
  traits:
  - Keyboard

- guid: b642521e-7c4b-45d0-b3b7-6084e786aa22
  name: MouseMacOS
  displayName: Mouse (macOS)
  traits:
  - Pointer
  - Mouse

- guid: ff0896da-9c98-4489-94c3-4b244162c372
  name: WindowsGamingInputGamepad
  displayName: Gamepad (Windows.Gaming.Input)
  traits:
  - ExplicitlyPollableDevice
  - Gamepad

", 0)]