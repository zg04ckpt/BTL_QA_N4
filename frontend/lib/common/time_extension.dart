extension TimeAgoIntExtension on int {
  String timeAgo() {
    final DateTime time = DateTime.fromMillisecondsSinceEpoch(this);
    final Duration difference = DateTime.now().difference(time);

    if (difference.inDays > 365) {
      return "${difference.inDays ~/ 365} years ago";
    } else if (difference.inDays > 30) {
      return "${difference.inDays ~/ 30} months ago";
    } else if (difference.inDays > 7) {
      return "${difference.inDays ~/ 7} weeks ago";
    } else if (difference.inDays > 0) {
      return "${difference.inDays} days ago";
    } else if (difference.inHours > 0) {
      return "${difference.inHours} hours ago";
    } else if (difference.inMinutes > 0) {
      return "${difference.inMinutes} minutes ago";
    } else {
      return "just now";
    }
  }
}
extension DateTimeFormatter on DateTime {
  String toFormattedString() {
    String twoDigits(int n) => n.toString().padLeft(2, '0');
    String hour = twoDigits(this.hour);
    String minute = twoDigits(this.minute);
    String day = twoDigits(this.day);
    String month = twoDigits(this.month);
    String year = this.year.toString();

    return "$hour:$minute - $day/$month/$year";
  }
}
