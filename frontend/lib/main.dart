import 'dart:io';
import 'package:cp_restaurants/services/commom_provider.dart';
import 'package:cp_restaurants/services/map_tile_provider.dart';
import 'package:cp_restaurants/view/auth/auth_view_model.dart';
import 'package:cp_restaurants/services/location_provider.dart';
import 'package:cp_restaurants/services/restaurant_provider.dart';
import 'package:cp_restaurants/services/review_provider.dart';
import 'package:cp_restaurants/view/splash/splash_view.dart';
import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter/material.dart';
import 'package:lottie/lottie.dart';
import 'package:permission_handler/permission_handler.dart';
import 'package:provider/provider.dart';
import 'common/color_extension.dart';
import 'firebase_bootstrap.dart';
import 'services/notification_service.dart';


void main() async {
  WidgetsFlutterBinding.ensureInitialized();
  HttpOverrides.global = MyHttpOverrides();
  await ensureFirebaseInitialized();
  FirebaseMessaging.onBackgroundMessage(firebaseMessagingBackgroundHandler);
  await setupFlutterNotifications();
  registerForegroundFcmHandlers();
  Permission.notification.request();
  AssetLottie("assets/animations/loading.json").load();
  runApp(
    const MyApp(),
  );
}

class MyApp extends StatefulWidget {
  const MyApp({super.key});

  @override
  State<MyApp> createState() => _MyAppState();
}

class _MyAppState extends State<MyApp> {
  String uRole = "";

  @override
  void initState() {
    super.initState();
  }

  // This widget is the root of your application.
  @override
  Widget build(BuildContext context) {
    return MultiProvider(
      providers: [
        ChangeNotifierProvider(create: (context) => LocationProvider()),
        ChangeNotifierProvider(create: (context) => RestaurantProvider()),
        ChangeNotifierProvider(create: (context) => ReviewProvider()),
        ChangeNotifierProvider(create: (context) => AuthViewModel()),
        ChangeNotifierProvider(create: (context) => CommonProvider()),
        ChangeNotifierProvider(create: (context) => MapTileProvider()),
      ],
      child: MaterialApp(
          title: 'CP Restaurants',
          debugShowCheckedModeBanner: false,
          theme: ThemeData(
              colorScheme: ColorScheme.fromSeed(seedColor: TColor.primary),
              useMaterial3: true,
              primaryColor: TColor.primary,
              fontFamily: "Quicksand"),
          builder: (context, child) {
            return MediaQuery.withClampedTextScaling(
              minScaleFactor: 1.0,
              maxScaleFactor: 1.25,
              child: child ?? const SizedBox.shrink(),
            );
          },
          home: const SplashView()),
    );
  }
}
