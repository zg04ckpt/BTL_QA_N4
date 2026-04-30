import 'package:cp_restaurants/common/color_extension.dart';
import 'package:cp_restaurants/global/global_data.dart';
import 'package:cp_restaurants/services/order_provider.dart';
import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import 'package:provider/provider.dart';

class OrderHistoryView extends StatefulWidget {
  const OrderHistoryView({super.key});

  @override
  State<OrderHistoryView> createState() => _OrderHistoryViewState();
}

class _OrderHistoryViewState extends State<OrderHistoryView> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (GlobalData.instance.userData != null) {
        context
            .read<OrderProvider>()
            .fetchUserOrders(GlobalData.instance.userData!.userId);
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        backgroundColor: Colors.white,
        elevation: 0.5,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back, color: Colors.black),
          onPressed: () => Navigator.pop(context),
        ),
        title: const Text(
          "Lịch sử đặt bàn",
          style: TextStyle(
              color: Colors.black, fontSize: 20, fontWeight: FontWeight.w700),
        ),
      ),
      backgroundColor: TColor.bg,
      body: Consumer<OrderProvider>(
        builder: (context, orderProvider, child) {
          if (orderProvider.isLoading) {
            return const Center(child: CircularProgressIndicator());
          }

          if (orderProvider.userOrders.isEmpty) {
            return const Center(
              child: Text(
                "Bạn chưa có đơn đặt bàn nào.",
                style: TextStyle(fontSize: 16, color: Colors.grey),
              ),
            );
          }

          return ListView.builder(
            padding: const EdgeInsets.symmetric(vertical: 15, horizontal: 12),
            itemCount: orderProvider.userOrders.length,
            itemBuilder: (context, index) {
              final order = orderProvider.userOrders[index];
              return Container(
                margin: const EdgeInsets.only(bottom: 12),
                padding: const EdgeInsets.all(15),
                decoration: BoxDecoration(
                  color: Colors.white,
                  borderRadius: BorderRadius.circular(12),
                  boxShadow: [
                    BoxShadow(
                      color: Colors.black.withOpacity(0.05),
                      blurRadius: 10,
                      offset: const Offset(0, 4),
                    ),
                  ],
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Text(
                          "Đơn #${order.id}",
                          style: TextStyle(
                              color: TColor.primary,
                              fontWeight: FontWeight.w700,
                              fontSize: 16),
                        ),
                        Container(
                          padding: const EdgeInsets.symmetric(
                              horizontal: 10, vertical: 4),
                          decoration: BoxDecoration(
                            color: _getStatusColor(order.status).withOpacity(0.1),
                            borderRadius: BorderRadius.circular(20),
                          ),
                          child: Text(
                            _getStatusText(order.status),
                            style: TextStyle(
                                color: _getStatusColor(order.status),
                                fontSize: 12,
                                fontWeight: FontWeight.w600),
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 10),
                    _infoRow(Icons.person, "Tên: ${order.name}"),
                    _infoRow(Icons.calendar_today, "Ngày: ${order.reservationTime}"),
                    _infoRow(Icons.group, "Số người: ${order.numOfMembers}"),
                    if (order.specialRequest.isNotEmpty)
                      _infoRow(Icons.note, "Yêu cầu: ${order.specialRequest}"),
                    const Divider(height: 20),
                    Text(
                      "Ngày đặt: ${DateFormat('dd/MM/yyyy HH:mm').format(DateTime.fromMillisecondsSinceEpoch(order.createdAt))}",
                      style: const TextStyle(color: Colors.grey, fontSize: 12),
                    ),
                  ],
                ),
              );
            },
          );
        },
      ),
    );
  }

  Widget _infoRow(IconData icon, String text) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        children: [
          Icon(icon, size: 18, color: Colors.grey),
          const SizedBox(width: 8),
          Expanded(
            child: Text(
              text,
              style: const TextStyle(fontSize: 14, color: Colors.black87),
            ),
          ),
        ],
      ),
    );
  }

  Color _getStatusColor(int status) {
    switch (status) {
      case 0:
        return Colors.orange; // Chờ xác nhận
      case 1:
        return Colors.blue; // Đã xác nhận
      case 2:
        return Colors.green; // Đã hoàn thành
      case 3:
        return Colors.red; // Đã hủy
      default:
        return Colors.grey;
    }
  }

  String _getStatusText(int status) {
    switch (status) {
      case 0:
        return "Đang chờ";
      case 1:
        return "Đã xác nhận";
      case 2:
        return "Thành công";
      case 3:
        return "Đã hủy";
      default:
        return "Không xác định";
    }
  }
}
