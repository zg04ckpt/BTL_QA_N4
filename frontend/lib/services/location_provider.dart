// ignore_for_file: avoid_print

import 'dart:developer';

import 'package:cp_restaurants/global/global_data.dart';
import 'package:cp_restaurants/services/location_service.dart';
import 'package:flutter/cupertino.dart';
import 'package:geocoding/geocoding.dart';
import 'package:geolocator/geolocator.dart';

class LocationProvider with ChangeNotifier {
  Position? _currentPosition;
  Position? get currentPostion => _currentPosition;

  final LocationService _locationService = LocationService();

  Placemark? _currentLocationName;
  Placemark? get currentLocationName => _currentLocationName;

  Future<void> determinePosition() async {
    bool serviceEnabled;
    LocationPermission permission;

    log("time ${DateTime.now().millisecondsSinceEpoch}");

    serviceEnabled = await Geolocator.isLocationServiceEnabled();

    if (!serviceEnabled) {
      _currentPosition = null;
      notifyListeners();
      return;
    }
    log("time ${DateTime.now().millisecondsSinceEpoch}");

    permission = await Geolocator.checkPermission();

    if (permission == LocationPermission.denied) {
      permission = await Geolocator.requestPermission();

      if (permission == LocationPermission.denied) {
        _currentPosition = null;
        notifyListeners();
        return;
      }
    }

    if (permission == LocationPermission.deniedForever) {
      _currentPosition = null;
      notifyListeners();
      return;
    }

    log("time ${DateTime.now().millisecondsSinceEpoch}");

    _currentPosition = await Geolocator.getCurrentPosition(
      locationSettings: const LocationSettings(
        accuracy: LocationAccuracy.high,
      ),
    );
    print(_currentPosition);
    log("time ${DateTime.now().millisecondsSinceEpoch}");

    GlobalData.instance.userPosition = _currentPosition;

    _currentLocationName =
        await _locationService.getLocationName(_currentPosition);

    print(_currentLocationName);

    notifyListeners();
  }

  // ask the permission

  // get the location

  // get the placemark
}
