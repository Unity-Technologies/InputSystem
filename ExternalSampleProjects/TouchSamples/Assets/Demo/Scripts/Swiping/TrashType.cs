namespace InputSamples.Demo.Swiping
{
    /// <summary>
    /// Types of trash.
    /// </summary>
    public enum TrashType
    {
        Glass = 1 << 0,
        Metal = 1 << 1,
        Paper = 1 << 2,
        Plastic = 1 << 3
    }
}
