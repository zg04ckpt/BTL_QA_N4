import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:google_maps_flutter/google_maps_flutter.dart' as gms;
import 'package:latlong2/latlong.dart' as ll;

/// Full-screen location picker. **OpenStreetMap is the default** so tiles always
/// load; Google Maps is optional (requires valid API key + SDK enabled in Cloud Console).
class GoogleMapPickerView extends StatefulWidget {
  final double initialLat;
  final double initialLon;

  const GoogleMapPickerView({
    super.key,
    required this.initialLat,
    required this.initialLon,
  });

  @override
  State<GoogleMapPickerView> createState() => _GoogleMapPickerViewState();
}

class _GoogleMapPickerViewState extends State<GoogleMapPickerView> {
  late double _lat;
  late double _lng;
  late final ll.LatLng _osmBootCenter;

  /// OSM first — avoids blank grey maps when Google API key / billing is misconfigured.
  bool _useGoogleMaps = false;

  @override
  void initState() {
    super.initState();
    final rawLat =
        widget.initialLat.isFinite ? widget.initialLat : 21.0278;
    final rawLon =
        widget.initialLon.isFinite ? widget.initialLon : 105.8342;
    _lat = rawLat.clamp(-85.0, 85.0);
    _lng = rawLon.clamp(-180.0, 180.0);
    _osmBootCenter = ll.LatLng(_lat, _lng);
  }

  void _applyPick(double lat, double lng) {
    setState(() {
      _lat = lat.clamp(-85.0, 85.0);
      _lng = lng.clamp(-180.0, 180.0);
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: () => Navigator.pop(context),
        ),
        title: const Text('Chọn vị trí'),
        actions: [
          IconButton(
            tooltip: _useGoogleMaps
                ? 'Chuyển sang OpenStreetMap'
                : 'Thử Google Maps (cần API key hợp lệ)',
            onPressed: () {
              setState(() {
                _useGoogleMaps = !_useGoogleMaps;
              });
            },
            icon: Icon(_useGoogleMaps ? Icons.layers : Icons.map),
          ),
          TextButton.icon(
            onPressed: () =>
                Navigator.of(context).pop<gms.LatLng>(gms.LatLng(_lat, _lng)),
            icon: const Icon(Icons.check),
            label: const Text('Xác nhận'),
          ),
        ],
      ),
      body: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Material(
            color: Theme.of(context).colorScheme.surfaceContainerHighest,
            child: Padding(
              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
              child: Text(
                _useGoogleMaps
                    ? 'Google Maps: nếu màn hình xám, hãy bật Maps SDK + billing và đúng hạn chế API key trên Google Cloud Console, hoặc chuyển sang OSM (biểu tượng lớp).'
                    : 'OpenStreetMap: chạm vào bản đồ để đặt ghim. Tọa độ vẫn là WGS84 (dùng được cho backend như Google).',
                style: Theme.of(context).textTheme.bodySmall,
              ),
            ),
          ),
          Expanded(
            child: _useGoogleMaps ? _buildGoogleMap(context) : _buildOsmMap(),
          ),
        ],
      ),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () =>
            Navigator.of(context).pop<gms.LatLng>(gms.LatLng(_lat, _lng)),
        icon: const Icon(Icons.check_circle_outline),
        label: const Text('Dùng vị trí này'),
      ),
    );
  }

  Widget _buildOsmMap() {
    return FlutterMap(
      options: MapOptions(
        initialCenter: _osmBootCenter,
        initialZoom: 16,
        minZoom: 3,
        maxZoom: 19,
        keepAlive: true,
        onTap: (_, ll.LatLng p) => _applyPick(p.latitude, p.longitude),
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
              point: ll.LatLng(_lat, _lng),
              width: 44,
              height: 44,
              alignment: Alignment.bottomCenter,
              child: const Icon(Icons.place, color: Colors.red, size: 40),
            ),
          ],
        ),
      ],
    );
  }

  Widget _buildGoogleMap(BuildContext context) {
    final target = gms.LatLng(_lat, _lng);
    return gms.GoogleMap(
      key: ValueKey(_useGoogleMaps),
      mapType: gms.MapType.normal,
      initialCameraPosition:
          gms.CameraPosition(target: gms.LatLng(_lat, _lng), zoom: 16),
      myLocationEnabled: true,
      myLocationButtonEnabled: true,
      zoomControlsEnabled: true,
      compassEnabled: true,
      padding: EdgeInsets.only(
        top: MediaQuery.paddingOf(context).top,
        bottom: 88,
      ),
      onMapCreated: (_) {
        if (kDebugMode) {
          debugPrint('GoogleMap ready at $_lat, $_lng');
        }
      },
      onTap: (gms.LatLng p) => _applyPick(p.latitude, p.longitude),
      markers: {
        gms.Marker(
          markerId: const gms.MarkerId('picked_location'),
          position: target,
        ),
      },
    );
  }
}
