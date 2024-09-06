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

namespace Grumpy.Utilities.HighResTimer
{

    public class HighResTimerException : System.Exception {

        public HighResTimerException(string message) : base(message) { }
    }   

    public enum TimerMode {
        OneShot = 0,
        Periodic = 1
    }

    /// <summary>
    /// Represents the event data for a timer event, providing details about 
    /// the state of the timer when the event was triggered.
    /// </summary>
    public class TimerEventArgs : EventArgs {

        /// <summary>
        /// Gets the unique identifier for the timer that raised the event.
        /// </summary>
        public int TimerID { get; private set; }

        /// <summary>
        /// Gets current ticks number since the timer started.
        /// </summary>
        public ulong ClickNumber { get; private set; }

        /// <summary>
        /// Gets the total number of missed ticks that have occurred 
        /// since the timer started.
        /// </summary>
        public ulong MissedClicks { get; private set; }

        /// <summary>
        /// Gets the precise date and time when the event was triggered.
        /// </summary>
        public System.DateTime Time { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimerEventArgs"/> class 
        /// with the specified timer ID, number of clicks, and event time.
        /// </summary>
        /// <param name="timerID">The unique identifier for the timer.</param>
        /// <param name="clicks">The total number of clicks (or ticks) 
        /// that have occurred.</param>
        /// <param name="time">The precise date and time when the event 
        /// was triggered.</param>
        public TimerEventArgs(int timerID, ulong clicks, 
            ulong missedClicks, System.DateTime time) {

            TimerID = timerID;
            ClickNumber = clicks;
            MissedClicks = missedClicks;
            Time = time;
        }
    }

    /// <summary>
    /// Represents a method that handles multimedia timer events.
    /// </summary>
    /// <param name="timerID">The unique identifier of the timer that 
    /// triggered the event.</param>
    /// <param name="tickNumber">The total number of ticks that have 
    /// occurred since the timer started.</param>
    /// <param name="time">The precise date and time when the event 
    /// was triggered.</param>
    /// 
    public delegate void TimerProc( int timerID, ulong tickNumber, DateTime time);

    /// <summary>
    /// Represents a multimedia timer that can trigger 
    /// events at specified intervals.
    /// </summary>
    public class HighResTimer {

        private static TimerCaps _systemsCaps;
        private TimerProc? _userTimerProc;

        int _timerId;
        uint _periodMs;
        uint _resolutionMs;
        
        private ulong _tickCounter;
        private ulong _lockCounter;
        private ulong _eventCounter;
        private TimerMode _mode;
        private string _lastError;
        private object _lockCounterLock;
        private ManualResetEvent? _waitHandle;
        static HighResTimer() {

            NativeMMTimerWrap.GetTimerCaps(out _systemsCaps);
        }


        public HighResTimer() {

            _timerId = 0;
            _periodMs = 0;
            _resolutionMs = 0;
            _tickCounter = 0;
            _lockCounter = 0;
            _eventCounter = 0;
            _mode = TimerMode.Periodic;
            _lastError = string.Empty;
            _lockCounterLock = new object();
            _waitHandle = null;
        }


        /// <summary>
        /// Occurs when the timer event is triggered.
        /// </summary>
        public EventHandler<TimerEventArgs>? TimerEvent;


        /// <summary>
        /// Initializes a new instance of the <see cref="HighResTimer"/> 
        /// class with the specified settings.
        /// </summary>
        /// <param name="periodMs">The timer period in milliseconds.</param>
        /// <param name="resolutionMs">The timer resolution in milliseconds. 
        /// Defaults to 0.</param>
        /// <param name="userCallback">A delegate that will be called when 
        /// the timer event occurs.</param>
        /// <param name="operatingMode">The mode of the timer 
        /// (one-shot or periodic).</param>
        /// <param name="autoStart">Indicates whether the timer should 
        /// start automatically.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when 
        /// the period or resolution is out of range.</exception>
        /// <exception cref="HighResTimerException">Thrown when the timer 
        /// fails to start.</exception>
        ///
        public HighResTimer(uint periodMs, uint resolutionMs = 0, 
            TimerProc? userCallback = null, 
            TimerMode operatingMode = TimerMode.Periodic, bool autoStart = true): 
                this() 
        {

            if (  periodMs > _systemsCaps.PeriodMax 
                || periodMs < _systemsCaps.PeriodMin) {

                throw new System.ArgumentOutOfRangeException("periodMs",
                    $"Period must be between {_systemsCaps.PeriodMin} " +
                    $"and {_systemsCaps.PeriodMax} ms.");
            }

            if (resolutionMs > _systemsCaps.PeriodMax) {

                throw new System.ArgumentOutOfRangeException("resolutionMs",
                    $"Resolution must be less than  {_systemsCaps.PeriodMax} ms.");
            }

            TimerEvent = null;
            _userTimerProc = userCallback;

            _periodMs = periodMs;
            _resolutionMs = resolutionMs;
            
            _tickCounter = 0;
            _lockCounter = 0;
            _eventCounter = 0;
            
            _mode = operatingMode;

            if (autoStart) {

                if (!Start()) {

                    throw new HighResTimerException(LastError);
                }
            }
        }

