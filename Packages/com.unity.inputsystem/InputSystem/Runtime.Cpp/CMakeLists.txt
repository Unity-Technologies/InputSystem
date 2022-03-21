cmake_minimum_required(VERSION 3.15) # because of CMP0091

option(INCLUDE_NATIVE_TESTS "Include Catch2 based native tests in the native library" ON)
option(BUILD_STANDALONE_APP "Build standalone testing fixture" ON)

set(CMAKE_CXX_STANDARD 17)

cmake_policy(SET CMP0068 NEW) # @rpath on macOS
cmake_policy(SET CMP0091 NEW) # CRT runtime selection on MSVC
set(CMAKE_MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>") # don't depend on MSVC CRT dll

project(InputRuntime)

set(R "${CMAKE_CURRENT_SOURCE_DIR}")

# ---------------------------------------------------------------------------------------------------------------------
# ClangSharpPInvokeGenerator setup
# ---------------------------------------------------------------------------------------------------------------------

if (${CMAKE_SYSTEM_NAME} MATCHES "Darwin")
    # ClangSharpPInvokeGenerator 13 on macOS is currently is not in the greatest shape
    # I compiled it from sources targeting llvm from Homebrew:
    # 1) brew install llvm
    # 2) clone ClangSharp locally
    # 3) patch ClangSharp/sources/ClangSharp.PInvokeGenerator/PInvokeGenerator.cs line 75 to 'if (!clangVersion.Contains("13.0.1"))', otherwise it will complain
    # 4) patch ClangSharp/sources/libClangSharp/CMakeLists.txt to have 'target_link_libraries(ClangSharp PRIVATE libclang clang-cpp LLVM)'
    # 5) build libClangSharp as per readme, I've used 'cmake -DPATH_TO_LLVM=/opt/homebrew/opt/llvm -G Ninja ../../..'
    # 6) build ClangSharpPInvokeGenerator as per readme, I've used 'dotnet build -c Release'
    # 7) copy libClangSharp into ClangSharpPInvokeGenerator build folder
    # 8) create ClangSharpPInvokeGenerator.sh with chmod +x and write the following:
    # export DYLD_LIBRARY_PATH=/opt/homebrew/opt/llvm/lib
    # /Users/dmytro/Documents/ClangSharp/artifacts/bin/sources/ClangSharpPInvokeGenerator/Release/net6.0/ClangSharpPInvokeGenerator "$@"
    set(ClangSharpPInvokeGeneratorExe "/Users/dmytro/Documents/ClangSharp/artifacts/bin/sources/ClangSharpPInvokeGenerator/Release/net6.0/ClangSharpPInvokeGenerator.sh")

    # on my older mac I had to force it to find toolchain sources, but on a new mac it somehow just work
    set(ClangSharpPInvokeGeneratorPlatformOptions
#        --additional="--sysroot=/usr/local/opt/llvm"
        --additional="--target=arm64-apple-darwin" #x86_64-apple-darwin"
#        --additional="-Wno-nullability-completeness"
#        --include-directory /Applications/Xcode.app/Contents/Developer/Platforms/MacOSX.platform/Developer/SDKs/MacOSX.sdk/usr/include
#        --include-directory /usr/local/opt/llvm/include/c++/v1
#        --include-directory /usr/local/Cellar/llvm/13.0.1/lib/clang/13.0.1/include
        )

else ()
    # stock one, installed via dotnet tool install
    set(ClangSharpPInvokeGeneratorExe "ClangSharpPInvokeGenerator")
    set(ClangSharpPInvokeGeneratorPlatformOptions)
endif ()

# ---------------------------------------------------------------------------------------------------------------------
# Input Runtime Bindings
# ---------------------------------------------------------------------------------------------------------------------

set(NativeBindingOutput
    ${R}/Runtime.CSharp/NativeBinding.cs
)
file(GLOB_RECURSE NativeBindingInputs
    ${R}/Runtime.Cpp/*.h
)
set(NativeBindingInputsAsArgs)
foreach(NativeBindingInput ${NativeBindingInputs})
    list(APPEND NativeBindingInputsAsArgs "--file" ${NativeBindingInput})
endforeach()
add_custom_command(
    OUTPUT ${NativeBindingOutput}
    COMMAND ${ClangSharpPInvokeGeneratorExe} ${ClangSharpPInvokeGeneratorPlatformOptions}
    --libraryPath InputNative
    --namespace Unity.InputSystem.Runtime
    --methodClassName Native
    #--remap Input_Control_GetTypeIdForUsageValue_FunctionPtr=TransparentBridge.Input_Control_GetTypeIdForUsageValue_FunctionPtr
    #--exclude InputAssertFormatted
    --define-macro INPUT_BINDING_GENERATION
    --define-macro INPUT_BUILD_NATIVETESTS
    --headerFile ${R}/CSharpPrefix.txt
    #--config compatible-codegen
    #--config generate-tests-nunit
    --include-directory ${R}/Runtime.Cpp
    ${NativeBindingInputsAsArgs}
    --output ${NativeBindingOutput}
    #--test-output ${R}/Runtime.CSharp/NativeBindingTests.cs
    DEPENDS ${NativeBindingInputs}
    VERBATIM
)
add_custom_target(InputNative_Bindings DEPENDS ${NativeBindingOutput})

# ---------------------------------------------------------------------------------------------------------------------
# Input Native Device Database
# ---------------------------------------------------------------------------------------------------------------------

set(NativeSourceGeneratorExe "${R}/DeviceDatabaseGenerator~/InputNativeSourceGenerator/bin/Release/net6.0/NativeSourceGenerator.dll")

set(NativeDeviceDatabaseOutput
    ${R}/Runtime.Cpp/_BuiltInDeviceDatabase.h
)
add_custom_command(
    OUTPUT ${NativeDeviceDatabaseOutput}
    COMMAND "dotnet"
    ${NativeSourceGeneratorExe}
    -o ${NativeDeviceDatabaseOutput}
    DEPENDS ${NativeSourceGeneratorExe}
    VERBATIM
)
add_custom_target(InputNative_NativeDeviceDatabase DEPENDS ${NativeDeviceDatabaseOutput})

# ---------------------------------------------------------------------------------------------------------------------
# Input Runtime Native library
# ---------------------------------------------------------------------------------------------------------------------

file(GLOB_RECURSE InputNative_Src
    ${R}/Runtime.Cpp/*.cpp
    ${R}/Runtime.Cpp/*.h
)

if(INCLUDE_NATIVE_TESTS)
    file(GLOB_RECURSE InputNative_TestsSrc
        ${R}/Development/Catch2/*.cpp
        ${R}/Development/Catch2/*.h
    )
else()
    set(InputNative_TestsSrc)
endif()

add_library(InputNative SHARED ${InputNative_Src} ${InputNative_TestsSrc} ${NativeDeviceDatabaseOutput})

add_dependencies(InputNative InputNative_Bindings InputNative_NativeDeviceDatabase)

target_include_directories(InputNative PUBLIC
    ${R}/Runtime.Cpp
    ${R}/Development/Catch2
)

target_compile_definitions(InputNative PRIVATE
    INPUT_BUILD_DYNAMICLIBRARY
)
target_compile_definitions(InputNative INTERFACE
    INPUT_USE_DYNAMICLIBRARY
)

if(INCLUDE_NATIVE_TESTS)
target_compile_definitions(InputNative PRIVATE
    CATCH_AMALGAMATED_CUSTOM_MAIN
)
target_compile_definitions(InputNative PUBLIC
    INPUT_BUILD_NATIVETESTS
)
endif()

if (${CMAKE_SYSTEM_NAME} MATCHES "Darwin")
add_custom_command(TARGET InputNative
    POST_BUILD
    COMMAND codesign -s - $<TARGET_FILE:InputNative>)
endif()

#set_target_properties(RuntimeNext.Native PROPERTIES OUTPUT_NAME RuntimeNext.Native)
#set_target_properties(RuntimeNext.Native PROPERTIES RUNTIME_OUTPUT_DIRECTORY ${R}/Build/${Target.Platform}.${Target.Arch})
#set_target_properties(RuntimeNext.Native PROPERTIES LIBRARY_OUTPUT_DIRECTORY ${R}/Build/${Target.Platform}.${Target.Arch})
#set_target_properties(RuntimeNext.Native PROPERTIES ARCHIVE_OUTPUT_DIRECTORY ${R}/Build/${Target.Platform}.${Target.Arch})
#set_target_properties(RuntimeNext.Native PROPERTIES NO_SONAME TRUE) # TODO not clear if this is needed
#set_target_properties(RuntimeNext.Native PROPERTIES BUILD_WITH_INSTALL_NAME_DIR TRUE)
#set_target_properties(RuntimeNext.Native PROPERTIES INSTALL_NAME_DIR "")

# target_compile_options(RuntimeNext.Native PRIVATE "/doc") so MSVC can generate XDC files from xmldocs in the repo

# ---------------------------------------------------------------------------------------------------------------------
# Standalone test suite
# ---------------------------------------------------------------------------------------------------------------------

if(BUILD_STANDALONE_APP)

if (${CMAKE_SYSTEM_NAME} MATCHES "Darwin")
set(Target.Platform "macOS")
else()
set(Target.Platform "Windows")
endif()

file(GLOB_RECURSE StandaloneNative_Src
    ${R}/Development/Standalone.Common/*.cpp
    ${R}/Development/Standalone.Common/*.h
    ${R}/Development/Standalone.${Target.Platform}/*.cpp
    ${R}/Development/Standalone.${Target.Platform}/*.mm
    ${R}/Development/Standalone.${Target.Platform}/*.m
    ${R}/Development/Standalone.${Target.Platform}/*.h
    )

add_executable(StandaloneNative ${StandaloneNative_Src})

target_link_libraries(StandaloneNative
    InputNative
)

target_include_directories(StandaloneNative PRIVATE
    ${R}/Development/Standalone.Core
    ${R}/Development/Standalone.${Target.Platform}
)

#add_custom_command(TARGET StandaloneDevelopment.Input.Native POST_BUILD # this needs some massaging
#    COMMAND ${CMAKE_COMMAND} -E copy_if_different
#    ${R}/Build/${Target.Platform}.${Target.Arch}/${Target.LibName}
#    $<TARGET_FILE_DIR:StandaloneDevelopment.Input.Native>)
#add_custom_command(TARGET StandaloneDevelopment.Input.Native POST_BUILD # this needs some massaging
#    COMMAND ${CMAKE_COMMAND} -E copy_if_different
#    ${R}/Development/SDL/SDL2.dll
#    $<TARGET_FILE_DIR:StandaloneDevelopment.Input.Native>)
#add_custom_command(TARGET StandaloneDevelopment.Input.Native POST_BUILD # this needs some massaging
#    COMMAND ${CMAKE_COMMAND} -E copy_if_different
#    ${R}/Development/SDL/libSDL2-2.0.dylib
#    $<TARGET_FILE_DIR:StandaloneDevelopment.Input.Native>)

endif()
