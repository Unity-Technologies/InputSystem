using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Input.Utilities
{
    // Helper to avoid array allocations if there's only a single value in the
    // array.
    internal struct InlinedArray<TValue>
    {
        // We inline the first value so if there's only one, there's
        // no additional allocation. If more are added, we allocate an array.
        public TValue firstValue;
        public TValue[] additionalValues;

        public int Count
        {
            get
            {
                var count = 0;
                if (firstValue != null)
                    ++count;
                if (additionalValues != null)
                    count += additionalValues.Length;
                return count;
            }
        }

        public TValue this[int index]
        {
            get
            {
                if (index == 0 && firstValue != null)
                    return firstValue;

                if (additionalValues == null)
                    throw new IndexOutOfRangeException();

                return additionalValues[index - 1];
            }
        }

        public void Clear()
        {
            firstValue = default(TValue);
            additionalValues = null;
        }

        public InlinedArray<TValue> Clone()
        {
            return new InlinedArray<TValue>
            {
                firstValue = firstValue,
                additionalValues = additionalValues != null ? (TValue[])additionalValues.Clone() : null
            };
        }

        public TValue[] ToArray()
        {
            return ArrayHelpers.Join(firstValue, additionalValues);
        }

        public void Append(TValue value)
        {
            if (firstValue == null)
            {
                firstValue = value;
            }
            else if (additionalValues == null)
            {
                additionalValues = new TValue[1];
                additionalValues[0] = value;
            }
            else
            {
                var numAdditionalProcessors = additionalValues.Length;
                Array.Resize(ref additionalValues, numAdditionalProcessors + 1);
                additionalValues[numAdditionalProcessors] = value;
            }
        }

        public void Remove(TValue value)
        {
            if (EqualityComparer<TValue>.Default.Equals(firstValue, value))
            {
                if (additionalValues != null)
                {
                    firstValue = additionalValues[0];
                    if (additionalValues.Length == 1)
                        additionalValues = null;
                    else
                    {
                        Array.Copy(additionalValues, 1, additionalValues, 0, additionalValues.Length - 1);
                        Array.Resize(ref additionalValues, additionalValues.Length - 1);
                    }
                }
                else
                {
                    firstValue = default(TValue);
                }
            }
            else if (additionalValues != null)
            {
                var numAdditionalProcessors = additionalValues.Length;
                for (var i = 0; i < numAdditionalProcessors; ++i)
                {
                    if (EqualityComparer<TValue>.Default.Equals(additionalValues[i], value))
                    {
                        if (numAdditionalProcessors == 1)
                        {
                            // Remove only entry in array.
                            additionalValues = null;
                        }
                        else if (i == numAdditionalProcessors - 1)
                        {
                            // Remove entry at end.
                            Array.Resize(ref additionalValues, numAdditionalProcessors - 1);
                        }
                        else
                        {
                            // Remove entry at beginning or in middle by pasting together
                            // into a new array.
                            var newAdditionalProcessors = new TValue[numAdditionalProcessors - 1];
                            if (i > 0)
                            {
                                // Copy element before entry.
                                Array.Copy(additionalValues, 0, newAdditionalProcessors, 0, i);
                            }
                            if (i != numAdditionalProcessors - 1)
                            {
                                // Copy elements after entry.
                                Array.Copy(additionalValues, i + 1, newAdditionalProcessors, i,
                                    numAdditionalProcessors - i - 1);
                            }
                        }
                        break;
                    }
                }
            }
        }
    }
}
