// Renders a real factor intro at three screen sizes and checks the layout
// holds: everything the player needs is on screen, nothing overflows, and the
// story text still has room once the photo has taken its share.

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:cardio_care_quest/features/dashboard/widgets/intro_story_view.dart';
import 'package:cardio_care_quest/features/games/game_stories.dart';
import 'package:cardio_care_quest/features/games/intro_story.dart';

const _story = IntroStory(
  title: 'Eat for Your Heart',
  pages: [
    IntroPage(
      heading: 'Food and your blood pressure',
      paragraphs: [
        'What you eat has a direct effect on your blood pressure. Two things '
            'matter most: eating less salt and eating more potassium-rich foods.',
      ],
      buttonLabel: 'Next',
      isLast: false,
    ),
    IntroPage(
      paragraphs: [
        'Salt makes your body hold on to water, which raises the pressure '
            'inside your arteries.',
        'You do not need a perfect diet. Small swaps, made often, add up.',
      ],
      buttonLabel: 'Next',
      isLast: false,
    ),
    IntroPage(
      heading: 'How to play',
      paragraphs: ['🥦 <b>Broccoli</b> — high in potassium, helps clear salt.'],
      buttonLabel: 'See the games',
      isLast: true,
    ),
  ],
);

Widget _harness(Size size) => MediaQuery(
      data: MediaQueryData(size: size),
      child: MaterialApp(
        home: Scaffold(
          body: IntroStoryView(
            category: GameCategory.diet,
            story: _story,
            controller: PageController(),
            page: 0,
            onPageChanged: (_) {},
            onNext: () {},
            onSkip: () {},
          ),
        ),
      ),
    );

// Small phone, large phone, tablet.
const _sizes = <String, Size>{
  'iPhone SE': Size(320, 568),
  'iPhone 17 Pro Max': Size(440, 956),
  'iPad': Size(834, 1194),
};

void main() {
  _sizes.forEach((name, size) {
    testWidgets('intro layout holds on $name', (tester) async {
      tester.view.physicalSize = size;
      tester.view.devicePixelRatio = 1.0;
      addTearDown(tester.view.reset);

      await tester.pumpWidget(_harness(size));
      await tester.pumpAndSettle();

      // No RenderFlex overflow on any size.
      expect(tester.takeException(), isNull, reason: '$name overflowed');

      // The controls a player needs are all present.
      expect(find.text('Skip'), findsOneWidget, reason: '$name: no Skip');
      expect(find.text('Next'), findsOneWidget, reason: '$name: no Next');
      expect(find.text('Food and your blood pressure'), findsOneWidget,
          reason: '$name: heading missing');

      // Buttons are reachable, not pushed off the bottom.
      final skip = tester.getRect(find.text('Skip'));
      expect(skip.bottom, lessThanOrEqualTo(size.height),
          reason: '$name: Skip is off screen');

      // The photo takes a share of the height, never more than a quarter or so,
      // so the story is never squeezed off the page.
      final photo = tester.getRect(find.byType(Image).first);
      expect(photo.height, lessThanOrEqualTo(size.height * 0.32),
          reason: '$name: photo too tall');
      expect(photo.height, greaterThanOrEqualTo(100),
          reason: '$name: photo too small to read');

      // Body text is left-aligned with the photo (one consistent margin).
      final side = size.width >= 600 ? 32.0 : 20.0;
      expect(photo.left, closeTo(side, 0.5), reason: '$name: photo margin');
    });
  });

  testWidgets('last page still offers Skip alongside the final button',
      (tester) async {
    tester.view.physicalSize = const Size(440, 956);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.reset);

    await tester.pumpWidget(MediaQuery(
      data: const MediaQueryData(size: Size(440, 956)),
      child: MaterialApp(
        home: Scaffold(
          body: IntroStoryView(
            category: GameCategory.diet,
            story: _story,
            controller: PageController(initialPage: 2),
            page: 2,
            onPageChanged: (_) {},
            onNext: () {},
            onSkip: () {},
          ),
        ),
      ),
    ));
    await tester.pumpAndSettle();

    expect(find.text('See the games'), findsOneWidget);
    expect(find.text('Skip'), findsOneWidget);
  });
}
