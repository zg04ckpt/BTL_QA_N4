import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;

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
    setState(() {
      isLoading = true;
    });

    var headers = {
      'Authorization':
          'Bearer ya29.c.c0ASRK0GbRqgucECRBRzYlaDg7llbuzZkBgXVRfnClDL-bZmUJVMlF67fgStoqrcK2aQsCCX3_dERe-L735iAan7OWKDXpmgPbGZdCeKR1YSiqXw-V6oGnVn5tu9tU3KpDBW7kj6mIoYAhvq_J8cGwG99Np-_hTvtxjbj_mAsCWbAc4rkqlfQhiR3rOmTMAoYBR8iDDuPgzMZtxHLH1gvuU01f0V8HAuyFy-trePJ_xz-hheEEdOlCG0obVNyedZ6SQ8n2qR7imNhdCsV7E0C2Qf9CHdrJqnAPBFjdhIZLXKlybXamHfLFz7t1gB__iuffAtrx4zT1DSetXa_C0V_NNvN55hQJ1GpVbClABio-bMDFtNWaRlzd0_gT384KR47VdWnu96woqW80y_QQ-xW1oBp-Mdo42RF21inaaqna1g52o9qeW-6SOseMOMQ061b6dBa7i9up5wFl3tRmIQgBFx8JBfXVn5neYpg_hl_oFXtaunUvoc06pl1XqiaosccW0m-x7SeayJ9UViQe-X5q8XBwj4s8V6IjZRlnJxRhcVv5_-fcg_8hM4tekv7YvfkVFj23Mq8lgYwV2cMlemJx-X7B4lY80FYi6dZoU5ahauhOzheWarf0Z-8OiO9M-foR7r9yf9Zgtv18MfhB1JyJyQczuJd8i8zB80FlFm8c-9wlyOnrl7S0vUSIxS76rzWSkM0wZaXYn8Zf1yFOYhc3jSR-WOMmtcimv1Xb7ysm_2IVcFY_25zZI7t1rvBUStp06ZeFghYrIVBbtoy3i8ilhIhI-aWzii9xp7ylJze73XZlMmqJBRWm8t-Z-qeijzzof77Xc8j2dd73cIMnaO6bWWBRRoxcBf8a39OS_hi-1By3wBgFUX3S6XsYjz5wr-bmom8bQ7r81s-5J9gri1Bvau-g7aZ-WahrXdiM_3J7g28tp1FQbkkMy-W3XfRpvtstuBipOUa5c7Mriw_t1QV-f6pxsY0uWXsWf74BzJeFMvYpptlRB_dzqB5',
      'Content-Type': 'application/json',
    };
    var request = http.Request(
        'POST',
        Uri.parse(
            'https://fcm.googleapis.com/v1/projects/restaurant-review-6c611/messages:send'));
    request.body = json.encode({
      "message": {
        "topic": "all_users",
        "notification": {
          "title": titleController.text,
          "body": bodyController.text
        }
      }
    });
    request.headers.addAll(headers);

    http.StreamedResponse response = await request.send();

    if (response.statusCode == 200) {
      print(await response.stream.bytesToString());
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text("Notification posted successfully!")),
      );
    } else {
      print(response.reasonPhrase);
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text("Failed to post notification")),
      );
    }

    setState(() {
      isLoading = false;
    });

    Navigator.of(context).pop(); // Close dialog after submitting
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
            decoration: const InputDecoration(labelText: "Tiêu đề"),
          ),
          TextField(
            controller: bodyController,
            decoration: const InputDecoration(labelText: "Nội dung"),
          ),
        ],
      ),
      actions: [
        isLoading
            ? const CircularProgressIndicator()
            : TextButton(
                onPressed: () {
                  Navigator.of(context).pop(); // Close the dialog
                },
                child: const Text("Huỷ"),
              ),
        ElevatedButton(
          onPressed: isLoading ? null : _postNotification,
          child: const Text("Đăng"),
        ),
      ],
    );
  }
}
