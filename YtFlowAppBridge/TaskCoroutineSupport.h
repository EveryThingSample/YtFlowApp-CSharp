// TaskCoroutineSupport.h
#pragma once

#include <coroutine>  // For coroutine support
#include <exception>  // For exception handling
#include <future>     // For std::promise
#include <ppltasks.h> // For Concurrency::task

// ====================================================
// Step 1: Define the promise_type for Concurrency::task
// ====================================================

template <typename T> struct TaskPromise
{
    std::promise<T> promise; // Used to store the result or exception

    // Returns a Concurrency::task that wraps the future from the promise
    Concurrency::task<T> get_return_object()
    {
        return Concurrency::task<T>(promise.get_future());
    }

    // Coroutine does not suspend at the start
    std::suspend_never initial_suspend() noexcept
    {
        return {};
    }

    // Coroutine does not suspend at the end
    std::suspend_never final_suspend() noexcept
    {
        return {};
    }

    // Called when the coroutine returns a value
    void return_value(T value)
    {
        promise.set_value(std::move(value)); // Set the value in the promise
    }

    // Called when an unhandled exception occurs in the coroutine
    void unhandled_exception()
    {
        promise.set_exception(std::current_exception()); // Set the exception in the promise
    }
};

// Specialize std::coroutine_traits for Concurrency::task
namespace std
{
    template <typename T, typename... Args> struct coroutine_traits<Concurrency::task<T>, Args...>
    {
        using promise_type = TaskPromise<T>; // Use TaskPromise as the promise_type
    };
}

// ====================================================
// Step 2: Define the awaitable adapter for Concurrency::task
// ====================================================

template <typename T> struct TaskAwaiter
{
    Concurrency::task<T> task;

    // Check if the task is already completed
    bool await_ready() const noexcept
    {
        return task.is_done(); // Return true if the task is already done
    }
    // Suspend the coroutine and schedule the task to resume it when done
    void await_suspend(std::coroutine_handle<> handle) const
    {
        task.then([handle]() mutable {
            handle.resume(); // Resume the coroutine when the task completes
        });
    }

    // Return the result of the task when the coroutine resumes
    T await_resume() const
    {
        return task.get(); // Get the result of the task
    }
};

// Overload operator co_await for Concurrency::task
template <typename T> TaskAwaiter<T> operator co_await(Concurrency::task<T> task) noexcept
{
    return TaskAwaiter<T>{std::move(task)};
}

namespace winrt::Windows::Foundation
{
    template <typename T> struct std::coroutine_traits<Concurrency::task<T>>
    {
        struct promise_type
        {
            Concurrency::task<T> get_return_object()
            {
                return Concurrency::task<T>{};
            }
            std::suspend_never initial_suspend() noexcept
            {
                return {};
            }
            std::suspend_never final_suspend() noexcept
            {
                return {};
            }
            void return_value(T value) noexcept
            {
            }
            void unhandled_exception() noexcept
            {
                std::terminate();
            }
        };
    };

}

template <typename T> struct task_awaiter
{
    Concurrency::task<T> m_task;

    bool await_ready() const noexcept
    {
        return m_task.is_done();
    }

    void await_suspend(std::coroutine_handle<> h) const
    {
        m_task.then([h](T) { h.resume(); });
    }
    T await_resume()
    {
        return m_task.get();
    }
};

template <typename T> task_awaiter<T> operator co_await(Concurrency::task<T> t)
{
    return task_awaiter<T>{std::move(t)};
}
