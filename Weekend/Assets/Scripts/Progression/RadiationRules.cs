// CORE RULES: foil suppresses the fish's human metamorphosis; stages are shared by rendering and dialogue.
namespace DeathBySeventhsDawn.Progression
{
    public enum FishStage
    {
        Ordinary,
        HumanFace,
        Speaking,
        GrowingLimbs,
        Imitating,
        Replacement
    }

    public static class RadiationRules
    {
        public static FishStage Fish(int day, bool protectedTank)
        {
            if (protectedTank || day < 3)
                return FishStage.Ordinary;
            if (day == 3)
                return FishStage.HumanFace;
            if (day == 4)
                return FishStage.Speaking;
            if (day == 5)
                return FishStage.GrowingLimbs;
            return day == 6 ? FishStage.Imitating : FishStage.Replacement;
        }
    }
}
