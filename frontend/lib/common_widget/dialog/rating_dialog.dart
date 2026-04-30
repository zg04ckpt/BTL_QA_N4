import 'dart:developer';

import 'package:cached_network_image/cached_network_image.dart';
import 'package:cp_restaurants/data/models/review_model.dart';
import 'package:cp_restaurants/global/global_data.dart';
import 'package:cp_restaurants/network/api_util.dart';
import 'package:custom_rating_bar/custom_rating_bar.dart';
import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'dart:io';
import 'package:image_picker/image_picker.dart';

class ReviewDialog extends StatefulWidget {
  final String? restaurantName;
  final int resId;
  final VoidCallback onSubmitedReview;
  final ReviewModel? initReview;

  const ReviewDialog(
      {super.key,
      this.restaurantName,
      required this.resId,
      required this.onSubmitedReview,
      this.initReview});

  @override
  State<ReviewDialog> createState() => _ReviewDialogState();
}

class _ReviewDialogState extends State<ReviewDialog> {
  double _rating = 5;
  final TextEditingController _reviewController = TextEditingController();
  final List<File> _imageFiles = [];
  final List<String> _imagePath = [];

  bool isLoading = false;

  String _toShortErrorMessage(Object error) {
    final raw = error.toString().replaceFirst('Exception: ', '').trim();
    final backendMessage = raw.contains(': ')
        ? raw.split(': ').last.trim()
        : raw;
    final technicalMarkers = [
      'client error - the request contains bad syntax',
      'read more about status codes',
      'dioexception [bad response]',
      'in order to resolve this exception',
    ];
    final isTechnicalLongMessage = technicalMarkers
        .any((marker) => backendMessage.toLowerCase().contains(marker));

    if (backendMessage.isNotEmpty &&
        !backendMessage.toLowerCase().startsWith('network error') &&
        !isTechnicalLongMessage) {
      return backendMessage;
    }

    final message = raw.toLowerCase();
    if (message.contains('scan') && message.contains('qr')) {
      return 'Bạn cần quét QR trước khi đánh giá.';
    }
    if (message.contains('expired')) {
      return 'QR đã hết hạn, vui lòng quét lại.';
    }
    if (message.contains('network error')) {
      return 'Không gửi được đánh giá. Vui lòng thử lại.';
    }
    return 'Gửi đánh giá thất bại.';
  }

  void _showMessage(String message, {bool isError = true}) {
    if (isError) {
      showDialog(
        context: context,
        builder: (dialogContext) => AlertDialog(
          title: const Text('Thông báo'),
          content: Text(message),
          actions: [
            TextButton(
              onPressed: () => Navigator.of(dialogContext).pop(),
              child: const Text('OK'),
            ),
          ],
        ),
      );
      return;
    }

    final rootContext = Navigator.of(context, rootNavigator: true).context;
    final messenger = ScaffoldMessenger.maybeOf(rootContext);
    if (messenger == null) return;
    messenger.hideCurrentSnackBar();
    messenger.showSnackBar(
      SnackBar(
        dismissDirection: DismissDirection.up,
        behavior: SnackBarBehavior.floating,
        duration: const Duration(seconds: 2),
        backgroundColor: isError ? Colors.redAccent : Colors.green,
        content: Text(message),
      ),
    );
  }

  @override
  void initState() {
    super.initState();
    if (widget.initReview != null) {
      _rating = widget.initReview!.rate;
      _reviewController.text = widget.initReview!.review;
      _imagePath.addAll(widget.initReview?.imageUrls ?? []);
      setState(() {});
    }
  }

