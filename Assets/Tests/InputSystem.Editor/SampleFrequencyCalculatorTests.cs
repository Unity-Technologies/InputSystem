using NUnit.Framework;
using Unity.Collections;
using UnityEngine.InputSystem.Editor;
using UnityEngine.InputSystem.LowLevel;

namespace Tests.InputSystem.Editor
{
    public class SampleFrequencyCalculatorTests
    {
        private SampleFrequencyCalculator m_Sut; // Software Under Test

        /// <summary>
        /// Adds a number of samples to the calculator under test.
        /// </summary>
        /// <param name="samplesRealtimeSinceStartup">List of timestamps to be propagated as events to the calculator.</param>
        private void GivenSamples(params double[] samplesRealtimeSinceStartup)
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
                        m_Sut.ProcessSample(ptr);
                    }
                }
            }
        }

        [Test]
        public void FrequencyShouldBeZero_WhenCalculatorHasNoSamples()
        {
            m_Sut = new SampleFrequencyCalculator(10, 0.0);
            Assert.That(m_Sut.frequency, Is.EqualTo(0.0f));
        }

        [Test]
        public void FrequencyShouldBeZero_WhenCalculatorHasSamplesButPeriodHasNotElapsed()
        {
            m_Sut = new SampleFrequencyCalculator(10.0f, 1.0);
            GivenSamples(1.1, 1.2, 1.9);
            m_Sut.Update(1.99);
            Assert.That(m_Sut.frequency, Is.EqualTo(0.0f));
        }

        [Test]
        public void MetricsShouldOnlyUpdate_WhenPeriodHasElapsed()
        {
            m_Sut = new SampleFrequencyCalculator(10.0f, 0.0);
            GivenSamples(0.3, 0.4, 0.5);
            m_Sut.Update(1.0);
            Assert.That(m_Sut.frequency, Is.EqualTo(3.0f));
        }

        [Test]
        public void MetricsShouldBeBasedOnActualElapsedPeriod_WhenMoreThanAPeriodHasElapsed()
        {
            m_Sut = new SampleFrequencyCalculator(10.0f, 0.0);
            GivenSamples(0.5, 0.3, 0.4, 0.5);
            m_Sut.Update(2.0);
            Assert.That(m_Sut.frequency, Is.EqualTo(2.0f));
        }

        [Test]
        public void MetricsShouldNotBeUpdated_WhenLessThanAPeriodHasElapsed()
        {
            m_Sut = new SampleFrequencyCalculator(10.0f, 0.0);
            GivenSamples(0.5, 0.3, 0.4, 0.5);
            m_Sut.Update(2.0);
            m_Sut.Update(2.9);
            Assert.That(m_Sut.frequency, Is.EqualTo(2.0f));
        }

        [Test]
        public void MetricsShouldReset_WhenNotReceivingASampleForAPeriod()
        {
            m_Sut = new SampleFrequencyCalculator(1.0f, 0.0);
            GivenSamples(0.5, 0.3, 0.4, 0.5);
            m_Sut.Update(1.0);
            m_Sut.Update(2.0);
            Assert.That(m_Sut.frequency, Is.EqualTo(0.0f));
        }
    }
}
