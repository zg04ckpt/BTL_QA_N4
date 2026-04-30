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
          return placemarks[0];
        }
      } catch (e) {
        print("Error fetching location name");
      }

      return null;
    }
    return null;
  }

  static Future<bool> checkAndRequestLocationPermission() async {
    var locationPermission = await Permission.locationWhenInUse.status;

    if (locationPermission.isGranted) {
      return true;
    }

    if (locationPermission.isDenied || locationPermission.isRestricted) {
      locationPermission = await Permission.locationWhenInUse.request();
    }

    if (locationPermission.isPermanentlyDenied) {
      await openAppSettings();
      return false;
    }

    return locationPermission.isGranted;
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
