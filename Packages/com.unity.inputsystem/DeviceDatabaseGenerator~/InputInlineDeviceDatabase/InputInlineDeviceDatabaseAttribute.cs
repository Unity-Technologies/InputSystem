// TODO this is needed for older roslyn source generators
// In newer ones we can use RegisterForPostInitialization to spawn the atribute from the source generator itself

[System.AttributeUsage(System.AttributeTargets.Assembly, AllowMultiple=true)]
public sealed class InputInlineDeviceDatabaseAttribute: System.Attribute
{
    public string Value { get; }
    public int Priority { get; }

    public InputInlineDeviceDatabaseAttribute(string value, int priority = 1) // TODO there is some BS how source generator reads this value, so using 1 for now
    {
        Value = value;
        Priority = priority;
    }
}

