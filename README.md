# HighResolutionTImerForDotNetCore
High-Resolution Multimedia Timer Library

Timer for triggering periodic or one-shot events at specified intervals. It leverages system multimedia timers to achieve accurate timing and offers an easy-to-use API for integrating high-resolution timers into your .NET applications, but still resolution and accuracy limited to ~1 - 2 ms.
Single shot mode and first cycle in contineous mode typically a longer than requested time.   

Features
One-shot or periodic mode: The timer can be configured to fire once or repeatedly at a set interval.
High-resolution timer: Achieve millisecond precision using multimedia timers.
Event-driven architecture: The timer can raise events or invoke a user-defined callback upon each tick.
Multithreaded safety: Safely handles timer ticks and callbacks in multithreaded environments.
Error handling: Includes custom exceptions and error messages to help with debugging.

This librarys also include simple managed wrap class for Windows multimedia timer and allows using it directly.

Installation
Simply add the provided HighResTimer class to your project. Ensure your project references the necessary system namespaces like System.Threading and System.Timers.

Usage
1. Initializing a Timer
To create and start a high-resolution timer, specify the timer period, optional resolution, and the mode (one-shot or periodic):

csharp
// Create a timer with a period of 100ms in periodic mode
HighResTimer timer = new HighResTimer(100, 0, TimerCallback, TimerMode.Periodic, true);

// Callback function to handle timer events
void TimerCallback(int timerID, ulong tickNumber, DateTime time) {
    Console.WriteLine($"Timer {timerID} ticked at {time}, tick number {tickNumber}");
}
2. Handling Timer Events
Alternatively, you can handle timer events using the TimerEvent event:

csharp example:
Copy code
// Attach event handler for the timer event
timer.TimerEvent += (sender, e) => {
    Console.WriteLine($"Timer {e.TimerID} event at {e.Time}, tick number {e.ClickNumber}");
};
3. Controlling the Timer
You can start and stop the timer manually:

csharp example:

// Start the timer
bool isStarted = timer.Start();

// Stop the timer in repeat mode.
bool isStopped = timer.Stop();
4. Waiting for a Timer Event
You can also wait for a specified duration using the Wait method, which will block until the timer event occurs or the duration elapses:

csharp exaple:
bool waitResult = timer.Wait(200); // Wait for 200ms
5. Error Handling
Errors encountered during timer operations are accessible via the LastError property:

csharp example:
if (!timer.Start()) {
    Console.WriteLine($"Failed to start timer: {timer.LastError}");
}
Custom Exceptions
The library includes the HighResTimerException class for custom exceptions, thrown when the timer fails to start or encounters an error:

csharp example:

try {
    timer.Start();
} catch (HighResTimerException ex) {
    Console.WriteLine(ex.Message);
}

API Reference
HighResTimer Class
Constructor: HighResTimer(uint periodMs, uint resolutionMs = 0, TimerProc? userCallback = null, TimerMode mode = TimerMode.Periodic, bool autoStart = true)

periodMs: The interval between timer events in milliseconds.
resolutionMs: The timer resolution in milliseconds.
userCallback: Optional callback function for handling timer events.
mode: The timer mode, either OneShot or Periodic.
autoStart: Whether to automatically start the timer upon initialization.
Properties:

TickCounter: Number of successfully processed ticks.
MisedTickCounter: Number of missed ticks.
LastError: The last error encountered by the timer.
Methods:

Start(): Starts the timer.
Stop(): Stops the timer.
Wait(uint duration): Waits for the specified duration, returning true if the timer event occurs, or false if the wait times out.
TimerEventArgs Class
Event data for timer events, containing:

TimerID: The unique identifier of the timer.
ClickNumber: The number of ticks since the timer started.
MissedClicks: The number of missed ticks.
Time: The precise date and time the event was triggered.
TimerMode Enum
OneShot: The timer fires once after the specified period.
Periodic: The timer fires repeatedly at the specified interval.
TimerProc Delegate
A delegate used for handling multimedia timer events.

License
This library is released under the MIT License. Feel free to use and modify it for personal or commercial projects.

For further details and advanced use cases, refer to the source code comments and Unit test code provided.