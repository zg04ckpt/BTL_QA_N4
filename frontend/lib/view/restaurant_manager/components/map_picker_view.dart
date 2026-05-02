// ignore_for_file: avoid_print

import 'package:cp_restaurants/common_widget/map_location_picker.dart';
import 'package:flutter/material.dart';

class MapPickerView extends StatefulWidget {
  const MapPickerView({super.key});

  @override
  State<MapPickerView> createState() => _MapPickerViewState();
}

class _MapPickerViewState extends State<MapPickerView> {
  double _lat = 10.762622;
  double _lon = 106.660172;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Chọn vị trí trên bản đồ'),
      ),
      body: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Padding(
            padding: const EdgeInsets.all(12),
            child: Text(
              'Tọa độ: ${_lat.toStringAsFixed(6)}, ${_lon.toStringAsFixed(6)}',
              style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w600),
            ),
          ),
          Expanded(
            child: MapLocationPicker(
              latitude: _lat,
              longitude: _lon,
              initialZoom: 11,
              minZoom: 5,
              maxZoom: 16,
              onLocationConfirmed: (la, lo) {
                setState(() {
                  _lat = la;
                  _lon = lo;
                });
                print('$la $lo');
              },
            ),
          ),
        ],
      ),
    );
  }
}
