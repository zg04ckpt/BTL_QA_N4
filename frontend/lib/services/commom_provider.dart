import 'package:connectivity_plus/connectivity_plus.dart';
import 'package:firebase_messaging/firebase_messaging.dart';

import 'package:flutter/cupertino.dart';
import 'package:shared_preferences/shared_preferences.dart';

class CommonProvider with ChangeNotifier {
  bool isConnect = true;

  List<int> topicIds = [];
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

  Future<ConnectivityResult> checkConnectivity() async {
    final connectivityResult = await Connectivity().checkConnectivity();

    if (connectivityResult == ConnectivityResult.mobile) {
    } else if (connectivityResult == ConnectivityResult.wifi) {
    } else if (connectivityResult == ConnectivityResult.ethernet) {
    } else if (connectivityResult == ConnectivityResult.vpn) {
    } else if (connectivityResult == ConnectivityResult.bluetooth) {
    } else if (connectivityResult == ConnectivityResult.other) {
    } else if (connectivityResult == ConnectivityResult.none) {
      isConnect = false;
      notifyListeners();
    }

    return connectivityResult;
  }

  listenConnectivityChange(BuildContext? context) {
    checkConnectivity();
    Connectivity().onConnectivityChanged.listen((ConnectivityResult result) {
      if (result == ConnectivityResult.none) {
        if (context != null) {
          isConnect = false;
        }
      } else {
        isConnect = true;
      }
      notifyListeners();
    });
  }
}
