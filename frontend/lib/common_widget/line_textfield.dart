import 'package:flutter/material.dart';
import '../common/color_extension.dart';

class LineTextField extends StatelessWidget {
  final TextEditingController controller;
  final String hitText;
  final bool obscureText;
  final TextInputType? keyboardType;
  final String? Function(String?)? validator;
  final String? Function(String?)? onChanged;

  final Widget? suffixIcon;
  final bool isClear;
  final VoidCallback? onClearPressed;
  final int? maxLength;

  const LineTextField({
    super.key,
    required this.hitText,
    required this.controller,
    this.obscureText = false,
    this.keyboardType,
    this.isClear = false,
    this.onClearPressed,
    this.validator,
    this.suffixIcon, this.onChanged, this.maxLength,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 15),
      child: TextFormField(
        validator: validator,
        maxLength: maxLength,
        controller: controller,
        keyboardType: keyboardType,
        autovalidateMode: AutovalidateMode.onUnfocus,
        obscureText: obscureText,
        style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w700),
        decoration: InputDecoration(
          suffix: suffixIcon,
          hintText: hitText,
          enabledBorder: UnderlineInputBorder(
            borderSide: BorderSide(color: TColor.gray),
          ),
          focusedBorder: UnderlineInputBorder(
            borderSide: BorderSide(color: TColor.primary),
          ),
          suffixIcon: isClear
              ? IconButton(
                  onPressed: () {
                    if (onClearPressed != null) {
                      onClearPressed!();
                    }
                  },
                  icon: Image.asset(
                    "assets/img/cancel.png",
                    width: 15,
                  ))
              : null,
        ),
      ),
    );
  }
}

class RoundTextField extends StatelessWidget {
  final TextEditingController controller;
  final String hitText;
  final bool obscureText;
  final TextInputType? keyboardType;
  final bool isClear;
  final VoidCallback? onClearPressed;
  final Widget? leftIcon;
  final Function(String) onSubmitted;
  final Function(String)?  onChanged;

  const RoundTextField(
      {super.key,
      required this.hitText,
      required this.controller,
      this.obscureText = false,
      this.keyboardType,
      this.isClear = false,
      this.leftIcon,
      this.onClearPressed, required this.onSubmitted, this.onChanged});

  @override
  Widget build(BuildContext context) {
    return Container(
      height: 40,
      decoration: BoxDecoration(
          color: TColor.gray.withOpacity(0.2),
          borderRadius: BorderRadius.circular(15)),
      child: TextField(
        controller: controller,
        keyboardType: keyboardType,
        onSubmitted: onSubmitted ,
        onChanged: onChanged,
        obscureText: obscureText,
        style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w700),
        decoration: InputDecoration(
            hintText: hitText,
            contentPadding:
                const EdgeInsets.symmetric(vertical: 4, horizontal: 8),
            enabledBorder: InputBorder.none,
            focusedBorder: InputBorder.none,
            prefixIcon: leftIcon,
            suffixIcon: isClear
                ? IconButton(
                    onPressed: () {
                      if (onClearPressed != null) {
                        onClearPressed!();
                      }
                    },
                    icon: Image.asset(
                      "assets/img/cancel.png",
                      width: 15,
                    ))
                : null),
      ),
    );
  }
}
