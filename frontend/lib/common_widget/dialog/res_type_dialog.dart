import 'package:cp_restaurants/data/containt.dart';
import 'package:flutter/material.dart';

class ResTypeDialog extends StatefulWidget {
  const ResTypeDialog({
    super.key,
    required this.onConfirm,
    required this.initType,
  });

  final Function(String) onConfirm;
  final String initType; // Danh sách các loại đã chọn trước đó

  @override
  State<ResTypeDialog> createState() => _ResTypeDialogState();
}

class _ResTypeDialogState extends State<ResTypeDialog> {
  

  String selectedTypes = "";

  @override
  void initState() {
    super.initState();
    // Khởi tạo selectedTypes với giá trị từ initType
    selectedTypes = widget.initType;
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      titlePadding: const EdgeInsets.all(0),
      title: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          IconButton(
            icon: const Icon(Icons.cancel, color: Colors.red),
            onPressed: () => Navigator.of(context).pop(),
          ),
          const Text('Select Types'),
          IconButton(
            icon: const Icon(
              Icons.check,
              color: Colors.green,
            ),
            onPressed: selectedTypes.isNotEmpty
                ? () {
                    widget.onConfirm(selectedTypes);
                    Navigator.of(context).pop();
                  }
                : () {
                    ScaffoldMessenger.of(context).showSnackBar(
                      const SnackBar(
                        content: Text("Select a type to continue"),
                      ),
                    );
                  },
          ),
        ],
      ),
      content: SizedBox(
        width: double.maxFinite,
        child: ListView(
          children: restaurantTypes.keys.map((type) {
            return CheckboxListTile(
              title: Text(type),
              value: selectedTypes.contains(type),
              onChanged:
                  selectedTypes.length < 3 || selectedTypes.contains(type)
                      ? (bool? value) {
                          setState(() {
                            if (value == true) {
                              selectedTypes=type;
                            } else {
                              selectedTypes="";
                            }
                          });
                        }
                      : null,
            );
          }).toList(),
        ),
      ),
    );
  }
}
