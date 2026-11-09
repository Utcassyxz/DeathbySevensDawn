using DeathBySeventhsDawn.Knowledge;

namespace DeathBySeventhsDawn.Progression
{
    public enum CrisisKind { None, Building, City, Cosmic }

    // Both waiting and bed sleep use this rule; no second calendar or debug-only solution.
    public static class CrisisRules
    {
        public const int BuildingDay = 7, CityDay = 5, CosmicDay = 7;
#if UNITY_EDITOR
        public static bool LegacyValidation;
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]static void ResetValidation(){LegacyValidation=false;}
#else
        public const bool LegacyValidation=false;
#endif
        public static CrisisKind Due(int day, ProgressionState state)
        {
            if(!LegacyValidation)return day==7&&!state.day7BombDefused&&!state.stageOneComplete?CrisisKind.Building:CrisisKind.None;
            if (day == 3 && !state.buildingCrisisResolved) return CrisisKind.Building;
            if (day == CityDay && !state.cityCrisisResolved) return CrisisKind.City;
            if (day == CosmicDay && !state.daySevenCrisisResolved) return CrisisKind.Cosmic;
            return CrisisKind.None;
        }

        public static bool CanResolve(CrisisKind kind, int day, ProgressionState state)
        {
            string id = kind == CrisisKind.Building ? (LegacyValidation?"deduction-building-plan":"deduction-day7-plan") : kind == CrisisKind.City ? "deduction-city-orders" : "deduction-observatory";
            int deadline = kind == CrisisKind.Building ? (LegacyValidation?3:BuildingDay) : kind == CrisisKind.City ? CityDay : CosmicDay;
            return kind != CrisisKind.None && (LegacyValidation||kind!=CrisisKind.Building||day==7) && day <= deadline && state.journal.Exists(e => e != null && e.id == id) && (kind!=CrisisKind.Building || state.journal.Exists(e=>e!=null&&e.id=="deduction-source-location"));
        }
    }
}
