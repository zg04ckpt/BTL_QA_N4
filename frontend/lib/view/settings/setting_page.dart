import 'package:flutter/material.dart';

Container buildSettingItem(
    {required String title,
    required bool value,
    required Function(bool) onChanged}) {
  return Container(
    height: 60,
    padding: const EdgeInsets.all(16),
    width: double.infinity,
    decoration: BoxDecoration(
      borderRadius: BorderRadius.circular(12),
      color: const Color.fromARGB(255, 94, 160, 31),
      boxShadow: const [
        BoxShadow(
          color: Colors.black12,
          blurRadius: 2,
          offset: Offset(0, 2),
        )
      ],
    ),
    child: Row(
      children: [
        Text(
          title,
          style: const TextStyle(
            fontSize: 18,
            fontWeight: FontWeight.bold,
            color: Colors.white,
          ),
        ),
        const Spacer(),
        Switch(value: true, onChanged: (value) {})
      ],
    ),
  );
}
