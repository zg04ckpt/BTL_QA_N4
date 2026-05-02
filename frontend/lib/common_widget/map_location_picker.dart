import 'package:cp_restaurants/services/map_tile_provider.dart';
import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart';
import 'package:provider/provider.dart';

/// Chọn vị trí bằng tâm bản đồ — **không** gọi Nominatim.
/// Tọa độ chỉ gửi ra ngoài khi người dùng nhấn [onLocationConfirmed] (không cập nhật realtime khi kéo map).
class MapLocationPicker extends StatefulWidget {
  const MapLocationPicker({
    super.key,
    required this.latitude,
    required this.longitude,
    required this.onLocationConfirmed,
    this.height = 300,
    this.initialZoom = 15,
    this.minZoom = 3,
    this.maxZoom = 22,
    this.confirmLabel = 'Xác nhận vị trí',
  });

  final double latitude;
  final double longitude;
  final void Function(double lat, double lon) onLocationConfirmed;
  final double height;
  final double initialZoom;
  final double minZoom;
  final double maxZoom;
  final String confirmLabel;

  @override
  State<MapLocationPicker> createState() => _MapLocationPickerState();
}

class _MapLocationPickerState extends State<MapLocationPicker> {
  static const LatLng _fallback = LatLng(10.762622, 106.660172);

  late final MapController _mapController;

  LatLng get _seed {
    if (widget.latitude == 0 && widget.longitude == 0) {
      return _fallback;
    }
    return LatLng(widget.latitude, widget.longitude);
  }

  @override
  void initState() {
    super.initState();
    _mapController = MapController();
  }

  void _confirm() {
    final c = _mapController.camera.center;
    widget.onLocationConfirmed(c.latitude, c.longitude);
  }

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      height: widget.height,
      child: Consumer<MapTileProvider>(
        builder: (context, tiles, _) {
          return Stack(
            fit: StackFit.expand,
            children: [
              FlutterMap(
                mapController: _mapController,
                options: MapOptions(
                  initialCenter: _seed,
                  initialZoom: widget.initialZoom,
                  minZoom: widget.minZoom,
                  maxZoom: widget.maxZoom,
                  interactionOptions: const InteractionOptions(
                    flags: InteractiveFlag.all,
                  ),
                ),
                children: [
                  tiles.tileLayer(),
                ],
              ),
              Positioned.fill(
                child: IgnorePointer(
                  child: Align(
                    alignment: Alignment.center,
                    child: Padding(
                      padding: const EdgeInsets.only(bottom: 24),
                      child: Icon(
                        Icons.location_on,
                        size: 48,
                        color: Theme.of(context).colorScheme.error,
                      ),
                    ),
                  ),
                ),
              ),
              Positioned(
                left: 8,
                right: 8,
                bottom: 8,
                child: Material(
                  elevation: 2,
                  borderRadius: BorderRadius.circular(8),
                  color: Theme.of(context).colorScheme.surface,
                  child: Padding(
                    padding: const EdgeInsets.symmetric(
                        horizontal: 8, vertical: 6),
                    child: SizedBox(
                      width: double.infinity,
                      child: FilledButton.icon(
                        onPressed: _confirm,
                        icon: const Icon(Icons.check_circle_outline, size: 20),
                        label: Text(widget.confirmLabel),
                      ),
                    ),
                  ),
                ),
              ),
            ],
          );
        },
      ),
    );
  }
}
