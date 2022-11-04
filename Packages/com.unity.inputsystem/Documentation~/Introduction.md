# Introduction to the Input System

Input allows the user to control your game or application using a device, touch, or gestures. You can program in-app elements, such as the graphic user interface (GUI) or a user avatar, to respond to user input in different ways.

Unity supports input from many types of devices, including:

- Keyboards and mice
- Joysticks
- Controllers
- Touch screens
- Movement-sensing capabilities of mobile devices, such as accelerometers or gyroscopes
- VR and AR controllers

Unity supports input through two separate systems, one older, and one newer.

The older system, which is built-in to the editor, is called the [Input Manager](https://docs.unity3d.com/Manual/class-InputManager.html). The Input Manager is part of the core Unity platform and is the default, if you do not install the Input System Package.

**This package is the newer, more flexible system**, and is referred to as "The Input System Package", or just **"The Input System"**. To use it, you must [install it into your project using the Package Manager](Installation.md).

During the installation process for the Input System package, the installer offers to automatically deactivate the older built-in system. ([Read more](Installation))