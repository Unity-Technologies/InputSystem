namespace UnityEngine.InputSystem.Experimental
{
    public struct Conditional
    {
        // TODO Consider implementing it as a Filter overload that also takes an observable (push) or a predicate (pull)
        //      Ideally conditional should be as far down the chain as possible, but likely this is a higher level add-on. Hence, one may want to find out how far down the chain one can go without violating single observer. Condition cannot go below that. On the other hand, on a Bindable input condition could unsubscribe/subscribe.
        
        
        
    }
}