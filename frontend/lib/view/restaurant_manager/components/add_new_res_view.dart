import 'dart:developer';
import 'dart:io';

import 'package:cp_restaurants/common_widget/address_picker.dart';
import 'package:cp_restaurants/common_widget/dialog/app_dialog.dart';
import 'package:cp_restaurants/common_widget/loading_widget.dart';
import 'package:cp_restaurants/data/containt.dart';
import 'package:cp_restaurants/data/models/address.dart';
import 'package:cp_restaurants/global/global_data.dart';
import 'package:cp_restaurants/services/location_provider.dart';
import 'package:cp_restaurants/services/restaurant_provider.dart';
import 'package:cp_restaurants/common_widget/location_preview_map.dart';
import 'package:cp_restaurants/view/auth/signup_view.dart';
import 'package:flutter/material.dart';
import 'package:geocoding/geocoding.dart';
import 'package:google_maps_flutter/google_maps_flutter.dart';

import 'package:provider/provider.dart';

import '../../../common/app_picker.dart';
import '../../../data/models/restaurant.dart';
import '../../../services/image_service.dart';
import 'google_map_picker_view.dart';

class AddRestaurantPage extends StatefulWidget {
  const AddRestaurantPage({Key? key}) : super(key: key);

  @override
  State<AddRestaurantPage> createState() => _AddRestaurantPageState();
}

class _AddRestaurantPageState extends State<AddRestaurantPage> {
  final _formKey = GlobalKey<FormState>();
  final TextEditingController _nameController = TextEditingController();
  final TextEditingController _emailController = TextEditingController();
  final TextEditingController _descriptionController = TextEditingController();

  final TextEditingController _phoneNumberController = TextEditingController();

  final TextEditingController _addressController = TextEditingController();
  final TextEditingController _addressDetailController =
      TextEditingController();
  Address? address;

  final TextEditingController _categoryController = TextEditingController();

  double lat = 0;
  double long = 0;

  File? avatarImage;
  final List<File> _imageFiles = [];
  String resTypes = "";

