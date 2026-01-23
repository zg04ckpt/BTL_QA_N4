import 'package:flutter/material.dart';

class SearchBarView extends StatelessWidget {
  const SearchBarView({super.key});

  @override
  Widget build(BuildContext context) {
    return Container(
      height: 45,
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
      decoration: BoxDecoration(
          color: Colors.blueGrey[50], borderRadius: BorderRadius.circular(12)),
      child: const Row(
        children: [
          Icon(Icons.search),
          SizedBox(
            width: 12,
          ),
          Text(
            "Tìm kiếm nhà hàng..",
            style: TextStyle(fontSize: 16),
          )
        ],
      ),
    );
  }
}
