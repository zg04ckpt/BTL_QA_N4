import 'package:flutter/material.dart';

class ScannerOverlay extends CustomPainter {
  const ScannerOverlay({
    required this.scanWindow,
    this.borderRadius = 12.0,
    this.cornerThickness = 4.0,
    this.extensionLength = 50.0,
    this.offsetY = 0,
  });

  final Rect scanWindow;
  final double borderRadius;
  final double cornerThickness;
  final double extensionLength;
  final double offsetY;

  @override
  void paint(Canvas canvas, Size size) {
    final shiftedScanWindow = scanWindow.shift(Offset(0, offsetY));
    // TODO: use `Offset.zero & size` instead of Rect.largest
    // we need to pass the size to the custom paint widget
  final backgroundPath = Path()..addRect(Offset.zero & size);


    final cutoutPath = Path()
      ..addRRect(
        RRect.fromRectAndCorners(
          shiftedScanWindow,
          topLeft: Radius.circular(borderRadius),
          topRight: Radius.circular(borderRadius),
          bottomLeft: Radius.circular(borderRadius),
          bottomRight: Radius.circular(borderRadius),
        ),
      );

    final backgroundPaint = Paint()
      ..color = Colors.red.withOpacity(0.5)
      ..style = PaintingStyle.fill
      ..blendMode = BlendMode.dstOut;

    final backgroundWithCutout = Path.combine(
      PathOperation.difference,
      backgroundPath,
      cutoutPath,
    );
    canvas.drawPath(backgroundWithCutout, backgroundPaint);

    final borderPaint = Paint()
      ..color = Colors.white
      ..style = PaintingStyle.stroke
      ..strokeWidth = cornerThickness;

    drawExtendedRoundedCorners(canvas, borderPaint, shiftedScanWindow);

    // final borderRect = RRect.fromRectAndCorners(
    //   scanWindow,
    //   topLeft: Radius.circular(borderRadius),
    //   topRight: Radius.circular(borderRadius),
    //   bottomLeft: Radius.circular(borderRadius),
    //   bottomRight: Radius.circular(borderRadius),
    // );

    // // First, draw the background,
    // // with a cutout area that is a bit larger than the scan window.
    // // Finally, draw the scan window itself.
    // canvas.drawPath(backgroundWithCutout, backgroundPaint);
    // canvas.drawRRect(borderRect, borderPaint);
  }

  void drawExtendedRoundedCorners(
    Canvas canvas,
    Paint paint,
    Rect shiftedScanWindow,
  ) {
    // Top-left corner
    canvas.drawLine(
      shiftedScanWindow.topLeft + Offset(extensionLength, 0),
      shiftedScanWindow.topLeft + Offset(borderRadius, 0),
      paint,
    );
    canvas.drawLine(
      shiftedScanWindow.topLeft + Offset(0, extensionLength),
      shiftedScanWindow.topLeft + Offset(0, borderRadius),
      paint,
    );
    canvas.drawArc(
      Rect.fromCircle(
        center: shiftedScanWindow.topLeft + Offset(borderRadius, borderRadius),
        radius: borderRadius,
      ),
      3.14, // Start angle (180 degrees in radians)
      1.57, // Sweep angle (90 degrees in radians)
      false,
      paint,
    );

    // Top-right corner
    canvas.drawLine(
      shiftedScanWindow.topRight + Offset(-extensionLength, 0),
      shiftedScanWindow.topRight + Offset(-borderRadius, 0),
      paint,
    );
    canvas.drawLine(
      shiftedScanWindow.topRight + Offset(0, extensionLength),
      shiftedScanWindow.topRight + Offset(0, borderRadius),
      paint,
    );
    canvas.drawArc(
      Rect.fromCircle(
        center:
            shiftedScanWindow.topRight + Offset(-borderRadius, borderRadius),
        radius: borderRadius,
      ),
      -1.57, // Start angle (-90 degrees in radians)
      1.57, // Sweep angle (90 degrees in radians)
      false,
      paint,
    );

    // Bottom-right corner
    canvas.drawLine(
      shiftedScanWindow.bottomRight + Offset(-extensionLength, 0),
      shiftedScanWindow.bottomRight + Offset(-borderRadius, 0),
      paint,
    );
    canvas.drawLine(
      shiftedScanWindow.bottomRight + Offset(0, -extensionLength),
      shiftedScanWindow.bottomRight + Offset(0, -borderRadius),
      paint,
    );
    canvas.drawArc(
      Rect.fromCircle(
        center: shiftedScanWindow.bottomRight +
            Offset(-borderRadius, -borderRadius),
        radius: borderRadius,
      ),
      0, // Start angle (0 degrees in radians)
      1.57, // Sweep angle (90 degrees in radians)
      false,
      paint,
    );

    // Bottom-left corner
    canvas.drawLine(
      shiftedScanWindow.bottomLeft + Offset(extensionLength, 0),
      shiftedScanWindow.bottomLeft + Offset(borderRadius, 0),
      paint,
    );
    canvas.drawLine(
      shiftedScanWindow.bottomLeft + Offset(0, -extensionLength),
      shiftedScanWindow.bottomLeft + Offset(0, -borderRadius),
      paint,
    );
    canvas.drawArc(
      Rect.fromCircle(
        center:
            shiftedScanWindow.bottomLeft + Offset(borderRadius, -borderRadius),
        radius: borderRadius,
      ),
      1.57, // Start angle (90 degrees in radians)
      1.57, // Sweep angle (90 degrees in radians)
      false,
      paint,
    );
  }

  @override
  bool shouldRepaint(ScannerOverlay oldDelegate) {
    return scanWindow != oldDelegate.scanWindow ||
        borderRadius != oldDelegate.borderRadius;
  }
}
