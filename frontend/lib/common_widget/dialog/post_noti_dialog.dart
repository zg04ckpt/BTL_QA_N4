import 'package:cp_restaurants/network/api_util.dart';
import 'package:flutter/material.dart';

class PostNotificationDialog extends StatefulWidget {
  const PostNotificationDialog({super.key});

  @override
  _PostNotificationDialogState createState() => _PostNotificationDialogState();
}

class _PostNotificationDialogState extends State<PostNotificationDialog> {
  TextEditingController titleController = TextEditingController();
  TextEditingController bodyController = TextEditingController();
  bool isLoading = false;

  Future<void> _postNotification() async {
    if (titleController.text.isEmpty || bodyController.text.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text("Vui lòng nhập đầy đủ tiêu đề và nội dung")),
      );
      return;
    }

    setState(() {
      isLoading = true;
    });

    try {
      final response = await APIService.instance.request(
        '/api/Notifications/send',
        DioMethod.post,
        param: {
          "topic": "all_users",
          "title": titleController.text,
          "body": bodyController.text
        },
      );

      if (response.statusCode == 200) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text("Đã gửi thông báo thành công!")),
        );
        Navigator.of(context).pop();
      } else {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text("Gửi thông báo thất bại")),
        );
      }
    } catch (e) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text("Lỗi: ${e.toString()}")),
      );
    } finally {
      setState(() {
        isLoading = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: const Text("Đăng thông báo"),
      content: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          TextField(
            controller: titleController,
            decoration: const InputDecoration(
              labelText: "Tiêu đề",
              hintText: "Nhập tiêu đề thông báo",
            ),
          ),
          const SizedBox(height: 10),
          TextField(
            controller: bodyController,
            maxLines: 3,
            decoration: const InputDecoration(
              labelText: "Nội dung",
              hintText: "Nhập nội dung thông báo",
            ),
          ),
        ],
      ),
      actions: [
        if (!isLoading)
          TextButton(
            onPressed: () => Navigator.of(context).pop(),
            child: const Text("Huỷ"),
          ),
        ElevatedButton(
          onPressed: isLoading ? null : _postNotification,
          child: isLoading
              ? const SizedBox(
                  width: 20,
                  height: 20,
                  child: CircularProgressIndicator(strokeWidth: 2),
                )
              : const Text("Gửi"),
        ),
      ],
    );
  }
}
