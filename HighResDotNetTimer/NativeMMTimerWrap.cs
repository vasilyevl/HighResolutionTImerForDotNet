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

using System.Runtime.InteropServices;

namespace Grumpy.Utilities.HighResTimer
{
    [Flags]
    internal enum MMTimerMode : uint
    {
        OneShot = 0,
        Periodic = 1,
        CallBackFunction = 0,
        SetEvent = 16,
        PulseEvent = 32,
        KillSynchroneous = 0x100,
        PeriodicCallbackFunction = Periodic | CallBackFunction,
        PeriodicSetEvent = Periodic | SetEvent,
        PeriodicPulseEvent = Periodic | PulseEvent,
        PeriodicCallbackFunctionKillSynchroneous = PeriodicCallbackFunction | KillSynchroneous,
    }

    /// <summary>
    /// Represents the callback function that is called by the system after 
    /// a specified interval has elapsed.
    /// </summary>
    /// <param name="uTimerID">
    /// The identifier of the timer. This value is returned by the timeSetEvent
    /// function when the timer event is created.
    /// </param>
    /// <param name="uMsg">
    /// This parameter is reserved and is not used.
    /// </param>
    /// <param name="dwUser">
    /// The user-defined value that was specified in the call to timeSetEvent.
    /// </param>
    /// <param name="dw1">
    /// Reserved.
    /// </param>
    /// <param name="dw2">
    /// Reserved.
    /// </param>
    /// <remarks>
    /// The TimeProc delegate is used to handle timer events. The callback 
    /// function is called when the timer interval specified in timeSetEvent
    /// has elapsed. The function should execute as quickly as possible 
    /// because it is called directly by the system. Any lengthy processing 
    /// in the callback function can interfere with the operation of other 
    /// system functions.
    /// </remarks>

    public delegate void TimeProc(int id, int msg, 
        int user, int param1, int param2);


    // See TIMECAPS structure  see timeapi.h for more information
    //https://docs.microsoft.com/en-us/windows/win32/api/timeapi/ns-timeapi-timecaps
    public struct TimerCaps
    {
        public uint PeriodMin;
        public uint PeriodMax;
    }

    public class NativeMMTimerWrap
    {
        /// <summary>
        /// Retrieves the capabilities of the system's timer and returns the 
        /// information in a 
        /// <see cref="TimerCaps"/> structure.
        /// </summary>
        /// <param name="caps">When this method returns, contains a 
        /// <see cref="TimerCaps"/> structure with the timer's 
        /// capabilities.</param>
        /// <returns>
        /// <c>true</c> if the operation was successful; otherwise, 
        /// <c>false</c>.
        /// </returns>
        /// <remarks>
        /// The <see cref="GetTimerCaps"/> method initializes the 
        /// <see cref="TimerCaps"/> 
        /// structure and fills it with the system timer's capabilities 
        /// by invoking the <c>GetDevCaps</c> function. The size of the 
        /// <see cref="TimerCaps"/> structure is calculated using 
        /// <c>Marshal.SizeOf</c>. 
        /// The method returns <c>true</c> if the capabilities are 
        /// retrieved successfully 
        /// (i.e., if <c>GetDevCaps</c> returns 0); otherwise, it 
        /// returns <c>false</c>.
        /// </remarks>
        public static bool GetTimerCaps(out TimerCaps caps) {
            
            caps = new TimerCaps();
            var r = GetDevCaps(ref caps, Marshal.SizeOf(typeof(TimerCaps)));

            return (r == 0);
        } 

        /// <summary>
        /// The GetDevCaps wraps timeGetDevCaps function. 
        /// Queries the timer device to determine its resolution.
        /// </summary>
        /// <param name="caps"></param>
        /// A pointer to a TIMECAPS structure. This structure is filled 
        /// with information about the resolution of the timer device.
        /// <param name="sizeOfTimerCaps"></param>
        /// The size, in bytes, of the TIMECAPS structure.
        /// <returns></returns>
        [DllImport("winmm.dll", SetLastError = true,
            EntryPoint = "timeGetDevCaps")]
        public static extern int GetDevCaps(ref TimerCaps caps,
            int sizeOfTimerCaps);

