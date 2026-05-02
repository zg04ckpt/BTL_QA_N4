import 'package:cp_restaurants/common/map_coordinates.dart';
import 'package:cp_restaurants/global/global_data.dart';
import 'package:cp_restaurants/services/map_tile_provider.dart';
import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:provider/provider.dart';

/// Chọn nền OSM / Carto / vệ tinh — dùng từ nút layers trên map hoặc Hồ sơ.
Future<void> showMapTileStyleSheet(BuildContext context) async {
  await showModalBottomSheet<void>(
    context: context,
    showDragHandle: true,
    builder: (ctx) => SafeArea(
      child: Consumer<MapTileProvider>(
        builder: (ctx, mapTiles, _) {
          return Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              const Padding(
                padding: EdgeInsets.all(16),
                child: Text(
                  'Nền bản đồ',
                  style: TextStyle(fontSize: 18, fontWeight: FontWeight.w700),
                ),
              ),
              ...MapTileStyle.values.map((s) {
                return RadioListTile<MapTileStyle>(
                  value: s,
                  groupValue: mapTiles.style,
                  title: Text(mapTileStyleLabelVi(s)),
                  onChanged: (v) async {
                    if (v != null) {
                      await mapTiles.setStyle(v);
                      if (ctx.mounted) Navigator.pop(ctx);
                    }
                  },
                );
              }),
              const SizedBox(height: 8),
            ],
          );
        },
      ),
    ),
  );
}

/// Bản đồ nhúng: tile theo [MapTileProvider], marker đúng [latitude]/[longitude].
class AppEmbeddedMap extends StatelessWidget {
  const AppEmbeddedMap({
    super.key,
    required this.latitude,
    required this.longitude,
    this.height = 300,
    this.initialZoom = 16,
    this.showLayerPicker = true,
  });

  final double latitude;
  final double longitude;
  final double height;
  final double initialZoom;
  final bool showLayerPicker;

  @override
  Widget build(BuildContext context) {
    final center = resolveMapCenter(
      restaurantLat: latitude,
      restaurantLon: longitude,
      userPosition: GlobalData.instance.userPosition,
    );
    final zoom = usedRestaurantCoordsForMap(latitude, longitude)
        ? initialZoom
        : (initialZoom - 2).clamp(10.0, 18.0);

    return SizedBox(
      height: height,
      child: Consumer<MapTileProvider>(
            builder: (context, mapTiles, _) {
              return Stack(
                fit: StackFit.expand,
                children: [
                  FlutterMap(
                    options: MapOptions(
                      initialCenter: center,
                      initialZoom: zoom,
                      minZoom: 3,
                      maxZoom: 22,
                      interactionOptions: const InteractionOptions(
                        flags: InteractiveFlag.all,
                      ),
                    ),
                    children: [
                      mapTiles.tileLayer(),
                      MarkerLayer(
                        markers: [
                          Marker(
                            point: center,
                            width: 48,
                            height: 56,
                            alignment: Alignment.bottomCenter,
                            child: Icon(
                              Icons.location_on,
                              size: 48,
                              color: Theme.of(context).colorScheme.error,
                            ),
                          ),
                        ],
                      ),
                    ],
                  ),
                  if (showLayerPicker)
                    Positioned(
                      top: 8,
                      right: 8,
                      child: Material(
                        elevation: 2,
                        borderRadius: BorderRadius.circular(8),
                        color: Colors.white,
                        child: InkWell(
                          onTap: () => showMapTileStyleSheet(context),
                          borderRadius: BorderRadius.circular(8),
                          child: Padding(
                            padding: const EdgeInsets.symmetric(
                              horizontal: 10,
                              vertical: 8,
                            ),
                            child: Row(
                              mainAxisSize: MainAxisSize.min,
                              children: [
                                Icon(
                                  Icons.layers_outlined,
                                  size: 20,
                                  color: Theme.of(context).colorScheme.primary,
                                ),
                                const SizedBox(width: 6),
                                Text(
                                  mapTileStyleShortVi(mapTiles.style),
                                  style: const TextStyle(
                                    fontSize: 12,
                                    fontWeight: FontWeight.w600,
                                  ),
                                ),
                              ],
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