  UniqueKey imagesListKey = UniqueKey();
  bool isLoading = false;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      lat = context.read<LocationProvider>().currentPostion?.latitude ?? 21.0278;
      long =
          context.read<LocationProvider>().currentPostion?.longitude ?? 105.8342;
      setState(() {});
    });
  }

  @override
  void dispose() {
    _nameController.dispose();
    _addressController.dispose();
    _categoryController.dispose();
    _addressDetailController.dispose();
    super.dispose();
  }

  Future<void> _saveRestaurant() async {
    final fields = [
      address?.district,
      address?.city,
      address?.ward,
    ];

    if (fields.any((field) => field == null || field.isEmpty)) {
      showSnackBar(
        context,
        "Vui lòng điền đủ các trường thông tin...!",
      );
      return;
    }
    if (_formKey.currentState!.validate()) {
      if (avatarImage == null) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text("Chọn ảnh đại diện để tiếp tục"),
          ),
        );
        return;
      }
      final safeAddress = address!;
      final normalizedAddress = Address(
        id: safeAddress.id,
        street: safeAddress.street,
        city: safeAddress.city,
        district: safeAddress.district,
        ward: safeAddress.ward,
        detail: _addressDetailController.text.trim(),
        lat: lat,
        lon: long,
      );

      setState(() {
        isLoading = true;
      });
      List<String> imageUrls = [];
      try {
        for (var file in _imageFiles) {
          String url = await ImageService.uploadImage(file);
          imageUrls.add(url);
        }

        String avtUrl = await ImageService.uploadImage(avatarImage!);

        Restaurant newRes = Restaurant(
          id: -1,
          address: normalizedAddress,
          description: _descriptionController.text,
          averageScore: 0,
          photoUrls: imageUrls,
          cateId: restaurantTypes.keys
                  .toList()
                  .indexWhere((key) => key == resTypes) +
              1,
          email: _emailController.text,
          phoneNumber: _phoneNumberController.text,
          status: 1,
          totalReviews: 0,
          userId: GlobalData.instance.userData?.userId ?? 0,
          avtImage: avtUrl,
          name: _nameController.text,
        );
        log(newRes.toJson().toString());
        bool result = await context
            .read<RestaurantProvider>()
            .createRestaurant(newRes.toJson());
        if (result) {
          Navigator.of(context).pop();
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(
              backgroundColor: Colors.green,
              content: Text(
                "Thêm nhà hàng mới thành công,vui lòng chờ quản trị viên xác nhận",
                style: TextStyle(color: Colors.white),
              ),
            ),
          );
        } else {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(
              backgroundColor: Colors.red,
              content: Text(
                "Đã có lỗi xảy ra, vui lòng thử lại sau",
              ),
            ),
          );
        }

        setState(() {
          isLoading = false;
        });

        // Navigator.pop(context, true);
      } catch (e) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            backgroundColor: Colors.red,
            content: Text(
              "Đã có lỗi xảy ra, vui lòng thử lại sau",
            ),
          ),
        );
      }
    }
  }

  Future<void> _pickLocationWithGoogleMap() async {
    final picked = await Navigator.of(context).push<LatLng>(
      MaterialPageRoute(
        builder: (_) => GoogleMapPickerView(
          initialLat: lat,
          initialLon: long,
        ),
      ),
    );

    if (picked == null) return;

    lat = picked.latitude;
    long = picked.longitude;

    try {
      final places = await placemarkFromCoordinates(lat, long);
      if (places.isNotEmpty && _addressDetailController.text.trim().isEmpty) {
        final p = places.first;
        final text = [p.street, p.subLocality, p.locality]
            .where((e) => e != null && e.trim().isNotEmpty)
            .join(", ");
        if (text.isNotEmpty) {
          _addressDetailController.text = text;
        }
      }
    } catch (_) {}

    if (mounted) {
      setState(() {});
    }
  }

  Future<bool?> _showCancelConfirmationDialog() async {
    return showDialog<bool>(
      context: context,
      builder: (BuildContext context) {
        return AlertDialog(
          title: const Text('Huỷ'),
          content: const Text('Are you sure you want to cancel?'),
          actions: <Widget>[
            TextButton(
              child: const Text('No'),
              onPressed: () => Navigator.of(context).pop(false),
            ),
            TextButton(
              child: const Text('Yes'),
              onPressed: () => Navigator.of(context).pop(true),
            ),
          ],
        );
      },
    );
  }

  @override
  Widget build(BuildContext context) {
    return WillPopScope(
      onWillPop: () async {
        bool? shouldCancel = await _showCancelConfirmationDialog();
        return shouldCancel ?? false;
      },
      child: Stack(
        children: [
          Scaffold(
            appBar: AppBar(
              title: const Text('Thêm nhà hàng',
                  style: TextStyle(fontWeight: FontWeight.bold)),
              leading: IconButton(
                icon: const Icon(Icons.arrow_back),
                onPressed: () async {
                  bool? shouldCancel = await _showCancelConfirmationDialog();
                  if (shouldCancel == true) {
                    Navigator.pop(context);
                  }
                },
              ),
            ),
            body: SingleChildScrollView(
              child: Padding(
                padding: const EdgeInsets.all(16.0),
                child: Form(
                  key: _formKey,
                  child: Column(
                    children: <Widget>[
                      GestureDetector(
                        onTap: () async {
                          avatarImage = await AppPicker.pickImage();
                          if (avatarImage != null) {
                            setState(() {});
                          }
                        },
                        child: Container(
                          width: 160,
                          height: 160,
                          decoration: BoxDecoration(
                            borderRadius: BorderRadius.circular(12),
                            border: Border.all(color: Colors.grey),
                            color: Colors.white,
                          ),
                          child: avatarImage == null
                              ? Column(
                                  mainAxisAlignment: MainAxisAlignment.center,
                                  children: [
                                    Image.asset(
                                      'assets/img/photo.png',
                                      width: 40,
                                      height: 40,
                                    ),
                                    const SizedBox(height: 8),
                                    const Text(
                                      'Chọn ảnh đại diện',
                                      style: TextStyle(
                                          fontSize: 12, color: Colors.black),
                                      textAlign: TextAlign.center,
                                    ),
                                  ],
                                )
                              : ClipRRect(
                                  borderRadius: BorderRadius.circular(12),
                                  child: Image.file(avatarImage!,
                                      fit: BoxFit.cover)),
                        ),
                      ),
                      TextFormField(
                        controller: _nameController,
                        decoration:
                            const InputDecoration(labelText: 'Tên nhà hàng'),
                        validator: (value) {
                          if (value == null || value.isEmpty) {
                            return 'Vui lòng nhập tên nhà hàng';
                          }
                          return null;
                        },
                      ),
                      TextFormField(
                        controller: _emailController,
                        decoration: const InputDecoration(labelText: 'Email'),
                        validator: (value) {
                          if (value == null || value.isEmpty) {
                            return 'Vui lòng nhập email';
                          }
                          final emailRegex = RegExp(
                              r'^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$');
                          if (!emailRegex.hasMatch(value)) {
                            return 'Email không hợp lệ';
                          }
                          return null;
                        },
                      ),
                      TextFormField(
                        controller: _phoneNumberController,
                        decoration:
                            const InputDecoration(labelText: 'Số điện thoại'),
                        validator: (value) {
                          if (value == null || value.isEmpty) {
                            return 'Vui lòng nhập số điện thoại';
                          }
                          if (value.length > 11 || value.length < 9) {
                            return "Số điện thoại trong khoảng 9-11 số";
                          }
                          return null;
                        },
                      ),
                      const SizedBox(height: 8),
                      Container(
                        height: 150,
                        padding: const EdgeInsets.all(8),
                        decoration: BoxDecoration(
                            borderRadius: BorderRadius.circular(8),
                            border: Border.all(width: 1.5)),
                        child: TextFormField(
                          maxLines: 10,
                          maxLength: 2000,
                          controller: _descriptionController,
                          decoration: const InputDecoration(
                              labelText: 'Mô tả',
                              contentPadding: EdgeInsets.zero),
                          validator: (value) {
                            if (value == null || value.isEmpty) {
                              return 'Vui lòng nhập Mô tả';
                            }
                            return null;
                          },
                        ),
                      ),
                      AddressPicker(
                        onAddressChanged: (vlue) {
                          var addr = Address(
                            id: 0,
                            street: "",
                            city: vlue.province ?? "",
                            district: vlue.district ?? "",
                            ward: vlue.ward ?? "",
                            detail: "",
                          );
                          address = addr;
                        },
                        buildItem: (text) {
                          return Text(text ?? "",
                              style: const TextStyle(color: Colors.blue));
                        },
                      ),
                      TextFormField(
                        controller: _addressDetailController,
                        decoration: const InputDecoration(
                            labelText: 'Địa chỉ chi tiết'),
                        validator: (value) {
                          if (value == null || value.isEmpty) {
                            return 'Vui lòng nhập địa chỉ chi tiết';
                          }
                          return null;
                        },
                      ),
                      const SizedBox(height: 12),
                      GestureDetector(
                        onTap: () => AppDialog.showResTypeDialog(
                          context,
                          onConfirm: (p0) {
                            resTypes = p0;
                            setState(() {});
                          },
                          initType: resTypes,
                        ),
                        child: AbsorbPointer(
                          child: TextField(
                            decoration: InputDecoration(
                              labelText: resTypes == ""
                                  ? 'Chọn danh mục nhà hàng'
                                  : resTypes,
                              hintText: 'Nhấn để chọn',
                              border: const OutlineInputBorder(),
                            ),
                          ),
                        ),
                      ),
                      const SizedBox(height: 12),
                      Align(
                        alignment: Alignment.centerLeft,
                        child: Text(
                          "Toa do chinh xac: $lat, $long",
                          style: const TextStyle(
                              color: Colors.green, fontSize: 16),
                        ),
                      ),
                      const SizedBox(height: 8),
                      SizedBox(
                        width: double.infinity,
                        child: OutlinedButton.icon(
                          onPressed: _pickLocationWithGoogleMap,
                          icon: const Icon(Icons.map_outlined),
                          label: const Text("Chọn vị trí trên bản đồ"),
                        ),
                      ),
                      const SizedBox(height: 20),
                      if (lat != 0 || long != 0)
                        LocationPreviewMap(lat: lat, lon: long),
                      const SizedBox(height: 20),
                      const Align(
                        alignment: Alignment.centerLeft,
                        child: Text(
                          "Ảnh mô tả",
                          style: TextStyle(
                            fontSize: 16,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                      ),
                      const SizedBox(height: 12),
                      Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        crossAxisAlignment: CrossAxisAlignment.end,
                        children: [
                          InkWell(
                            onTap: () async {
                              List<File>? newList = [];
                              newList = await AppPicker.pickImages();
                              if (newList != null) {
                                _imageFiles.addAll(newList);
                              }
                              setState(() {
                                imagesListKey = UniqueKey();
                              });
                            },
                            child: Container(
                              height: 80,
                              width: 80,
                              decoration: BoxDecoration(
                                  color:
                                      const Color.fromARGB(255, 156, 192, 112),
                                  borderRadius: BorderRadius.circular(0)),
                              child: const Center(
                                child: Icon(
                                  Icons.add_photo_alternate_outlined,
                                  color: Colors.white,
                                  size: 36,
                                ),
                              ),
                            ),
                          ),
                          const SizedBox(width: 12),
                          if (_imageFiles.isNotEmpty)
                            Expanded(
                              key: imagesListKey,
                              child: SingleChildScrollView(
                                scrollDirection: Axis.horizontal,
                                child: Row(
                                  children: _imageFiles.map((file) {
                                    return Padding(
                                      padding: const EdgeInsets.only(right: 0),
                                      child: Stack(
                                        children: [
                                          Container(
                                            height: 90,
                                            width: 90,
                                            alignment: Alignment.bottomLeft,
                                            child: Image.file(
                                              file,
                                              width: 80,
                                              height: 80,
                                              fit: BoxFit.cover,
                                            ),
                                          ),
                                          const Positioned(
                                            top: 0,
                                            right: 0,
                                            child: Icon(
                                              Icons.highlight_remove_outlined,
                                              color: Colors.red,
                                            ),
                                          )
                                        ],
                                      ),
                                    );
                                  }).toList(),
                                ),
                              ),
                            ),
                        ],
                      ),
                      const SizedBox(height: 20),
                      Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          ElevatedButton(
                            onPressed: () async {
                              bool? shouldCancel =
                                  await _showCancelConfirmationDialog();
                              if (shouldCancel == true) {
                                Navigator.pop(context);
                              }
                            },
                            child: const Text('Huỷ'),
                          ),
                          ElevatedButton(
                            onPressed: _saveRestaurant,
                            child: const Text('Lưu'),
                          ),
                        ],
                      ),
                    ],
                  ),
                ),
              ),
            ),
          ),
          if (isLoading) const LoadingWidget()
        ],
      ),
    );
  }
}
