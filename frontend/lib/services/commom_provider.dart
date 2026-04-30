import 'package:connectivity_plus/connectivity_plus.dart';
import 'package:cp_restaurants/data/models/category_model.dart';
import 'package:cp_restaurants/network/api_util.dart';
import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter/cupertino.dart';
import 'package:shared_preferences/shared_preferences.dart';

class CommonProvider with ChangeNotifier {
  bool isConnect = true;

  List<int> topicIds = [];
  List<CategoryModel> categories = [];
  static const String key = "topic";

  Future<void> saveListTopic(List<int> list) async {
    try {
      final SharedPreferences prefs = await SharedPreferences.getInstance();

      List<String> stringList = list.map((e) => e.toString()).toList();
      await prefs.setStringList(key, stringList);
      print("Danh sách đã được lưu: $list");
    } catch (e) {
      print("Lỗi khi lưu danh sách: $e");
    }
  }

  bool isUseFingerPrint = false;

  Future<void> setIsUseFingerPrint(bool value) async {
    final SharedPreferences prefs = await SharedPreferences.getInstance();
    await prefs.setBool("isUseFingerPrint", value);
    isUseFingerPrint = value;
    notifyListeners();
  }

  Future<void> getIsUseFingerPrint() async {
    final SharedPreferences prefs = await SharedPreferences.getInstance();
    isUseFingerPrint = prefs.getBool("isUseFingerPrint") ?? false;
    notifyListeners();
  }

  bool isUseManagerOnly = false;

  Future<void> setIsUseManagerOnly(bool value) async {
    final SharedPreferences prefs = await SharedPreferences.getInstance();
    await prefs.setBool("isUseManagerOnly", value);
    isUseManagerOnly = value;
    notifyListeners();
  }

  Future<void> getIsUseManagerOnly() async {
    final SharedPreferences prefs = await SharedPreferences.getInstance();
    isUseManagerOnly = prefs.getBool("isUseManagerOnly") ?? false;
    notifyListeners();
  }

  Future<List<int>> getTopics() async {
    List<int> intList = [];
    try {
      final SharedPreferences prefs = await SharedPreferences.getInstance();
      List<String>? stringList = prefs.getStringList(key);

      if (stringList != null) {
        intList = stringList.map((e) => int.parse(e)).toList();
        topicIds = intList;
        print("Danh sách đã lấy ra: $intList");
      } else {
        print("Không có danh sách nào được lưu.");
      }
    } catch (e) {
      print("Lỗi khi lấy danh sách: $e");
    }
    return intList;
  }

  Future<void> addOrRemoveTopic(int value) async {
    try {
      List<int> currentList = await getTopics();

      if (currentList.contains(value)) {
        currentList.remove(value);
        topicIds.remove(value);
        await FirebaseMessaging.instance.unsubscribeFromTopic(value.toString());

        print("Đã xóa giá trị $value khỏi danh sách.");
      } else {
        await FirebaseMessaging.instance.subscribeToTopic(value.toString());
        currentList.add(value);
        topicIds.add(value);

        print("Đã thêm giá trị $value vào danh sách.");
      }

      await saveListTopic(currentList);
    } catch (e) {
      print("Lỗi khi thêm hoặc xóa giá trị khỏi danh sách: $e");
    }
  }

  Future<List<ConnectivityResult>> checkConnectivity() async {
    final connectivityResults = await Connectivity().checkConnectivity();
    final hasConnection = connectivityResults
        .any((result) => result != ConnectivityResult.none);
    isConnect = hasConnection;
    notifyListeners();
    return connectivityResults;
  }

  listenConnectivityChange(BuildContext? context) {
    checkConnectivity();
    Connectivity().onConnectivityChanged.listen((List<ConnectivityResult> results) {
      final hasConnection =
          results.any((result) => result != ConnectivityResult.none);
      if (context != null) {
        isConnect = hasConnection;
      }
      notifyListeners();
    });
  }

  Future<void> fetchCategories() async {
    try {
      final response = await APIService.instance.request(
        '/api/Categories',
        DioMethod.get,
      );

      if (response.statusCode == 200) {
        List<dynamic> data = response.data as List<dynamic>;
        categories = data.map((json) => CategoryModel.fromJson(json)).toList();
        notifyListeners();
      }
    } catch (e) {
      print("Error fetching categories: $e");
    }
  }
}
