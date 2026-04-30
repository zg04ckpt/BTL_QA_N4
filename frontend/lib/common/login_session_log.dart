import 'dart:developer' as developer;

import 'package:flutter/foundation.dart';

/// Log cho luồng Splash / đăng nhập / GetUserById (grep `LoginSession` trong console hoặc DevTools).
void loginSessionLog(
  String message, [
  Object? error,
  StackTrace? stackTrace,
]) {
  const tag = '[LoginSession]';
  debugPrint('$tag $message');
  if (error != null) {
    debugPrint('$tag error: $error');
  }
  if (stackTrace != null) {
    debugPrint('$tag stack:\n$stackTrace');
  }
  developer.log(
    message,
    name: 'LoginSession',
    error: error,
    stackTrace: stackTrace,
  );
}
