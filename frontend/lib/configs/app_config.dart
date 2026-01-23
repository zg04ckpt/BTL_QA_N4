

class AppConfigs {
  AppConfigs._();

  static const String appName = "P-Food";

  ///API Env
  static String get baseUrl => "";

  static const dateDisplayFormat = 'dd/MM/yyyy';
  static const dateTimeDisplayFormat = 'dd/MM/yyyy HH:mm';

  static const dateTimeAPIFormat =
      "YYYY-MM-DDThh:mm:ssTZD"; //Use DateTime.parse(date) instead of ...
  static const dateAPIFormat = 'dd/MM/yyyy';

  ///Date range
  static final identityMinDate = DateTime(1900, 1, 1);
  static final identityMaxDate = DateTime.now();
  static final birthMinDate = DateTime(1900, 1, 1);
  static final birthMaxDate = DateTime.now();

  ///Font
  static const fontFamily = 'Roboto';

  ///Max file
  static const maxAttachFile = 5;

  static const scrollThreshold = 500.0;
}

class FirebaseConfig {
  //Todo
}

class DatabaseConfig {
  //Todo
  static const int version = 1;
}

class MovieAPIConfig {
  static const String apiKey = '26763d7bf2e94098192e629eb975dab0';
}

class UpGraderAPIConfig {
  static const String apiKey = '';
}