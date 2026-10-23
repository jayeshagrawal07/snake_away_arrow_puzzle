# Build and Release Guide — Snake Escape

## Target Platforms
- **Primary**: Android (API 24+)
- **Secondary**: iOS (iOS 14+)

## Android Build Checklist
1. Switch platform to Android in Build Settings.
2. Verify package name in ProjectSettings (`com.DefaultCompany.snakechaingame` -> `com.company.snakeescape`).
3. Set Target Architectures: ARM64 and ARMv7.
4. Set Scripting Backend: IL2CPP.
5. Create Keystore for release signing.
6. Build App Bundle (`.aab`) for Google Play.

## iOS Build Checklist
1. Switch platform to iOS in Build Settings.
2. Set Bundle Identifier and Team ID in Xcode.
3. Verify target SDK is iOS 14.0 or higher.
4. Archive in Xcode and upload to TestFlight.
