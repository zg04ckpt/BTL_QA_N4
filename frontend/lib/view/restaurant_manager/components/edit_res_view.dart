import 'dart:developer';
import 'dart:io';

import 'package:cached_network_image/cached_network_image.dart';
import 'package:cp_restaurants/common_widget/address_picker.dart';
import 'package:cp_restaurants/common_widget/dialog/app_dialog.dart';
import 'package:cp_restaurants/common_widget/loading_widget.dart';
import 'package:cp_restaurants/data/containt.dart';
import 'package:cp_restaurants/data/models/address.dart';
import 'package:cp_restaurants/data/models/local_address.dart';
import 'package:cp_restaurants/global/global_data.dart';
import 'package:cp_restaurants/network/api_util.dart';
import 'package:cp_restaurants/services/location_provider.dart';
import 'package:cp_restaurants/services/restaurant_provider.dart';
import 'package:cp_restaurants/view/auth/signup_view.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:location_picker_flutter_map/location_picker_flutter_map.dart';

import 'package:provider/provider.dart';

import '../../../common/app_picker.dart';
import '../../../data/models/restaurant.dart';
import '../../../services/image_service.dart';

class EditResView extends StatefulWidget {
  const EditResView({Key? key, required this.fObj}) : super(key: key);

  final Restaurant fObj;

  @override
  State<EditResView> createState() => _EditResViewState();
}

class _EditResViewState extends State<EditResView> {
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
  List<String> _imageUrl = [];

  String resTypes = "";

  UniqueKey imagesListKey = UniqueKey();
  bool isLoading = false;

  late Restaurant resData;

