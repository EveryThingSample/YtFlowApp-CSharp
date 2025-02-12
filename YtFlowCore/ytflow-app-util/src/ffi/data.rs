use std::ffi::CStr;
use std::os::raw::c_char;
use std::panic::AssertUnwindSafe;
use std::ptr::null_mut;

#[allow(non_camel_case_types)]
use ytflow::data::{Connection as ytflow_connection, Database as ytflow_database};
use ytflow::data::{
    DataError, Plugin, Profile, Proxy, ProxyGroup, ProxySubscription, Resource,
    ResourceGitHubRelease, ResourceUrl,
};

use crate::profile::{export_profile_toml, parse_profile_toml};

use super::error::ytflow_result;
use super::interop::{serialize_buffer, serialize_string_buffer};

#[no_mangle]
#[cfg(windows)]
pub unsafe extern "C" fn ytflow_db_new_win32(path: *const u16, len: usize) -> ytflow_result {
    use std::ffi::OsString;
    use std::os::windows::ffi::OsStringExt;
    ytflow_result::catch_result_unwind(move || {
        let path = unsafe { OsString::from_wide(std::slice::from_raw_parts(path, len)) };
        ytflow::data::Database::open(path).map(|db| (Box::into_raw(Box::new(db)) as *mut _, 0))
    })
}

#[no_mangle]
#[cfg(unix)]
pub unsafe extern "C" fn ytflow_db_new_unix(path: *const u8, len: usize) -> ytflow_result {
    use std::ffi::OsStr;
    use std::os::unix::ffi::OsStrExt;
    ytflow_result::catch_result_unwind(move || {
        let path = unsafe { OsStr::from_bytes(std::slice::from_raw_parts(path, len)) };
        ytflow::data::Database::open(path).map(|db| (Box::into_raw(Box::new(db)) as *mut _, 0))
    })
}

#[no_mangle]
pub unsafe extern "C" fn ytflow_db_free(db: *mut ytflow_database) -> ytflow_result {
    ytflow_result::catch_ptr_unwind(move || {
        unsafe { drop(Box::from_raw(db)) };
        (null_mut(), 0)
    })
}

#[no_mangle]
pub unsafe extern "C" fn ytflow_db_conn_new(db: *const ytflow_database) -> ytflow_result {
    ytflow_result::catch_result_unwind(move || {
        let db = unsafe { &*db };
        db.connect()
            .map(|conn| (Box::into_raw(Box::new(conn)) as *mut _, 0))
    })
}

#[no_mangle]
pub unsafe extern "C" fn ytflow_db_conn_free(conn: *mut ytflow_connection) -> ytflow_result {
    ytflow_result::catch_ptr_unwind(AssertUnwindSafe(move || {
        unsafe { drop(Box::from_raw(conn)) };
        (null_mut(), 0)
    }))
}

#[no_mangle]
pub unsafe extern "C" fn ytflow_profiles_get_all(conn: *const ytflow_connection) -> ytflow_result {
    ytflow_result::catch_result_unwind(AssertUnwindSafe(move || {
        let conn = unsafe { &*conn };
        Profile::query_all(conn).map(|p| serialize_buffer(&p))
    }))
}

#[no_mangle]
pub unsafe extern "C" fn ytflow_plugins_get_by_profile(
    profile_id: u32,
    conn: *const ytflow_connection,
) -> ytflow_result {
    ytflow_result::catch_result_unwind(AssertUnwindSafe(move || {
        let conn = unsafe { &*conn };
        Plugin::query_all_by_profile(profile_id.into(), conn).map(|p| serialize_buffer(&p))
    }))
}

#[no_mangle]
pub unsafe extern "C" fn ytflow_plugins_get_entry(
    profile_id: u32,
    conn: *const ytflow_connection,
) -> ytflow_result {
    ytflow_result::catch_result_unwind(AssertUnwindSafe(move || {
        let conn = unsafe { &*conn };
        Plugin::query_entry_by_profile(profile_id.into(), conn).map(|p| serialize_buffer(&p))
    }))
}

