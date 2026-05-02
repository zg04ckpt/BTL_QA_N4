import 'package:cached_network_image/cached_network_image.dart';
import 'package:cp_restaurants/network/url_helper.dart';
import 'package:flutter/material.dart';

class ImagePreviewDialog extends StatelessWidget {
  const ImagePreviewDialog({super.key, required this.imageUrl});

  final String imageUrl;

  @override
  Widget build(BuildContext context) {
    final u = resolveMediaUrl(imageUrl);
    if (u.isEmpty) {
      return Dialog(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Text(
            'Không có ảnh',
            textAlign: TextAlign.center,
            style: TextStyle(color: Colors.grey.shade700),
          ),
        ),
      );
    }
    return Dialog(
      child: SizedBox(
        child: CachedNetworkImage(
          width: 300,
          fit: BoxFit.contain,
          imageUrl: u,
          placeholder: (context, url) => const CircularProgressIndicator(),
        ),
      ),
    );
  }
}
