import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:examshield_mobile/ui/screens/notifications_screen.dart';

Widget buildSubject() => const MaterialApp(home: NotificationsScreen());

void main() {
  testWidgets('shows Notifications title', (tester) async {
    await tester.pumpWidget(buildSubject());
    expect(find.text('Notifications'), findsOneWidget);
  });

  testWidgets('shows Clear all button', (tester) async {
    await tester.pumpWidget(buildSubject());
    expect(find.text('Clear all'), findsOneWidget);
  });

  testWidgets('renders notification tiles', (tester) async {
    await tester.pumpWidget(buildSubject());
    expect(find.byType(ListTile), findsWidgets);
  });

  testWidgets('shows at least one notification message', (tester) async {
    await tester.pumpWidget(buildSubject());
    // The static notification list contains items with subtitles
    expect(find.byType(ListTile), findsAtLeastNWidgets(1));
  });
}