#[no_mangle]
pub unsafe extern "C" fn ytflow_profile_create(
    name: *const c_char,
    locale: *const c_char,
    conn: *const ytflow_connection,
) -> ytflow_result {
    ytflow_result::catch_result_unwind(AssertUnwindSafe(move || {
        let name = unsafe { CStr::from_ptr(name) };
        let locale = unsafe { CStr::from_ptr(locale) };
        let conn = unsafe { &*conn };
        Profile::create(
            name.to_string_lossy().into_owned(),
            locale.to_string_lossy().into_owned(),
            conn,
        )
        .map(|id| (id as _, 0))
    }))
}

#[no_mangle]
pub unsafe extern "C" fn ytflow_profile_update(
    profile_id: u32,
    name: *const c_char,
    locale: *const c_char,
    conn: *const ytflow_connection,
) -> ytflow_result {
    ytflow_result::catch_result_unwind(AssertUnwindSafe(move || {
        let name = unsafe { CStr::from_ptr(name) };
        let locale = unsafe { CStr::from_ptr(locale) };
        let conn = unsafe { &*conn };
        Profile::update(
            profile_id,
            name.to_string_lossy().into_owned(),
            locale.to_string_lossy().into_owned(),
            conn,
        )
        .map(|()| (null_mut(), 0))
    }))
}

#[no_mangle]
pub unsafe extern "C" fn ytflow_profile_delete(
    profile_id: u32,
    conn: *const ytflow_connection,
) -> ytflow_result {
    ytflow_result::catch_result_unwind(AssertUnwindSafe(move || {
        let conn = unsafe { &*conn };
        Profile::delete(profile_id, conn).map(|()| (null_mut(), 0))
    }))
}

#[no_mangle]
pub unsafe extern "C" fn ytflow_profile_export_toml(
    profile_id: u32,
    conn: *const ytflow_connection,
) -> ytflow_result {
    ytflow_result::catch_result_unwind(AssertUnwindSafe(move || {
        let conn = unsafe { &*conn };
        export_profile_toml(profile_id.into(), conn)
            .map(|p| p.map(serialize_string_buffer).unwrap_or((null_mut(), 0)))
    }))
}

#[no_mangle]
pub unsafe extern "C" fn ytflow_profile_parse_toml(
    toml: *const u8,
    toml_len: usize,
) -> ytflow_result {
    ytflow_result::catch_result_unwind(AssertUnwindSafe(move || {
        let toml = unsafe { std::slice::from_raw_parts(toml, toml_len) };
        parse_profile_toml(toml).map(|p| serialize_buffer(&p))
    }))
}

#[no_mangle]
pub unsafe extern "C" fn ytflow_plugin_create(
    profile_id: u32,
    name: *const c_char,
    desc: *const c_char,
    plugin: *const c_char,
    plugin_version: u16,
    param: *const u8,
    param_len: usize,
    conn: *const ytflow_connection,
) -> ytflow_result {
    ytflow_result::catch_result_unwind(AssertUnwindSafe(move || {
        let name = unsafe { CStr::from_ptr(name) };
        let desc = unsafe { CStr::from_ptr(desc) };
        let plugin = unsafe { CStr::from_ptr(plugin) };
        let conn = unsafe { &*conn };
        Plugin::create(
            profile_id.into(),
            name.to_string_lossy().into_owned(),
            desc.to_string_lossy().into_owned(),
            plugin.to_string_lossy().into_owned(),
            plugin_version,
            unsafe { std::slice::from_raw_parts(param, param_len).to_vec() },
            conn,
        )
        .map(|id| (id as _, 0))
    }))
}

#[no_mangle]
pub unsafe extern "C" fn ytflow_plugin_update(
    plugin_id: u32,
    profile_id: u32,
    name: *const c_char,
    desc: *const c_char,
    plugin: *const c_char,
    plugin_version: u16,
    param: *const u8,
    param_len: usize,
    conn: *const ytflow_connection,
) -> ytflow_result {
    ytflow_result::catch_result_unwind(AssertUnwindSafe(move || {
        let name = unsafe { CStr::from_ptr(name) };
        let desc = unsafe { CStr::from_ptr(desc) };
        let plugin = unsafe { CStr::from_ptr(plugin) };
        let conn = unsafe { &*conn };
        Plugin::update(
            plugin_id,
            profile_id.into(),
            name.to_string_lossy().into_owned(),
            desc.to_string_lossy().into_owned(),
            plugin.to_string_lossy().into_owned(),
            plugin_version,
            unsafe { std::slice::from_raw_parts(param, param_len).to_vec() },
            conn,
        )
        .map(|()| (null_mut(), 0))
    }))
}

