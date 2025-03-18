using NUnit.Framework;
using Unity.Collections;
using UnityEngine.InputSystem.Editor;
using UnityEngine.InputSystem.LowLevel;

namespace Tests.InputSystem.Editor
{
    public class InputLatencyCalculatorTests
    {
        private InputLatencyCalculator m_Sut; // Software Under Test

        /// <summary>
        /// Adds a number of samples to the calculator under test.
        /// </summary>
        /// <param name="currentRealtimeSinceStartup">The current timestamp (reflecting wall-clock time).</param>
        /// <param name="samplesRealtimeSinceStartup">List of timestamps to be propagated as events to the calculator.</param>
        private void GivenSamples(double currentRealtimeSinceStartup, params double[] samplesRealtimeSinceStartup)
        {
            var n = samplesRealtimeSinceStartup.Length;
            using (InputEventBuffer buffer = new InputEventBuffer())
            {
                unsafe
                {
                    for (var i = 0; i < n; ++i)
                    {
                        var ptr = buffer.AllocateEvent(InputEvent.kBaseEventSize, 2048, Allocator.Temp);
                        ptr->time = samplesRealtimeSinceStartup[i];
                        m_Sut.ProcessSample(ptr, currentRealtimeSinceStartup);
                    }
                }
            }
        }

        private void AssertNoMetrics()
        {
            Assert.True(float.IsNaN(m_Sut.averageLatencySeconds));
            Assert.True(float.IsNaN(m_Sut.minLatencySeconds));
            Assert.True(float.IsNaN(m_Sut.maxLatencySeconds));
        }

        [Test]
        public void MetricsShouldBeUndefined_WhenCalculatorHasNoSamples()
        {
            m_Sut = new InputLatencyCalculator(0.0);
            AssertNoMetrics();
            m_Sut.Update(10.0);
            AssertNoMetrics();
        }

        [Test]
        public void MetricsShouldBeDefined_WhenCalculatorHasSamplesButPeriodHasNotElapsed()
        {
            m_Sut = new InputLatencyCalculator(1.0);
            GivenSamples(1.9, 1.1, 1.2, 1.9);
            m_Sut.Update(1.99);
            AssertNoMetrics();
        }

        [Test]
        public void MetricsShouldOnlyUpdate_WhenPeriodHasElapsed()
        {
            m_Sut = new InputLatencyCalculator(0.0);
            GivenSamples(0.5, 0.3, 0.4, 0.5);
            m_Sut.Update(1.0);
            Assert.That(m_Sut.averageLatencySeconds, Is.EqualTo(0.1f));
            Assert.That(m_Sut.minLatencySeconds, Is.EqualTo(0.0f));
            Assert.That(m_Sut.maxLatencySeconds, Is.EqualTo(0.2f));
        }

        [Test]
        public void MetricShouldNotBeAffected_WhenMoreThanAPeriodHasElapsed()
        {
            m_Sut = new InputLatencyCalculator(0.0);
            GivenSamples(0.5, 0.3, 0.4, 0.5);
            m_Sut.Update(2.0);
            Assert.That(m_Sut.averageLatencySeconds, Is.EqualTo(0.1f));
            Assert.That(m_Sut.minLatencySeconds, Is.EqualTo(0.0f));
            Assert.That(m_Sut.maxLatencySeconds, Is.EqualTo(0.2f));
        }

        [Test]
        public void MetricsShouldReset_WhenNotReceivingASampleForAPeriod()
        {
            m_Sut = new InputLatencyCalculator(0.0);
            GivenSamples(0.5, 0.3, 0.4, 0.5);
            m_Sut.Update(1.0);
            m_Sut.Update(2.0);
            AssertNoMetrics();
        }
    }
}
