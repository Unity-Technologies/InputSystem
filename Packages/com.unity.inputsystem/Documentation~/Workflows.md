
# Input System Workflows

There are multiple ways to use the Input System, and the workflow that’s right for you depends on how quickly you want to get up and running, how flexible you want your input code to be, how you prefer to code, and how you prefer to set things up in the Unity Editor.

To understand the different workflows so that you can choose between them, it’s important to first understand the [terms and concepts](Concepts) used to describe them.

There are multiple ways to work with the Input System, each offering different levels of flexibility and abstraction. These can be broadly divided into four main workflows, each of which adds a layer of abstraction, and therefore flexibility, to the previous.

|   |   |
|---|---|
|**Directly Reading Device States**<br/><br/>Your script explicitly refers to device controls and reads the values directly.<br/><br/>Can be the fastest way to set up input for one device, but it is the least flexible workflow. [Read more](./Workflow-Direct) |![image alt text](Images/Workflow-Direct.svg)|
|**Using Embedded Actions**<br/><br/>Your script uses the InputAction class directly. The actions display in your script’s inspector, and allow you to configure them in the editor. [Read more](./Workflow-Embedded)|![image alt text](Images/Workflow-Embedded.svg)|
|**Using an Actions Asset**<br/><br/>Your script does not define actions directly. Instead your script references an Input Actions asset which defines your actions. The Input Actions window provides a UI to define, configure, and organize all your Actions into useful groupings. [Read more](./Workflow-ActionsAsset)|![image alt text](Images/Workflow-ActionsAsset.svg)|
|**Using an Actions Asset and a  PlayerInput component**<br/><br/>In addition to using an Actions Asset, the PlayerInput component provides a UI in the inspector to connect actions to event handlers in your script, removing the need for any intermediary code between the Input System and your Action Methods. [Read more](./Workflow-PlayerInput)|![image alt text](Images/Workflow-PlayerInput.svg)|