#[no_mangle]
pub unsafe extern "C" fn ytflow_plugin_delete(
    plugin_id: u32,
    conn: *const ytflow_connection,
) -> ytflow_result {
    ytflow_result::catch_result_unwind(AssertUnwindSafe(move || {
        let conn = unsafe { &*conn };
        Plugin::delete(plugin_id, conn).map(|()| (null_mut(), 0))
    }))
}

#[no_mangle]
pub unsafe extern "C" fn ytflow_plugin_set_as_entry(
    plugin_id: u32,
    profile_id: u32,
    conn: *const ytflow_connection,
) -> ytflow_result {
    ytflow_result::catch_result_unwind(AssertUnwindSafe(move || {
        let conn = unsafe { &*conn };
        Plugin::set_as_entry(profile_id.into(), plugin_id.into(), conn).map(|()| (null_mut(), 0))
    }))
}

#[no_mangle]
pub unsafe extern "C" fn ytflow_plugin_unset_as_entry(
    plugin_id: u32,
    profile_id: u32,
    conn: *const ytflow_connection,
) -> ytflow_result {
    ytflow_result::catch_result_unwind(AssertUnwindSafe(move || {
        let conn = unsafe { &*conn };
        Plugin::unset_as_entry(profile_id.into(), plugin_id.into(), conn).map(|()| (null_mut(), 0))
    }))
}

#[no_mangle]
pub unsafe extern "C" fn ytflow_proxy_group_get_all(
    conn: *const ytflow_connection,
) -> ytflow_result {
    ytflow_result::catch_result_unwind(AssertUnwindSafe(move || {
        let conn = unsafe { &*conn };
        ProxyGroup::query_all(conn).map(|p| serialize_buffer(&p))
    }))
}

#[no_mangle]
pub unsafe extern "C" fn ytflow_proxy_group_get_by_id(
    proxy_group_id: u32,
    conn: *const ytflow_connection,
) -> ytflow_result {
    ytflow_result::catch_result_unwind(AssertUnwindSafe(move || {
        let conn = unsafe { &*conn };
        ProxyGroup::query_by_id(proxy_group_id as usize, conn).map(|p| serialize_buffer(&p))
    }))
}

#[no_mangle]
pub unsafe extern "C" fn ytflow_proxy_group_create(
    name: *const c_char,
    r#type: *const c_char,
    conn: *const ytflow_connection,
) -> ytflow_result {
    ytflow_result::catch_result_unwind(AssertUnwindSafe(move || {
        let name = unsafe { CStr::from_ptr(name) };
        let r#type = unsafe { CStr::from_ptr(r#type) };
        let conn = unsafe { &*conn };
        ProxyGroup::create(
            name.to_string_lossy().into_owned(),
            r#type.to_string_lossy().into_owned(),
            conn,
        )
        .map(|id| (id as _, 0))
    }))
}

#[no_mangle]
pub unsafe extern "C" fn ytflow_proxy_group_create_subscription(
    name: *const c_char,
    format: *const c_char,
    url: *const c_char,
    conn: *mut ytflow_connection,
) -> ytflow_result {
    ytflow_result::catch_result_unwind(AssertUnwindSafe(move || {
        let name = unsafe { CStr::from_ptr(name) };
        let format = unsafe { CStr::from_ptr(format) };
        let url = unsafe { CStr::from_ptr(url) };
        let conn = unsafe { &mut *conn };
        ProxyGroup::create_subscription(
            name.to_string_lossy().into_owned(),
            format.to_string_lossy().into_owned(),
            url.to_string_lossy().into_owned(),
            conn,
        )
        .map(|id| (id as _, 0))
    }))
}

