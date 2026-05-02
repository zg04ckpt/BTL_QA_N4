import 'dart:developer';

import 'package:cached_network_image/cached_network_image.dart';
import 'package:cp_restaurants/data/models/review_model.dart';
import 'package:cp_restaurants/global/global_data.dart';
import 'package:cp_restaurants/network/api_util.dart';
import 'package:cp_restaurants/network/url_helper.dart';
import 'package:cp_restaurants/view/auth/signup_view.dart';
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

  /// Thông báo từ API (vd. bình luận tiêu cực / ML) — hiển thị ngay trên popup.
  String? _bannerMessage;

  static String? _readApiMessage(dynamic data) {
    if (data is! Map) return null;
    final m = data['message'] ?? data['Message'];
    return m?.toString();
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

  Future<void> submitReview() async {
    setState(() {
      isLoading = true;
      _bannerMessage = null;
    });

    try {
      for (var img in _imageFiles) {
        final up = await APIService.instance.uploadImage(img);
        if (up.statusCode != 200 && up.statusCode != 201) {
          if (!mounted) return;
          setState(() {
            isLoading = false;
            _bannerMessage =
                'Tải ảnh lên thất bại (${up.statusCode}). Thử lại sau.';
          });
          return;
        }
        final data = up.data;
        if (data is Map && data['image'] != null) {
          _imagePath.add(data['image'].toString());
        }
      }

      final reviewModel = ReviewModel(
        id: widget.initReview?.id,
        imageUrls: List<String>.from(_imagePath),
        rate: _rating,
        resId: widget.resId,
        review: _reviewController.text,
        userName: GlobalData.instance.userData?.name ?? "Giấu tên",
        userId: GlobalData.instance.userData?.userId ?? 0,
        createDate: DateTime.now().millisecondsSinceEpoch,
      );

      log(widget.initReview == null
          ? reviewModel.toCreateJson().toString()
          : reviewModel.toUpdateJson().toString());

      final Response<dynamic> response = widget.initReview == null
          ? await APIService.instance.request(
              '/api/reviews',
              DioMethod.post,
              formData: reviewModel.toCreateJson(),
            )
          : await APIService.instance.request(
              '/api/reviews/${reviewModel.id}',
              DioMethod.put,
              formData: reviewModel.toUpdateJson(),
            );

      log(response.statusCode.toString());
      if (!mounted) return;

      if (response.statusCode == 200) {
        showSnackBar(
          context,
          widget.initReview == null
              ? "Thêm đánh giá thành công"
              : "Sửa đánh giá thành công",
          false,
        );
        widget.onSubmitedReview();
        Navigator.of(context).pop();
        return;
      }

      final serverMsg = _readApiMessage(response.data);
      setState(() {
        _bannerMessage =
            serverMsg ?? 'Không gửi được (${response.statusCode}).';
      });
    } on DioException catch (e, st) {
      log('submitReview DioException: $e', stackTrace: st);
      if (!mounted) return;
      var msg = _readApiMessage(e.response?.data) ??
          (e.message != null && e.message!.isNotEmpty
              ? e.message!
              : 'Gửi đánh giá thất bại');
      setState(() {
        _bannerMessage = msg;
      });

    } catch (e, st) {
      log('submitReview: $e', stackTrace: st);
      if (!mounted) return;
      setState(() {
        _bannerMessage = e.toString();
      });
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
    final maxH = MediaQuery.sizeOf(context).height * 0.88;
    final maxW = MediaQuery.sizeOf(context).width - 32;

    Widget scrollableForm() {
      return SingleChildScrollView(
        child: Padding(
          padding: const EdgeInsets.all(16.0),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
                  if (_bannerMessage != null &&
                      _bannerMessage!.trim().isNotEmpty)
                    Padding(
                      padding: const EdgeInsets.only(bottom: 12),
                      child: Material(
                        color: Colors.deepOrange.shade50,
                        borderRadius: BorderRadius.circular(10),
                        child: Padding(
                          padding: const EdgeInsets.all(12),
                          child: Row(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Icon(
                                Icons.warning_amber_rounded,
                                color: Colors.deepOrange.shade800,
                                size: 26,
                              ),
                              const SizedBox(width: 10),
                              Expanded(
                                child: Text(
                                  _bannerMessage!,
                                  style: TextStyle(
                                    color: Colors.grey.shade900,
                                    fontSize: 14,
                                    height: 1.35,
                                  ),
                                ),
                              ),
                              InkWell(
                                onTap: () {
                                  setState(() => _bannerMessage = null);
                                },
                                child: Padding(
                                  padding: const EdgeInsets.only(left: 4),
                                  child: Icon(
                                    Icons.close,
                                    size: 20,
                                    color: Colors.grey.shade700,
                                  ),
                                ),
                              ),
                            ],
                          ),
                        ),
                      ),
                    ),
                  Text(
                    widget.restaurantName ?? "Edit",
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
                    onChanged: (_) {
                      if (_bannerMessage != null) {
                        setState(() => _bannerMessage = null);
                      }
                    },
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
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
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
                      const SizedBox(width: 16),
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
                                                  imageUrl: resolveMediaUrl(
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
                        )
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
      );
    }

    return Dialog(
      insetPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 24),
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(15),
      ),
      child: ConstrainedBox(
        constraints: BoxConstraints(maxWidth: maxW, maxHeight: maxH),
        child: isLoading
            ? Container(
                padding: const EdgeInsets.all(24),
                decoration: BoxDecoration(
                  borderRadius: BorderRadius.circular(12),
                  color: Colors.green[100],
                ),
                child: const Center(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      CircularProgressIndicator(),
                      SizedBox(height: 20),
                      Text("Đang tải đánh giá…"),
                    ],
                  ),
                ),
              )
            : scrollableForm(),
      ),
    );
  }
}