        /// <summary>
        /// The SetEvent wraps timeSetEvent function. Sets a specified timer 
        /// event. The multimedia timer runs in its own thread. After the 
        /// event is activated, it calls the specified callback function 
        /// or sets or pulses the specified event object.
        /// Each call to timeSetEvent for periodic timer events requires a 
        /// corresponding call to the timeKillEvent function.
        /// </summary>
        /// <param name="delay"></param>
        /// Event delay, in milliseconds. 
        /// If this value is not in the range of the minimum and maximum 
        /// event delays supported by the timer, the function returns an error.   
        /// <param name="resolution"></param>
        /// Resolution of the timer event, in milliseconds. 
        /// The resolution increases with smaller values; a resolution of 0 
        /// indicates periodic events should occur with the greatest possible
        /// accuracy. To reduce system overhead, however, you should use 
        /// the maximum value appropriate for your application.
        /// <param name="proc"></param>
        /// Pointer to a callback function that is called once upon expiration 
        /// of a single event or periodically upon expiration of periodic 
        /// events. If fuEvent specifies the TIME_CALLBACK_EVENT_SET or 
        /// TIME_CALLBACK_EVENT_PULSE flag, then the lpTimeProc parameter is 
        /// interpreted as a handle to an event object. 
        /// The event will be set or pulsed upon completion of a single event 
        /// or periodically upon completion of periodic events.
        /// <param name="userData"></param>
        /// User-supplied callback data.
        /// <param name="eventType"></param>
        /// Timer event type. 
        ///     TIME_ONESHOT	Event occurs once, after uDelay milliseconds.
        ///     TIME_PERIODIC Event occurs every uDelay milliseconds.
        ///     
        ///     The fuEvent parameter may also include one of the following 
        ///     values.
        ///     
        ///     TIME_CALLBACK_FUNCTION	When the timer expires, 
        ///                             Windows calls the function pointed 
        ///                             to by the lpTimeProc parameter. This 
        ///                             is the default.
        ///     TIME_CALLBACK_EVENT_SET When the timer expires, Windows calls 
        ///                             the SetEvent function to set the event
        ///                             pointed to by the lpTimeProc parameter.
        ///                             The dwUser parameter is ignored.
        ///     TIME_CALLBACK_EVENT_PULSE When the timer expires, 
        ///                             Windows calls the PulseEvent function 
        ///                             to pulse the event pointed to by the 
        ///                             lpTimeProc parameter.The dwUser 
        ///                             parameter is ignored.
        ///     TIME_KILL_SYNCHRONOUS   Passing this flag prevents an event 
        ///                             from occurring after the timeKillEvent
        ///                             function is called.
        /// <returns>
        ///     Returns an identifier for the timer event if successful or an
        ///     error otherwise. This function returns NULL if it fails and 
        ///     the timer event was not created. This identifier is also 
        ///     passed to the callback function.
        /// </returns>

        [DllImport("winmm.dll", SetLastError = true,
            EntryPoint = "timeSetEvent")]
        public static extern int SetEvent(uint delay, uint resolution,
            TimeProc proc, int userData, uint eventType);

        /// <summary>
        /// The KillEvent wraps timeKillEvent function. Cancels a specified 
        /// timer event.
        /// </summary>
        /// <param name="id"></param>
        /// Identifier of the timer event to cancel. This identifier was 
        /// returned by the timeSetEvent function when the timer event 
        /// was set up.
        /// <returns>
        /// Returns TIMERR_NOERROR if successful or MMSYSERR_INVALPARAM if 
        /// the specified timer event does not exist.
        /// </returns>
        [DllImport("winmm.dll", SetLastError = true,
            EntryPoint = "timeKillEvent")]
        public static extern int KillEvent(int id);


        /// <summary>
        /// The BeginPeriod waps timeBeginPeriod function. Sets minimum timer 
        /// resolution, in milliseconds, for the application or device driver. 
        /// A lower value specifies a higher (more accurate) resolution.
        /// </summary>
        /// <param name="uPeriod"></param>
        /// Minimum timer resolution, in milliseconds, for the application 
        /// or device driver. A lower value specifies a higher (more accurate) 
        /// resolution.
        /// <returns></returns>
        /// Returns TIMERR_NOERROR if successful or TIMERR_NOCANDO if the 
        /// resolution specified in uPeriod is out of range.
        /// <remarks>
        /// Call this function immediately before using timer services, 
        /// and call the EndPeriod function immediately after you are 
        /// finished using the timer services.
        /// </remarks>
        
        [DllImport("winmm.dll", SetLastError = true,
            EntryPoint = "timeBeginPeriod")]
        public static extern uint BeginPeriod(uint uPeriod);

        /// <summary>
        /// The EndPeriod wraps timeBeginPeriod function. 
        /// Clears a previously set by BeginPeriod minimum timer resolution.
        /// </summary>
        /// <param name="uPeriod"></param>
        /// Minimum timer resolution specified in the previous call to the 
        /// BeginPeriod function.
        /// <returns></returns>
        /// Returns TIMERR_NOERROR if successful or TIMERR_NOCANDO if the 
        /// resolution specified in uPeriod is out of range.
        /// <remarks>
        /// Call this function immediately after you are finished using timer
        /// services.You must match each call to BeginPeriod with a call 
        /// to EndPeriod, specifying the same minimum resolution in both 
        /// calls. An application can make multiple timeBeginPeriod calls as 
        /// long as each call is matched with a call to timeEndPeriod.
        /// </remarks>       

        [DllImport("winmm.dll", SetLastError = true,
            EntryPoint = "timeEndPeriod")]
        public static extern uint EndPeriod(uint uMilliseconds);





    }
}
