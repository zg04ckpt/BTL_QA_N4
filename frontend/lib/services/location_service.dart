// ignore_for_file: avoid_print
import 'package:permission_handler/permission_handler.dart';
import 'package:geocoding/geocoding.dart';
import 'package:geolocator/geolocator.dart';
import 'package:url_launcher/url_launcher.dart';

class LocationService {
  Future<Placemark?> getLocationName(Position? position) async {
    if (position != null) {
      try {
        final placemarks = await placemarkFromCoordinates(
            position.latitude, position.longitude);

        if (placemarks.isNotEmpty) {
          return placemarks[1];
          // i changed the plaace mark index from [0] to [1]
        }
      } catch (e) {
        print("Error fetching location name");
      }

      return null;
    }
    return null;
  }

  static Future<bool> checkAndRequestLocationPermission() async {
    // Check location permission
    PermissionStatus locationPermission = await Permission.location.status;

    // Check if location permission is granted
    if (locationPermission.isGranted) {
      // Permission is already granted
      return true;
    }

    // Request permission if not granted
    if (locationPermission.isDenied || locationPermission.isRestricted) {
      locationPermission = await Permission.location.request();
    }

    // Request precise location permission (only on Android 12+)
    PermissionStatus preciseLocationPermission =
        await Permission.locationAlways.status;
    if (preciseLocationPermission.isDenied) {
      preciseLocationPermission = await Permission.locationAlways.request();
    }

    // Return true if both location and precise permissions are granted
    return locationPermission.isGranted && preciseLocationPermission.isGranted;
  }

  static Future<void> openMap(double latitude, double longitude) async {
    String googleUrl =
        'https://www.google.com/maps/dir/?api=1&destination=$latitude,$longitude';
    if (await canLaunchUrl(Uri.parse(googleUrl))) {
      await launchUrl(Uri.parse(googleUrl));
    } else {
      throw 'Could not open the map.';
    }
  }
}
