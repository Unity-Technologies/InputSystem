---
uid: input-system-testing
---

# Testing

Write automated input tests that drive the Input System from code without physical devices.

The Input System includes support for automated input tests. Generated input in tests is treated the same as input from platform backends at runtime, so you can verify gameplay and UI logic without depending on connected hardware. Work through the topics below in order: configure your test assemblies, set up a test fixture, then write tests.

| **Topic** | **Description** |
| :--- | :--- |
| **[Set up test assemblies](set-up-test-assemblies.md)** | Configure your project so Input System test code is included in test builds. |
| **[Set up test fixtures](set-up-test-fixtures.md)** | Run each test against an isolated Input System instance using InputTestFixture. |
| **[Write tests](write-tests.md)** | Add devices and simulate input using the InputTestFixture API. |

## Additional resources

- [Actions](actions.md)
- [The Player Input component](player-input-component.md)
- [Layouts](layouts.md)
