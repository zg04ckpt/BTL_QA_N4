import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart';

/// OSM tiles (flutter_map) — works without Google Maps API billing/restrictions.
class LocationPreviewMap extends StatelessWidget {
  final double lat;
  final double lon;
  final double height;

  const LocationPreviewMap({
    super.key,
    required this.lat,
    required this.lon,
    this.height = 300,
  });

  @override
  Widget build(BuildContext context) {
    final center = LatLng(lat, lon);
    return SizedBox(
      height: height,
      child: FlutterMap(
        options: MapOptions(
          initialCenter: center,
          initialZoom: 15,
          minZoom: 3,
          maxZoom: 19,
          keepAlive: true,
        ),
        children: [
          TileLayer(
            urlTemplate: 'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
            userAgentPackageName: 'com.example.cp_restaurants',
            maxZoom: 19,
          ),
          MarkerLayer(
            markers: [
              Marker(
                point: center,
                width: 44,
                height: 44,
                alignment: Alignment.bottomCenter,
                child: const Icon(Icons.place, color: Colors.red, size: 40),
              ),
            ],
          ),
        ],
      ),
    );
  }
}
