---
uid: input-system-about-action-assets
---

# About action assets

The Input System stores your configuration of [Input Actions](actions.md) and their associated [bindings](bindings.md), [action maps](create-edit-delete-action-maps.md) and [Control Schemes](control-schemes.md) in an [action asset](action-assets.md) file. These Assets have the `.inputactions` file extension and are stored in a plain JSON format.

## Project-wide action assets

The Input System creates an action asset when you set up the [default project-wide actions](about-project-wide-actions.md), which is the most common and recommended workflow, but you can also [create new empty action assets](./create-empty-action-asset.md) directly in the Project window.

## Action maps

Actions assets allow you to group sets of related actions into [action maps](./create-edit-delete-action-maps.md). For example, in an open-world city game, you might create separate action maps for different situations such as exploring on foot, driving a car, flying an aircraft, or navigating UI interfaces. This means, for most common scenarios, you don't need to use more than one Input action asset.
