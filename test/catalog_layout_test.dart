// The five factor rows should share the screen rather than sit in a short
// stack with dead space beneath them, on any phone, without ever squashing a
// row so small the photo and two lines of text stop being readable.

import 'package:flutter_test/flutter_test.dart';
import 'package:cardio_care_quest/features/dashboard/screens/game_catalog_screen.dart';

// Height left for the list once the status bar and app bar are taken off.
const _sizes = <String, List<double>>{  // [height, width]
  'iPhone SE': [568 - 20 - 56, 320],
  'iPhone 17': [852 - 59 - 56, 393],
  'iPhone 17 Pro Max': [956 - 59 - 56, 440],
  'iPad': [1194 - 24 - 56, 834],
};

void main() {
  group('five factors', () {
    _sizes.forEach((name, dims) {
      final viewport = dims[0];
      final width = dims[1];
      test('$name uses the space without squashing rows', () {
        final h = catalogRowHeight(
            viewportHeight: viewport, viewportWidth: width, tileCount: 5);

        expect(h, greaterThanOrEqualTo(kMinCatalogRowHeight),
            reason: '$name: row below the readable minimum');
        expect(h, lessThanOrEqualTo(maxCatalogRowHeight(width)),
            reason: '$name: row stretched past the maximum');

        final fits = catalogRowsFit(
          viewportHeight: viewport,
          tileCount: 5,
          rowHeight: h,
        );

        if (fits) {
          // Whatever is left over must be less than one more row's worth,
          // otherwise there is still an obvious gap at the bottom.
          final used = h * 5 + kCatalogRowGap * 4 + kCatalogPadding.vertical;
          final leftover = viewport - used;
          expect(leftover, lessThan(kMinCatalogRowHeight),
              reason: '$name: $leftover pt of dead space left over');
        }
      });
    });
  });

  test('tall screens grow the rows beyond the old fixed 96pt', () {
    final maxPhone = catalogRowHeight(
        viewportHeight: 956 - 59 - 56, viewportWidth: 440, tileCount: 5);
    expect(maxPhone, greaterThan(96.0),
        reason: 'rows should expand to fill a large phone');
  });

  test('small screens keep rows readable and let the list scroll', () {
    const viewport = 568.0 - 20 - 56;
    final h = catalogRowHeight(
        viewportHeight: viewport, viewportWidth: 320, tileCount: 5);
    expect(h, kMinCatalogRowHeight);
    expect(
      catalogRowsFit(viewportHeight: viewport, tileCount: 5, rowHeight: h),
      isFalse,
      reason: 'a small phone must still be able to scroll',
    );
  });

  test('a sixth tile (Your Goals) is handled, not overflowed', () {
    const viewport = 956.0 - 59 - 56;
    final h = catalogRowHeight(
        viewportHeight: viewport, viewportWidth: 440, tileCount: 6);
    expect(h, greaterThanOrEqualTo(kMinCatalogRowHeight));
    expect(h, lessThanOrEqualTo(maxCatalogRowHeight(440)));
  });

  test('an empty list does not divide by zero', () {
    expect(
        catalogRowHeight(
            viewportHeight: 800, viewportWidth: 393, tileCount: 0),
        kMinCatalogRowHeight);
  });
}
