import 'package:cp_restaurants/firebase_options.dart';
import 'package:firebase_core/firebase_core.dart';

/// Native (google-services) may create the default Firebase app before Dart runs.
/// [Firebase.apps] can still appear empty, so [Firebase.initializeApp] throws
/// [duplicate-app]. Treat that as success.
Future<void> ensureFirebaseInitialized() async {
  try {
    await Firebase.initializeApp(
      options: DefaultFirebaseOptions.currentPlatform,
    );
  } on FirebaseException catch (e) {
    if (e.code != 'duplicate-app') rethrow;
  } catch (e) {
    if (!e.toString().contains('duplicate-app')) rethrow;
  }
}
