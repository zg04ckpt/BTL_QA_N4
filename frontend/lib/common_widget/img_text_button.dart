import 'package:cp_restaurants/common_widget/app_network_image.dart';
import 'package:flutter/material.dart';

class ImgTextButton extends StatelessWidget {
  final String image;
  final VoidCallback onPressed;
  const ImgTextButton(
      {super.key, required this.image, required this.onPressed});

  @override
  Widget build(BuildContext context) {
    var media = MediaQuery.of(context).size;

    return TextButton(
      onPressed: onPressed,
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.center,
        children: [
          ClipRRect(
            borderRadius: BorderRadius.circular(5),
            child: AppNetworkImage(
              pathOrUrl: image,
              width: media.width * 0.25,
              height: media.width * 0.25,
              fit: BoxFit.cover,
            ),
          ),
        ],
      ),
    );
  }
}
