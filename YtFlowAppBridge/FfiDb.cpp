#include "pch.h"
#include "FfiDb.h"
#include "Bridge.FfiDb.g.cpp"
#include "extension.h"
namespace winrt::YtFlowAppBridge::Bridge::implementation
{
    // FfiDb *FfiDb::s_current; // Initialize the static pointer to nullptr

    // winrt::YtFlowAppBridge::implementation::FfiDb FfiDb::_ffiDb;

    FfiDb::FfiDb()
    {
    }

    winrt::YtFlowAppBridge::Bridge::FfiDb implementation::FfiDb::Open(hstring dbPath)
    {
        std::wstring_view wstrView{dbPath.c_str()};
        // s_current = new FfiDb(winrt::YtFlowAppBridge::implementation::FfiDb::Open(dbPath));
        WriteLine("open path:" + ToStdString(dbPath));
        _ffiDb = winrt::YtFlowAppBridge::implementation::FfiDb::Open(wstrView);
        WriteLine("success open");
        return *this;
        //*s_current;
    }

    // Static method to get the singleton instance of FfiDb
    winrt::YtFlowAppBridge::Bridge::FfiDb FfiDb::Current()
    {
        // Return the current static instance
        static auto instance = winrt::make<FfiDb>();
        return instance;
    }

    // Equals method that compares the current object with another object
    bool FfiDb::Equals(IInspectable const &obj)
    {
        // First check if obj is nullptr
        if (!obj)
        {
            return false;
        }

        // Check if the passed object is of type FfiDb
        if (auto other = obj.try_as<FfiDb>())
        {
            // return other.get() == this;
            //  Now that we know it's the same type, compare the objects
            //  For simplicity, we will assume they are equal (in a real use case, compare internal state)
            // return this->_ffiDb == other->_ffiDb;
            return true;
        }

        return false; // Return false if the types don't match
    }

    bool implementation::FfiDb::IsOpened()
    {
        return _ffiDb != nullptr;
    }

    winrt::YtFlowAppBridge::Bridge::FfiConn implementation::FfiDb::Connect()
    {
        // WriteLine("FfiDb::Connect()");
        auto c = _ffiDb.Connect();
        // WriteLine("FfiDb::Connect() success");
        // FfiConn a(c);
        return winrt::make<FfiConn>(c);
    }

}
