import Foundation

#if canImport(BidscubeSDK)
import BidscubeSDK
#endif

private func normalizedUserId(_ raw: UnsafePointer<CChar>?) -> String? {
    guard let raw else { return nil }
    let value = String(cString: raw).trimmingCharacters(in: .whitespacesAndNewlines)
    return value.isEmpty ? nil : value
}

@_cdecl("BidscubeUnityNativeSyncInitialize")
public func BidscubeUnityNativeSyncInitialize(
    _ baseUrl: UnsafePointer<CChar>?,
    _ enableLogging: Bool,
    _ enableDebugMode: Bool,
    _ defaultAdTimeoutMs: Int32,
    _ userId: UnsafePointer<CChar>?
) {
#if canImport(BidscubeSDK)
    let url = baseUrl.map { String(cString: $0) } ?? "https://ssp-bcc-ads.com/sdk"
    var builder = SDKConfig.Builder()
        .baseURL(url)
        .enableLogging(enableLogging)
        .enableDebugMode(enableDebugMode)
        .defaultAdTimeout(Int(defaultAdTimeoutMs))

    if let uid = normalizedUserId(userId) {
        builder = builder.userId(uid)
    }

    if BidscubeSDK.isInitialized() {
        if let uid = normalizedUserId(userId) {
            BidscubeSDK.setUserId(uid)
        }
        return
    }

    BidscubeSDK.initialize(config: builder.build())
#endif
}

@_cdecl("BidscubeUnityNativeSetUserId")
public func BidscubeUnityNativeSetUserId(_ userId: UnsafePointer<CChar>?) {
#if canImport(BidscubeSDK)
    guard BidscubeSDK.isInitialized() else { return }
    BidscubeSDK.setUserId(normalizedUserId(userId))
#endif
}