#[no_mangle]
pub unsafe extern "C" fn ytflow_proxy_group_rename(
    proxy_group_id: u32,
    name: *const c_char,
    conn: *const ytflow_connection,
) -> ytflow_result {
    ytflow_result::catch_result_unwind(AssertUnwindSafe(move || {
        let name = unsafe { CStr::from_ptr(name) };
        let conn = unsafe { &*conn };
        ProxyGroup::rename(proxy_group_id, name.to_string_lossy().into_owned(), conn)
            .map(|()| (null_mut(), 0))
    }))
}

#[no_mangle]
pub unsafe extern "C" fn ytflow_proxy_group_delete(
    proxy_group_id: u32,
    conn: *const ytflow_connection,
) -> ytflow_result {
    ytflow_result::catch_result_unwind(AssertUnwindSafe(move || {
        let conn = unsafe { &*conn };
        ProxyGroup::delete(proxy_group_id, conn).map(|()| (null_mut(), 0))
    }))
}

#[no_mangle]
pub unsafe extern "C" fn ytflow_proxy_subscription_query_by_proxy_group_id(
    proxy_group_id: u32,
    conn: *const ytflow_connection,
) -> ytflow_result {
    ytflow_result::catch_result_unwind(AssertUnwindSafe(move || {
        let conn = unsafe { &*conn };
        ProxySubscription::query_by_proxy_group_id(proxy_group_id, conn)
            .map(|p| serialize_buffer(&p))
    }))
}

#[no_mangle]
pub unsafe extern "C" fn ytflow_proxy_subscription_update_retrieved_by_proxy_group_id(
    proxy_group_id: u32,
    upload_bytes_used: *const u64,
    download_bytes_used: *const u64,
    bytes_total: *const u64,
    expires_at: *const c_char,
    conn: *const ytflow_connection,
) -> ytflow_result {
    ytflow_result::catch_result_unwind(AssertUnwindSafe(move || {
        let upload_bytes_used = unsafe { upload_bytes_used.as_ref().copied() };
        let download_bytes_used = unsafe { download_bytes_used.as_ref().copied() };
        let bytes_total = unsafe { bytes_total.as_ref().copied() };
        let expires_at = if expires_at.is_null() {
            None
        } else {
            Some(unsafe { CStr::from_ptr(expires_at).to_string_lossy().into_owned() })
        };
        let conn = unsafe { &*conn };
        ProxySubscription::update_retrieved_by_proxy_group_id(
            proxy_group_id,
            upload_bytes_used,
            download_bytes_used,
            bytes_total,
            expires_at,
            conn,
        )
        .map(|()| (null_mut(), 0))
    }))
}

#[no_mangle]
pub unsafe extern "C" fn ytflow_proxy_get_by_proxy_group(
    proxy_group_id: u32,
    conn: *const ytflow_connection,
) -> ytflow_result {
    ytflow_result::catch_result_unwind(AssertUnwindSafe(move || {
        let conn = unsafe { &*conn };
        Proxy::query_all_by_group(proxy_group_id.into(), conn).map(|p| serialize_buffer(&p))
    }))
}

#[no_mangle]
pub unsafe extern "C" fn ytflow_proxy_create(
    proxy_group_id: u32,
    name: *const c_char,
    proxy: *const u8,
    proxy_len: usize,
    proxy_version: u16,
    conn: *const ytflow_connection,
) -> ytflow_result {
    ytflow_result::catch_result_unwind(AssertUnwindSafe(move || {
        let name = unsafe { CStr::from_ptr(name) };
        let conn = unsafe { &*conn };
        Proxy::create(
            proxy_group_id.into(),
            name.to_string_lossy().into_owned(),
            unsafe { std::slice::from_raw_parts(proxy, proxy_len).to_vec() },
            proxy_version,
            conn,
        )
        .map(|id| (id as _, 0))
    }))
}

