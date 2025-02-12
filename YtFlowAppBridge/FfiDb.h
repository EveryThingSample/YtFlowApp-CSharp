#pragma once
#include "Bridge.FfiDb.g.h"
#include "CoreFfi.h"
#include <FfiConn.h>

namespace winrt::YtFlowAppBridge::Bridge::implementation
{
    struct FfiDb : FfiDbT<FfiDb>
    {
      public:
        FfiDb();
        hstring GetStr()
        {
            return winrt::to_hstring("sss");
        }
        winrt::YtFlowAppBridge::Bridge::FfiDb Open(hstring dbPath);
        // Static method to get the singleton instance of FfiDb
        static winrt::YtFlowAppBridge::Bridge::FfiDb Current();

        bool Equals(IInspectable const &obj);
        bool IsOpened();
        winrt::YtFlowAppBridge::Bridge::FfiConn Connect();

      private:
        // FfiDb(winrt::YtFlowAppBridge::implementation::FfiDb *_ffiDb);

        // Static member variable to store the singleton instance of FfiDb
        winrt::YtFlowAppBridge::implementation::FfiDb _ffiDb;
    };
}

namespace winrt::YtFlowAppBridge::Bridge::factory_implementation
{
    struct FfiDb : FfiDbT<FfiDb, implementation::FfiDb>
    {
    };
}