  Future<void> _pickImages() async {
    final pickedFiles = await ImagePicker().pickMultiImage();

    setState(() {
      final currentCount = _imageFiles.length;
      final remainingSlots = 4 - currentCount;

      if (remainingSlots > 0) {
        _imageFiles.addAll(
          pickedFiles
              .take(remainingSlots)
              .map((pickedFile) => File(pickedFile.path)),
        );
      }

      if (_imageFiles.length >= 4) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Bạn chỉ được phép chọn tối đa 4 ảnh.'),
          ),
        );
      }
    });
  }

  void submitReview() async {
    setState(() {
      isLoading = true;
    });
    try {
      for (var img in _imageFiles) {
        var response = await APIService.instance.uploadImage(img);
        var responseData = response.data;

        String path = responseData['image'];
        _imagePath.add(path);
      }
      ReviewModel reviewModel = ReviewModel(
        id: widget.initReview?.id ?? -1,
        imageUrls: _imagePath,
        rate: _rating,
        resId: widget.resId,
        review: _reviewController.text,
        userName: GlobalData.instance.userData?.name ?? "Giấu tên",
        userId: GlobalData.instance.userData?.userId ?? 0,
        createDate: DateTime.now().millisecondsSinceEpoch,
      );
      log(reviewModel.toJson().toString());
      Response<dynamic> response;
      if (widget.initReview == null) {
        response = await APIService.instance.request(
          '/api/reviews',
          DioMethod.post,
          formData: reviewModel.toJson(),
        );
      } else {
        response = await APIService.instance.request(
          '/api/reviews/${reviewModel.id}',
          DioMethod.put,
          formData: reviewModel.toJson(),
        );
      }
      log(response.statusCode.toString());
      if (response.statusCode == 200) {
        if (widget.initReview == null) {
            _showMessage("Thêm đánh giá thành công", isError: false);
        } else {
            _showMessage("Sửa đánh giá thành công", isError: false);
        }
        widget.onSubmitedReview();
        if (mounted) {
          Navigator.of(context).pop();
        }
        return;
      }
      if (mounted) {
        _showMessage("Không thể gửi đánh giá");
      }
    } catch (e) {
      if (mounted) {
        _showMessage(_toShortErrorMessage(e));
      }
    } finally {
      if (mounted) {
        setState(() {
          isLoading = false;
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Dialog(
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(15),
      ),
      child: isLoading
          ? Container(
              height: 200,
              width: 200,
              decoration: BoxDecoration(
                borderRadius: BorderRadius.circular(12),
                color: Colors.green[100],
              ),
              child: const Center(
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  crossAxisAlignment: CrossAxisAlignment.center,
                  children: [
                    CircularProgressIndicator(),
                    SizedBox(height: 20),
                    Text("Uploading review"),
                  ],
                ),
              ),
            )
          : Padding(
              padding: const EdgeInsets.all(16.0),
              child: SingleChildScrollView(
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  crossAxisAlignment: CrossAxisAlignment.center,
                  children: [
                  Text(
                    widget.restaurantName ?? "Edit",
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                    textAlign: TextAlign.center,
                    style: const TextStyle(
                      fontSize: 20,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                  const SizedBox(height: 20),
                  RatingBar(
                    size: 40,
                    filledIcon: Icons.star,
                    alignment: Alignment.center,
                    emptyIcon: Icons.star_border,
                    onRatingChanged: (value) {
                      setState(() {
                        _rating = value;
                      });
                    },
                    initialRating: _rating,
                    maxRating: 5,
                  ),
                  const SizedBox(height: 20),
                  TextField(
                    controller: _reviewController,
                    maxLength: 200,
                    maxLines: 4,
                    decoration: const InputDecoration(
                      hintText: "Viết đánh giá của bạn...",
                      border: OutlineInputBorder(
                          borderRadius: BorderRadius.all(Radius.circular(12))),
                    ),
                  ),
                  const Align(
                    alignment: Alignment.centerLeft,
                    child: Text(
                      "Ảnh mô tả:",
                      style: TextStyle(
                        fontWeight: FontWeight.bold,
                        fontSize: 18,
                      ),
                    ),
                  ),
                  Row(
                    crossAxisAlignment: CrossAxisAlignment.end,
                    children: [
                      InkWell(
                        onTap: _pickImages,
                        child: SizedBox(
                          height: 100,
                          width: 80,
                          child: Align(
                            alignment: Alignment.bottomCenter,
                            child: Container(
                              height: 80,
                              width: 80,
                              decoration: BoxDecoration(
                                  color:
                                      const Color.fromARGB(255, 156, 192, 112),
                                  borderRadius: BorderRadius.circular(6)),
                              child: const Center(
                                child: Icon(
                                  Icons.photo,
                                  color: Colors.white,
                                ),
                              ),
                            ),
                          ),
                        ),
                      ),
                      const SizedBox(width: 12),
                      if (_imageFiles.isNotEmpty || _imagePath.isNotEmpty)
                        Expanded(
                          child: SizedBox(
                            height: 100,
                            child: ListView.builder(
                              // shrinkWrap: true,
                              scrollDirection: Axis.horizontal,
                              itemCount: _imageFiles.length + _imagePath.length,
                              itemBuilder: (context, index) {
                                return SizedBox(
                                  height: 100,
                                  width: 100,
                                  child: Stack(
                                    children: [
                                      Positioned(
                                          bottom: 0,
                                          left: 0,
                                          child: index > _imagePath.length - 1
                                              ? Image.file(
                                                  _imageFiles[index -
                                                      _imagePath.length],
                                                  width: 80,
                                                  height: 80,
                                                  fit: BoxFit.cover,
                                                )
                                              : CachedNetworkImage(
                                                  imageUrl:
                                                      APIService.instance
                                                          .resolveMediaUrl(
                                                              _imagePath[index]),
                                                  width: 80,
                                                  height: 80,
                                                  fit: BoxFit.cover,
                                                )),
                                      Positioned(
                                        top: 0,
                                        right: 0,
                                        child: IconButton(
                                            onPressed: () {
                                              if (index >
                                                  _imageFiles.length - 1) {
                                                _imageFiles.removeAt(
                                                    index - _imagePath.length);
                                              } else {
                                                _imagePath.removeAt(index);
                                              }
                                              setState(() {});
                                            },
                                            icon: Container(
                                              decoration: const BoxDecoration(
                                                color: Colors.white,
                                                shape: BoxShape.circle,
                                              ),
                                              child: const Icon(
                                                Icons.cancel,
                                                color: Colors.red,
                                              ),
                                            )),
                                      )
                                    ],
                                  ),
                                );
                              },
                            ),
                          ),
                        ),
                    ],
                  ),
                  Align(
                      alignment: Alignment.centerRight,
                      child: Text("${_imageFiles.length}/4")),
                  const SizedBox(height: 20),
                  Row(
                    mainAxisAlignment: MainAxisAlignment.end,
                    children: [
                      TextButton(
                        onPressed: () {
                          Navigator.of(context).pop();
                        },
                        child: const Text(
                          "Huỷ",
                          style: TextStyle(
                              color: Colors.red,
                              fontWeight: FontWeight.bold,
                              fontSize: 18),
                        ),
                      ),
                      ElevatedButton(
                        onPressed: submitReview,
                        child: const Text(
                          "Lưu",
                          style: TextStyle(
                            fontWeight: FontWeight.bold,
                            fontSize: 18,
                          ),
                        ),
                      ),
                    ],
                  ),
                ],
                ),
              ),
            ),
    );
  }
}
