import 'package:flutter/material.dart';
import 'package:pretty_qr_code/pretty_qr_code.dart';

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
  State<PrettyQrAnimatedView> createState() => _PrettyQrAnimatedViewState();
}

class _PrettyQrAnimatedViewState extends State<PrettyQrAnimatedView> {
  @protected
  late PrettyQrDecoration previosDecoration;

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

  @override
  Widget build(BuildContext context) {
    return Container(
      margin: const EdgeInsets.all(20),
      padding: const EdgeInsets.all(20.0),
      decoration: BoxDecoration(
          color: Colors.white,
          border: Border.all(color: Colors.black),
          borderRadius: BorderRadius.circular(8)),
      child: Column(
        children: [
          const Text(
            "Scan QR\nto Review this Restaurant",
            textAlign: TextAlign.center,
            style: TextStyle(fontWeight: FontWeight.w600, fontSize: 20),
          ),
          const Text(
            '*You can scan and review later in the "Review" section.',
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
    );
  }
}