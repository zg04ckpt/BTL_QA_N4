import 'package:cp_restaurants/common/time_extension.dart';
import 'package:cp_restaurants/global/global_data.dart';
import 'package:cp_restaurants/network/api_util.dart';
import 'package:cp_restaurants/view/auth/signup_view.dart';
import 'package:flutter/material.dart';
import 'package:flutter_datetime_picker_plus/flutter_datetime_picker_plus.dart';

class OrderBottomSheet extends StatefulWidget {
  const OrderBottomSheet({super.key, required this.resId});
  final int resId;

  @override
  State<OrderBottomSheet> createState() => _OrderBottomSheetState();
}

class _OrderBottomSheetState extends State<OrderBottomSheet> {
  DateTime? choicedDateTime;
  final TextEditingController _couterController = TextEditingController();
  final TextEditingController _noteController = TextEditingController();
  final TextEditingController _phoneNumberController = TextEditingController();

  @override
  void initState() {
    super.initState();
    _phoneNumberController.text =
        GlobalData.instance.userData?.phoneNumber ?? "";
  }

  Future<void> addNewOrder() async {
    var response = await APIService.instance.request(
      "/api/Orders",
      DioMethod.post,
      param: {
        "name": GlobalData.instance.userData?.name ?? ".",
        "phoneNumber": _phoneNumberController.text,
        "email": GlobalData.instance.userData?.email ?? "",
        "userId": GlobalData.instance.userData?.userId ?? 0,
        "restaurantId": widget.resId,
        "numOfMembers": int.parse(_couterController.text),
        "reservationTime": choicedDateTime?.toFormattedString(),
        "specialRequest": _noteController.text,
        "createdAt": DateTime.now().millisecondsSinceEpoch
      },
    );
    if (response.statusCode == 200) {
      showSnackBar(context,
          "Gửi yêu cầu đặt bàn thành công, chờ nhân viên xác nhận", false);
      Navigator.pop(context);
    }
  }

