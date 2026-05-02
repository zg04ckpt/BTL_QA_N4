import 'package:cp_restaurants/common/color_extension.dart';
import 'package:cp_restaurants/common_widget/app_network_image.dart';
import 'package:cp_restaurants/data/models/user_data.dart';
import 'package:cp_restaurants/network/api_util.dart';
import 'package:dio/dio.dart';
import 'package:flutter/material.dart';

/// Quản lý user qua REST API ([UserController]). Trạng thái: **1** = hoạt động, **0** = khóa.
class UserManagementView extends StatefulWidget {
  const UserManagementView({super.key});

  @override
  State<UserManagementView> createState() => _UserManagementViewState();
}

enum _UserFilter { all, active, locked }

class _UserManagementViewState extends State<UserManagementView> {
  final TextEditingController _searchController = TextEditingController();
  List<UserData> _users = [];
  bool _loading = false;
  String? _error;
  _UserFilter _filter = _UserFilter.all;

  @override
  void initState() {
    super.initState();
    _loadUsers();
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  Future<void> _loadUsers() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final response = await APIService.instance.request(
        '/api/User/GetAllUsers',
        DioMethod.get,
      );
      if (response.statusCode != 200 || response.data == null) {
        setState(() => _error = 'HTTP ${response.statusCode}');
        return;
      }
      final raw = response.data;
      if (raw is! List) {
        setState(() => _error = 'Định dạng danh sách không hợp lệ');
        return;
      }
      final list = <UserData>[];
      for (final item in raw) {
        if (item is Map<String, dynamic>) {
          list.add(UserData.fromJson(item));
        } else if (item is Map) {
          list.add(UserData.fromJson(Map<String, dynamic>.from(item)));
        }
      }
      setState(() => _users = list);
    } on DioException catch (e) {
      setState(() => _error = e.response?.data?.toString() ?? e.message);
    } catch (e) {
      setState(() => _error = '$e');
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _confirmSetStatus(UserData u, int newStatus) async {
    final active = newStatus == 1;
    final ok = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text(active ? 'Mở khóa tài khoản?' : 'Khóa tài khoản?'),
        content: Text(
          active
              ? 'Người dùng "${u.name}" sẽ có thể đăng nhập lại.'
              : 'Người dùng "${u.name}" sẽ không thể đăng nhập cho đến khi được mở khóa.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text('Huỷ'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(ctx, true),
            style: FilledButton.styleFrom(
              backgroundColor: active ? TColor.primary : TColor.color1,
            ),
            child: Text(active ? 'Mở khóa' : 'Khóa'),
          ),
        ],
      ),
    );
    if (ok != true || !mounted) return;
    await _setUserStatus(u, newStatus);
  }

  Future<void> _setUserStatus(UserData u, int newStatus) async {
    try {
      final response = await APIService.instance.request(
        '/api/User/UpdateUser/${u.userId}',
        DioMethod.put,
        param: {'status': newStatus},
      );
      if (response.statusCode != null &&
          response.statusCode! >= 200 &&
          response.statusCode! < 300) {
        if (mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(
              content: Text(
                newStatus == 1 ? 'Đã mở khóa tài khoản' : 'Đã khóa tài khoản',
              ),
              behavior: SnackBarBehavior.floating,
            ),
          );
        }
        await _loadUsers();
      } else {
        if (mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(
              content: Text('Lỗi: HTTP ${response.statusCode}'),
              backgroundColor: TColor.color1,
            ),
          );
        }
      }
    } on DioException catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(e.response?.data?.toString() ?? e.message ?? '$e'),
            backgroundColor: TColor.color1,
          ),
        );
      }
    }
  }

  List<UserData> get _filtered {
    var list = _users;
    switch (_filter) {
      case _UserFilter.active:
        list = list.where((u) => u.state == 1).toList();
        break;
      case _UserFilter.locked:
        list = list.where((u) => u.state != 1).toList();
        break;
      case _UserFilter.all:
        break;
    }
    final q = _searchController.text.trim().toLowerCase();
    if (q.isEmpty) return list;
    return list
        .where((u) =>
            u.email.toLowerCase().contains(q) ||
            u.name.toLowerCase().contains(q) ||
            u.phoneNumber.toLowerCase().contains(q))
        .toList();
  }

  String _initials(UserData u) {
    final n = u.name.trim();
    if (n.isEmpty) {
      return u.email.isNotEmpty ? u.email[0].toUpperCase() : '?';
    }
    final parts = n.split(RegExp(r'\s+'));
    if (parts.length >= 2) {
      return '${parts.first[0]}${parts.last[0]}'.toUpperCase();
    }
    return n[0].toUpperCase();
  }

  @override
  Widget build(BuildContext context) {
    final rows = _filtered;

    return Scaffold(
      backgroundColor: TColor.bg,
      appBar: AppBar(
        title: const Text('Người dùng'),
        backgroundColor: Colors.white,
        foregroundColor: TColor.text,
        elevation: 0,
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh_rounded),
            onPressed: _loading ? null : _loadUsers,
            tooltip: 'Tải lại',
          ),
        ],
      ),
      body: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Container(
            color: Colors.white,
            padding: const EdgeInsets.fromLTRB(16, 0, 16, 12),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                TextField(
                  controller: _searchController,
                  decoration: InputDecoration(
                    hintText: 'Tìm theo tên, email, SĐT…',
                    prefixIcon: Icon(Icons.search_rounded, color: TColor.gray),
                    filled: true,
                    fillColor: TColor.bg,
                    border: OutlineInputBorder(
                      borderRadius: BorderRadius.circular(12),
                      borderSide: BorderSide.none,
                    ),
                    contentPadding: const EdgeInsets.symmetric(
                      horizontal: 12,
                      vertical: 12,
                    ),
                  ),
                  onChanged: (_) => setState(() {}),
                ),
                const SizedBox(height: 10),
                SingleChildScrollView(
                  scrollDirection: Axis.horizontal,
                  child: Row(
                    children: [
                      _FilterChip(
                        label: 'Tất cả (${_users.length})',
                        selected: _filter == _UserFilter.all,
                        onSelected: () =>
                            setState(() => _filter = _UserFilter.all),
                      ),
                      const SizedBox(width: 8),
                      _FilterChip(
                        label:
                            'Hoạt động (${_users.where((u) => u.state == 1).length})',
                        selected: _filter == _UserFilter.active,
                        onSelected: () =>
                            setState(() => _filter = _UserFilter.active),
                      ),
                      const SizedBox(width: 8),
                      _FilterChip(
                        label:
                            'Đã khóa (${_users.where((u) => u.state != 1).length})',
                        selected: _filter == _UserFilter.locked,
                        onSelected: () =>
                            setState(() => _filter = _UserFilter.locked),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
          if (_error != null)
            Padding(
              padding: const EdgeInsets.all(12),
              child: Material(
                color: TColor.color1.withValues(alpha: 0.12),
                borderRadius: BorderRadius.circular(8),
                child: Padding(
                  padding: const EdgeInsets.all(12),
                  child: Row(
                    children: [
                      Icon(Icons.error_outline, color: TColor.color1),
                      const SizedBox(width: 8),
                      Expanded(
                        child: Text(
                          _error!,
                          style: TextStyle(color: TColor.color1, fontSize: 13),
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ),
          Expanded(
            child: _loading
                ? Center(child: CircularProgressIndicator(color: TColor.primary))
                : rows.isEmpty
                    ? Center(
                        child: Column(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            Icon(Icons.people_outline,
                                size: 56, color: TColor.gray),
                            const SizedBox(height: 12),
                            Text(
                              'Không có người dùng phù hợp',
                              style: TextStyle(
                                color: TColor.gray,
                                fontSize: 16,
                              ),
                            ),
                          ],
                        ),
                      )
                    : RefreshIndicator(
                        color: TColor.primary,
                        onRefresh: _loadUsers,
                        child: ListView.separated(
                          padding: const EdgeInsets.all(16),
                          itemCount: rows.length,
                          separatorBuilder: (_, __) =>
                              const SizedBox(height: 10),
                          itemBuilder: (context, i) {
                            final u = rows[i];
                            final active = u.state == 1;
                            return _UserCard(
                              user: u,
                              active: active,
                              initials: _initials(u),
                              onToggleLock: () => _confirmSetStatus(
                                u,
                                active ? 0 : 1,
                              ),
                            );
                          },
                        ),
                      ),
          ),
        ],
      ),
    );
  }
}

class _FilterChip extends StatelessWidget {
  const _FilterChip({
    required this.label,
    required this.selected,
    required this.onSelected,
  });

  final String label;
  final bool selected;
  final VoidCallback onSelected;

  @override
  Widget build(BuildContext context) {
    return FilterChip(
      label: Text(label),
      selected: selected,
      onSelected: (_) => onSelected(),
      selectedColor: TColor.primary.withValues(alpha: 0.2),
      checkmarkColor: TColor.primary,
      labelStyle: TextStyle(
        color: selected ? TColor.primary : TColor.text,
        fontWeight: selected ? FontWeight.w700 : FontWeight.w500,
        fontSize: 12,
      ),
      side: BorderSide(
        color: selected ? TColor.primary : TColor.gray.withValues(alpha: 0.4),
      ),
      padding: const EdgeInsets.symmetric(horizontal: 4),
    );
  }
}

class _UserCard extends StatelessWidget {
  const _UserCard({
    required this.user,
    required this.active,
    required this.initials,
    required this.onToggleLock,
  });

  final UserData user;
  final bool active;
  final String initials;
  final VoidCallback onToggleLock;

  @override
  Widget build(BuildContext context) {
    return Material(
      elevation: 1,
      shadowColor: Colors.black12,
      borderRadius: BorderRadius.circular(14),
      color: Colors.white,
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            ClipOval(
              child: user.avtImage != null && user.avtImage!.isNotEmpty
                  ? AppNetworkImage(
                      pathOrUrl: user.avtImage,
                      width: 52,
                      height: 52,
                      fit: BoxFit.cover,
                    )
                  : Container(
                      width: 52,
                      height: 52,
                      color: TColor.primary.withValues(alpha: 0.15),
                      alignment: Alignment.center,
                      child: Text(
                        initials,
                        style: TextStyle(
                          color: TColor.primary,
                          fontWeight: FontWeight.w800,
                          fontSize: 18,
                        ),
                      ),
                    ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Expanded(
                        child: Text(
                          user.name.isEmpty ? '(Chưa có tên)' : user.name,
                          style: const TextStyle(
                            fontWeight: FontWeight.w700,
                            fontSize: 16,
                          ),
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                        ),
                      ),
                      Container(
                        padding: const EdgeInsets.symmetric(
                          horizontal: 8,
                          vertical: 4,
                        ),
                        decoration: BoxDecoration(
                          color: active
                              ? TColor.primary.withValues(alpha: 0.15)
                              : TColor.color1.withValues(alpha: 0.12),
                          borderRadius: BorderRadius.circular(20),
                        ),
                        child: Text(
                          active ? 'Hoạt động' : 'Đã khóa',
                          style: TextStyle(
                            fontSize: 11,
                            fontWeight: FontWeight.w700,
                            color: active ? TColor.primary : TColor.color1,
                          ),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 4),
                  Text(
                    user.email,
                    style: TextStyle(color: TColor.gray, fontSize: 13),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  ),
                  if (user.phoneNumber.isNotEmpty) ...[
                    const SizedBox(height: 2),
                    Text(
                      user.phoneNumber,
                      style: TextStyle(color: TColor.gray, fontSize: 13),
                    ),
                  ],
                  const SizedBox(height: 6),
                  Row(
                    children: [
                      Icon(
                        Icons.badge_outlined,
                        size: 14,
                        color: TColor.gray,
                      ),
                      const SizedBox(width: 4),
                      Text(
                        user.role.isEmpty ? '—' : user.role,
                        style: TextStyle(
                          color: TColor.text.withValues(alpha: 0.85),
                          fontSize: 12,
                        ),
                      ),
                      const Spacer(),
                      Text(
                        'ID ${user.userId}',
                        style: TextStyle(
                          color: TColor.gray,
                          fontSize: 11,
                        ),
                      ),
                    ],
                  ),
                ],
              ),
            ),
            const SizedBox(width: 4),
            Tooltip(
              message: active ? 'Khóa tài khoản' : 'Mở khóa',
              child: IconButton.filledTonal(
                onPressed: onToggleLock,
                icon: Icon(
                  active ? Icons.lock_rounded : Icons.lock_open_rounded,
                  size: 22,
                ),
                style: IconButton.styleFrom(
                  backgroundColor: active
                      ? TColor.color1.withValues(alpha: 0.12)
                      : TColor.primary.withValues(alpha: 0.15),
                  foregroundColor: active ? TColor.color1 : TColor.primary,
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
