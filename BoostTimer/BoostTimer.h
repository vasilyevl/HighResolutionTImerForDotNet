#pragma once

using namespace System;

namespace Grumpy {
	namespace Utilities {
		namespace BoostTimer {

            // BoostSteadyTimerWrapper.h

            #pragma once

            #include <boost/asio.hpp>
            #include <boost/asio/io_context.hpp>
            #include <boost/asio/steady_timer.hpp>

            #include <chrono>

            using namespace System;

            namespace TimerWrapper {

                /**
                 * \brief A C++/CLI wrapper for boost::asio::steady_timer to use in .NET applications.
                 */
                public ref class SteadyTimer
                {
                private:
                    boost::asio::io_context* _ioContext;  // Correct usage is io_context, not io_service
                    boost::asio::steady_timer* _timer;
                    std::chrono::steady_clock::time_point _startTime;
                    std::chrono::steady_clock::time_point _stopTime;

                public:
                    /**
                     * \brief Constructor that initializes the timer and the io_context.
                     */
                    SteadyTimer()
                    {
                        _ioContext = new boost::asio::io_context();  // io_context is the correct type to use in modern Boost
                        _timer = new boost::asio::steady_timer(*_ioContext);
                    }

                    /**
                     * \brief Destructor that frees resources.
                     */
                    ~SteadyTimer()
                    {
                        this->!SteadyTimer();
                    }

                    /**
                     * \brief Finalizer for cleanup, in case the destructor is not called.
                     */
                    !SteadyTimer()
                    {
                        if (_timer != nullptr)
                        {
                            delete _timer;
                            _timer = nullptr;
                        }

                        if (_ioContext != nullptr)
                        {
                            delete _ioContext;
                            _ioContext = nullptr;
                        }
                    }

                    /**
                     * \brief Start the timer for measuring time intervals.
                     */
                    void Start()
                    {
                        _startTime = std::chrono::steady_clock::now();
                    }

                    /**
                     * \brief Stop the timer for measuring time intervals.
                     */
                    void Stop()
                    {
                        _stopTime = std::chrono::steady_clock::now();
                    }

                    /**
                     * \brief Get the elapsed time in seconds since the timer started.
                     * \return Elapsed time in seconds.
                     */
                    double GetElapsedSeconds()
                    {
                        auto elapsed = std::chrono::duration_cast<std::chrono::seconds>(_stopTime - _startTime);
                        return elapsed.count();
                    }

                    /**
                     * \brief Get the elapsed time in milliseconds since the timer started.
                     * \return Elapsed time in milliseconds.
                     */
                    double GetElapsedMilliseconds()
                    {
                        auto elapsed = std::chrono::duration_cast<std::chrono::milliseconds>(_stopTime - _startTime);
                        return elapsed.count();
                    }

                    /**
                     * \brief Get the elapsed time in nanoseconds since the timer started.
                     * \return Elapsed time in nanoseconds.
                     */
                    Int64 GetElapsedNanoseconds()
                    {
                        auto elapsed = std::chrono::duration_cast<std::chrono::nanoseconds>(_stopTime - _startTime);
                        return elapsed.count();
                    }

                    /**
                     * \brief Perform a blocking, synchronous wait until the timer expires.
                     */
                    void Wait()
                    {
                        _timer->wait();
                    }

                    /**
                     * \brief Set the timer to expire at a specific absolute time.
                     * \param time A DateTime object representing the expiration time.
                     */
                    void ExpiresAt(DateTime^ time)
                    {
                        // Convert DateTime^ to std::chrono::time_point (via system_clock)
                        std::chrono::system_clock::time_point expiry = std::chrono::system_clock::from_time_t((time_t)time->ToFileTime());
                        _timer->expires_at(expiry);
                    }

                    /**
                     * \brief Set the timer to expire after a certain duration from now.
                     * \param milliseconds Number of milliseconds from now until the timer expires.
                     */
                    void ExpiresFromNow(int milliseconds)
                    {
                        _timer->expires_from_now(std::chrono::milliseconds(milliseconds));
                    }

                    /**
                     * \brief Perform an asynchronous wait, and invoke the callback once the timer expires.
                     * \param milliseconds Number of milliseconds until the timer expires.
                     * \param handler A callback to be executed when the timer expires.
                     */
                    void AsyncWait(int milliseconds, Action^ handler)
                    {
                        _timer->expires_after(std::chrono::milliseconds(milliseconds));
                        _timer->async_wait([handler](const boost::system::error_code& ec)
                            {
                                if (!ec)
                                {
                                    handler();
                                }
                            });

                        _ioContext->run();
                        _ioContext->reset();
                    }

                    /**
                     * \brief Cancel all pending asynchronous wait operations on the timer.
                     * \return The number of asynchronous operations that were cancelled.
                     */
                    int Cancel()
                    {
                        return _timer->cancel();
                    }

                    /**
                     * \brief Cancel one pending asynchronous wait operation on the timer.
                     * \return The number of asynchronous operations that were cancelled.
                     */
                    int CancelOne()
                    {
                        return _timer->cancel_one();
                    }
                };
            }



		}

	}
}