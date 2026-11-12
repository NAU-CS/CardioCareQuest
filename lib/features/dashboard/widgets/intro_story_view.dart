// The visual half of a factor intro: photo, story pages, dots, and the two
// buttons. Kept apart from CategoryIntroScreen, which owns navigation and
// telemetry, so the layout can be rendered on its own at any screen size —
// see test/intro_layout_test.dart, which checks it on a small phone, a large
// phone, and a tablet.

import 'package:flutter/material.dart';

import '../../../core/theme/app_colors.dart';
import '../../games/game_stories.dart';
import '../../games/intro_story.dart';

class IntroStoryView extends StatelessWidget {
  final GameCategory category;
  final IntroStory story;
  final PageController controller;
  final int page;
  final ValueChanged<int> onPageChanged;
  final VoidCallback onNext;
  final VoidCallback onSkip;

  const IntroStoryView({
    super.key,
    required this.category,
    required this.story,
    required this.controller,
    required this.page,
    required this.onPageChanged,
    required this.onNext,
    required this.onSkip,
  });

  @override
  Widget build(BuildContext context) {
    final pages = story.pages;

    return LayoutBuilder(
      builder: (context, constraints) {
        // The photo takes a share of the height rather than a fixed size, so a
        // small phone keeps room for the words and a tall one does not leave a
        // gap. The clamp stops it collapsing or dominating at the extremes.
        final imageHeight =
            (constraints.maxHeight * 0.30).clamp(120.0, 300.0).toDouble();
        final side = constraints.maxWidth >= 600 ? 32.0 : 20.0;

        return Column(
          children: [
            Padding(
              padding: EdgeInsets.fromLTRB(side, 4, side, 16),
              child: ClipRRect(
                borderRadius: BorderRadius.circular(20),
                child: SizedBox(
                  height: imageHeight,
                  width: double.infinity,
                  child: Image.asset(
                    category.image,
                    fit: BoxFit.cover,
                    errorBuilder: (context, error, stack) => Container(
                      color: AppColors.primaryLight,
                      child: Icon(
                        category.icon,
                        color: AppColors.primary,
                        size: 56,
                      ),
                    ),
                  ),
                ),
              ),
            ),

            // The story itself. Each page scrolls on its own so long text still
            // reaches the bottom on a small screen.
            Expanded(
              child: PageView.builder(
                controller: controller,
                onPageChanged: onPageChanged,
                itemCount: pages.length,
                // Short pages sit centred in the space rather than clinging
                // to the top and leaving a hole above the buttons; long ones
                // fall back to scrolling from the top.
                itemBuilder: (context, i) => LayoutBuilder(
                  builder: (context, page) => SingleChildScrollView(
                    padding: EdgeInsets.symmetric(horizontal: side),
                    child: ConstrainedBox(
                      constraints: BoxConstraints(minHeight: page.maxHeight),
                      child: Column(
                        mainAxisAlignment: MainAxisAlignment.center,
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [IntroPageText(page: pages[i])],
                      ),
                    ),
                  ),
                ),
              ),
            ),

            IntroDots(count: pages.length, active: page),

            Padding(
              padding: EdgeInsets.fromLTRB(side, 8, side, 16),
              child: Row(
                children: [
                  // Skip stays available on every page, including the last, so
                  // a player who has read enough is never made to page through.
                  Expanded(
                    child: TextButton(
                      onPressed: onSkip,
                      style: TextButton.styleFrom(
                        minimumSize: const Size.fromHeight(52),
                        foregroundColor: AppColors.subtitle,
                      ),
                      child: const Text(
                        'Skip',
                        style: TextStyle(
                          fontSize: 17,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    flex: 2,
                    child: FilledButton(
                      onPressed: onNext,
                      style: FilledButton.styleFrom(
                        backgroundColor: AppColors.primary,
                        minimumSize: const Size.fromHeight(52),
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(14),
                        ),
                      ),
                      child: Text(
                        pages[page].buttonLabel,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: const TextStyle(
                          fontSize: 17,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ],
        );
      },
    );
  }
}

class IntroPageText extends StatelessWidget {
  final IntroPage page;
  const IntroPageText({
    super.key,required this.page});

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        if (page.heading != null) ...[
          Text(
            page.heading!,
            style: const TextStyle(
              color: AppColors.title,
              fontSize: 24,
              fontWeight: FontWeight.bold,
              height: 1.25,
            ),
          ),
          const SizedBox(height: 14),
        ],
        for (final p in page.paragraphs) ...[
          IntroRichLine(text: p),
          const SizedBox(height: 14),
        ],
        const SizedBox(height: 8),
      ],
    );
  }
}

// Draws a paragraph that may contain <b> and <i>, the only two tags the intro
// sources use.
class IntroRichLine extends StatelessWidget {
  final String text;
  const IntroRichLine({
    super.key,required this.text});

  static final _re = RegExp(r'<(/?)(b|i)>');

  @override
  Widget build(BuildContext context) {
    const base = TextStyle(
      color: AppColors.body,
      fontSize: 18,
      height: 1.5,
    );

    final spans = <TextSpan>[];
    var bold = false;
    var italic = false;
    var index = 0;

    void emit(String chunk) {
      if (chunk.isEmpty) return;
      spans.add(TextSpan(
        text: chunk,
        style: base.copyWith(
          fontWeight: bold ? FontWeight.bold : null,
          fontStyle: italic ? FontStyle.italic : null,
          color: bold ? AppColors.title : null,
        ),
      ));
    }

    for (final m in _re.allMatches(text)) {
      emit(text.substring(index, m.start));
      final closing = m.group(1) == '/';
      if (m.group(2) == 'b') bold = !closing;
      if (m.group(2) == 'i') italic = !closing;
      index = m.end;
    }
    emit(text.substring(index));

    return Text.rich(TextSpan(children: spans), style: base);
  }
}

class IntroDots extends StatelessWidget {
  final int count;
  final int active;
  const IntroDots({
    super.key,required this.count, required this.active});

  @override
  Widget build(BuildContext context) {
    if (count < 2) return const SizedBox(height: 8);
    return Semantics(
      label: 'Page ${active + 1} of $count',
      child: Row(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          for (var i = 0; i < count; i++)
            AnimatedContainer(
              duration: const Duration(milliseconds: 200),
              margin: const EdgeInsets.symmetric(horizontal: 4),
              height: 8,
              width: i == active ? 22 : 8,
              decoration: BoxDecoration(
                color: i == active
                    ? AppColors.primary
                    : AppColors.primary.withValues(alpha: 0.25),
                borderRadius: BorderRadius.circular(4),
              ),
            ),
        ],
      ),
    );
  }
}
