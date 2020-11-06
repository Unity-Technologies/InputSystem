using System;

namespace Examples
{
    /// <summary>
    /// The Calculator class provides basic math calculations.
    /// </summary>
    /// <remarks>The `math` functions implemented by <c>Calculator</c> favor *speed* over _accuracy_.
    /// Use <see cref="SlowCalculator"/> when you need accurate results.
    ///   
    /// Currently this calculator only implements Sum&lt;T&gt;(T) which can be linked to
    /// as <see cref="Sum{T}(T)"/>. 
    ///
    /// Supported langwords:
    /// * <see langword="null"/>
    /// * <see langword="static"/>
    /// * <see langword="virtual"/>
    /// * <see langword="true"/>
    /// * <see langword="false"/>
    /// * <see langword="null"/>
    /// * <see langword="abstract"/>
    /// * <see langword="sealed"/>
    /// * <see langword="async"/>
    /// * <see langword="await"/>
    /// * <see langword="async/await"/>
    /// * <see langword="unsafe"/>
    /// * <see langword="in"/>
    /// * <see langword="out"/>
    /// * <see langword="ref"/>
    /// * <see langword="namespace"/>
    /// * <see langword="using"/>
    /// * <see langword="where"/>
    /// * <see langword="base"/>
    /// * <see langword="this"/>
    /// * <see langword="yield"/>
    /// * <see langword="event"/>
    /// </remarks>
    /// <seealso cref="SlowCalculator"/>
    /// <seealso cref="SlowCalculator.Sum{T, R, S}(T, R)"/>
    /// <seealso cref="SlowCalculator.Sum{T, R, S}(T, R, S)"/>
    /// <example>
    ///     The following example shows how to sum:
    ///     <code>
    ///         var calc = new Calculator();
    ///         calc.Sum(73.23);
    ///         calc.Sum(21.3);
    ///         print(calc.Result());
    ///     </code>
    /// </example>
    public class Calculator
{
        /// <summary>
        /// The storage register of this calculator.
        /// </summary>
        /// <value>Stores the intermediate results of the current calculation.</value>
        public int Register;

    /// <summary>
    /// Adds the operand to the Register.
    /// </summary>
    /// <param name="operand">The number to add to the current sum.</param>
     /// <typeparam name="T">A numeric type.</typeparam>
    /// <returns>The result so far.</returns>
    /// <exception cref="NullReferenceException">Thrown if operand is null.</exception>
    /// <exception cref="UnityEditor.Build.BuildFailedException">Build Failed.</exception>
    public T Sum<T>(T operand)
    {
            if (operand == null)
                throw (new NullReferenceException("operands cannot be null."));

        Register += (int)(object)operand;
        return (T)(object)Register;
    }

    /// <summary>
    /// Gets the result of the current operation.
    /// </summary>
    /// <returns>The current Register value.</returns>
    public int Result()
    {
        return Register;
    }

    /// <summary>
    /// Clears the current calculation, setting Register to zero.
    /// </summary>
    public void Clear()
    {
        Register = 0;
    }

    }

    ///<summary>The slower, more accurate calculator.</summary>
    /// <remarks>
    /// 
    /// <![CDATA[
    /// Within this Character Data block you can
    /// use double dashes as much as you want (along with <, &, ', and ");
    /// however, you can't use the CEND sequence. If you need to use CEND you must escape one of the
    /// brackets or the greater-than sign using concatenated CDATA sections.
    /// ]]>
    /// </remarks>
    public class SlowCalculator{
    
    /// <summary>Generic function</summary>
    public T Sum<T,R,S>(T t, R r, S s) where T: new()
    {
        return new T();
    }
    /// <summary>Generic function</summary>
    public T Sum<T,R,S>(T t, R r) where T: new()
    {
        return new T();
    }
}
}