using System;

namespace UnityEngine.InputSystem.Experimental
{
    /// <summary>
    /// Symbols are constants providing a symbolic physical representation if a control that is often referencing
    /// a label, shape or spatial location of a control that allows a user to identify which control is being
    /// referred to. Symbols are useful for debugging and/or rebinding UI or UI describing controls used to
    /// interact with the game or application if dynamically generated.
    /// </summary>
    public struct Symbol
    {
        public const uint Unspecified = 0;
        
        public const uint BuiltinLowerRangeInclusive = 0x10;
        public const uint BuiltinUpperRangeExclusive = 0xffff7000;
        
        public const uint CustomLowerRangeInclusive = 0xffff7000;
        public const uint CustomUpperRangeExclusive = 0xffffffff;

        /// <summary>
        /// Built-in symbol range may only be added to by Unity developers and represents symbols part of
        /// standard models.
        /// </summary>
        #region Built-in symbol range
        
        public const uint KeyW = 0x00000010;
        public const uint KeyS = 0x00000011;
        public const uint KeyA = 0x00000012;
        public const uint KeyD = 0x00000013;
        
        public const uint Cross = 0x00001000;
        public const uint Square = 0x00001001;
        public const uint Ring = 0x00001002;
        public const uint Triangle = 0x00001003;
        
        #endregion
        
        // TODO Custom symbol range is added to by registering a symbol range used by an extensions. E.g. RegisterSymbolRange(100) acquires a dynamic range of 100 symbols and returns the lower (inclusive) range of the allocated interval. This
        
        public static bool IsBuiltInRange(uint symbol)
        {
            return symbol >= BuiltinLowerRangeInclusive && symbol < BuiltinUpperRangeExclusive;
        }

        public static bool IsCustomRange(uint symbol)
        {
            return symbol >= CustomLowerRangeInclusive && symbol < CustomUpperRangeExclusive;
        }

        public static bool IsValid(uint symbol)
        {
            if (symbol >= BuiltinLowerRangeInclusive && symbol < BuiltinUpperRangeExclusive)
                return true;
            if (symbol >= CustomLowerRangeInclusive && symbol < CustomUpperRangeExclusive)
                return true;
            return false;
        }
    }

    // [InputSymbolEnum]
    public enum CustomSymbol
    {
        A, 
        B, 
        C
    }
    
    // PoC
    // [InputSymbol()]
    public partial struct CustomSymbols
    {
        public CustomSymbol FromSymbol(uint symbol)
        {
            return (CustomSymbol)(symbol - Symbol.CustomLowerRangeInclusive); // TODO Should be code generated, but then needs compile-time range
        }
    }

    // TODO This should not be in core package, likely it should even be a data file or database.
    // TODO Use case is to based on desired result provide an alternative representation to raw symbols, e.g. display text or textures or play audio or similar.
    public struct TextSymbol
    {
        public static string ToString(uint symbol)
        {
            switch (symbol)
            {
                case Symbol.KeyW:     return "W";
                case Symbol.KeyA:     return "A";
                case Symbol.KeyS:     return "S";
                case Symbol.KeyD:     return "D";
                case Symbol.Cross:    return "Cross";
                case Symbol.Square:   return "Square";
                case Symbol.Triangle: return "Triangle";
                case Symbol.Ring:     return "Ring";
                default:              return string.Empty;
            }
        }
    }
}