        /// <summary>
        /// Waits for the specified duration and returns a boolean indicating success.
        /// </summary>
        /// <param name="duration">The duration to wait for in milliseconds.</param>
        /// <returns>
        /// Returns <c>true</c> if the wait completed within the specified time; 
        /// otherwise, <c>false</c> if the wait timed out or was interrupted.
        /// </returns>
        /// <remarks>
        /// This function uses a multimedia timer to trigger an event after the specified 
        /// duration, and waits for the event with a timeout of twice the duration.
        /// If the event is triggered before the timeout, the function returns <c>true</c>.
        /// </remarks>

        public bool Wait( uint duration) {

            _timerId = NativeMMTimerWrap.SetEvent(duration,
                               _systemsCaps.PeriodMin,
                               WaitCallback,
                               0,
                               (uint)MMTimerMode.OneShot);

            _waitHandle = new ManualResetEvent(false);
            Boolean r = _waitHandle.WaitOne((int)duration * 2);
            _waitHandle.Close();
            _waitHandle = null;

            return r;
        }



        /// <summary>
        /// Starts the multimedia timer.
        /// </summary>
        /// <returns>True if the timer starts successfully; 
        /// otherwise, false.</returns>
        public bool Start() {
            try {

                _timerId = NativeMMTimerWrap.SetEvent(_periodMs,
                    _resolutionMs,
                    TimerCallback,
                    0,
                    _mode == TimerMode.OneShot ?
                       (uint)MMTimerMode.OneShot :
                       (uint)MMTimerMode.PeriodicCallbackFunctionKillSynchroneous);

                if (_timerId == 0) {

                    LastError = $"Failed to set MM timer event in \"{_mode}\" mode.";
                }

                return _timerId != 0;
            }
            catch (Exception ex) {
             
                LastError = ex.Message;
                return false;
            }
        }





        /// <summary>
        /// Stops the multimedia timer.
        /// </summary>
        /// <returns>True if the timer stops successfully; 
        /// otherwise, false.</returns>
        /// 
        public bool Stop() {
            
            try { 
            
                var r =  NativeMMTimerWrap.KillEvent(_timerId);
                
                if (!(r == 0)) {
                
                    LastError = $"Failed to stop MM timer event in " +
                        $"\"{_mode}\" mode.";
                }

                return r == 0;
            }
            catch (Exception ex) {
                
                LastError = ex.Message;
                return false;
            }
        }

        private object _timerProcLock = new object();

        /// <summary>
        /// The callback method that is invoked when the timer event occurs.
        /// </summary>
        /// <param name="id">The timer ID.</param>
        /// <param name="msg">The message ID.</param>
        /// <param name="user">User data passed to the callback.</param>
        /// <param name="dw1">Additional data.</param>
        /// <param name="dw2">Additional data.</param>
        ///
        private void TimerCallback(int id, int msg, int user, 
            int dw1, int dw2) {

            var time = System.DateTime.Now; 
            bool lockTaken = false;

            try {

                Monitor.TryEnter(_timerProcLock, Math.Max(1, 
                    (int)_periodMs /2 ), ref lockTaken);

                if (lockTaken) {
            
                    lock (_tickLock) {

                        _tickCounter++;
                        _eventCounter = _tickCounter + _lockCounter;
                    }

                    if (_userTimerProc != null) {

                        _userTimerProc.Invoke(id, _eventCounter, time);
                    }
                    
                    var eventHandler = TimerEvent;
                    TimerEvent?.Invoke(this, 
                        new TimerEventArgs(id, _eventCounter, 
                                           _lockCounter, time));
                }
                else {
                    lock (_lockCounterLock) {

                        _lockCounter++;
                    }
                }
            }
            catch (System.Exception ex) {

                LastError = ex.Message;
            }
            finally {

                if (lockTaken) {

                    Monitor.Exit(_timerProcLock);
                }
            }
        }


        private void WaitCallback(int id, int msg, int user, 
                       int dw1, int dw2) {

            var r = _waitHandle?.Set();
        }


        private object _tickLock = new object();

        /// <summary>
        /// Gets number of processed ticks (number of ticks for which 
        /// callback and/or event handler were called successfully).
        /// </summary>
        ///
        public ulong TickCounter {

            get {
                lock (_tickLock) {

                    return _tickCounter;
                }
            }
        }


        /// <summary>
        /// Gets the count of missed ticks (ticks for which the timer
        /// failed to call callback and/or event handler).
        /// </summary>
        public ulong MisedTickCounter {

            get {
                lock (_lockCounterLock) {

                    return _lockCounter;
                }
            }
        }

        private object _errorLock = new object();

        /// <summary>
        /// Gets the last error message encountered by the timer.
        /// </summary>
        public string LastError {
            get {
                lock (_errorLock) {

                    return _lastError;
                }
            }
            private set {
                lock (_errorLock) {

                    _lastError = value;
                }
            }   
        }
    }
}