#[no_mangle]
pub unsafe extern "C" fn ytflow_proxy_update(
    proxy_id: u32,
    name: *const c_char,
    proxy: *const u8,
    proxy_len: usize,
    proxy_version: u16,
    conn: *const ytflow_connection,
) -> ytflow_result {
    ytflow_result::catch_result_unwind(AssertUnwindSafe(move || {
        let name = unsafe { CStr::from_ptr(name) };
        let conn = unsafe { &*conn };
        Proxy::update(
            proxy_id,
            name.to_string_lossy().into_owned(),
            unsafe { std::slice::from_raw_parts(proxy, proxy_len).to_vec() },
            proxy_version,
            conn,
        )
        .map(|()| (null_mut(), 0))
    }))
}

#[no_mangle]
pub unsafe extern "C" fn ytflow_proxy_delete(
    proxy_id: u32,
    conn: *const ytflow_connection,
) -> ytflow_result {
    ytflow_result::catch_result_unwind(AssertUnwindSafe(move || {
        let conn = unsafe { &*conn };
        Proxy::delete(proxy_id, conn).map(|()| (null_mut(), 0))
    }))
}

#[no_mangle]
pub unsafe extern "C" fn ytflow_proxy_reorder(
    proxy_group_id: u32,
    range_start_order: i32,
    range_end_order: i32,
    moves: i32,
    conn: *mut ytflow_connection,
) -> ytflow_result {
    ytflow_result::catch_result_unwind(AssertUnwindSafe(move || {
        let conn = unsafe { &mut *conn };
        Proxy::reorder(
            proxy_group_id.into(),
            range_start_order,
            range_end_order,
            moves,
            conn,
        )
        .map(|()| (null_mut(), 0))
    }))
}

#[no_mangle]
pub unsafe extern "C" fn ytflow_proxy_batch_update_by_group(
    proxy_group_id: u32,
    new_proxies_buf: *const u8,
    new_proxies_buf_len: usize,
    conn: *mut ytflow_connection,
) -> ytflow_result {
    ytflow_result::catch_result_unwind(AssertUnwindSafe(move || {
        let new_proxies_buf = if new_proxies_buf_len == 0 {
            &[][..]
        } else {
            unsafe { std::slice::from_raw_parts(new_proxies_buf, new_proxies_buf_len) }
        };
        let new_proxies =
            cbor4ii::serde::from_slice(new_proxies_buf).map_err(|_| DataError::InvalidData {
                domain: "proxies update",
                field: "proxies_buf",
            })?;
        let conn = unsafe { &mut *conn };
        Proxy::batch_update_by_group(proxy_group_id.into(), new_proxies, conn)
            .map(|()| (null_mut(), 0))
    }))
}

#[no_mangle]
pub unsafe extern "C" fn ytflow_resource_get_all(conn: *const ytflow_connection) -> ytflow_result {
    ytflow_result::catch_result_unwind(AssertUnwindSafe(move || {
        let conn = unsafe { &*conn };
        Resource::query_all(conn).map(|r| serialize_buffer(&r))
    }))
}

#[no_mangle]
pub unsafe extern "C" fn ytflow_resource_delete(
    resource_id: u32,
    conn: *const ytflow_connection,
) -> ytflow_result {
    ytflow_result::catch_result_unwind(AssertUnwindSafe(move || {
        let conn = unsafe { &*conn };
        Resource::delete(resource_id, conn).map(|()| (null_mut(), 0))
    }))
}

#[no_mangle]
pub unsafe extern "C" fn ytflow_resource_create_with_url(
    key: *const c_char,
    r#type: *const c_char,
    local_file: *const c_char,
    url: *const c_char,
    conn: *mut ytflow_connection,
) -> ytflow_result {
    ytflow_result::catch_result_unwind(AssertUnwindSafe(move || {
        let key = unsafe { CStr::from_ptr(key) };
        let r#type = unsafe { CStr::from_ptr(r#type) };
        let local_file = unsafe { CStr::from_ptr(local_file) };
        let url = unsafe { CStr::from_ptr(url) };
        let conn = unsafe { &mut *conn };
        Resource::create_with_url(
            key.to_string_lossy().into_owned(),
            r#type.to_string_lossy().into_owned(),
            local_file.to_string_lossy().into_owned(),
            url.to_string_lossy().into_owned(),
            conn,
        )
        .map(|id| (id as _, 0))
    }))
}

