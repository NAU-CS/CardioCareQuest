import 'package:flutter/material.dart';

import 'package:cardio_care_quest/core/widgets/twine_questionnaire_host.dart';

// Loads the Bingo Bash HTML game in the web-view host (no GPS needed).
class WhileYouWaitGame extends StatelessWidget {
  const WhileYouWaitGame({super.key});

  @override
  Widget build(BuildContext context) {
    return const TwineQuestionnaireHost(
      surveyId: 'while_you_wait',
      title: 'While You Wait',
      htmlAsset: 'assets/game/while_you_wait.html',
    );
  }
}
