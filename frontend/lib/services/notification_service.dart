import 'dart:convert';
import 'dart:developer';
import 'dart:io';

import 'package:cp_restaurants/firebase_bootstrap.dart';
import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_local_notifications/flutter_local_notifications.dart';

@pragma('vm:entry-point')
Future<void> firebaseMessagingBackgroundHandler(RemoteMessage message) async {
  await ensureFirebaseInitialized();
  await setupFlutterNotifications();
  showFlutterNotification(message);

  debugPrint('Handling a background message ${message.messageId}');
}

late AndroidNotificationChannel channel;

bool isFlutterLocalNotificationsInitialized = false;

Future<void> setupFlutterNotifications() async {
  String? token = await FirebaseMessaging.instance.getToken();
  log("token: $token");
  if (isFlutterLocalNotificationsInitialized) {
    return;
  }
  channel = const AndroidNotificationChannel(
    'high_importance_channel',
    'High Importance Notifications',
    description: 'This channel is used for important notifications.',
    importance: Importance.high,
  );

  flutterLocalNotificationsPlugin = FlutterLocalNotificationsPlugin();

  await flutterLocalNotificationsPlugin
      .resolvePlatformSpecificImplementation<
          AndroidFlutterLocalNotificationsPlugin>()
      ?.createNotificationChannel(channel);

  await FirebaseMessaging.instance.setForegroundNotificationPresentationOptions(
    alert: true,
    badge: true,
    sound: true,
  );
  isFlutterLocalNotificationsInitialized = true;
}

/// FCM khi app đang mở (foreground): hiển thị qua local notification.
void registerForegroundFcmHandlers() {
  FirebaseMessaging.onMessage.listen((RemoteMessage message) {
    log('FCM onMessage: ${message.messageId}');
    showFlutterNotification(message);
  });
}

void showFlutterNotification(RemoteMessage message) {
  if (kIsWeb) return;

  final RemoteNotification? notification = message.notification;
  if (notification == null) return;

  final int id = notification.hashCode & 0x7fffffff;
  final String? title = notification.title;
  final String? body = notification.body;

  if (Platform.isAndroid) {
    flutterLocalNotificationsPlugin.show(
      id,
      title,
      body,
      NotificationDetails(
        android: AndroidNotificationDetails(
          channel.id,
          channel.name,
          channelDescription: channel.description,
          icon: 'launch_background',
        ),
      ),
    );
  } else if (Platform.isIOS || Platform.isMacOS) {
    flutterLocalNotificationsPlugin.show(
      id,
      title,
      body,
      const NotificationDetails(
        iOS: DarwinNotificationDetails(
          presentAlert: true,
          presentBadge: true,
          presentSound: true,
        ),
      ),
    );
  }
}

late FlutterLocalNotificationsPlugin flutterLocalNotificationsPlugin;

int _messageCount = 0;

String constructFCMPayload(String? token) {
  _messageCount++;
  return jsonEncode({
    'token': token,
    'data': {
      'via': 'FlutterFire Cloud Messaging!!!',
      'count': _messageCount.toString(),
    },
    'notification': {
      'title': 'Hello FlutterFire!',
      'body': 'This notification (#$_messageCount) was created via FCM!',
    },
  });
}

class MyHttpOverrides extends HttpOverrides {
  @override
  HttpClient createHttpClient(SecurityContext? context) {
    return super.createHttpClient(context)
      ..badCertificateCallback =
          (X509Certificate cert, String host, int port) => true;
  }
}
