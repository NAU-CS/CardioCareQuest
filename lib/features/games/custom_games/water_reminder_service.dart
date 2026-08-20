// A small in-app water reminder. No push notifications: while the app is open,
// this shows a friendly SnackBar a while after the patient asks to be reminded.
// The MaterialApp wires its messenger key to `messengerKey` so the reminder can
// appear on any screen.

import 'dart:async';

import 'package:flutter/material.dart';

class WaterReminderService {
  WaterReminderService._();
  static final WaterReminderService instance = WaterReminderService._();

  // Shared with MaterialApp so we can show a SnackBar from outside a screen.
  final GlobalKey<ScaffoldMessengerState> messengerKey =
      GlobalKey<ScaffoldMessengerState>();

  Timer? _timer;

  // Show a water reminder after [after]. A new call replaces any pending one.
  void scheduleReminder({Duration after = const Duration(hours: 2)}) {
    _timer?.cancel();
    _timer = Timer(after, () {
      messengerKey.currentState
        ?..hideCurrentSnackBar()
        ..showSnackBar(
          const SnackBar(
            content: Text('💧 Time for a glass of water. Stay hydrated!'),
            duration: Duration(seconds: 6),
          ),
        );
    });
  }

  void cancel() {
    _timer?.cancel();
    _timer = null;
  }
}
