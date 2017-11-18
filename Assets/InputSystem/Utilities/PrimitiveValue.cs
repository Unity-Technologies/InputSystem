namespace ISX
{
    // A string or numeric value.
    public unsafe struct PrimitiveValue
    {
        private string m_String;
        private fixed byte m_Numeric[8];
    }
}
