import 'package:flutter/material.dart';

class AppSnackBar {
  static void loginRequired(BuildContext context, String action) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        dismissDirection: DismissDirection.up,
        behavior: SnackBarBehavior.floating,
        duration: const Duration(seconds: 2),
        backgroundColor: Colors.redAccent,
        content: Text(
          "Please login to $action",
          style: const TextStyle(
            fontSize: 15,
            color: Colors.white,
          ),
        ),
      ),
    );
  }

  /// Rút gọn exception + stack để hiển thị (toast không nên vô hạn ký tự).
  static String describeError(Object e, [StackTrace? st, int maxLen = 1400]) {
    final b = StringBuffer(e.toString());
    if (st != null) {
      b.writeln();
      b.writeln(st.toString());
    }
    final s = b.toString().trim();
    if (s.length <= maxLen) return s;
    return '${s.substring(0, maxLen)}…\n(truncated)';
  }

  static void show(BuildContext context, String message, [bool isError = false]) {
    _showSnack(
      context,
      message,
      isError: isError,
      duration: Duration(seconds: isError ? 6 : 3),
    );
  }

  /// Toast chi tiết (phiên đăng nhập / Splash): tiêu đề ngắn + mô tả dài, có cuộn.
  static void showDetailed(
    BuildContext context,
    String title,
    String detail, {
    bool isError = true,
    Duration? duration,
  }) {
    final full = '$title\n\n$detail';
    _showSnack(
      context,
      full,
      isError: isError,
      duration: duration ?? const Duration(seconds: 14),
      scrollable: true,
    );
  }

  static void _showSnack(
    BuildContext context,
    String message, {
    required bool isError,
    required Duration duration,
    bool scrollable = false,
  }) {
    final messenger = ScaffoldMessenger.of(context);
    messenger.hideCurrentSnackBar();
    messenger.showSnackBar(
      SnackBar(
        behavior: SnackBarBehavior.floating,
        duration: duration,
        backgroundColor: isError ? Colors.red.shade900 : Colors.green.shade700,
        content: scrollable
            ? ConstrainedBox(
                constraints: const BoxConstraints(maxHeight: 240),
                child: SingleChildScrollView(
                  child: SelectableText(
                    message,
                    style: const TextStyle(
                      color: Colors.white,
                      fontSize: 12,
                      height: 1.35,
                    ),
                  ),
                ),
              )
            : Text(
                message,
                style: const TextStyle(color: Colors.white, fontSize: 14),
              ),
        action: SnackBarAction(
          label: 'Đóng',
          textColor: Colors.white70,
          onPressed: () => messenger.hideCurrentSnackBar(),
        ),
      ),
    );
  }
}
