using System.Collections.Generic;
namespace DeathBySeventhsDawn.Progression
{
    // Additive v7 validation. Original journey fields and migration ownership stay in ProgressionSave.
    public static class SaveValidation
    {
        public static List<string> CheckStage(ProgressionState state)
        {
            var errors=new List<string>();
            if(state.neighborNightBeat<0||state.neighborNightBeat>7)errors.Add("night-beat-range");
            if(state.neighborNightCaughtCount<0||state.day7LoopCount<0)errors.Add("negative-loop-counter");
            if(state.stageReturnDay!=0&&state.stageReturnDay!=2&&state.stageReturnDay!=7)errors.Add("return-day");
            if(state.stageOneComplete&&!state.day7BombDefused)errors.Add("completion-with-armed-device");
            if(float.IsNaN(state.savedMinute)||float.IsInfinity(state.savedMinute))errors.Add("invalid-minute");
            return errors;
        }
        public static void NormalizeStage(ProgressionState state)
        {
            state.stageEvents=state.stageEvents??new List<string>();
            if(state.neighborNightBeat<0||state.neighborNightBeat>7)state.neighborNightBeat=0;
            state.neighborNightCaughtCount=System.Math.Max(0,state.neighborNightCaughtCount);state.day7LoopCount=System.Math.Max(0,state.day7LoopCount);
            if(state.stageReturnDay!=0&&state.stageReturnDay!=2&&state.stageReturnDay!=7)state.stageReturnDay=0;
            if(state.stageOneComplete&&!state.day7BombDefused)state.stageOneComplete=false;
            if(float.IsNaN(state.savedMinute)||float.IsInfinity(state.savedMinute))state.savedMinute=-1;
        }
    }
}
