#!/bin/sh
echo "----- System ------"
wc `find Packages/com.unity.inputsystem/InputSystem -name "*.cs"`
echo "----- Tests -------"
wc `find Packages/com.unity.inputsystem/Tests -name "*.cs"`

