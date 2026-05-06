# ReceiptWise - Platform Permissions Guide

## Overview
ReceiptWise requires specific permissions to access device features for receipt capture.

---

## Android Permissions

### Required Permissions
Defined in `Platforms/Android/AndroidManifest.xml`:
<uses-permission android:name="android.permission.CAMERA" /> <uses-permission android:name="android.permission.READ_MEDIA_IMAGES" /> <uses-permission android:name="android.permission.READ_EXTERNAL_STORAGE" android:maxSdkVersion="32" /> <uses-permission android:name="android.permission.WRITE_EXTERNAL_STORAGE" android:maxSdkVersion="28" /> <uses-permission android:name="android.permission.POST_NOTIFICATIONS" />

### Permission Handling

#### Camera (Android 6.0+)
- **Purpose**: Capture receipt photos
- **Runtime Permission**: Yes
- **Request Flow**:
  1. User taps "Camera" button
  2. App checks permission status
  3. If not granted, shows system permission dialog
  4. On grant, opens camera
  5. On deny, shows error message

#### Storage (Android 13+)
- **Android 13+**: Use `READ_MEDIA_IMAGES`
- **Android 10-12**: Use `READ_EXTERNAL_STORAGE`
- **Android 9 and below**: Use `WRITE_EXTERNAL_STORAGE`

### Testing Permissions

---

Grant camera permission manually
adb shell pm grant com.receiptwise.app android.permission.CAMERA
Revoke permission
adb shell pm revoke com.receiptwise.app android.permission.CAMERA
Check permission status
adb shell dumpsys package com.receiptwise.app | grep permission

## iOS Permissions

### Required Keys
Defined in `Platforms/iOS/Info.plist`:
<key>NSCameraUsageDescription</key> <string>This app needs access to the camera to capture receipt images</string>
<key>NSPhotoLibraryUsageDescription</key> <string>This app needs access to photos to import receipt images</string>
<key>NSPhotoLibraryAddUsageDescription</key> <string>This app needs permission to save receipt images to your photo library</string>

### Permission Handling
- **First Request**: iOS shows system permission dialog with custom message
- **Denial**: User must manually enable in Settings app
- **Check Status**: Use `Permissions.CheckStatusAsync<Permissions.Camera>()`

### Testing Permissions

Reset permissions in iOS Simulator:
Settings → Privacy & Security → Camera → Reset Location & Privacy

---

## macOS (Mac Catalyst)

### Required Entitlements
Add to `Platforms/MacCatalyst/Entitlements.plist`:
<key>com.apple.security.device.camera</key> <true/> <key>com.apple.security.personal-information.photos-library</key> <true/>
---

## Windows

### Capabilities
Add to `Package.appxmanifest`:
<Capabilities> <DeviceCapability Name="webcam"/> <uap:Capability Name="picturesLibrary"/> </Capabilities>
---

## Permission Flow in Code

### Camera Permission Check
var status = await Permissions.CheckStatusAsync<Permissions.Camera>(); if (status != PermissionStatus.Granted) { status = await Permissions.RequestAsync<Permissions.Camera>(); if (status != PermissionStatus.Granted) { // Permission denied - show error return; } } // Permission granted - open camera
### Photo Library Permission
var status = await Permissions.CheckStatusAsync<Permissions.Photos>(); if (status != PermissionStatus.Granted) { status = await Permissions.RequestAsync<Permissions.Photos>(); }
---

## Troubleshooting

### Android: "Permission Denied" Error
**Solution:**
1. Check `AndroidManifest.xml` has correct permissions
2. Verify target SDK version (33+)
3. Test on physical device (some emulators have issues)
4. Manually grant permissions via `adb shell`

### iOS: Permission Dialog Not Showing
**Solution:**
1. Verify `Info.plist` has usage descriptions
2. Delete and reinstall app
3. Reset simulator permissions

### Camera Not Opening
**Solution:**
1. Test on physical device (emulators may not have camera)
2. Check device camera app works independently
3. Review logs for specific error messages

---

## Best Practices

1. **Request Permissions Just-in-Time**: Only when user taps Camera/Import
2. **Explain Why**: Use clear, friendly messages
3. **Graceful Degradation**: Offer alternatives if permission denied
4. **Don't Request on Launch**: Wait for user action
5. **Test on Real Devices**: Emulators behave differently

