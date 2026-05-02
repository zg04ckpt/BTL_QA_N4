import 'package:cp_restaurants/network/api_util.dart';
import 'package:dio/dio.dart';
import 'package:flutter/material.dart';

/// Khớp backend [NotificationsController]: POST api/Notifications/send
/// Body: { "topic", "title", "body" } — dùng topic `all_user` như app đã subscribe.
class PostNotificationDialog extends StatefulWidget {
  const PostNotificationDialog({super.key});

  @override
  State<PostNotificationDialog> createState() => _PostNotificationDialogState();
}

class _PostNotificationDialogState extends State<PostNotificationDialog> {
  final TextEditingController titleController = TextEditingController();
  final TextEditingController bodyController = TextEditingController();
  final TextEditingController topicController =
      TextEditingController(text: 'all_user');
  bool isLoading = false;

  Future<void> _postNotification() async {
    final title = titleController.text.trim();
    final body = bodyController.text.trim();
    final topic = topicController.text.trim().isEmpty
        ? 'all_user'
        : topicController.text.trim();

    if (title.isEmpty || body.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Nhập đủ tiêu đề và nội dung.')),
      );
      return;
    }

    setState(() => isLoading = true);

    try {
      final response = await APIService.instance.request(
        '/api/Notifications/send',
        DioMethod.post,
        param: {
          'topic': topic,
          'title': title,
          'body': body,
        },
      );

      if (!mounted) return;

      if (response.statusCode != null &&
          response.statusCode! >= 200 &&
          response.statusCode! < 300) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Đã gửi thông báo qua backend.')),
        );
        Navigator.of(context).pop();
      } else {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Lỗi server: ${response.statusCode}')),
        );
      }
    } on DioException catch (e) {
      if (!mounted) return;
      final msg = e.response?.data?.toString() ?? e.message ?? '$e';
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Gửi thất bại: $msg')),
      );
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Lỗi: $e')),
      );
    } finally {
      if (mounted) setState(() => isLoading = false);
    }
  }

  @override
  void dispose() {
    titleController.dispose();
    bodyController.dispose();
    topicController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: const Text('Đăng thông báo (FCM topic)'),
      content: SingleChildScrollView(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            TextField(
              controller: topicController,
              decoration: const InputDecoration(
                labelText: 'Topic (mặc định: all_user)',
                hintText: 'all_user',
              ),
            ),
            TextField(
              controller: titleController,
              decoration: const InputDecoration(labelText: 'Tiêu đề'),
            ),
            TextField(
              controller: bodyController,
              decoration: const InputDecoration(labelText: 'Nội dung'),
              maxLines: 3,
            ),
          ],
        ),
      ),
      actions: [
        if (isLoading)
          const Padding(
            padding: EdgeInsets.all(8.0),
            child: SizedBox(
              width: 24,
              height: 24,
              child: CircularProgressIndicator(strokeWidth: 2),
            ),
          )
        else
          TextButton(
            onPressed: () => Navigator.of(context).pop(),
            child: const Text('Huỷ'),
          ),
        ElevatedButton(
          onPressed: isLoading ? null : _postNotification,
          child: const Text('Gửi'),
        ),
      ],
    );
  }
}
