import 'package:cloud_firestore/cloud_firestore.dart';
import 'package:flutter/material.dart';

import '../../../data/models/user_data.dart';

class UserManagementView extends StatefulWidget {
  const UserManagementView({super.key});

  @override
  State<UserManagementView> createState() => _UserManagementViewState();
}

class _UserManagementViewState extends State<UserManagementView> {
  TextEditingController searchController = TextEditingController();
  UserData? searchedUser; // Để lưu kết quả tìm kiếm
  bool isLoading = false;

  // Hàm tìm kiếm user theo email
  Future<void> searchUserByEmail(String email) async {
    setState(() {
      isLoading = true;
    });

    try {
      QuerySnapshot querySnapshot = await FirebaseFirestore.instance
          .collection('users')
          .where('email', isEqualTo: email)
          .limit(1)
          .get();

      if (querySnapshot.docs.isNotEmpty) {
        setState(() {
          // searchedUser =
          //     UserData.fromJson(querySnapshot.docs.first.);
        });
      } else {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('No user found with this email')),
        );
      }
    } catch (e) {
      print("Error searching user: $e");
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Error searching user: $e')),
      );
    }

    setState(() {
      isLoading = false;
    });
  }

  Future<void> toggleUserState(String userId, String currentState) async {
    try {
      String newState = currentState == "lock" ? "open" : "lock";

      // Cập nhật trạng thái của người dùng trong Firestore
      await FirebaseFirestore.instance
          .collection('users')
          .doc(userId)
          .update({'state': newState});

      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('User state updated to $newState')),
      );

      setState(() {
        // searchedUser!.state = newState;
      });
    } catch (e) {
      print("Error updating user state: $e");
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Error updating user state: $e')),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text("User Management")),
      body: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          children: [
            // Thanh tìm kiếm theo email
            TextField(
              controller: searchController,
              decoration: InputDecoration(
                labelText: "Search by Email",
                suffixIcon: IconButton(
                  icon: const Icon(Icons.search),
                  onPressed: () {
                    searchUserByEmail(searchController.text.trim());
                  },
                ),
              ),
            ),
            const SizedBox(height: 20),

            // Hiển thị thông tin người dùng nếu tìm thấy
            isLoading
                ? const CircularProgressIndicator()
                : searchedUser != null
                    ? Column(
                        children: [
                          Text("Name: ${searchedUser!.name}"),
                          Text("Email: ${searchedUser!.email}"),
                          Text("Mobile: ${searchedUser!.phoneNumber}"),
                          Text("Role: ${searchedUser!.role}"),
                          Text("State: ${searchedUser!.state}"),
                          const SizedBox(height: 10),
                          ElevatedButton(
                            onPressed: () {
                              showDialog(
                                context: context,
                                builder: (context) => AlertDialog(
                                  title: Text(searchedUser!.state == "lock"
                                      ? "Unlock User"
                                      : "Lock User"),
                                  content: Text(
                                    searchedUser!.state == "lock"
                                        ? "Are you sure you want to unlock this user?"
                                        : "Are you sure you want to lock this user?",
                                  ),
                                  actions: [
                                    TextButton(
                                      onPressed: () {
                                        Navigator.of(context).pop();
                                      },
                                      child: const Text("Huỷ"),
                                    ),
                                    ElevatedButton(
                                      onPressed: () {
                                        // toggleUserState(searchedUser!.userId,
                                        //     searchedUser!.state);
                                        // Navigator.of(context).pop();
                                      },
                                      child: Text(searchedUser!.state == "lock"
                                          ? "Unlock"
                                          : "Lock"),
                                    ),
                                  ],
                                ),
                              );
                            },
                            child: Text(searchedUser!.state == "lock"
                                ? "Unlock User"
                                : "Lock User"),
                          ),
                        ],
                      )
                    : const Text("No user found"),
          ],
        ),
      ),
    );
  }
}