  @override
  Widget build(BuildContext context) {
    final maxH = MediaQuery.sizeOf(context).height * 0.92;
    return SafeArea(
      child: Container(
        constraints: BoxConstraints(maxHeight: maxH),
        padding: const EdgeInsets.all(20),
        decoration: const BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.only(
            topLeft: Radius.circular(16),
            topRight: Radius.circular(16),
          ),
        ),
        child: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              IconButton(
                onPressed: () {
                  Navigator.of(context).pop();
                },
                icon: const Icon(
                  Icons.arrow_back_ios_new_outlined,
                ),
              ),
              const Text(
                "Đặt bàn",
                style: TextStyle(fontSize: 24, fontWeight: FontWeight.bold),
              ),
              const SizedBox(width: 20)
            ],
          ),
          const SizedBox(height: 20),
          Row(mainAxisAlignment: MainAxisAlignment.center, children: [
            const Expanded(
              flex: 2,
              child: Align(
                alignment: Alignment.centerRight,
                child: Text(
                  "Thời gian: ",
                  style: TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              flex: 4,
              child: InkWell(
                onTap: () {
                  DatePicker.showDateTimePicker(
                    context,
                    locale: LocaleType.vi,
                    currentTime: choicedDateTime,
                    onChanged: (time) {
                      setState(() {
                        choicedDateTime = time;
                      });
                    },
                  );
                },
                child: Container(
                  padding:
                      const EdgeInsets.symmetric(horizontal: 8, vertical: 8),
                  decoration: BoxDecoration(
                      borderRadius: BorderRadius.circular(6),
                      color: Colors.green.withOpacity(0.3),
                      border: Border.all(color: Colors.black),
                      boxShadow: const []),
                  child: Text(
                    choicedDateTime == null
                        ? "Chọn thời gian"
                        : choicedDateTime!.toFormattedString(),
                    maxLines: 3,
                    overflow: TextOverflow.ellipsis,
                    textAlign: TextAlign.center,
                    style: const TextStyle(
                      fontSize: 16,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                ),
              ),
            )
          ]),
          const SizedBox(height: 12),
          Row(mainAxisAlignment: MainAxisAlignment.center, children: [
            const Expanded(
              flex: 2,
              child: Align(
                alignment: Alignment.centerRight,
                child: Text(
                  "Số người: ",
                  style: TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              flex: 4,
              child: SizedBox(
                height: 50,
                // width: 200,
                child: TextField(
                  controller: _couterController,
                  keyboardType: const TextInputType.numberWithOptions(),
                  decoration: InputDecoration(
                    filled: true,
                    fillColor: Colors.white,
                    focusColor: Colors.white,
                    contentPadding: const EdgeInsets.symmetric(horizontal: 8),
                    focusedBorder: OutlineInputBorder(
                      borderSide:
                          const BorderSide(color: Colors.black, width: 2.0),
                      borderRadius: BorderRadius.circular(6),
                    ),
                    border: OutlineInputBorder(
                      borderSide:
                          const BorderSide(color: Colors.black, width: 2.0),
                      borderRadius: BorderRadius.circular(6),
                    ),
                    enabledBorder: OutlineInputBorder(
                      borderSide:
                          const BorderSide(color: Colors.black, width: 2.0),
                      borderRadius: BorderRadius.circular(6),
                    ),
                    hintText: 'Số người',
                    hintStyle: const TextStyle(color: Colors.grey),
                  ),
                ),
              ),
            ),
          ]),
          const SizedBox(height: 12),
          Row(mainAxisAlignment: MainAxisAlignment.center, children: [
            const Expanded(
              flex: 2,
              child: Align(
                alignment: Alignment.centerRight,
                child: Text(
                  "SDT: ",
                  style: TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              flex: 4,
              child: SizedBox(
                height: 50,
                // width: 200,
                child: TextField(
                  controller: _phoneNumberController,
                  keyboardType: const TextInputType.numberWithOptions(),
                  decoration: InputDecoration(
                    filled: true,
                    fillColor: Colors.white,
                    focusColor: Colors.white,
                    contentPadding: const EdgeInsets.symmetric(horizontal: 8),
                    focusedBorder: OutlineInputBorder(
                      borderSide:
                          const BorderSide(color: Colors.black, width: 2.0),
                      borderRadius: BorderRadius.circular(6),
                    ),
                    border: OutlineInputBorder(
                      borderSide:
                          const BorderSide(color: Colors.black, width: 2.0),
                      borderRadius: BorderRadius.circular(6),
                    ),
                    enabledBorder: OutlineInputBorder(
                      borderSide:
                          const BorderSide(color: Colors.black, width: 2.0),
                      borderRadius: BorderRadius.circular(6),
                    ),
                    hintText: 'Số điện thoại',
                    hintStyle: const TextStyle(color: Colors.grey),
                  ),
                ),
              ),
            ),
          ]),
          const SizedBox(height: 12),
          Row(mainAxisAlignment: MainAxisAlignment.center, children: [
            const Expanded(
              flex: 2,
              child: Align(
                alignment: Alignment.centerRight,
                child: Text(
                  "Ghi chú: ",
                  style: TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              flex: 4,
              child: SizedBox(
                height: 50,
                // width: 200,
                child: TextField(
                  controller: _noteController,
                  decoration: InputDecoration(
                    filled: true,
                    fillColor: Colors.white,
                    focusColor: Colors.white,
                    contentPadding: const EdgeInsets.symmetric(horizontal: 8),
                    focusedBorder: OutlineInputBorder(
                      borderSide:
                          const BorderSide(color: Colors.black, width: 2.0),
                      borderRadius: BorderRadius.circular(6),
                    ),
                    border: OutlineInputBorder(
                      borderSide:
                          const BorderSide(color: Colors.black, width: 2.0),
                      borderRadius: BorderRadius.circular(6),
                    ),
                    enabledBorder: OutlineInputBorder(
                      borderSide:
                          const BorderSide(color: Colors.black, width: 2.0),
                      borderRadius: BorderRadius.circular(6),
                    ),
                    hintText: 'Ghi chú',
                    hintStyle: const TextStyle(color: Colors.grey),
                  ),
                ),
              ),
            ),
          ]),
          const SizedBox(height: 20),
          Center(
            child: InkWell(
              onTap: () {
                addNewOrder();
              },
              child: Container(
                height: 50,
                constraints: const BoxConstraints(minWidth: 160),
                alignment: Alignment.center,
                padding: const EdgeInsets.symmetric(horizontal: 24),
                decoration: BoxDecoration(
                  borderRadius: BorderRadius.circular(12),
                  color: Colors.green,
                ),
                child: const Text(
                  "Xác nhận",
                  style: TextStyle(
                      color: Colors.white,
                      fontSize: 18,
                      fontWeight: FontWeight.bold),
                ),
              ),
            ),
          ),
            ],
          ),
        ),
      ),
    );
  }
}
