// ignore_for_file: avoid_print
import 'package:flutter/material.dart';
import 'package:geocoding/geocoding.dart';
import 'package:geolocator/geolocator.dart';
import 'package:permission_handler/permission_handler.dart';
import 'package:url_launcher/url_launcher.dart';

class LocationService {
  Future<Placemark?> getLocationName(Position? position) async {
    if (position == null) return null;
    try {
      final placemarks = await placemarkFromCoordinates(
        position.latitude,
        position.longitude,
      );

      if (placemarks.isNotEmpty) {
        return placemarks.first;
      }
    } catch (e) {
      print("Error fetching location name: $e");
    }
    return null;
  }

  /// Xin quyền vị trí khi đang dùng app (không yêu cầu “luôn luôn”).
  static Future<bool> checkAndRequestLocationPermission() async {
    var status = await Permission.location.status;
    if (status.isGranted) return true;
    if (status.isPermanentlyDenied) return false;
    if (status.isDenied || status.isRestricted) {
      status = await Permission.location.request();
    }
    return status.isGranted;
  }

  /// Hiển thị giải thích + xin quyền + mở cài đặt nếu bị chặn vĩnh viễn.
  static Future<void> ensureLocationForApp(BuildContext context) async {
    final serviceOn = await Geolocator.isLocationServiceEnabled();
    if (!serviceOn) {
      if (!context.mounted) return;
      await showDialog<void>(
        context: context,
        builder: (ctx) => AlertDialog(
          title: const Text('Bật định vị'),
          content: const Text(
            'Ứng dụng cần định vị để hiển thị nhà hàng gần bạn. Vui lòng bật GPS/định vị trong Cài đặt.',
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(ctx),
              child: const Text('Để sau'),
            ),
            FilledButton(
              onPressed: () async {
                Navigator.pop(ctx);
                await Geolocator.openLocationSettings();
              },
              child: const Text('Mở cài đặt'),
            ),
          ],
        ),
      );
    }

    var permission = await Geolocator.checkPermission();
    if (permission == LocationPermission.denied) {
      permission = await Geolocator.requestPermission();
    }

    if (permission == LocationPermission.deniedForever) {
      if (!context.mounted) return;
      await showDialog<void>(
        context: context,
        builder: (ctx) => AlertDialog(
          title: const Text('Quyền định vị'),
          content: const Text(
            'Quyền định vị đã bị từ chối vĩnh viễn. Hãy bật trong Cài đặt > Ứng dụng.',
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(ctx),
              child: const Text('Đóng'),
            ),
            FilledButton(
              onPressed: () async {
                Navigator.pop(ctx);
                await openAppSettings();
              },
              child: const Text('Mở cài đặt app'),
            ),
          ],
        ),
      );
      return;
    }

    if (permission == LocationPermission.denied) {
      await checkAndRequestLocationPermission();
    }
  }

  static Future<void> openMap(double latitude, double longitude) async {
    final googleUrl =
        'https://www.google.com/maps/dir/?api=1&destination=$latitude,$longitude';
    if (await canLaunchUrl(Uri.parse(googleUrl))) {
      await launchUrl(Uri.parse(googleUrl));
    } else {
      throw 'Could not open the map.';
    }
  }
}
