// ignore_for_file: avoid_print

import 'dart:async';
import 'dart:developer';

import 'package:cp_restaurants/global/global_data.dart';
import 'package:cp_restaurants/services/location_service.dart';
import 'package:flutter/cupertino.dart';
import 'package:geocoding/geocoding.dart';
import 'package:geolocator/geolocator.dart';

class LocationProvider with ChangeNotifier {
  Position? _currentPosition;

  /// Vị trí GPS hiện tại (đã xin quyền).
  Position? get currentPosition => _currentPosition;

  final LocationService _locationService = LocationService();

  Placemark? _currentLocationName;
  Placemark? get currentLocationName => _currentLocationName;

  Future<void> determinePosition() async {
    try {
      final serviceEnabled = await Geolocator.isLocationServiceEnabled();
      if (!serviceEnabled) {
        _currentPosition = null;
        _currentLocationName = null;
        GlobalData.instance.userPosition = null;
        notifyListeners();
        return;
      }

      var permission = await Geolocator.checkPermission();
      if (permission == LocationPermission.denied) {
        permission = await Geolocator.requestPermission();
      }

      if (permission == LocationPermission.denied ||
          permission == LocationPermission.deniedForever) {
        _currentPosition = null;
        _currentLocationName = null;
        GlobalData.instance.userPosition = null;
        notifyListeners();
        return;
      }

      Position? pos;
      try {
        pos = await Geolocator.getCurrentPosition(
          desiredAccuracy: LocationAccuracy.high,
          timeLimit: const Duration(seconds: 30),
        );
      } on TimeoutException {
        log('determinePosition: timeout, trying last known position');
        pos = await Geolocator.getLastKnownPosition();
      } catch (e, st) {
        log('determinePosition: $e\n$st');
        pos = await Geolocator.getLastKnownPosition();
      }

      _currentPosition = pos;
      GlobalData.instance.userPosition = pos;

      if (pos != null) {
        _currentLocationName = await _locationService.getLocationName(pos);
      } else {
        _currentLocationName = null;
      }

      notifyListeners();
    } catch (e, st) {
      log('determinePosition failed: $e\n$st');
      _currentPosition = null;
      _currentLocationName = null;
      GlobalData.instance.userPosition = null;
      notifyListeners();
    }
  }
}