  @override
  void initState() {
    super.initState();
    initData();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      lat = context.read<LocationProvider>().currentPostion?.latitude ?? 0;
      long = context.read<LocationProvider>().currentPostion?.longitude ?? 0;
      setState(() {});
    });
  }

  void initData() {
    resData = widget.fObj;
    _nameController.text = resData.name;
    _addressDetailController.text = resData.address.detail;
    _emailController.text = resData.email;
    _phoneNumberController.text = resData.phoneNumber;
    _descriptionController.text = resData.description;
    address = widget.fObj.address;
    _imageUrl = resData.photoUrls;
    resTypes = resData.category;
    setState(() {});
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

    setState(() {
      isLoading = true;
    });
    List<String> imageUrls = [];
    try {
      for (var file in _imageFiles) {
        String url = await ImageService.uploadImage(file);
        imageUrls.add(url);
      }
      String avtUrl = resData.avtImage;
      if (avatarImage != null) {
        avtUrl = await ImageService.uploadImage(avatarImage!);
      }
      Restaurant newRes = Restaurant(
        id: widget.fObj.id,
        address: address!,
        description: _descriptionController.text,
        photoUrls: imageUrls,
        cateId:
            restaurantTypes.keys.toList().indexWhere((key) => key == resTypes) +
                1,
        email: _emailController.text,
        phoneNumber: _phoneNumberController.text,
        status: 1,
        userId: GlobalData.instance.userData?.userId ?? 0,
        avtImage: avtUrl,
        name: _nameController.text,
      );
      log(newRes.toJson().toString());
      bool result =
          await context.read<RestaurantProvider>().editRestaurant(newRes);
      if (result) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            backgroundColor: Colors.green,
            content: Text(
              "Sửa thông tin nhà hàng mới thành công",
              style: TextStyle(color: Colors.white),
            ),
          ),
        );
        Navigator.of(context).pop();
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

  Future<bool?> _showCancelConfirmationDialog() async {
    return showDialog<bool>(
      context: context,
      builder: (BuildContext context) {
        return AlertDialog(
          title: const Text('Huỷ'),
          content: const Text('Bạn có muốn huỷ các thay đổi?'),
          actions: <Widget>[
            TextButton(
              child: const Text('Không'),
              onPressed: () => Navigator.of(context).pop(false),
            ),
            TextButton(
              child: const Text('Có'),
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
              title: const Text('Sửa thông tin nhà hàng',
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
                              ? Stack(
                                  children: [
                                    ClipRRect(
                                      borderRadius: BorderRadius.circular(12),
                                      child: CachedNetworkImage(
                                        imageUrl:
                                            '${APIService.instance.baseUrl}/${widget.fObj.avtImage}',
                                        fit: BoxFit.cover,
                                      ),
                                    ),
                                    Center(
                                      child: Container(
                                        padding: const EdgeInsets.all(8),
                                        decoration: BoxDecoration(
                                          color: Colors.white,
                                          borderRadius:
                                              BorderRadius.circular(12),
                                        ),
                                        child: Column(
                                          mainAxisSize: MainAxisSize.min,
                                          mainAxisAlignment:
                                              MainAxisAlignment.center,
                                          children: [
                                            Image.asset(
                                              'assets/img/photo.png',
                                              width: 30,
                                              height: 30,
                                            ),
                                          ],
                                        ),
                                      ),
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
                          return null;
                        },
                      ),
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
                        initData: LocalAddress(
                            district: address?.district,
                            province: address?.city,
                            ward: address?.ward),
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
                            return 'Please enter an address';
                          }
                          return null;
                        },
                      ),
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
                          "Toạ độ: $lat:$long}",
                          style: const TextStyle(
                              color: Colors.green, fontSize: 16),
                        ),
                      ),
                      const SizedBox(height: 20),
                      if (lat != 0)
                        SizedBox(
                          height: 300,
                          // width: 300,
                          child: FlutterLocationPicker(
                            initPosition: LatLong(lat, long),
                            selectLocationButtonStyle: ButtonStyle(
                              backgroundColor:
                                  WidgetStateProperty.all(Colors.blue),
                            ),
                            selectedLocationButtonTextstyle:
                                const TextStyle(fontSize: 18),
                            selectLocationButtonText: 'Lấy vị trí này',
                            selectLocationButtonLeadingIcon:
                                const Icon(Icons.check),
                            initZoom: 11,
                            minZoomLevel: 5,
                            maxZoomLevel: 16,
                            trackMyPosition: true,
                            onError: (e) => log(e.toString()),
                            onPicked: (pickedData) {
                              setState(() {
                                lat = pickedData.latLong.latitude;
                                long = pickedData.latLong.longitude;
                              });
                            },
                            onChanged: (pickedData) {
                              if (kDebugMode)
                                print(pickedData.latLong.latitude);
                              if (kDebugMode)
                                print(pickedData.latLong.longitude);
                              if (kDebugMode) print(pickedData.address);
                              if (kDebugMode) print(pickedData.addressData);
                            },
                          ),
                        ),
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
                          if (_imageFiles.isNotEmpty || _imageUrl.isNotEmpty)
                            Expanded(
                              child: SizedBox(
                                height: 100,
                                child: ListView.builder(
                                  // shrinkWrap: true,
                                  scrollDirection: Axis.horizontal,
                                  itemCount:
                                      _imageFiles.length + _imageUrl.length,
                                  itemBuilder: (context, index) {
                                    return SizedBox(
                                      height: 100,
                                      width: 100,
                                      child: Stack(
                                        children: [
                                          Positioned(
                                              bottom: 0,
                                              left: 0,
                                              child:
                                                  index > _imageUrl.length - 1
                                                      ? Image.file(
                                                          _imageFiles[index -
                                                              _imageUrl.length],
                                                          width: 80,
                                                          height: 80,
                                                          fit: BoxFit.cover,
                                                        )
                                                      : CachedNetworkImage(
                                                          imageUrl:
                                                              '${APIService.instance.baseUrl}/${_imageUrl[index]}',
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
                                                    _imageFiles.removeAt(index -
                                                        _imageUrl.length);
                                                  } else {
                                                    _imageUrl.removeAt(index);
                                                  }
                                                  setState(() {});
                                                },
                                                icon: Container(
                                                  decoration:
                                                      const BoxDecoration(
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
                            onPressed: () {},
                            child: Text(
                              resData.status == 3 ? 'Đóng cửa' : 'Mở cửa',
                              style: TextStyle(
                                  color: resData.status == 3
                                      ? Colors.red
                                      : Colors.green,
                                  fontSize: 20,
                                  fontWeight: FontWeight.bold),
                            ),
                          ),
                          ElevatedButton(
                            onPressed: _saveRestaurant,
                            child: const Text(
                              'Lưu',
                              style: TextStyle(
                                  fontSize: 20, fontWeight: FontWeight.bold),
                            ),
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
