import 'package:cp_restaurants/data/models/order_data.dart';
import 'package:cp_restaurants/network/api_util.dart';
import 'package:flutter/material.dart';

class OrdersScreen extends StatefulWidget {
  final int resId;

  const OrdersScreen({Key? key, required this.resId}) : super(key: key);

  @override
  State<OrdersScreen> createState() => _OrdersScreenState();
}

class _OrdersScreenState extends State<OrdersScreen> {
  bool isLoading = false;
  List<OrderData> newOrders = [];
  List<OrderData> acceptedOrders = [];
  List<OrderData> rejectedOrders = [];

  Future<void> getListOrder() async {
    // try {
    var response = await APIService.instance
        .request('/api/Orders/restaurant/${widget.resId}', DioMethod.get);

    if (response.statusCode == 200) {
      List<OrderData> allOrders = (response.data as List)
          .map((json) => OrderData.fromJson(json as Map<String, dynamic>))
          .toList();

      // Chia danh sách theo trạng thái
      newOrders = allOrders.where((order) => order.status == 0).toList();
      acceptedOrders = allOrders.where((order) => order.status == 1).toList();
      rejectedOrders = allOrders.where((order) => order.status == 2).toList();
    } else {
      throw Exception("Không có dữ liệu đơn hàng");
    }
    setState(() {
      isLoading = false;
    });
  }

  @override
  void initState() {
    getListOrder();
    super.initState();
  }

  @override
  Widget build(BuildContext context) {
    return DefaultTabController(
      length: 3,
      child: Scaffold(
        appBar: AppBar(
          backgroundColor: Colors.green,
          leading: IconButton(
            icon: const Icon(Icons.arrow_back, color: Colors.white),
            onPressed: () => Navigator.pop(context),
          ),
          title: const Text(
            "Danh sách đơn hàng",
            style: TextStyle(color: Colors.white, fontWeight: FontWeight.bold),
          ),
          bottom: const TabBar(
            labelColor: Colors.white,
            unselectedLabelColor: Colors.greenAccent,
            indicatorColor: Colors.white,
            tabs: [
              Tab(text: "Mới"),
              Tab(text: "Chấp nhận"),
              Tab(text: "Từ chối"),
            ],
          ),
        ),
        body: isLoading
            ? const Center(
                child: CircularProgressIndicator(),
              )
            : TabBarView(
                children: [
                  _buildOrderList(newOrders),
                  _buildOrderList(acceptedOrders),
                  _buildOrderList(rejectedOrders),
                ],
              ),
      ),
    );
  }

  Widget _buildOrderList(List<OrderData> filteredOrders) {
    if (filteredOrders.isEmpty) {
      return const Center(
        child: Text(
          "Không có đơn hàng",
          style: TextStyle(fontSize: 16, color: Colors.grey),
        ),
      );
    }

    return ListView.builder(
      itemCount: filteredOrders.length,
      itemBuilder: (context, index) {
        final order = filteredOrders[index];
        return Card(
          margin: const EdgeInsets.symmetric(vertical: 8, horizontal: 16),
          elevation: 4,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(12),
          ),
          child: Padding(
            padding: const EdgeInsets.all(8.0),
            child: Row(
              children: [
                CircleAvatar(
                  backgroundColor: Colors.green,
                  child: Text(
                    "${order.numOfMembers}",
                    style: const TextStyle(color: Colors.white),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        order.name,
                        style: const TextStyle(
                            fontWeight: FontWeight.bold, fontSize: 16),
                      ),
                      const SizedBox(height: 4),
                      Text(
                        "Thời gian: ${order.reservationTime}\nYêu cầu: ${order.specialRequest}",
                        style:
                            const TextStyle(fontSize: 14, color: Colors.grey),
                      ),
                    ],
                  ),
                ),
                Row(
                  children: [
                    if (order.status == 0 || order.status == 1)
                      IconButton(
                        icon: const Icon(Icons.close, color: Colors.red),
                        onPressed: () =>
                            _showConfirmationDialog(context, order, false),
                      ),
                    if (order.status == 0 || order.status == 2)
                      IconButton(
                        icon: const Icon(Icons.check, color: Colors.green),
                        onPressed: () =>
                            _showConfirmationDialog(context, order, true),
                      ),
                  ],
                ),
              ],
            ),
          ),
        );
      },
    );
  }

  void _showConfirmationDialog(
      BuildContext context, OrderData order, bool isAccept) {
    showDialog(
      context: context,
      builder: (BuildContext context) {
        return AlertDialog(
          title: Text(isAccept ? "Xác nhận đồng ý" : "Xác nhận từ chối"),
          content: Text(
            isAccept
                ? "Bạn có chắc muốn đồng ý đơn hàng của ${order.name} không?"
                : "Bạn có chắc muốn từ chối đơn hàng của ${order.name} không?",
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.of(context).pop(), // Đóng dialog
              child: const Text("Hủy"),
            ),
            TextButton(
              onPressed: () async {
                await APIService.instance.request(
                    '/api/Orders/${order.id}/status', DioMethod.put,
                    formData: isAccept ? 1 : 2);
                getListOrder();
                Navigator.of(context).pop(); // Đóng dialog
              },
              child: Text(
                isAccept ? "Đồng ý" : "Từ chối",
                style: TextStyle(color: isAccept ? Colors.green : Colors.red),
              ),
            ),
          ],
        );
      },
    );
  }
}
