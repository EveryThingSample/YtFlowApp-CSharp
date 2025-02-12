#include "pch.h"
#include "CoreRpc.h"

#include "VectorBuffer.h"
#include <coroutine>
#include <ppltasks.h>
#include <tinycbor/cbor.h>

#include "extension.h"
//
// template <typename T> struct TaskAwaiter
//{
//    concurrency::task<T> task;
//
//    bool await_ready() const noexcept
//    {
//        return task.is_done();
//    }
//
//    void await_suspend(std::coroutine_handle<> handle) const
//    {
//        task.then([handle]() mutable { handle.resume(); });
//    }
//
//    T await_resume() const
//    {
//        return task.get();
//    }
//};
//
// template <typename T> TaskAwaiter<T> operator co_await(concurrency::task<T> task) noexcept
//{
//    return TaskAwaiter<T>{std::move(task)};
//}

using namespace nlohmann;
using namespace winrt;
using namespace Windows::Networking;
using namespace Sockets;
using namespace Windows::Storage::Streams;

namespace winrt::YtFlowAppBridge::implementation
{
    using concurrency::task;

    const char *RpcException::what() const throw()
    {
        return msg.data();
    }

    bool RpcPluginInfo::operator==(RpcPluginInfo const &that) const
    {
        return id == that.id && hashcode == that.hashcode;
    }

}
