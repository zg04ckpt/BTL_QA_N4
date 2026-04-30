import 'dart:io';

import 'package:google_maps_flutter_android/google_maps_flutter_android.dart';
import 'package:google_maps_flutter_platform_interface/google_maps_flutter_platform_interface.dart';

import 'package:cp_restaurants/services/commom_provider.dart';
import 'package:cp_restaurants/view/auth/auth_view_model.dart';
import 'package:cp_restaurants/firebase_options.dart';
import 'package:cp_restaurants/services/location_provider.dart';
import 'package:cp_restaurants/services/restaurant_provider.dart';
import 'package:cp_restaurants/services/review_provider.dart';
import 'package:cp_restaurants/view/splash/splash_view.dart';
import 'package:firebase_core/firebase_core.dart';
import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter/material.dart';
import 'package:lottie/lottie.dart';
import 'package:permission_handler/permission_handler.dart';
import 'package:provider/provider.dart';
import 'common/color_extension.dart';
import 'services/notification_service.dart';
import 'services/admin_provider.dart';


Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();

  // Android: renderer must be chosen before any GoogleMap is built; texture mode
  // (useAndroidViewSurface = false) is the recommended default and avoids blank maps on many devices.
  if (Platform.isAndroid) {
    final maps = GoogleMapsFlutterPlatform.instance;
    if (maps is GoogleMapsFlutterAndroid) {
      try {
        await maps.initializeWithRenderer(AndroidMapRenderer.latest);
      } catch (e, st) {
        debugPrint('Google Maps initializeWithRenderer: $e\n$st');
      }
      maps.useAndroidViewSurface = false;
      try {
        await maps.warmup();
      } catch (e) {
        debugPrint('Google Maps warmup: $e');
      }
    }
  }

  HttpOverrides.global = MyHttpOverrides();
  
  try {
    await Firebase.initializeApp(options: DefaultFirebaseOptions.currentPlatform);
  } catch (e) {
    debugPrint("Firebase initialization failed: $e");
  }

  runApp(const MyApp());

  // Khởi tạo các dịch vụ phụ ở background
  _initializeSecondaryServices();
}

Future<void> _initializeSecondaryServices() async {
  try {
    print("Initializing secondary services...");
    FirebaseMessaging.onBackgroundMessage(firebaseMessagingBackgroundHandler);
    await setupFlutterNotifications();
    print("Notifications initialized.");
    await Permission.notification.request();
    print("Permissions requested.");
    await AssetLottie("assets/animations/loading.json").load();
    print("Assets loaded.");
  } catch (e) {
    print("Error initializing secondary services: $e");
  }
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
        ChangeNotifierProvider(create: (context) => AdminProvider())
      ],
      child: MaterialApp(
          title: 'CP Restaurants',
          debugShowCheckedModeBanner: false,
          theme: ThemeData(
              colorScheme: ColorScheme.fromSeed(seedColor: TColor.primary),
              useMaterial3: true,
              primaryColor: TColor.primary,
              fontFamily: "Quicksand"),
          home: const SplashView()),
    );
  }
}
