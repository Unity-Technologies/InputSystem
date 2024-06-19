namespace UnityEngine.InputSystem.Experimental
{
    public static class Combine
    {
        // TODO See if there is some trick we can utilize to keep decent syntax but not type-erase sources
        public static CombineLatest<T0, T1, IObservableInput<T0>, IObservableInput<T1>> Latest<T0, T1>(
            IObservableInput<T0> source0, IObservableInput<T1> source1)
            where T0 : struct
            where T1 : struct
        {
            return new CombineLatest<T0, T1, IObservableInput<T0>, IObservableInput<T1>>(source0, source1);
        }
        
        public static Merge<T, IObservableInput<T>> Merge<T>(
            IObservableInput<T> source0, IObservableInput<T> source1)
            where T : struct
        {
            return new Merge<T, IObservableInput<T>>(source0, source1);
        }
        
        public static Chord<TSourceOther> Chord<TSourceOther>(TSourceOther source0, TSourceOther source1)
            where TSourceOther : IObservableInput<bool>, IDependencyGraphNode
        {
            return new Chord<TSourceOther>(source0, source1);
        }
    }
}