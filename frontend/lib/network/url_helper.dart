import 'package:cp_restaurants/network/api_util.dart';

/// Ghép [path] từ API (relative hoặc đầy đủ) với base URL — tránh // kép và URL sai.
String resolveMediaUrl(String? path) {
  if (path == null || path.trim().isEmpty) return '';
  final p = path.trim();
  if (p.startsWith('http://') || p.startsWith('https://')) return p;
  final base = APIService.instance.baseUrl;
  final b = base.endsWith('/') ? base.substring(0, base.length - 1) : base;
  final suffix = p.startsWith('/') ? p : '/$p';
  return '$b$suffix';
}
