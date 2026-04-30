import 'dart:async';
import 'dart:developer';

import 'package:cp_restaurants/global/global_data.dart';
import 'package:cp_restaurants/network/api_util.dart';
import 'package:cp_restaurants/view/auth/signup_view.dart';
import 'package:cp_restaurants/view/restaurant/restaurant_detail_view.dart';
import 'package:cp_restaurants/view/scan_qr/components/scan_overlay.dart';
import 'package:flutter/material.dart';
import 'package:lottie/lottie.dart';
import 'package:mobile_scanner/mobile_scanner.dart';
import 'package:provider/provider.dart';

import '../../services/restaurant_provider.dart';

class ScanPage extends StatefulWidget {
  const ScanPage({super.key});

  @override
  State<ScanPage> createState() => _MyScannerScreenState();
}

class _MyScannerScreenState extends State<ScanPage>
    with SingleTickerProviderStateMixin {
  final MobileScannerController controller = MobileScannerController(
    returnImage: true,
    formats: [BarcodeFormat.all, BarcodeFormat.unknown],
  );
  double _zoomFactor = 0.0;
  StreamSubscription<BarcodeCapture>? _barcodeSubscription;
  bool _isProcessingBarcode = false;
  late MobileScannerState currentState;
  @override
  void initState() {
    super.initState();
    controller.start();
    controller.addListener(() {
      currentState = controller.value;
    });

    _barcodeSubscription = controller.barcodes.listen((barcodeCapture) {
      if (!_isProcessingBarcode && barcodeCapture.image != null) {
        _isProcessingBarcode = true;

        try {
          if (currentState.torchState == TorchState.on) {
            controller.toggleTorch();
          }
        } catch (e) {
          log(e.toString());
        }
        _handleBarcodeCapture(barcodeCapture);
      }
    });
  }

  Future<void> _handleBarcodeCapture(BarcodeCapture barcodeCapture) async {
    if (barcodeCapture.barcodes[0].displayValue == null) {
      return;
    }
    setState(() {});

    int resId = int.parse(
        barcodeCapture.barcodes[0].displayValue?.split('_')[1] ?? '0');
    if (resId == 0) {
      showSnackBar(context, "Đã có lỗi xảy ra, vui lòng thử lại sau");
      return;
    }
    var response1 = await APIService.instance
        .request('/api/QRInformation', DioMethod.post, param: {
      "id": 0,
      "userId": GlobalData.instance.userData?.userId,
      "restaurantId": resId,
      "createTime": DateTime.now().millisecondsSinceEpoch
    });

    if (response1.statusCode != 200 && response1.statusCode != 201) {
      showSnackBar(context, "Đã có lỗi xảy ra, vui lòng thử lại sau");
      return;
    }

    var resData =
        await context.read<RestaurantProvider>().getRestaurantById(resId);

    if (resData == null) {
      showSnackBar(context, "Đã có lỗi xảy ra, vui lòng thử lại sau");
      return;
    }

    await Navigator.push(
      context,
      MaterialPageRoute(
        builder: (context) => RestaurantDetailView(fObj: resData),
      ),
    );
  }

  @override
  Future<void> dispose() async {
    super.dispose();
    _barcodeSubscription?.cancel();
    await controller.dispose();
  }

  Widget _buildZoomScaleSlider() {
    return ValueListenableBuilder(
      valueListenable: controller,
      builder: (context, state, child) {
        if (!state.isInitialized || !state.isRunning) {
          return const SizedBox.shrink();
        }

        return Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16),
          child: Row(
            children: [
              Expanded(
                child: Slider(
                  activeColor: Colors.amber,
                  inactiveColor: const Color(0xffD9D9D9).withOpacity(0.45),
                  value: _zoomFactor,
                  onChanged: (value) {
                    setState(() {
                      _zoomFactor = value;
                      controller.setZoomScale(value);
                    });
                  },
                ),
              ),
            ],
          ),
        );
      },
    );
  }

  @override
  Widget build(BuildContext context) {
    final scanWindow = Rect.fromCenter(
      center: MediaQuery.sizeOf(context).center(Offset.zero),
      width: 300,
      height: 300,
    );
    return Scaffold(
      body: SafeArea(
        child: Center(
          child: MobileScanner(
            controller: controller,
            errorBuilder:
                (BuildContext context, MobileScannerException error) {
              return Container();
            },
            overlayBuilder: (context, constraints) {
              return Stack(
                fit: StackFit.expand,
                children: [
                  Positioned(
                    top: scanWindow.top - 50,
                    right: MediaQuery.sizeOf(context).width - scanWindow.right,
                    left: scanWindow.left,
                    child: Lottie.asset(
                      "assets/animations/anim_scan.json",
                    ),
                  ),
                  ValueListenableBuilder(
                    valueListenable: controller,
                    builder: (context, value, child) {
                      if (!value.isInitialized ||
                          !value.isRunning ||
                          value.error != null) {
                        return const SizedBox();
                      }

                      return CustomPaint(
                        painter: ScannerOverlay(
                          scanWindow: scanWindow,
                          cornerThickness: 5,
                          offsetY: -100,
                        ),
                      );
                    },
                  ),
                  Column(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Column(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Row(
                            children: [
                              const SizedBox(
                                width: 16,
                              ),
                              const Expanded(
                                child: Text(
                                  "Quét mã QR",
                                  textAlign: TextAlign.center,
                                  style: TextStyle(
                                      color: Colors.green,
                                      fontSize: 24,
                                      fontWeight: FontWeight.bold),
                                ),
                              ),
                              IconButton(
                                onPressed: () {
                                  Navigator.of(context).pop();
                                },
                                icon: const Icon(
                                  Icons.cancel,
                                  size: 30,
                                  color: Colors.red,
                                ),
                              ),
                              const SizedBox(
                                width: 16,
                              ),
                            ],
                          ),
                          const SizedBox(
                            height: 12,
                          ),
                          const Text(
                            "Căn mã QR vào vùng quét",
                            style: TextStyle(fontSize: 18, color: Colors.green),
                          ),
                        ],
                      ),
                      Column(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          _buildZoomScaleSlider(),
                          const SizedBox(
                            height: 30,
                          ),
                          const Row(
                            children: [],
                          ),
                          const SizedBox(
                            height: 30,
                          ),
                        ],
                      ),
                    ],
                  ),
                ],
              );
            },
          ),
        ),
      ),
    );
  }
}
