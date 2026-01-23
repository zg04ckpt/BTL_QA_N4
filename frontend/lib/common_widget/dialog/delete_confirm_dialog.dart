
import 'package:flutter/material.dart';



AlertDialog deleteConfirmDialog (BuildContext context,{
  required String deleteContent,
  required VoidCallback onDelete,
}){
  return AlertDialog(
        title: const Text('Confirm Deletion'),
        content: Text('Are you sure you want to delete this $deleteContent?'),
        actions: <Widget>[
          TextButton(
            onPressed: () {
              Navigator.of(context).pop(); // Đóng dialog khi nhấn Huỷ
            },
            child: const Text('Huỷ'),
          ),
          ElevatedButton(
            onPressed: () async {
              onDelete.call();
              Navigator.of(context).pop(); 
            },
            style: ElevatedButton.styleFrom(
              backgroundColor: Colors.red, // Màu cho nút xoá
            ),
            child: const Text('Delete'),
          ),
        ],
      );
    
}

