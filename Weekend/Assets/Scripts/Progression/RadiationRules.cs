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
        Replacement,
        Human
    }

    public static class RadiationRules
    {
        public static bool CanSeal(int day) => day < 3;
        public static FishStage Fish(int day, bool protectedTank)
        {
            if (protectedTank || day < 3)
                return FishStage.Ordinary;
            return day == 3 ? FishStage.Speaking : FishStage.Human;
        }
    }
}
