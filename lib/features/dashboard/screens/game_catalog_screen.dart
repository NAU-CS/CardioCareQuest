// Game Catalog - a grid of category tiles, one per health topic.
// Flat tiles instead of expandable sections (easier for older users).
// "Your Goals" tile appears only if the user has made custom games.
// The BP-logging game is reached from the dashboard, not here.

import 'dart:math';

import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../../core/providers/user_data_manager.dart';
import '../../../core/theme/app_colors.dart';
import '../../games/custom_games/custom_game.dart';
import '../../games/custom_games/custom_games_repository.dart';
import '../../games/game_stories.dart';
import 'category_intro_screen.dart';
import 'custom_games_screen.dart';

// Row height limits. The minimum keeps the photo and two lines of text
// comfortable; the maximum stops a row looking stretched on a tall screen.
const double kMinCatalogRowHeight = 96.0;

// Phones and tablets get different ceilings. A single low cap left a tablet
// with most of a row's worth of empty space under the last tile — the very gap
// this layout exists to remove — while a single high cap would have made a
// phone row look like a banner.
const double kMaxCatalogRowHeightPhone = 150.0;
const double kMaxCatalogRowHeightTablet = 220.0;

// Width at which the layout starts treating the screen as a tablet.
const double kTabletWidth = 600.0;

double maxCatalogRowHeight(double viewportWidth) =>
    viewportWidth >= kTabletWidth
        ? kMaxCatalogRowHeightTablet
        : kMaxCatalogRowHeightPhone;

// Vertical padding around the list and the gap between rows. Named so the
// height calculation and the widget that lays the rows out cannot drift apart.
const EdgeInsets kCatalogPadding = EdgeInsets.fromLTRB(20, 16, 20, 24);
const double kCatalogRowGap = 14.0;

// How tall each row should be so the tiles share the screen instead of
// leaving a gap under the last one. Returns the clamped height; callers use
// catalogRowsFit to decide whether the list still needs to scroll.
double catalogRowHeight({
  required double viewportHeight,
  required double viewportWidth,
  required int tileCount,
}) {
  if (tileCount <= 0) return kMinCatalogRowHeight;
  final gaps = kCatalogRowGap * (tileCount - 1);
  final free = viewportHeight - kCatalogPadding.vertical - gaps;
  return (free / tileCount)
      .clamp(kMinCatalogRowHeight, maxCatalogRowHeight(viewportWidth));
}

// True when the rows fit at the height above, so scrolling can be switched off.
bool catalogRowsFit({
  required double viewportHeight,
  required int tileCount,
  required double rowHeight,
}) {
  final gaps = kCatalogRowGap * (tileCount - 1);
  return rowHeight * tileCount + gaps + kCatalogPadding.vertical <=
      viewportHeight;
}

// "1 game" / "3 games" for the row subtitle.
String _gameCountLabel(int count) => '$count ${count == 1 ? "game" : "games"}';

class GameCatalogScreen extends StatefulWidget {
  const GameCatalogScreen({super.key});

  @override
  State<GameCatalogScreen> createState() => _GameCatalogScreenState();
}

class _GameCatalogScreenState extends State<GameCatalogScreen> {
  // Category tiles, shuffled fresh each visit but stable during rebuilds.
  late final Map<GameCategory, List<GameStory>> _byCategory;
  late final List<GameCategory> _categories;

  // Remembers last visit's order so each new visit looks visibly different.
  static List<GameCategory>? _lastOrder;
  static final Random _rng = Random();

  @override
  void initState() {
    super.initState();
    _byCategory = GameCatalog.getCatalogGamesByCategory();
    _categories = _freshShuffledOrder(_byCategory.keys.toList());
    _lastOrder = List<GameCategory>.from(_categories);
  }

  // Shuffles categories, retrying until the order differs from last time.
  // Stops after 8 tries so a 1-item list doesn't loop forever.
  List<GameCategory> _freshShuffledOrder(List<GameCategory> cats) {
    if (cats.length < 2) return cats;
    final order = List<GameCategory>.from(cats);
    for (var attempt = 0; attempt < 8; attempt++) {
      order.shuffle(_rng);
      final prev = _lastOrder;
      // Accept the first order that isn't identical to last time's.
      if (prev == null || !_sameOrder(order, prev)) return order;
    }
    return order;
  }

  bool _sameOrder(List<GameCategory> a, List<GameCategory> b) {
    if (a.length != b.length) return false;
    for (var i = 0; i < a.length; i++) {
      if (a[i] != b[i]) return false;
    }
    return true;
  }

