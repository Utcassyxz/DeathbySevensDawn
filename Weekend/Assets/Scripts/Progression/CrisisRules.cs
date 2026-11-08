using DeathBySeventhsDawn.Knowledge;

namespace DeathBySeventhsDawn.Progression
{
    public enum CrisisKind { None, Building, City, Cosmic }

    // Both waiting and bed sleep use this rule; no second calendar or debug-only solution.
    public static class CrisisRules
    {
        public const int BuildingDay = 3, CityDay = 5, CosmicDay = 7;
        public static CrisisKind Due(int day, ProgressionState state)
        {
            if (day == BuildingDay && !state.buildingCrisisResolved) return CrisisKind.Building;
            if (day == CityDay && !state.cityCrisisResolved) return CrisisKind.City;
            if (day == CosmicDay && !state.daySevenCrisisResolved) return CrisisKind.Cosmic;
            return CrisisKind.None;
        }

        public static bool CanResolve(CrisisKind kind, int day, ProgressionState state)
        {
            string id = kind == CrisisKind.Building ? "deduction-building-plan" : kind == CrisisKind.City ? "deduction-city-orders" : "deduction-observatory";
            int deadline = kind == CrisisKind.Building ? BuildingDay : kind == CrisisKind.City ? CityDay : CosmicDay;
            return kind != CrisisKind.None && day <= deadline && state.journal.Exists(e => e != null && e.id == id) && (kind!=CrisisKind.Building || state.journal.Exists(e=>e!=null&&e.id=="deduction-source-location"));
        }
    }
}
