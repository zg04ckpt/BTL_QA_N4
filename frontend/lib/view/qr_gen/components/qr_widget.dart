import 'dart:io';
import 'dart:typed_data';
import 'dart:ui' as ui;

import 'package:flutter/material.dart';
import 'package:flutter/rendering.dart';
import 'package:gal/gal.dart';
import 'package:path_provider/path_provider.dart';
import 'package:pretty_qr_code/pretty_qr_code.dart';
import 'package:screenshot/screenshot.dart';

class PrettyQrAnimatedView extends StatefulWidget {
  @protected
  final QrImage qrImage;

  @protected
  final PrettyQrDecoration decoration;

  const PrettyQrAnimatedView({
    super.key,
    required this.qrImage,
    required this.decoration,
  });

  @override
  State<PrettyQrAnimatedView> createState() => PrettyQrAnimatedViewState();
}

class PrettyQrAnimatedViewState extends State<PrettyQrAnimatedView> {
  @protected
  late PrettyQrDecoration previosDecoration;

  final GlobalKey _globalKey = GlobalKey();

  @override
  void initState() {
    super.initState();

    previosDecoration = widget.decoration;
  }

  @override
  void didUpdateWidget(
    covariant PrettyQrAnimatedView oldWidget,
  ) {
    super.didUpdateWidget(oldWidget);

    if (widget.decoration != oldWidget.decoration) {
      previosDecoration = oldWidget.decoration;
    }
  }

  final ScreenshotController _screenshotController = ScreenshotController();

  Future<void> _captureAndSaveScreenshot() async {
    try {
      RenderRepaintBoundary boundary = _globalKey.currentContext!
          .findRenderObject() as RenderRepaintBoundary;
      ui.Image image = await boundary.toImage();
      ByteData? byteData =
          await (image.toByteData(format: ui.ImageByteFormat.png));
      if (byteData != null) {
        await Gal.putImageBytes(byteData.buffer.asUint8List());
      }
    } catch (e) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Lỗi: $e')),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        RepaintBoundary(
          key: _globalKey,
          child: Container(
            margin: const EdgeInsets.all(20),
            padding: const EdgeInsets.all(20.0),
            decoration: BoxDecoration(
                color: Colors.white,
                border: Border.all(color: Colors.black),
                borderRadius: BorderRadius.circular(8)),
            child: Column(
              children: [
                const Text(
                  "Quét QR\nđể đánh giá nhà hàng",
                  textAlign: TextAlign.center,
                  style: TextStyle(fontWeight: FontWeight.w600, fontSize: 20),
                ),
                const Text(
                  '*Bạn có thể quét và đánh giá bất kỳ lúc nào trong vòng 30 ngày',
                  textAlign: TextAlign.center,
                  style: TextStyle(fontWeight: FontWeight.w300, fontSize: 14),
                ),
                const SizedBox(height: 20),
                Padding(
                  padding: const EdgeInsets.symmetric(horizontal: 20),
                  child: TweenAnimationBuilder<PrettyQrDecoration>(
                    tween: PrettyQrDecorationTween(
                      begin: previosDecoration,
                      end: widget.decoration,
                    ),
                    curve: Curves.ease,
                    duration: const Duration(
                      milliseconds: 240,
                    ),
                    builder: (context, decoration, child) {
                      return PrettyQrView(
                        qrImage: widget.qrImage,
                        decoration: decoration,
                      );
                    },
                  ),
                ),
              ],
            ),
          ),
        ),
        ElevatedButton(onPressed: _captureAndSaveScreenshot, child: const Text("Lưu ảnh QR",))
      ],
    );
  }
}