  // Builds a tile per category plus "Your Goals" when the user has custom games.
  @override
  Widget build(BuildContext context) {
    final byCategory = _byCategory;
    final categories = _categories;
    final uid = context.select<UserDataProvider, String>((p) => p.uid);

    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: const Text(
          'Game Catalog',
          style: TextStyle(color: AppColors.title),
        ),
        backgroundColor: Colors.transparent,
        elevation: 0,
        iconTheme: const IconThemeData(color: AppColors.title),
      ),
      // Watches custom games live; "Your Goals" tile appears once they've made one.
      body: StreamBuilder<List<CustomGame>>(
        stream: uid.isEmpty
            ? const Stream<List<CustomGame>>.empty()
            : CustomGamesRepository.instance.watch(uid),
        builder: (context, snap) {
          final customGames = snap.data ?? const <CustomGame>[];

          final tiles = <Widget>[
            for (final cat in categories)
              _CatalogRow(
                image: cat.image,
                icon: cat.icon,
                label: cat.label,
                // Diet and Education are one game with chapters; the rest count
                // their games directly.
                subtitle: GameCatalog.chapterGameFor(cat) != null
                    ? '${byCategory[cat]!.length} chapters'
                    : _gameCountLabel(byCategory[cat]!.length),
                onTap: () => Navigator.push(
                  context,
                  MaterialPageRoute(
                    builder: (_) => CategoryIntroScreen(category: cat),
                  ),
                ),
              ),
            if (customGames.isNotEmpty)
              _CatalogRow(
                image: null,
                icon: Icons.auto_awesome_outlined,
                label: 'Your Goals',
                subtitle: _gameCountLabel(customGames.length),
                onTap: () => Navigator.push(
                  context,
                  MaterialPageRoute(
                    builder: (_) => const CustomGamesScreen(),
                  ),
                ),
              ),
          ];

          // The five factors should fill the screen rather than sit in a short
          // stack with dead space underneath. Each row grows to share whatever
          // height is going, within limits: never shorter than the photo needs,
          // never so tall it looks stretched on a big phone or tablet. If the
          // rows cannot fit even at their minimum (a small phone, or once
          // "Your Goals" appears), the list scrolls as before.
          // Because the rows are sized to fill rather than scroll, the last one
          // would otherwise sit under the home indicator on a gesture-nav
          // phone. SafeArea takes that strip out of the height first.
          return SafeArea(
            top: false,
            child: LayoutBuilder(
              builder: (context, constraints) {
              final rowHeight = catalogRowHeight(
                viewportHeight: constraints.maxHeight,
                viewportWidth: constraints.maxWidth,
                tileCount: tiles.length,
              );
              final fits = catalogRowsFit(
                viewportHeight: constraints.maxHeight,
                tileCount: tiles.length,
                rowHeight: rowHeight,
              );

              return ListView.separated(
                padding: kCatalogPadding,
                physics: fits
                    ? const NeverScrollableScrollPhysics()
                    : const AlwaysScrollableScrollPhysics(),
                itemCount: tiles.length,
                separatorBuilder: (context, i) =>
                    const SizedBox(height: kCatalogRowGap),
                itemBuilder: (context, i) => SizedBox(
                  height: rowHeight,
                  child: tiles[i],
                ),
              );

              },
            ),
          );
        },
      ),
    );
  }
}

// A wide row: photo on the left, topic name and game count on the right.
// The whole row is tappable; screen reader reads it as one line.
class _CatalogRow extends StatelessWidget {
  final String? image; // Photo asset path, or null to fall back to the icon.
  final IconData icon;
  final String label;
  final String subtitle; // e.g. "2 chapters" or "1 game".
  final VoidCallback onTap;

  const _CatalogRow({
    required this.image,
    required this.icon,
    required this.label,
    required this.subtitle,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return Semantics(
      button: true,
      label: '$label, $subtitle',
      child: Material(
        color: Colors.white,
        borderRadius: BorderRadius.circular(20),
        child: InkWell(
          onTap: onTap,
          borderRadius: BorderRadius.circular(20),
          child: Container(
            decoration: BoxDecoration(
              borderRadius: BorderRadius.circular(20),
              border: Border.all(color: AppColors.cardBorder),
              boxShadow: [
                BoxShadow(
                  color: AppColors.accent.withValues(alpha: 0.08),
                  blurRadius: 12,
                  offset: const Offset(0, 4),
                ),
              ],
            ),
            child: Row(
              children: [
                // Left: the topic photo, filling a rounded square.
                ClipRRect(
                  borderRadius: const BorderRadius.horizontal(
                    left: Radius.circular(20),
                  ),
                  child: AspectRatio(
                    aspectRatio: 1,
                    child: image == null
                        ? _IconTile(icon: icon)
                        : Image.asset(
                            image!,
                            fit: BoxFit.cover,
                            // If the photo is missing, show the icon instead of
                            // a broken image so the list still works.
                            errorBuilder: (context, error, stack) =>
                                _IconTile(icon: icon),
                          ),
                  ),
                ),
                const SizedBox(width: 16),
                // Right: the factor name and game count.
                Expanded(
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        label,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: const TextStyle(
                          color: AppColors.title,
                          fontSize: 18,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                      const SizedBox(height: 4),
                      Text(
                        subtitle,
                        style: const TextStyle(
                          color: AppColors.subtitle,
                          fontSize: 13,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                    ],
                  ),
                ),
                const Padding(
                  padding: EdgeInsets.only(right: 16),
                  child: Icon(Icons.chevron_right, color: AppColors.subtitle),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

// Fallback shown when a topic has no photo (or the photo fails to load).
class _IconTile extends StatelessWidget {
  final IconData icon;
  const _IconTile({required this.icon});

  @override
  Widget build(BuildContext context) {
    return Container(
      color: AppColors.primary.withValues(alpha: 0.1),
      child: Icon(icon, color: AppColors.primary, size: 36),
    );
  }
}
