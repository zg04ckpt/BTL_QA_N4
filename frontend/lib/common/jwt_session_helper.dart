import 'package:cp_restaurants/network/api_mapper.dart';

/// Reads the numeric application user id from a decoded JWT payload.
///
/// Backend ([UserService.LoginAsync]) sets claim **"Id"** to the user primary key.
/// **`sub`** is [JwtRegisteredClaimNames.Sub] from config — not the user id — so we
/// only treat `sub` as user id when it is purely numeric (some stacks use it that way).
class JwtSessionHelper {
  JwtSessionHelper._();

  static const String _nameIdClaimUri =
      'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier';

  /// Returns a positive user id, or null if the token does not contain one.
  static int? parseUserId(Map<String, dynamic> decoded) {
    for (final key in ['Id', 'id', 'nameid', _nameIdClaimUri]) {
      final v = decoded[key];
      if (v == null) continue;
      final id = ApiMapper.asInt(v, fallback: -1);
      if (id > 0) return id;
    }
    final sub = decoded['sub'];
    if (sub != null) {
      final s = sub.toString().trim();
      if (RegExp(r'^\d+$').hasMatch(s)) {
        final id = int.parse(s);
        if (id > 0) return id;
      }
    }
    return null;
  }
}
