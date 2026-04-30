import 'package:cp_restaurants/common_widget/location_preview_map.dart';
import 'package:flutter/material.dart';

class MapPickerView extends StatelessWidget {
  const MapPickerView({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: () => Navigator.pop(context),
        ),
        title: const Text('Bản đồ (OSM)'),
      ),
      body: const LocationPreviewMap(
        lat: 21.0278,
        lon: 105.8342,
        height: 400,
      ),
    );
  }
}
