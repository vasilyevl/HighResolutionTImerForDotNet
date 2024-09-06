/*
Copyright (c) 2024 vasilyevl (Grumpy). Permission is hereby granted, 
free of charge, to any person obtaining a copy of this software
and associated documentation files (the "Software"),to deal in the Software 
without restriction, including without limitation the rights to use, copy, 
modify, merge, publish, distribute, sublicense, and/or sell copies of the 
Software, and to permit persons to whom the Software is furnished to do so, 
subject to the following conditions:

The above copyright notice and this permission notice shall be included 
in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,FITNESS FOR A 
PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE 
OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

namespace HighResTester
{
    using Grumpy.Utilities.HighResTimer;
    using System.Runtime.InteropServices;
    using Xunit.Abstractions;
    using Xunit.Sdk;

    public class UnitTest
    {
        private readonly ITestOutputHelper _testOutputHelper;
        static TimerCaps _caps = new TimerCaps();
        uint _period = 3;
        uint _resolution = 1;

        uint _testDuration = 10000;

        DateTime _start = DateTime.Now;
        List<DateTime> _times = new List<DateTime>();
        List<double> _durations = new List<double>();

        public UnitTest(ITestOutputHelper testOutputHelper) {
            _testOutputHelper = testOutputHelper;
        }

 


        [Fact]
        public void Test1_MMWrapPeriodic() {

            _testOutputHelper.WriteLine("Test1 (\"Periodic\") started.");
            Int32 r  = NativeMMTimerWrap.GetDevCaps(ref _caps,
                 Marshal.SizeOf<TimerCaps>(UnitTest._caps));

            Assert.True(r == 0, "Faile to get MMTimer caps.");

            _testOutputHelper.WriteLine("MM timer caps received: \n" +
                $"Min - {_caps.PeriodMin}ms, max - {_caps.PeriodMax}ms.");

            _start = DateTime.Now;
 
            _times.Clear();

            uint resolution = Math.Max(_resolution, _caps.PeriodMin);   
            NativeMMTimerWrap.BeginPeriod(resolution);

            Int32 id = NativeMMTimerWrap.SetEvent(_period, 
                resolution, TimerCallback, 
                123, 
                (uint)TimerMode.Periodic);

            Assert.True(id != 0, "Failed to set MM timer event.");
            Thread.Sleep((int)_testDuration);

            NativeMMTimerWrap.KillEvent(id);
            NativeMMTimerWrap.EndPeriod(resolution);
            ReportResults();
        }


        [Fact]
        public void Test2_MMWrap_SingleShot() {
            _testOutputHelper.WriteLine("Test2 (\"Single Shot\") started.");
            var r = NativeMMTimerWrap.GetDevCaps(ref _caps,
                 Marshal.SizeOf<TimerCaps>(UnitTest._caps));

            Assert.True(r == 0, "Failed to get MMTimer caps.");


            _testOutputHelper.WriteLine("MM timer caps received: \n" +
                $"Min - {_caps.PeriodMin}ms, max - {_caps.PeriodMax}ms.");

            for (int i = 0; i < 100; i++) {

                _start = DateTime.Now;

                _times.Clear();

                uint resolution = Math.Max(_resolution, _caps.PeriodMin);
                NativeMMTimerWrap.BeginPeriod(resolution);
                Int32 id = NativeMMTimerWrap.SetEvent(_period,
                    resolution, 
                    TimerCallback,
                    123,
                    (uint)TimerMode.OneShot);

                Assert.True(id != 0, "Failed to set MM timer event.");
                Thread.Sleep(100);

                NativeMMTimerWrap.KillEvent(id);
                NativeMMTimerWrap.EndPeriod(resolution);

                ReportResults();
            }
        }

        [Fact]
        public void Test3_Periodic_W_Callback() {

            _times.Clear(); 
            var timer = new HighResTimer(periodMs: _period,
                resolutionMs: _resolution,
                userCallback: TimerProc, 
                operatingMode: TimerMode.Periodic, 
                autoStart: false);

            _start = DateTime.Now;
            
            timer.Start();
            
            Thread.Sleep((int)_testDuration);
            
            timer.Stop();

            _testOutputHelper.WriteLine($"MM timer \"Test3\" complete.\n" +
                $"\tTotal processed tickes: {timer.TickCounter}.\n" +
                $"\tTotal missed ticks: {timer.MisedTickCounter}.");

            ReportResults();
        }

        [Fact]
        public void Test4_Periodic_W_Event() {

            _times.Clear();
            var timer = new HighResTimer(periodMs: _period,
                resolutionMs: _resolution,
                userCallback: null,
                operatingMode: TimerMode.Periodic,
                autoStart: false);

            _start = DateTime.Now;
            timer.TimerEvent += EventHandler!;
            timer.Start();

            Thread.Sleep((int)_testDuration);

            timer.Stop();

            _testOutputHelper.WriteLine($"MM timer \"Test3\" complete.\n" +
                $"\tTotal processed tickes: {timer.TickCounter}.\n" +
                $"\tTotal missed ticks: {timer.MisedTickCounter}.");

            ReportResults();
        }


        [Fact]
        public void Test5_SingleShot_W_Event() {

            _durations.Clear();

            var timer = new HighResTimer(periodMs: _period,
                resolutionMs: _resolution,
                userCallback: null,
                operatingMode: TimerMode.OneShot,
                autoStart: false);

            
            timer.TimerEvent += SingleSHutEventHandler!;
            
            for( int i = 0; i < 50; i++) {

                _start = DateTime.Now;
                timer.Start();
                Thread.Sleep((int)_period*2); 
            }

            _testOutputHelper.WriteLine($"MM timer \"Test5\" complete.\n" +
                $"\tTotal processed tickes: {_times.Count}.");

            ReportSingleShotResults();
        }

        [Fact]
        public void Test6_Wait() {

            _durations.Clear();
            var accum = 0.0;
            var timer = new HighResTimer();

            for (int i = 0; i < 50; i++) {

                _start = DateTime.Now;
                var r = timer.Wait(_period);

                _durations.Add(((double)(DateTime.Now - _start).TotalMicroseconds) / 1000.0);
                accum += _durations[i];
                Assert.True(r, "Failed to wait for timer.");

                _testOutputHelper.WriteLine($"#{_durations.Count}.\"Test6\" " +
                    $"Wait lasted {_durations[i].ToString("F2")} ms. Requested time {_period}ms. " +
                    $"Error: {(100.0 * (_durations[i] - _period) / _period).ToString("F2")}%.");
            }

            _testOutputHelper.WriteLine($"MM timer \"Test6\" complete.\n" +
                $"\tTotal processed tickes: {_times.Count}.");

            _testOutputHelper.WriteLine($"Average duration: " +
                $"{(accum / _durations.Count).ToString("F2")}ms.");
        }
        private void TimerCallback(int id, int msg, int user, int dw1, int dw2) {
            _times.Add(DateTime.Now);
        }


        private void TimerProc(int timerID, ulong clicks, DateTime time) {

            _times.Add(time);

        }

        private void EventHandler(object sender, TimerEventArgs e) {
            _times.Add(e.Time);
        }


        private void SingleSHutEventHandler(object sender, TimerEventArgs e) {
           
            _durations.Add(((double)(DateTime.Now - _start).TotalMicroseconds)/1000.0);
        }

        private void ReportResults(){

            _testOutputHelper.WriteLine($"Start Time: {_start.ToString("HH:mm:ss.fff")}");
            _testOutputHelper.WriteLine($"Period: {_period}ms");
            for (int i = 0; i < _times.Count; i++) {
                Double delta = (_times[i] - ((i == 0) ? _start : _times[i - 1])).TotalMilliseconds;
                var error = delta - _period;
                _testOutputHelper.WriteLine($" #{i+1}. \tTime: {_times[i].ToString("HH:mm:ss.fff")}, delta: " +
                    $"{delta.ToString("F2")}ms. Error: {error.ToString("F2")}ms. /  {(100.0 * error / _period).ToString("F2")}%.");
            }
        }


        private void ReportSingleShotResults() {

            _testOutputHelper.WriteLine($"Period: {_period}ms");
            var accum = 0.0;
            for (int i = 0; i < _durations.Count; i++) {

                _testOutputHelper.WriteLine($" #{i + 1}. \tTime delta: " +
                    $"{_durations[i].ToString("F2")}ms."); 
            
                accum += _durations[i];
            }

            _testOutputHelper.WriteLine($"Average duration: {(accum / _durations.Count).ToString("F2")}ms.");

        }
    }
}