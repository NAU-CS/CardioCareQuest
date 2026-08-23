// The short story you land on after tapping a factor in the Game Catalog.
// The topic photo sits up top as a native image (a WebView loaded from an HTML
// string can't resolve bundled asset paths, so the photo stays outside it).
// Below it, an embedded WebView plays the factor's intro story, authored in
// Twine (assets/game-src/intro/*.tw) and spliced into the shared engine shell
// at load time. The story's final "See the games" button posts CCQ_OPEN_GAMES,
// which we catch here to continue on to the games list.

import 'package:flutter/material.dart';
import 'package:flutter/services.dart' show rootBundle;

import '../../../core/theme/app_colors.dart';
import '../../../core/widgets/twine_questionnaire_host.dart';
import '../../games/custom_games/custom_twine_builder.dart';
import '../../games/game_stories.dart';
import 'category_games_screen.dart';

class CategoryIntroScreen extends StatefulWidget {
  final GameCategory category;

  const CategoryIntroScreen({super.key, required this.category});

  @override
  State<CategoryIntroScreen> createState() => _CategoryIntroScreenState();
}

class _CategoryIntroScreenState extends State<CategoryIntroScreen> {
  // The built story HTML, produced once from the factor's .tw source.
  Future<String>? _storyHtml;

  @override
  void initState() {
    super.initState();
    _storyHtml = _buildStory();
  }

  Future<String> _buildStory() async {
    final template =
        await rootBundle.loadString('assets/game/custom_game_template.html');
    final twSource = await rootBundle.loadString(widget.category.introTwAsset);
    return buildStoryHtmlFromTw(
      twSource,
      template: template,
      title: widget.category.label,
    );
  }

  void _openGames() {
    if (!mounted) return;
    Navigator.pushReplacement(
      context,
      MaterialPageRoute(
        builder: (_) => CategoryGamesScreen(category: widget.category),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: Text(
          widget.category.label,
          style: const TextStyle(color: AppColors.title),
        ),
        backgroundColor: Colors.transparent,
        elevation: 0,
        iconTheme: const IconThemeData(color: AppColors.title),
      ),
      body: Column(
        children: [
          // Topic photo (native so the bundled asset loads crisply).
          Padding(
            padding: const EdgeInsets.fromLTRB(20, 8, 20, 12),
            child: ClipRRect(
              borderRadius: BorderRadius.circular(20),
              child: AspectRatio(
                aspectRatio: 16 / 9,
                child: Image.asset(
                  widget.category.image,
                  fit: BoxFit.cover,
                  errorBuilder: (context, error, stack) => Container(
                    color: AppColors.primary.withValues(alpha: 0.1),
                    child: Icon(
                      widget.category.icon,
                      color: AppColors.primary,
                      size: 56,
                    ),
                  ),
                ),
              ),
            ),
          ),

          // The intro story, played in a WebView below the photo.
          Expanded(
            child: FutureBuilder<String>(
              future: _storyHtml,
              builder: (context, snap) {
                if (snap.connectionState != ConnectionState.done) {
                  return const Center(child: CircularProgressIndicator());
                }
                if (!snap.hasData) {
                  // If the story fails to build, skip straight to the games.
                  return Center(
                    child: FilledButton(
                      onPressed: _openGames,
                      child: const Text('See the games'),
                    ),
                  );
                }
                return TwineQuestionnaireHost(
                  surveyId: 'intro_${widget.category.name}',
                  title: widget.category.label,
                  htmlContent: snap.data!,
                  onCustomBridgeMessage: (data, _) async {
                    if (data['type'] == 'CCQ_OPEN_GAMES') {
                      _openGames();
                      return true;
                    }
                    return false;
                  },
                );
              },
            ),
          ),
        ],
      ),
    );
  }
}
