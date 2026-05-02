import 'package:flutter/foundation.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:shared_preferences/shared_preferences.dart';

/// Gói tile Mapbox/OSM — dùng cùng applicationId để OSM chấp nhận request.
const String kMapTileUserAgentPackage = 'com.example.cp_restaurants';

/// Tile cho [FlutterLocationPicker]: package không gửi User-Agent chuẩn OSM → `tile.openstreetmap.org` trả 403.
/// Carto CDN Voyager tương thích `{s}` subdomain như mặc định của widget.
const String kLocationPickerTileUrl =
    'https://{s}.basemaps.cartocdn.com/rastertiles/voyager/{z}/{x}/{y}.png';

/// Nền bản đồ — lưu trong SharedPreferences, đồng bộ toàn app.
enum MapTileStyle {
  osm,
  cartoVoyager,
  esriSatellite,
}

MapTileStyle mapTileStyleFromName(String? name) {
  if (name == null) return MapTileStyle.osm;
  for (final v in MapTileStyle.values) {
    if (v.name == name) return v;
  }
  return MapTileStyle.osm;
}

String mapTileStyleLabelVi(MapTileStyle s) {
  switch (s) {
    case MapTileStyle.osm:
      return 'Đường phố (OpenStreetMap)';
    case MapTileStyle.cartoVoyager:
      return 'Carto — nhãn rõ';
    case MapTileStyle.esriSatellite:
      return 'Vệ tinh (Esri)';
  }
}

String mapTileStyleShortVi(MapTileStyle s) {
  switch (s) {
    case MapTileStyle.osm:
      return 'OSM';
    case MapTileStyle.cartoVoyager:
      return 'Carto';
    case MapTileStyle.esriSatellite:
      return 'Vệ tinh';
  }
}

class MapTileProvider extends ChangeNotifier {
  MapTileProvider() {
    _restore();
  }

  static const _prefsKey = 'map_tile_style';

  MapTileStyle _style = MapTileStyle.osm;
  MapTileStyle get style => _style;

  Future<void> _restore() async {
    try {
      final p = await SharedPreferences.getInstance();
      final name = p.getString(_prefsKey);
      final next = mapTileStyleFromName(name);
      if (_style != next) {
        _style = next;
        notifyListeners();
      }
    } catch (_) {}
  }

  Future<void> setStyle(MapTileStyle value) async {
    if (_style == value) return;
    _style = value;
    notifyListeners();
    try {
      final p = await SharedPreferences.getInstance();
      await p.setString(_prefsKey, value.name);
    } catch (_) {}
  }

  /// Layer tile hiện tại (flutter_map 6).
  TileLayer tileLayer() {
    switch (_style) {
      case MapTileStyle.osm:
        return TileLayer(
          urlTemplate: 'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
          userAgentPackageName: kMapTileUserAgentPackage,
          maxNativeZoom: 19,
          maxZoom: 22,
        );
      case MapTileStyle.cartoVoyager:
        return TileLayer(
          urlTemplate:
              'https://{s}.basemaps.cartocdn.com/rastertiles/voyager/{z}/{x}/{y}.png',
          subdomains: const ['a', 'b', 'c', 'd'],
          userAgentPackageName: kMapTileUserAgentPackage,
          maxNativeZoom: 20,
          maxZoom: 22,
        );
      case MapTileStyle.esriSatellite:
        return TileLayer(
          urlTemplate:
              'https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}',
          userAgentPackageName: kMapTileUserAgentPackage,
          maxNativeZoom: 19,
          maxZoom: 22,
        );
    }
  }
}
