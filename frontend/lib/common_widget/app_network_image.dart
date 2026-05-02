import 'package:cached_network_image/cached_network_image.dart';
import 'package:cp_restaurants/common/color_extension.dart';
import 'package:cp_restaurants/network/url_helper.dart';
import 'package:flutter/material.dart';

/// Ảnh mạng có placeholder / lỗi — tránh vỡ layout và ô trống khi URL sai.
class AppNetworkImage extends StatelessWidget {
  const AppNetworkImage({
    super.key,
    required this.pathOrUrl,
    this.fit = BoxFit.cover,
    this.width,
    this.height,
    this.borderRadius,
  });

  final String? pathOrUrl;
  final BoxFit fit;
  final double? width;
  final double? height;
  final BorderRadius? borderRadius;

  @override
  Widget build(BuildContext context) {
    final url = resolveMediaUrl(pathOrUrl);
    if (url.isEmpty) {
      return _placeholder();
    }
    Widget img = CachedNetworkImage(
      imageUrl: url,
      width: width,
      height: height,
      fit: fit,
      placeholder: (_, __) => _placeholder(loading: true),
      errorWidget: (_, __, ___) => _placeholder(),
    );
    if (borderRadius != null) {
      img = ClipRRect(borderRadius: borderRadius!, child: img);
    }
    return img;
  }

  Widget _placeholder({bool loading = false}) {
    return Container(
      width: width,
      height: height,
      color: TColor.secondary,
      alignment: Alignment.center,
      child: loading
          ? const SizedBox(
              width: 24,
              height: 24,
              child: CircularProgressIndicator(strokeWidth: 2),
            )
          : Icon(Icons.broken_image_outlined, color: TColor.gray, size: 32),
    );
  }
}
