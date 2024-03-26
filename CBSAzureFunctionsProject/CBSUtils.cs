using PlayFab.Samples;
using PlayFab;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;

namespace CBS
{
    public static class CBSUtils
    {
        private static Random rng = new Random();  

        public static void Shuffle<T>(this IList<T> list)  
        {  
            int n = list.Count;  
            while (n > 1) {  
                n--;  
                int k = rng.Next(n + 1);  
                T value = list[k];  
                list[k] = list[n];  
                list[n] = value;  
            }  
        }

        public static PlayFabAuthenticationContext GetAuthContext (this FunctionExecutionContext<dynamic> args)
        {
            var rawContext = (string)args.FunctionArgument["AuthContext"];
            var auth = string.IsNullOrEmpty(rawContext) ? new PlayFabAuthenticationContext() : JsonConvert.DeserializeObject<PlayFabAuthenticationContext>(rawContext);
            return auth;
        }

        public static BaseTaskState ApplyPlayerState(this CBSTask task, Dictionary<string, BaseTaskState> playerData, int level = 0)
        {
            var id = task.ID;
            var type = task.Type;
            var isLockedByLevel = task.IsLockedByLevel;
            var lockLevel = task.LockLevel;
            
            var newTaskState = new BaseTaskState();

            if (playerData.ContainsKey(id))
            {
                var achievementObject = !playerData.ContainsKey(id) ? new BaseTaskState {
                    IsComplete = false,
                    CurrentStep = 0,
                    Rewarded = false
                } : playerData[id];
                
                // check complete
                newTaskState.IsComplete = achievementObject.IsComplete;
                
                // check steps
                if (type == TaskType.STEPS)
                {
                    newTaskState.CurrentStep = achievementObject.CurrentStep;
                }
                // check reward
                newTaskState.Rewarded = achievementObject.Rewarded;
            }
            else
            {
                // set default state
                newTaskState.IsComplete = false;
                newTaskState.CurrentStep = 0;
                newTaskState.Rewarded = false;
            }
            
            // check lock
            if (isLockedByLevel)
            {
                newTaskState.IsAvailable = level >= lockLevel;
            }
            else
            {
                newTaskState.IsAvailable = true;
            }
            task.UpdateState(newTaskState);
            return newTaskState;
        }

        public static BaseTaskState AddPoints(this CBSTask task, int points, ModifyMethod method)
        {
            var state = task.TaskState;
            var type = task.Type;
            var curSteps = state.CurrentStep;
            var maxSteps = task.Steps;      

            if (type == TaskType.STEPS)
            {
                if (method == ModifyMethod.ADD)
                {
                    curSteps = curSteps + points;
                }
                else if (method == ModifyMethod.UPDATE)
                {
                    curSteps = points;
                }
                
                if (curSteps >= maxSteps)
                {
                    state.IsComplete = true;
                    curSteps = maxSteps;
                }
            }
            else
            {
                state.IsComplete = true;
            }
            state.CurrentStep = curSteps;
            task.UpdateState(state);
            return state;
        }
    }

    public class BattlePassUserStatesData
    {
        public Dictionary<string, BattlePassUserData> States = new Dictionary<string, BattlePassUserData>();
    }

    public class BattlePassUserData
    {
        public string BattlePassID;
        public int CurrentExp;
        public List<int> CollectedSimpleReward = new List<int>();
        public List<int> CollectedPremiumReward = new List<int>();
        public bool PremiumRewardAvailable;
    }

    public class DailyIndexObject
    {
        public int Day;
        public int Index;

        public DailyIndexInformation GetFullInformation()
        {
            return new DailyIndexInformation(this);
        }
    }

    public class DailyIndexInformation
    {
        // for testing
        private const int dailyOfset = 5;


        private int LastSavedDay { get; set; } 
        private int LastSavedIndex { get; set; }

        private long ZoneOfset { get; set; }

        public int TotalDayPassed 
        {
            get
            {
                var time = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
                //var fixedTime = time + ZoneOfset;
                var fixedTime = time + ZoneOfset + 86400000 * dailyOfset;
                var date = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                date = date.AddMilliseconds(fixedTime).ToLocalTime();
                var dayPassed = (int)Math.Floor(fixedTime / (1000f * 60f * 60f * 24f));
                return dayPassed;
            }
        }

        public bool HasActivityToday
        {
            get
            {
                return TotalDayPassed == LastSavedDay;
            }
        }

        public int CurrentIndex
        {
            get
            {
                return TotalDayPassed == LastSavedDay + 1 ? LastSavedIndex + 1 : LastSavedIndex;
            }
        }

        public int NextIndex
        {
            get
            {
                return CurrentIndex == 0 ? CurrentIndex + 1 : CurrentIndex;
            }
        }

        public DailyIndexInformation(DailyIndexObject indexObject, long zoneOfset = 0)
        {
            LastSavedDay = indexObject.Day;
            LastSavedIndex = indexObject.Index;
            ZoneOfset = zoneOfset;
        }
    }

    public class DailyTasksIndexObject : DailyIndexObject
    {
        public List<CBSTask> Tasks;
    }

    public class GrandTaskRewardResult
    {
        public bool Rewarded;
        public PrizeObject ReceivedReward;
    }
}