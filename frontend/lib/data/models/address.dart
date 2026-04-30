class Address {
  int id;
  String street;
  String city;
  String district;
  String ward;
  String detail;
  double lat;
  double lon;

  Address({
    this.id = -1,
     this.street ="",
    required this.city,
    required this.district,
    required this.ward,
    required this.detail,
    this.lat = 0,
    this.lon = 0,
  });

  @override
  String toString() {
    return "$city- $district - $ward - $detail";
  }

  // Convert JSON to Address
  factory Address.fromJson(String data) {
    Map<String, dynamic> map = stringToMap(data);
    return Address(
      id: map['id']??0,
      street: map['street'] ?? '',
      city: map['city'] ?? '',
      district: map['district'] ?? '',
      ward: map['ward'] ?? '',
      detail: map['detail'] ?? '',
      lat: double.tryParse(map['lat']?.toString() ?? '0') ?? 0.0,
      lon: double.tryParse(map['lon']?.toString() ?? '0') ?? 0.0,
    );
  }

  factory Address.fromMap(Map<String, dynamic> json) {
    return Address(
      id: json['id'] ?? -1,
      street: json['street'] ?? '',
      city: json['city'] ?? '',
      district: json['district'] ?? '',
      ward: json['ward'] ?? '',
      detail: json['detail'] ?? '',
      lat: (json['lat'] as num?)?.toDouble() ?? 0.0,
      lon: (json['lon'] as num?)?.toDouble() ?? 0.0,
    );
  }

  // Convert Address to JSON
  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'street': street,
      'city': city,
      'district': district,
      'ward': ward,
      'detail': detail,
      'lat': lat,
      'lon': lon
    };
  }
}

String formatToJson(String data) {
  // Thay dấu = thành :
  String formatted = data.replaceAll('=', ':');
  // Thêm dấu ngoặc kép xung quanh các khóa và giá trị (nếu cần)
  formatted = formatted.replaceAllMapped(
      RegExp(r'(\w+):'), (match) => '"${match[1]}":');
  return formatted;
}

Map<String, dynamic> stringToMap(String data) {
  // Loại bỏ dấu ngoặc đầu và cuối
  data = data.substring(1, data.length - 1);

  // Chia thành các cặp khóa-giá trị
  List<String> pairs = data.split(', ');

  // Tạo map từ các cặp khóa-giá trị
  Map<String, dynamic> result = {};
  for (String pair in pairs) {
    List<String> keyValue = pair.split('=');
    String key = keyValue[0];
    String value = keyValue[1];

    // Chuyển giá trị sang số nếu cần, hoặc để chuỗi
    result[key] = int.tryParse(value) ?? value;
  }

  return result;
}
