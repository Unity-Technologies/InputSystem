// Copyright (c) 2015 SharpYaml - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

// Code from coreclr with MIT License
// https://github.com/dotnet/coreclr/blob/e3eecaa56ec08d47941bc7191656a7559ac8b3c0/src/mscorlib/shared/System/Char.cs#L1018
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace SharpYaml
{
    internal static class CharHelper
    {
        internal const char HIGH_SURROGATE_START = '\ud800';
        internal const char HIGH_SURROGATE_END = '\udbff';
        internal const char LOW_SURROGATE_START = '\udc00';

        internal const char LOW_SURROGATE_END = '\udfff';

        // The starting codepoint for Unicode plane 1.  Plane 1 contains 0x010000 ~ 0x01ffff. 

        internal const int UNICODE_PLANE01_START = 0x10000;

        internal const int UNICODE_PLANE00_END = 0x00ffff;

        // The starting codepoint for Unicode plane 1.  Plane 1 contains 0x010000 ~ 0x01ffff.
        // The end codepoint for Unicode plane 16.  This is the maximum code point value allowed for Unicode.
        // Plane 16 contains 0x100000 ~ 0x10ffff.

        internal const int UNICODE_PLANE16_END = 0x10ffff;

        // char.IsHighSurrogate and char.IsLowSurrogate is not available on PCL 328

        public static bool IsHighSurrogate(char c)
        {
            return HIGH_SURROGATE_START <= c && c <= HIGH_SURROGATE_END;
        }

        public static bool IsLowSurrogate(char c)
        {
            return LOW_SURROGATE_START <= c && c <= LOW_SURROGATE_END;
        }

        public static int ConvertToUtf32(char highSurrogate, char lowSurrogate)
        {
            return (((highSurrogate - HIGH_SURROGATE_START) * 0x400) + (lowSurrogate - LOW_SURROGATE_START) + UNICODE_PLANE01_START);
        }

        public static unsafe string ConvertFromUtf32(int utf32)
        {
#if PROFILE328
            // For UTF32 values from U+00D800 ~ U+00DFFF, we should throw.  They 
            // are considered as irregular code unit sequence, but they are not illegal.
            if ((utf32 < 0 || utf32 > UNICODE_PLANE16_END) || (utf32 >= HIGH_SURROGATE_START && utf32 <= LOW_SURROGATE_END))
            {
                throw new ArgumentOutOfRangeException("utf32", "InvalidUTF32");
            }

            if (utf32 < UNICODE_PLANE01_START)
            {
                // This is a BMP character. 
                return Char.ToString((char)utf32);
            }
            unsafe
            {
                // This is a supplementary character.  Convert it to a surrogate pair in UTF-16.
                utf32 -= UNICODE_PLANE01_START;
                var address = new char[2];
                address[0] = (char)((utf32 / 0x400) + (int) HIGH_SURROGATE_START);
                address[1] = (char)((utf32 % 0x400) + (int) LOW_SURROGATE_START);
                return new string(address, 0, 2);
            }
#else
            return char.ConvertFromUtf32(utf32);
#endif
        }
    }
}