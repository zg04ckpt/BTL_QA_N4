import 'package:cp_restaurants/common/login_session_log.dart';
import 'package:cp_restaurants/data/models/user_data.dart';
import 'package:cp_restaurants/network/api_util.dart';
import 'package:firebase_auth/firebase_auth.dart';
import 'package:geolocator/geolocator.dart';

class GlobalData {
  GlobalData._privateConstructor();

  static final GlobalData instance = GlobalData._privateConstructor();

  bool isLogin = false;

  User? user;

  UserData? userData;

  Position? userPosition ;

  Future<void> fetchUserData(String userId) async {
    final parsed = int.tryParse(userId.trim());
    if (parsed == null || parsed <= 0) {
      loginSessionLog(
        'fetchUserData: userId không parse được sang int > 0 (raw="$userId")',
      );
      userData = null;
      return;
    }
    loginSessionLog('fetchUserData: gọi GetUserById id=$parsed');
    userData = await getUserById(parsed);
    if (userData == null) {
      loginSessionLog('fetchUserData: GetUserById trả về null cho id=$parsed');
    } else {
      loginSessionLog(
        'fetchUserData: OK userId=${userData!.userId}, email=${userData!.email}',
      );
    }
  }

  /// Returns null if the user is not found (204), request failed, or JSON could not be parsed.
  Future<UserData?> getUserById(int id) async {
    try {
      final response = await APIService.instance.request(
        '/api/User/GetUserById?id=$id',
        DioMethod.get,
      );

      final code = response.statusCode;
      final dataPreview = _shortPreview(response.data);

      if (code == 200 && response.data != null) {
        final raw = response.data;
        if (raw is! Map) {
          loginSessionLog(
            'getUserById($id): HTTP 200 nhưng body không phải Map, kiểu=${raw.runtimeType}, preview=$dataPreview',
          );
          return null;
        }
        final map = Map<String, dynamic>.from(raw);
        final payload = map['data'] is Map<String, dynamic>
            ? Map<String, dynamic>.from(map['data'] as Map)
            : map;
        try {
          return UserData.fromJson(payload);
        } catch (e, st) {
          loginSessionLog(
            'getUserById($id): lỗi parse UserData.fromJson',
            e,
            st,
          );
          return null;
        }
      }

      if (code == 204) {
        loginSessionLog(
          'getUserById($id): HTTP 204 — không có user với id này trên server',
        );
        return null;
      }

      loginSessionLog(
        'getUserById($id): HTTP $code (không xử lý được), preview=$dataPreview',
      );
      return null;
    } catch (e, st) {
      loginSessionLog('getUserById($id): exception khi gọi API', e, st);
      return null;
    }
  }

  static String _shortPreview(dynamic data, [int max = 400]) {
    final s = data.toString();
    if (s.length <= max) return s;
    return '${s.substring(0, max)}…';
  }

  
}
