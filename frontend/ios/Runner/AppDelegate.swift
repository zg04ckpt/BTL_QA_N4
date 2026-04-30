import UIKit
import Flutter
import GoogleMaps

@main
@objc class AppDelegate: FlutterAppDelegate {
  override func application(
    _ application: UIApplication,
    didFinishLaunchingWithOptions launchOptions: [UIApplication.LaunchOptionsKey: Any]?
  ) -> Bool {
    // Required for Google Maps tiles on iOS; Info.plist GMSApiKey alone is not enough.
    let plistKey = Bundle.main.object(forInfoDictionaryKey: "GMSApiKey") as? String
    let key = (plistKey?.isEmpty == false) ? plistKey! : "AIzaSyCVjyb0krhwVftbqnlE39lKWdtpKSvRRBA"
    GMSServices.provideAPIKey(key)
    GeneratedPluginRegistrant.register(with: self)
    return super.application(application, didFinishLaunchingWithOptions: launchOptions)
  }
}
