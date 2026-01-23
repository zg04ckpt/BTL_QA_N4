import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';

class ImagePreviewDialog extends StatelessWidget {
  const ImagePreviewDialog({super.key, required this.imageUrl});

  final String imageUrl;

  @override
  Widget build(BuildContext context) {
    return Dialog(
      child: SizedBox(
        child: CachedNetworkImage(
          width: 300,
          // height: 100,
          fit: BoxFit.contain,
          imageUrl: imageUrl,
          placeholder: (context, url) => const CircularProgressIndicator(),
        ),
      ),
    );
  }
}
