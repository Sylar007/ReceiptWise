# ReceiptWise - Platform Permissions Guide

## Overview
ReceiptWise requires specific permissions to access device features for receipt capture.

---

## Android Permissions

### Required Permissions
Defined in `Platforms/Android/AndroidManifest.xml`:


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

## iOS Permissions

### Required Keys
Defined in `Platforms/iOS/Info.plist`:

### Permission Handling
- **First Request**: iOS shows system permission dialog with custom message
- **Denial**: User must manually enable in Settings app
- **Check Status**: Use `Permissions.CheckStatusAsync<Permissions.Camera>()`

### Testing Permissions

Reset permissions in iOS Simulator:

---

## macOS (Mac Catalyst)

### Required Entitlements
Add to `Platforms/MacCatalyst/Entitlements.plist`:

---

## Windows

### Capabilities
Add to `Package.appxmanifest`:

---

## Permission Flow in Code

### Camera Permission Check

### Photo Library Permission

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