#[no_mangle]
pub unsafe extern "C" fn ytflow_resource_create_with_github_release(
    key: *const c_char,
    r#type: *const c_char,
    local_file: *const c_char,
    github_username: *const c_char,
    github_repo: *const c_char,
    asset_name: *const c_char,
    conn: *mut ytflow_connection,
) -> ytflow_result {
    ytflow_result::catch_result_unwind(AssertUnwindSafe(move || {
        let key = unsafe { CStr::from_ptr(key) };
        let r#type = unsafe { CStr::from_ptr(r#type) };
        let local_file = unsafe { CStr::from_ptr(local_file) };
        let github_username = unsafe { CStr::from_ptr(github_username) };
        let github_repo = unsafe { CStr::from_ptr(github_repo) };
        let asset_name = unsafe { CStr::from_ptr(asset_name) };
        let conn = unsafe { &mut *conn };
        Resource::create_with_github_release(
            key.to_string_lossy().into_owned(),
            r#type.to_string_lossy().into_owned(),
            local_file.to_string_lossy().into_owned(),
            github_username.to_string_lossy().into_owned(),
            github_repo.to_string_lossy().into_owned(),
            asset_name.to_string_lossy().into_owned(),
            conn,
        )
        .map(|id| (id as _, 0))
    }))
}

#[no_mangle]
pub unsafe extern "C" fn ytflow_resource_url_query_by_resource_id(
    resource_id: u32,
    conn: *const ytflow_connection,
) -> ytflow_result {
    ytflow_result::catch_result_unwind(AssertUnwindSafe(move || {
        let conn = unsafe { &*conn };
        ResourceUrl::query_by_resource_id(resource_id, conn).map(|r| serialize_buffer(&r))
    }))
}

#[no_mangle]
pub unsafe extern "C" fn ytflow_resource_url_update_retrieved_by_resource_id(
    resource_id: u32,
    etag: *const c_char,
    last_modified: *const c_char,
    conn: *const ytflow_connection,
) -> ytflow_result {
    ytflow_result::catch_result_unwind(AssertUnwindSafe(move || {
        let etag = if etag.is_null() {
            None
        } else {
            Some(
                unsafe { CStr::from_ptr(etag) }
                    .to_string_lossy()
                    .into_owned(),
            )
        };
        let last_modified = if last_modified.is_null() {
            None
        } else {
            Some({
                unsafe { CStr::from_ptr(last_modified) }
                    .to_string_lossy()
                    .into_owned()
            })
        };
        let conn = unsafe { &*conn };
        ResourceUrl::update_retrieved_by_resource_id(resource_id, etag, last_modified, conn)
            .map(|()| (null_mut(), 0))
    }))
}

#[no_mangle]
pub unsafe extern "C" fn ytflow_resource_github_release_query_by_resource_id(
    resource_id: u32,
    conn: *const ytflow_connection,
) -> ytflow_result {
    ytflow_result::catch_result_unwind(AssertUnwindSafe(move || {
        let conn = unsafe { &*conn };
        ResourceGitHubRelease::query_by_resource_id(resource_id, conn).map(|r| serialize_buffer(&r))
    }))
}

#[no_mangle]
pub unsafe extern "C" fn ytflow_resource_github_release_update_retrieved_by_resource_id(
    resource_id: u32,
    git_tag: *const c_char,
    release_title: *const c_char,
    conn: *const ytflow_connection,
) -> ytflow_result {
    ytflow_result::catch_result_unwind(AssertUnwindSafe(move || {
        let git_tag = unsafe { CStr::from_ptr(git_tag) };
        let release_title = unsafe { CStr::from_ptr(release_title) };
        let conn = unsafe { &*conn };
        ResourceGitHubRelease::update_retrieved_by_resource_id(
            resource_id,
            git_tag.to_string_lossy().into_owned(),
            release_title.to_string_lossy().into_owned(),
            conn,
        )
        .map(|()| (null_mut(), 0))
    }))
}
