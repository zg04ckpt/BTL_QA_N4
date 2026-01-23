import 'package:cp_restaurants/data/containt.dart';
import 'package:cp_restaurants/global/global_data.dart';
import 'package:cp_restaurants/network/api_util.dart';
import 'package:cp_restaurants/view/auth/signup_view.dart';
import 'package:flutter/material.dart';

class ReportDialog extends StatefulWidget {
  final int resId;
  final int reviewId;

  const ReportDialog({
    Key? key,
    required this.resId,
    required this.reviewId,
  }) : super(key: key);

  @override
  State<ReportDialog> createState() => _ReportDialogState();
}

class _ReportDialogState extends State<ReportDialog> {
  String? _selectedReason;
  final TextEditingController _otherReasonController = TextEditingController();
  bool isLoading = false;

  bool get _isOtherReasonSelected => _selectedReason == "Other";

  Future<void> _submitReport() async {
    String reason = _selectedReason!;
    if (_isOtherReasonSelected) {
      reason = _otherReasonController.text.trim();
    }

    if (reason.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Please provide a reason for reporting')),
      );
      return;
    }

    setState(() {
      isLoading = true;
    });

    final report = {
      'reason': reason,
      'status': 0,
      'reviewId': widget.reviewId,
      'userId': GlobalData.instance.userData?.userId ?? "",
    };

    try {
      await APIService.instance
          .request('/api/Report', DioMethod.post, param: report);
      Navigator.pop(context);
      showSnackBar(context,
          "Bạn đã báo cáo thành công, chúng tôi sẽ xem xét lại bình luận này");
    } catch (e) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Error: ${e.toString()}')),
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
      title: const Text('Report Review'),
      content: isLoading
          ? const SizedBox(
              height: 50,
              width: 50,
              child: Center(child: CircularProgressIndicator()))
          : Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                DropdownButton<String>(
                  value: _selectedReason,
                  isExpanded: true,
                  hint: const Text('Select a reason'),
                  items: reportReason.map((String reason) {
                    return DropdownMenuItem<String>(
                      value: reason,
                      child: Text(reason),
                    );
                  }).toList(),
                  onChanged: (String? newValue) {
                    setState(() {
                      _selectedReason = newValue;
                    });
                  },
                ),
                if (_isOtherReasonSelected)
                  TextField(
                    controller: _otherReasonController,
                    decoration: const InputDecoration(
                      labelText: 'Please specify',
                    ),
                  ),
              ],
            ),
      actions: [
        TextButton(
          onPressed: () => Navigator.pop(context),
          child: const Text('Huỷ'),
        ),
        ElevatedButton(
          onPressed: isLoading ? null : _submitReport,
          child: const Text(
            'Submit',
            style: TextStyle(color: Colors.red),
          ),
        ),
      ],
    );
  }
}
