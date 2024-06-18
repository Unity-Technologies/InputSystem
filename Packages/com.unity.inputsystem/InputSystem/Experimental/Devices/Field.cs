namespace UnityEngine.InputSystem.Experimental
{
    public readonly struct Field
    {
        public static readonly Field None = new(0, FieldEncoding.None);
        
        public enum FieldEncoding : byte
        {
            None = 0,
            Bit = 1
        }

        /// <summary>
        /// Describes the byte offset of this field into the stream source element type.
        /// </summary>
        public readonly int ByteOffset;
        private readonly FieldEncoding Encoding;
        
        private Field(int offset, FieldEncoding encoding = FieldEncoding.None)
        {
            ByteOffset = offset;
            Encoding = encoding;
        }

        public static Field Offset(int offset)
        {
            return new Field(offset: offset);
        }

        public static Field Bit(int offset, int bitIndex)
        {
            return new Field(offset: 0);
        }
    }
}
