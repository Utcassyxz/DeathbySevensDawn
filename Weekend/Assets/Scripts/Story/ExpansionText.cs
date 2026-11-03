using System;

namespace DeathBySeventhsDawn.Story
{
    [Serializable]
    public sealed class ExpansionText
    {
        public string curtainsClosed, bubbleTradeLine;
        public string[] decayCaptions, bubbleReturn, fishGoodChoices, fishBadChoices, fishGoodReplies, fishBadReplies, fishAllyPages;
        public string fishChoicePrompt, fishHelpTitle, fishHelpBody;
        public string[] fishChats, fishSilentChats;
        public string[] apocalypseCaptions, apocalypseReunion, apocalypseSpeakers;
    }
}
