/*
Copyright (c) 2018-2026 Convergence Systems Limited

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System.Runtime.CompilerServices;

// Android permissions required for CSLibrary2026 NetFinder (UDP broadcast on port 3000).
// These attributes are read by the Android manifest merger when this library is
// used as a dependency in an Android application.
//
// NOTE: These compile only when targeting Android (net10.0-android, etc.).
// The consuming app's AndroidManifest.xml should also include these permissions
// to ensure they are present regardless of how the library is built.

#if ANDROID
// NOTE: On .NET for Android the attribute lives in Android.App (it was Android.Runtime
// up to .NET 8). Android.Manifest.Permission no longer exposes AccessChangeWifiState,
// so that one is declared by its raw permission name.
[assembly: Android.App.UsesPermission(Android.Manifest.Permission.AccessWifiState)]
[assembly: Android.App.UsesPermission(Android.Manifest.Permission.AccessNetworkState)]
[assembly: Android.App.UsesPermission("android.permission.CHANGE_WIFI_STATE")]
[assembly: Android.App.UsesPermission(Android.Manifest.Permission.Internet)]
[assembly: Android.App.UsesPermission(Android.Manifest.Permission.ChangeWifiMulticastState)]
// Plugin.BLE typically declares BLUETOOTH permissions, included here for completeness
[assembly: Android.App.UsesPermission(Android.Manifest.Permission.Bluetooth)]
[assembly: Android.App.UsesPermission(Android.Manifest.Permission.BluetoothAdmin)]
[assembly: Android.App.UsesPermission(Android.Manifest.Permission.BluetoothConnect)]
[assembly: Android.App.UsesPermission(Android.Manifest.Permission.BluetoothScan)]
[assembly: Android.App.UsesPermission(Android.Manifest.Permission.BluetoothAdvertise)]
#endif
