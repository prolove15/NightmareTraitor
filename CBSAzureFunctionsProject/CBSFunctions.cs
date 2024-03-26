using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using PlayFab.ServerModels;
using PlayFab.GroupsModels;
using PlayFab.DataModels;
using PlayFab.Samples;
using PlayFab.AuthenticationModels;
using PlayFab.MultiplayerModels;
using PlayFab;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Net;
using PlayFab.Internal;
using CBS;

namespace CBS.Functions
{
    public static class CBSFunctions
    {
        // Global variables
        public const string ItemDefaultCatalog = "CBSItems";
        public const string  AzureSecretDataKey = "CBSAzureKey";
        public const string  AzureStorageDataKey = "CBSAzureStorage";
        public const string  MaxClanMembersKey = "CBSMaxClanMembers";
        public const string  DefaultClanMembers = "100";
        public const string  StaticsticExpKey = "PlayerExp";
        public const string  LevelTitleKey = "CBSLevelTable";

        public const string  FriendsAcceptTag = "Accept";
        public const string  FriendsRequestTag = "Request";

        public const string  ChatListTablePrefix = "chatlist";
        public const string  ClanTableID = "cbsclans";
        public const string  TournamentTablePrefix = "cbstournament4";
        public const string  ClanGroupTag = "CBSClan";

        public const string  TournamentDataKey = "CBSTournaments";
        public const string  PlayerTournamentKey = "CBSPlayerTournamentID";
        public const string  PlayerTournamentTableKey = "CBSPlayerTournamentTableID";
        public const string  TournamentGroupTag = "CBSTournament";

        public const string  DailyBonusDataKey = "CBSDailyBonus";
        public const string  RouletteDataKey = "CBSRoulette";
        public const string  BattlePassDataKey = "CBSBattlePass";
        public const string  PlayerDailyBonusDataKey = "CBSPlayerDailyBonus";
        public const string  PlayerDailyBonusIndexKey = "CBSPlayerDailyIndex";

        public const string  AchievementsDataKey = "CBSAchievements";
        public const string  PlayerAchievementsDataKey = "CBSPlayerAchievements";

        public const string DailyTasksDataKey = "CBSDailyTasks";
        public const string PlayerDailyTasksDataKey = "CBSPlayerDailyTasks";
        public const string PlayerDailyTasksIndexDataKey = "CBSPlayerDailyIndexTasks";

        public const string PlayerBattleDataKey = "CBSPlayerBattlePass";

        public static long ServerTimestamp
        {
            get => DateToTimestamp(DateTime.UtcNow);
        }

        // Playfab settings based on Azure environment variables
        private static PlayFabApiSettings FabSettingAPI = new PlayFabApiSettings   {  
            TitleId =
            Environment.GetEnvironmentVariable("PLAYFAB_TITLE_ID", EnvironmentVariableTarget.Process), 
            DeveloperSecretKey =
            Environment.GetEnvironmentVariable("PLAYFAB_DEV_SECRET_KEY", EnvironmentVariableTarget.Process)
        };

        // Profile API
        #region Profile API Methods

        [FunctionName("GetPlayerExperienceData")]
        public static async Task<dynamic> GetPlayerExperienceData([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var argsPlayerID = args["ProfileID"]?.ToString();
            var playerID = string.IsNullOrEmpty(argsPlayerID) ? context.CallerEntityProfile.Lineage.MasterPlayerAccountId : argsPlayerID;
    
            var currentExp = await GetPlayerStatisticValue(playerID, StaticsticExpKey);

            var levelData = await GetTitleData(LevelTitleKey);
            
            var result = ParseLevelDetail(levelData, currentExp);
            return JsonConvert.SerializeObject(result);
        }

        [FunctionName("AddPlayerExperience")]
        public static async Task<dynamic> AddPlayerExperience([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var argsPlayerID = (string)args["ProfileID"];
            var playerID = string.IsNullOrEmpty(argsPlayerID) ? context.CallerEntityProfile.Lineage.MasterPlayerAccountId : argsPlayerID;

            var experienceKey = (string)args["experienceKey"];
            var newExp = (int)args["expValue"];
            var levelGroupId = (string)args["levelGroupId"];
            
            var oldValue = await GetPlayerStatisticValue(playerID, experienceKey);
            var levelData = await GetTitleData(levelGroupId);
            var prevLevelResult = ParseLevelDetail(levelData, oldValue);
            var prevLevel = prevLevelResult.CurrentLevel;
            
            var resultValue = oldValue + newExp;

            var serverApi = new PlayFabServerInstanceAPI(FabSettingAPI);
            
            var request = new UpdatePlayerStatisticsRequest {
                PlayFabId = playerID, 
                Statistics = new List<StatisticUpdate>()
                {
                    new StatisticUpdate{
                        StatisticName = experienceKey,
                        Value = resultValue
                    }
                }
            };
            
            var playerStatResult = await serverApi.UpdatePlayerStatisticsAsync(request);

            var result = ParseLevelDetail(levelData, resultValue);
            var updatedLevel = result.CurrentLevel;
            if (updatedLevel > prevLevel)
            {
                var newLevelResult = await AsignNewLevelResult(result, levelData, updatedLevel, playerID);
                return JsonConvert.SerializeObject(newLevelResult);
            }
            return JsonConvert.SerializeObject(result);
        }

        [FunctionName("GetPlayerProfile")]
        public static async Task<dynamic> GetPlayerProfile([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var authContext = context.GetAuthContext();
            var argsPlayerID = (string)args["ProfileID"];
            var ProfileID = string.IsNullOrEmpty(argsPlayerID) ? context.CallerEntityProfile.Lineage.MasterPlayerAccountId : argsPlayerID;

            var LoadLevel = (bool)args["LoadLevel"];
            var LoadClan = (bool)args["LoadClan"];
            var LoadEntity = (bool)args["LoadEntity"];

            var serverApi = new PlayFabServerInstanceAPI(FabSettingAPI);
            var groupApi = new PlayFabGroupsInstanceAPI(FabSettingAPI, authContext);

            var getProfileRequest = new GetPlayerProfileRequest {
                PlayFabId = ProfileID,
                ProfileConstraints = new PlayerProfileViewConstraints {
                    ShowAvatarUrl = true,
                    ShowDisplayName = true
                }
            };
            
            var jsonLevel = string.Empty;
            var jsonClan = string.Empty;
            var entityID = string.Empty;
            
            if (LoadLevel == true)
            {
                var currentExp = await GetPlayerStatisticValue(ProfileID, StaticsticExpKey);
                var levelData = await GetTitleData(LevelTitleKey);
                var levelDetail = ParseLevelDetail(levelData, currentExp);
                jsonLevel = JsonConvert.SerializeObject(levelDetail);
            }
            
            if (LoadClan == true || LoadEntity)
            {
                var accountRequest = new GetUserAccountInfoRequest {
                    PlayFabId = ProfileID
                };
                var accountResult = await serverApi.GetUserAccountInfoAsync(accountRequest);
                var userInfo = accountResult.Result.UserInfo;
                var titleInfo = userInfo.TitleInfo;
                var entityInfo = titleInfo.TitlePlayerAccount;
                entityID = entityInfo.Id;
                
                if (LoadClan)
                {
                    var clanName = string.Empty;
                    var clanID = await GetUserClanID(entityID, authContext);
                    var exist = false;
                
                    if (clanID != string.Empty)
                    {
                        exist = true;
                        
                        var clanEntity = new PlayFab.GroupsModels.EntityKey {
                            Id = clanID,
                            Type = "group"
                        };
                        
                        var groupRequest = new PlayFab.GroupsModels.GetGroupRequest {
                            Group = clanEntity
                        };
                        
                        var groupResult = await groupApi.GetGroupAsync(groupRequest);
                        
                        clanName = groupResult.Result.GroupName;
                    }
                    
                    var clanResult = new {
                        ExistInClan = exist,
                        ClanID = clanID,
                        ClanName = clanName
                    };
                    
                    jsonClan = JsonConvert.SerializeObject(clanResult);
                }
            }
            
            var result = await serverApi.GetPlayerProfileAsync(getProfileRequest);

            var profile = result.Result.PlayerProfile;
            
            var profileResult = new ProfileData {
                ProfileID = profile.PlayerId,
                DisplayName = profile.DisplayName,
                AvatarUrl = profile.AvatarUrl,
                LevelData = jsonLevel,
                ClanData = jsonClan,
                EntityID = entityID
            };
            
            return JsonConvert.SerializeObject(profileResult);
        }
        
        // Playfab utils
        private static async Task<PlayFabAuthenticationContext> GetServerAuthContext()
        {
            var entityTokenRequest = new GetEntityTokenRequest();
            var authApi = new PlayFabAuthenticationInstanceAPI(FabSettingAPI);
            var entityTokenResult = await authApi.GetEntityTokenAsync(entityTokenRequest);
            var authContext = new PlayFabAuthenticationContext{
                EntityId = entityTokenResult.Result.Entity.Id,
                EntityType = entityTokenResult.Result.Entity.Type,
                EntityToken = entityTokenResult.Result.EntityToken
            };
            return authContext;
        }

        #endregion

        // Profile API
        #region Battle Pass API Methods

        [FunctionName("GetPlayerBattlePassStates")]
        public static async Task<dynamic> GetPlayerBattlePassStates([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var argsPlayerID = (string)args["ProfileID"] ?? string.Empty;
            var profileID = string.IsNullOrEmpty(argsPlayerID) ? context.CallerEntityProfile.Lineage.MasterPlayerAccountId : argsPlayerID;
            var battlePassID = (string)args["BattlePassID"] ?? string.Empty;
            var includeNotStated = args["IncludeNotStarted"] == null ? false : (bool)args["IncludeNotStarted"];
            var includeOutdated = args["IncludeOutdated"] == null ? false : (bool)args["IncludeOutdated"];
            var instances = await LoadBattlePassInstances(battlePassID, includeNotStated, includeOutdated);
            var userStates = await LoadBattlePassUserStates(profileID, instances);

            return JsonConvert.SerializeObject(new BattlePassPlayerStatesCallback{
                PlayerStates = userStates.Select(x=>x.Value).ToList()
            });
        }

        [FunctionName("GetBattlePassFullInformation")]
        public static async Task<dynamic> GetBattlePassFullInformation([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var argsPlayerID = (string)args["ProfileID"] ?? string.Empty;
            var profileID = string.IsNullOrEmpty(argsPlayerID) ? context.CallerEntityProfile.Lineage.MasterPlayerAccountId : argsPlayerID;
            var battlePassID = (string)args["BattlePassID"] ?? string.Empty;

            var instances = await LoadBattlePassInstances(battlePassID);
            var passInstance = instances.FirstOrDefault();
            if (passInstance != null)
            {
                var userStates = await LoadBattlePassUserStates(profileID, instances);
                var state = userStates.FirstOrDefault().Value;
                return JsonConvert.SerializeObject(new BattlePassFullInformationCallback
                {
                    PlayerState = state,
                    Instance = passInstance
                });
            }
            else
            {
                return JsonConvert.SerializeObject(new BattlePassFullInformationCallback());
            }
        }

        [FunctionName("AddBattlePassExpirience")]
        public static async Task<dynamic> AddBattlePassExpirience([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var argsPlayerID = (string)args["ProfileID"] ?? string.Empty;
            var profileID = string.IsNullOrEmpty(argsPlayerID) ? context.CallerEntityProfile.Lineage.MasterPlayerAccountId : argsPlayerID;
            var battlePassID = (string)args["BattlePassID"] ?? string.Empty;
            var expToAdd = args["Exp"] == null ? 0 : (int)args["Exp"];
            var addToAll = string.IsNullOrEmpty(battlePassID);

            var instances = await LoadBattlePassInstances(battlePassID);
            var addResult = await AddExpToBattlePassInstance(profileID, expToAdd, instances);

            return JsonConvert.SerializeObject(new BattlePassAddExpirienceCallback{
                ExpTable = addResult
            });
        }

        [FunctionName("GrantPremiumAccessToBattlePass")]
        public static async Task<dynamic> GrantPremiumAccessToBattlePass([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var argsPlayerID = (string)args["ProfileID"] ?? string.Empty;
            var profileID = string.IsNullOrEmpty(argsPlayerID) ? context.CallerEntityProfile.Lineage.MasterPlayerAccountId : argsPlayerID;
            var battlePassID = (string)args["BattlePassID"] ?? string.Empty;

            var instances = await LoadBattlePassInstances(battlePassID);
            if (instances == null || instances.Length == 0)
            {
                return BattlePassNotFound();
            }
            var instance = instances.FirstOrDefault();
            var userStates = await LoadBattlePassUserStates(profileID, instances);
            var state = userStates.ContainsKey(battlePassID) ? userStates[battlePassID] : new BattlePassUserInfo();

            var IsPremiumAccess = state.PremiumRewardAvailable;
            if (IsPremiumAccess)
            {
                return  PremiumAccessAlreadyGranted();
            }

            var userStatesData = await GetPlayerBattlePassData(profileID);
            var userDataTable = userStatesData.States ?? new Dictionary<string, BattlePassUserData>();
            var stateData = userDataTable.ContainsKey(battlePassID) ? userDataTable[battlePassID] : new BattlePassUserData();
            stateData.PremiumRewardAvailable = true;
            userDataTable[battlePassID] = stateData;
            userStatesData.States = userDataTable;

            state.PremiumRewardAvailable = true;
            userStates[battlePassID] = state;
            var saveResult = await SavePlayerBattlePassData(profileID, userStatesData);
            return JsonConvert.SerializeObject(saveResult);
        }

        [FunctionName("ResetPlayerStateForBattlePass")]
        public static async Task<dynamic> ResetPlayerStateForBattlePass([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var argsPlayerID = (string)args["ProfileID"] ?? string.Empty;
            var profileID = string.IsNullOrEmpty(argsPlayerID) ? context.CallerEntityProfile.Lineage.MasterPlayerAccountId : argsPlayerID;
            var battlePassID = (string)args["BattlePassID"] ?? string.Empty;

            var instances = await LoadBattlePassInstances(battlePassID);
            if (instances == null || instances.Length == 0)
            {
                return BattlePassNotFound();
            }
            var instance = instances.FirstOrDefault();
            var userStates = await LoadBattlePassUserStates(profileID, instances);
            var state = userStates.ContainsKey(battlePassID) ? userStates[battlePassID] : new BattlePassUserInfo();

            var userStatesData = await GetPlayerBattlePassData(profileID);
            var userDataTable = userStatesData.States ?? new Dictionary<string, BattlePassUserData>();
            var stateData = new BattlePassUserData();
            userDataTable[battlePassID] = stateData;
            userStatesData.States = userDataTable;

            userStates[battlePassID] = state;
            var saveResult = await SavePlayerBattlePassData(profileID, userStatesData);
            return JsonConvert.SerializeObject(saveResult);
        }

        [FunctionName("GetRewardFromBattlePassInstance")]
        public static async Task<dynamic> GetRewardFromBattlePassInstance([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var argsPlayerID = (string)args["ProfileID"] ?? string.Empty;
            var profileID = string.IsNullOrEmpty(argsPlayerID) ? context.CallerEntityProfile.Lineage.MasterPlayerAccountId : argsPlayerID;
            var battlePassID = (string)args["BattlePassID"] ?? string.Empty;
            var level = args["Level"] == null ? 0 : (int)args["Level"];
            var isPremium = args["IsPremium"] == null ? false : (bool)args["IsPremium"];

            var instances = await LoadBattlePassInstances(battlePassID);
            if (instances == null || instances.Length == 0)
            {
                return BattlePassNotFound();
            }
            var instance = instances.FirstOrDefault();
            var userStates = await LoadBattlePassUserStates(profileID, instances);
            var state = userStates.ContainsKey(battlePassID) ? userStates[battlePassID] : new BattlePassUserInfo();
            var levels = instance.LevelTree;
            if (levels == null || levels.Count == 0 || level > levels.Count)
            {
                return RewardNotFound();
            }

            var levelObject = levels[level];
            var awardToGrant = isPremium == true ? levelObject.PremiumReward : levelObject.DefaultReward;
            if (awardToGrant == null)
            {
                return RewardNotFound();
            }

            var IsPremiumAccess = state.PremiumRewardAvailable;
            if (!IsPremiumAccess && isPremium)
            {
                return RewardNotAvailable();
            }

            if (state.PlayerLevel < level || !state.IsActive)
            {
                return RewardNotAvailable();
            }

            var playerRecivedReward = isPremium ? state.CollectedPremiumReward : state.CollectedSimpleReward;
            playerRecivedReward = playerRecivedReward ?? new int[]{};
            var playerRecivedRewardList = playerRecivedReward.ToList();
            if (playerRecivedReward.Contains(level))
            {
                return RewardAlreadyReceived();
            }

            await AddPrizes(awardToGrant, profileID);

            playerRecivedRewardList.Add(level);

            var userStatesData = await GetPlayerBattlePassData(profileID);
            var userDataTable = userStatesData.States ?? new Dictionary<string, BattlePassUserData>();
            var stateData = userDataTable.ContainsKey(battlePassID) ? userDataTable[battlePassID] : new BattlePassUserData();

            if (isPremium)
            {
                stateData.CollectedPremiumReward = playerRecivedRewardList;
            }
            else
            {
                stateData.CollectedSimpleReward = playerRecivedRewardList;
            }

            userDataTable[battlePassID] = stateData;
            userStatesData.States = userDataTable;
            await SavePlayerBattlePassData(profileID, userStatesData);

            return JsonConvert.SerializeObject(new BattlePassGrantAwardCallback{
                InstanceID = battlePassID,
                IsPremium = isPremium,
                RecivedReward = awardToGrant
            });
        }

        public static async Task<Dictionary<string, int>> AddExpToBattlePassInstance(string profileID, int exp, BattlePassInstance [] instances)
        {
            var userStatesData = await GetPlayerBattlePassData(profileID);
            var userStates = userStatesData.States ?? new Dictionary<string, BattlePassUserData>();
            // check if instance is active
            var activeInstances = instances.Where(x=>IsBattleBassActive(x)).ToArray();
            var resultTable = new Dictionary<string, int>();
            for (int i = 0; i < activeInstances.Length; i++)
            {
                var instance = activeInstances[i];
                var levelCount = instance.LevelTree == null ? 0 : instance.LevelTree.Count;
                var maxExp = levelCount * instance.ExpStep;
                var instanceID = instance.ID;
                if (userStates.ContainsKey(instanceID))
                {
                    var currentExp = userStates[instanceID].CurrentExp;
                    currentExp+=exp;
                    if (currentExp > maxExp)
                        currentExp = maxExp;
                    userStates[instanceID].CurrentExp=currentExp;
                }
                else
                {
                    userStates[instanceID] = new BattlePassUserData();
                    var currentExp = userStates[instanceID].CurrentExp;
                    currentExp+=exp;
                    if (currentExp > maxExp)
                        currentExp = maxExp;
                    userStates[instanceID].CurrentExp=currentExp;
                }
                resultTable[instanceID] = exp;
            }
            userStatesData.States = userStates;
            var saveResult = await SavePlayerBattlePassData(profileID, userStatesData);
            return resultTable;
        }

        public static async Task<Dictionary<string, BattlePassUserInfo>> LoadBattlePassUserStates(string profileID, BattlePassInstance[] instances)
        {
            var userStatesResult = new Dictionary<string, BattlePassUserInfo>();
            var instanceCount = instances.Length;
            var userStatesData = await GetPlayerBattlePassData(profileID);
            var userStates = userStatesData.States ?? new Dictionary<string, BattlePassUserData>();

            for (int i=0;i<instanceCount;i++)
            {
                var passInstance = instances[i];
                var passID = passInstance.ID;
                var userState = userStates.ContainsKey(passID) ? userStates[passID] : new BattlePassUserData();
                var userResult = ParseUserBattlePassResult(profileID, passInstance, userState);
                userStatesResult[passID] = userResult;
            }
            return userStatesResult;
        }

        private static async Task<BattlePassInstance[]> LoadBattlePassInstances(string battlePassID = "", bool includeNotStated = false, bool includeOutdated = false)
        {
            var loadByPassID = !string.IsNullOrEmpty(battlePassID);
            var battlePassData = await GetBattlePassData();
            battlePassData ??= new BattlePassData();
            var dataArray = new List<BattlePassInstance>();
            if (loadByPassID)
            {
                dataArray = battlePassData.Instances.Where(x=>x.State == DevelopmentState.RELEASE && x.ID == battlePassID).ToList();
                return dataArray.ToArray();
            }
            dataArray = battlePassData.Instances.Where(x=>x.State == DevelopmentState.RELEASE).ToList();
            if (!includeNotStated)
            {
                dataArray = dataArray.Where(x=>ServerTimestamp > DateToTimestamp(x.Duration.Start)).ToList();
            }
            if (!includeOutdated)
            {
                dataArray = dataArray.Where(x=>ServerTimestamp < DateToTimestamp(x.Duration.End)).ToList();
            }
            return dataArray.ToArray();
        }

        private static BattlePassUserInfo ParseUserBattlePassResult(string profileID, BattlePassInstance instance, BattlePassUserData userData)
        {
            var collectedSimpleRewards = userData.CollectedSimpleReward;
            var collectedPremiumRewards = userData.CollectedPremiumReward;
            var passID = instance.ID;
            var isPremium = userData.PremiumRewardAvailable;
            var passName = instance.DisplayName;
            var userExp = userData.CurrentExp;
            var passExpStep = instance.ExpStep;
            var expOfCurrentLevel = userExp % passExpStep;
            var customRawData = instance.CustomRawData;
            var customDataClass = instance.CustomDataClassName;
            var badgeCount = 0;
            var isActive = IsBattleBassActive(instance);
            var period = instance.Duration;
            var milisecondsToStart = period == null ? 0 : DateToTimestamp(period.Start) - ServerTimestamp;
            if (milisecondsToStart < 0)
                milisecondsToStart = 0;
            var milisecondsToEnd = period == null || isActive == false ? 0 : DateToTimestamp(period.End) - ServerTimestamp;
            if (milisecondsToEnd < 0)
                milisecondsToEnd = 0;
            var milisecondsActive = period == null || isActive == false ? 0 : ServerTimestamp - DateToTimestamp(period.Start);
            if (milisecondsActive < 0)
                milisecondsActive = 0;

            var levelTree = instance.LevelTree == null ? new List<BattlePassLevel>() : instance.LevelTree;
            var levelCount = levelTree.Count;
            var maxLevel = levelCount;
            var currentLevel = userExp/passExpStep;
            if (currentLevel > maxLevel)
                currentLevel = maxLevel;
            var reachMaxLevel = currentLevel == maxLevel;

            //var levelsToCheck = Math.Clamp(currentLevel, 0, levelTree.Count);
            if (levelTree != null && levelTree.Count != 0 && isActive)
            {
                var levelsToCheck = reachMaxLevel ? levelCount -1 : currentLevel;
                for (int i=0;i<=levelsToCheck;i++)
                {
                    var level = levelTree[i];
                    var simpleReward = level.DefaultReward;
                    var premiumReward = level.PremiumReward;
                    if (simpleReward != null)
                    {
                        if (!collectedSimpleRewards.Contains(i))
                            badgeCount+=simpleReward.GetPositionCount();
                    }
                    if (premiumReward != null && isPremium)
                    {
                        if (!collectedPremiumRewards.Contains(i))
                            badgeCount+=premiumReward.GetPositionCount();
                    }
                }
            }

            return new BattlePassUserInfo{
                PlayerID = profileID,
                BattlePassID = passID,
                BattlePassName = passName,
                PlayerLevel = currentLevel,
                PlayerExp = userExp,
                ExpOfCurrentLevel = expOfCurrentLevel,
                ExpStep = passExpStep,
                PremiumRewardAvailable = isPremium,
                CollectedSimpleReward = collectedSimpleRewards.ToArray(),
                CollectedPremiumReward = collectedPremiumRewards.ToArray(),
                RewardBadgeCount = badgeCount,
                IsActive = isActive,
                MilisecondsToStart = milisecondsToStart,
                MilisecondsToEnd = milisecondsToEnd,
                MilisecondsActive = milisecondsActive,
                CustomRawData = customRawData,
                CustomDataClassName = customDataClass
            };
        }

        private static bool IsBattleBassActive(BattlePassInstance instance)
        {
            var period = instance.Duration;
            if (period == null)
                return false;
            return IsPeriodActive(period);
        }

        #endregion

        // Currency API
        #region Currency API Methods

        [FunctionName("AddVirtualCurrency")]
        public static async Task<dynamic> AddVirtualCurrency([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var argsPlayerID = (string)args["ProfileID"];
            var currencyCode = (string)args["Currency"];
            var currentAmount = (int)args["Amount"];
            var profileID = string.IsNullOrEmpty(argsPlayerID) ? context.CallerEntityProfile.Lineage.MasterPlayerAccountId : argsPlayerID;

            var serverApi = new PlayFabServerInstanceAPI(FabSettingAPI);

            var request = new AddUserVirtualCurrencyRequest{
                PlayFabId = profileID,
                Amount = currentAmount,
                VirtualCurrency = currencyCode
            };

            await serverApi.AddUserVirtualCurrencyAsync(request);

            return new OkObjectResult(string.Empty);
        }

        [FunctionName("DecreaseVirtualCurrency")]
        public static async Task<dynamic> DecreaseVirtualCurrency([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var argsPlayerID = (string)args["ProfileID"];
            var currencyCode = (string)args["Currency"];
            var currentAmount = (int)args["Amount"];
            var profileID = string.IsNullOrEmpty(argsPlayerID) ? context.CallerEntityProfile.Lineage.MasterPlayerAccountId : argsPlayerID;

            var serverApi = new PlayFabServerInstanceAPI(FabSettingAPI);

            var request = new SubtractUserVirtualCurrencyRequest{
                PlayFabId = profileID,
                Amount = currentAmount,
                VirtualCurrency = currencyCode
            };

            await serverApi.SubtractUserVirtualCurrencyAsync(request);

            return new OkObjectResult(string.Empty);
        }

        #endregion

        // Items
        #region Items API Methods

        [FunctionName("GrandRegistrationPrize")]
        public static async Task<dynamic> GrandRegistrationPrize([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var argsPlayerID = (string)args["playerID"];
            var levelGroupId = (string)args["levelGroupId"];
            var profileID = string.IsNullOrEmpty(argsPlayerID) ? context.CallerEntityProfile.Lineage.MasterPlayerAccountId : argsPlayerID;

            var serverApi = new PlayFabServerInstanceAPI(FabSettingAPI);

            var levelData = await GetTitleData(levelGroupId);
            if (!string.IsNullOrEmpty(levelData))
            {
                var levels = JsonConvert.DeserializeObject<LevelTable>(levelData);

                var prizeObject = levels.RegistrationPrize;
                await AddPrizes(prizeObject, profileID);
            }

            return new OkObjectResult(string.Empty);
        }

        [FunctionName("GrandItem")]
        public static async Task<dynamic> GrandItem([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var argsPlayerID = (string)args["playerID"];
            var itemsRaw = (string)args["items"];
            var catalogID = (string)args["catalogID"];
            var profileID = string.IsNullOrEmpty(argsPlayerID) ? context.CallerEntityProfile.Lineage.MasterPlayerAccountId : argsPlayerID;
            var itemIDs = JsonConvert.DeserializeObject<string []>(itemsRaw);

            var serverApi = new PlayFabServerInstanceAPI(FabSettingAPI);

            var request = new GrantItemsToUserRequest {
                PlayFabId = profileID,
                ItemIds = itemIDs.ToList(),
                CatalogVersion = catalogID
            };
            
            var result = await serverApi.GrantItemsToUserAsync(request);
            
            return JsonConvert.SerializeObject(result.Result);
        }

        [FunctionName("GrandBundle")]
        public static async Task<dynamic> GrandBundle([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var argsPlayerID = (string)args["playerID"];
            var itemID = (string)args["item"];
            var catalogID = (string)args["catalogID"];
            var profileID = string.IsNullOrEmpty(argsPlayerID) ? context.CallerEntityProfile.Lineage.MasterPlayerAccountId : argsPlayerID;

            var serverApi = new PlayFabServerInstanceAPI(FabSettingAPI);

            var request = new GrantItemsToUserRequest {
                PlayFabId = profileID,
                ItemIds = new List<string>() {itemID},
                CatalogVersion = catalogID
            };
            
            var result = await serverApi.GrantItemsToUserAsync(request);
            var grandResult = result.Result.ItemGrantResults.FirstOrDefault();
            var success = grandResult.Result;
            var instanceID = grandResult.ItemInstanceId;
            if (success == true)
            {
                var revokeRequest = new RevokeInventoryItemRequest {
                    ItemInstanceId = instanceID,
                    PlayFabId = profileID
                };
                await serverApi.RevokeInventoryItemAsync(revokeRequest);
            }
            else
            {
                return DefaultError();
            }
            
            return JsonConvert.SerializeObject(result.Result);
        }

        #endregion

        // Inventory
        #region  Inventory API Methods

        [FunctionName("RemoveInvertoryItem")]
        public static async Task<dynamic> RemoveInvertoryItem([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var argsPlayerID = (string)args["playerID"];
            var itemID = (string)args["item"];
            var profileID = string.IsNullOrEmpty(argsPlayerID) ? context.CallerEntityProfile.Lineage.MasterPlayerAccountId : argsPlayerID;

            var serverApi = new PlayFabServerInstanceAPI(FabSettingAPI);

            var revokeRequest = new RevokeInventoryItemRequest {
                ItemInstanceId = itemID,
                PlayFabId = profileID
            };

            var result = await serverApi.RevokeInventoryItemAsync(revokeRequest);
            return JsonConvert.SerializeObject(result);
        }

        [FunctionName("UpdateInvertoryItemData")]
        public static async Task<dynamic> UpdateInvertoryItemData([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var argsPlayerID = (string)args["playerID"];
            var itemID = (string)args["item"];
            var characterID = (string)args["characterID"];
            var dataKey = (string)args["dataKey"];
            var dataValue = (string)args["dataValue"];
            var profileID = string.IsNullOrEmpty(argsPlayerID) ? context.CallerEntityProfile.Lineage.MasterPlayerAccountId : argsPlayerID;

            var serverApi = new PlayFabServerInstanceAPI(FabSettingAPI);

            var dictData = new Dictionary<string, string>();
            dictData.Add(dataKey, dataValue);

            var updateRequest = new UpdateUserInventoryItemDataRequest {
                ItemInstanceId = itemID,
                PlayFabId = profileID,
                CharacterId = characterID,
                Data = dictData
            };

            var result = await serverApi.UpdateUserInventoryItemCustomDataAsync(updateRequest);
            return JsonConvert.SerializeObject(result);
        }

        #endregion

        // Chat
        #region Chat API Methods

        [FunctionName("GetDataFromTable")]
        public static async Task<dynamic> GetDataFromTable([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var tableID = (string)args["tableID"];
            var nTop = (int?)args["nTop"];
            var partitionKey = (string)args["partitionKey"];
            var rowKey = (string)args["rowKey"];
            
            var secretKey = await GetInternalTitleData(AzureSecretDataKey);
            var storageKey = await GetInternalTitleData(AzureStorageDataKey);

            var url = AzureUtils.GetTableQueryURL(storageKey, secretKey, tableID, rowKey, partitionKey, nTop);

            var requestClient = new HttpClient();
            var httpRequestMessage = AzureUtils.GetHeader(AzureRequestType.GET_TABLE_DATA, url);

            var response = await requestClient.SendAsync(httpRequestMessage);
            var rawData = await response.Content.ReadAsStringAsync();

            var data = JsonConvert.DeserializeObject<dynamic>(rawData);
            var azureResult = new AzureTableRequestResult(data);
            if (azureResult.Error != null)
            {
                var errorObj = azureResult.Error;
                if (errorObj.code == AzureUtils.TABLE_NOT_FOUND_CODE)
                {
                    return azureResult.EmptyValue();
                }
                else
                {
                    return DefaultError();
                }
            }
            else
            {
                return azureResult.RawResult;
            }
        }

        [FunctionName("InsertDataToTable")]
        public static async Task<dynamic> InsertDataToTable([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var tableID = (string)args["tableID"];
            var partitionKey = (string)args["partitionKey"];
            var rowKey = (string)args["rowKey"];
            var rawData = (string)args["rawData"];
            var createTable = (bool)args["createTable"];
            
            var secretKey = await GetInternalTitleData(AzureSecretDataKey);
            var storageKey = await GetInternalTitleData(AzureStorageDataKey);

            var url = AzureUtils.GetTableInsertURL(storageKey, secretKey, tableID);

            if (partitionKey == null)
            {
                var serverTime = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
                partitionKey = (10000000000000 - (long)serverTime).ToString();
            }
            var requestClient = new HttpClient();
            var httpRequestMessage = AzureUtils.GetHeader(AzureRequestType.INSERT_DATA, url);
            var contentBody = JsonConvert.SerializeObject(new 
            { 
                RowKey = rowKey,
                PartitionKey = partitionKey,
                RawData = rawData
            });
            httpRequestMessage.Content = new StringContent(contentBody, Encoding.UTF8, "application/json");
            var response = await requestClient.SendAsync(httpRequestMessage);
            var responseRawData = await response.Content.ReadAsStringAsync();

            var data = JsonConvert.DeserializeObject<dynamic>(responseRawData);
            var azureResult = new AzureTableRequestResult(data);

            if (azureResult.Error != null && createTable)
            {
                var errorBody = azureResult.Error;
                var errorCode = errorBody.code;
                if (errorCode == AzureUtils.TABLE_NOT_FOUND_CODE)
                {
                    var createResult = await CreateTable(tableID);
                    if (createResult.Error == null)
                    {
                        var requestClient2 = new HttpClient();
                        var httpRequestMessage2 = AzureUtils.GetHeader(AzureRequestType.INSERT_DATA, url);
                        httpRequestMessage2.Content = new StringContent(contentBody, Encoding.UTF8, "application/json");
                        var response2 = await requestClient2.SendAsync(httpRequestMessage2);
                        var responseRawData2 = await response2.Content.ReadAsStringAsync();

                        var data2 = JsonConvert.DeserializeObject<dynamic>(responseRawData2);
                        var azureResult2 = new AzureTableRequestResult(data2);
                        if (azureResult2.Error != null)
                        {
                            return DefaultError();
                        }
                        return azureResult2.RawResult;
                    }
                    else
                    {
                        return DefaultError();
                    }
                }
                else
                {
                    return DefaultError();
                }
            }
            else
            {
                return azureResult.RawResult;
            }
        }

        [FunctionName("ClearUnreadMessage")]
        public static async Task<dynamic> ClearUnreadMessage([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var profileID = (string)args["ProfileID"];
            var userID = (string)args["UserID"];       
            var tableID = ChatListTablePrefix + profileID;
            
            var secretKey = await GetInternalTitleData(AzureSecretDataKey);
            var storageKey = await GetInternalTitleData(AzureStorageDataKey);

            var url = AzureUtils.GetUpdateQueryURL(storageKey, secretKey, tableID, userID, string.Empty);

            var requestClient = new HttpClient();
            var httpRequestMessage = AzureUtils.GetHeader(AzureRequestType.PATCH_TABLES, url);
            var contentBody = JsonConvert.SerializeObject(new 
            { 
                UnreadCount = "0"
            });
            httpRequestMessage.Content = new StringContent(contentBody, Encoding.UTF8, "application/json");
            await requestClient.SendAsync(httpRequestMessage);
            return new OkObjectResult("Ok");
        }

        [FunctionName("UpdateTableData")]
        public static async Task<dynamic> UpdateTableData([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var tableID = (string)args["tableID"];
            var partitionKey = (string)args["partitionKey"] ?? string.Empty;
            var rowKey = (string)args["rowKey"] ?? string.Empty;
            var rawData = (string)args["rawData"] ?? string.Empty;
            
            var secretKey = await GetInternalTitleData(AzureSecretDataKey);
            var storageKey = await GetInternalTitleData(AzureStorageDataKey);

            var url = AzureUtils.GetUpdateQueryURL(storageKey, secretKey, tableID, rowKey, partitionKey);

            var requestClient = new HttpClient();
            var httpRequestMessage = AzureUtils.GetHeader(AzureRequestType.UPDATE_TABLE, url);
            var contentBody = JsonConvert.SerializeObject(new 
            { 
                RawData = rawData
            });
            httpRequestMessage.Content = new StringContent(contentBody, Encoding.UTF8, "application/json");
            await requestClient.SendAsync(httpRequestMessage);
            return new OkObjectResult("Ok");
        }

        [FunctionName("DeleteTableData")]
        public static async Task<dynamic> DeleteTableData([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var tableID = (string)args["tableID"];
            var partitionKey = (string)args["partitionKey"] ?? string.Empty;
            var rowKey = (string)args["rowKey"] ?? string.Empty;

            var secretKey = await GetInternalTitleData(AzureSecretDataKey);
            var storageKey = await GetInternalTitleData(AzureStorageDataKey);

            var url = AzureUtils.GetDeleteQueryURL(storageKey, secretKey, tableID, rowKey, partitionKey);
            var httpRequestMessage = AzureUtils.GetHeader(AzureRequestType.DELETE_TABLE, url);

            var requestClient = new HttpClient();
            var response = await requestClient.SendAsync(httpRequestMessage);
            return new OkObjectResult("Ok");
        }

        [FunctionName("GetTables")]
        public static async Task<dynamic> GetTables([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());

            var secretKey = await GetInternalTitleData(AzureSecretDataKey);
            var storageKey = await GetInternalTitleData(AzureStorageDataKey);

            var url = AzureUtils.GetTableSimpleURL(storageKey, secretKey);
            var httpRequestMessage = AzureUtils.GetHeader(AzureRequestType.GET_ALL_TABLES, url);

            var requestClient = new HttpClient();
            var response = await requestClient.SendAsync(httpRequestMessage);
            var responseRawData = await response.Content.ReadAsStringAsync();

            var data = JsonConvert.DeserializeObject<dynamic>(responseRawData);
            var azureResult = new AzureTableRequestResult(data);
            if (azureResult.Error != null)
            {
                return DefaultError();
            }
            return azureResult.RawResult;
        }

        [FunctionName("UpdateMessageList")]
        public static async Task<dynamic> UpdateMessageList([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var partitionKey = (string)args["partitionKey"] ?? string.Empty;
            var rowKey = (string)args["rowKey"] ?? string.Empty;
            var reciverID = (string)args["reciverID"] ?? string.Empty;
            var lastMessage = (string)args["lastMessage"] ?? string.Empty;

            var argsPlayerID = (string)args["ProfileID"];
            var senderID = string.IsNullOrEmpty(argsPlayerID) ? context.CallerEntityProfile.Lineage.MasterPlayerAccountId : argsPlayerID;

            var serverApi = new PlayFabServerInstanceAPI(FabSettingAPI);

            var getProfileRequest = new GetPlayerProfileRequest {
                PlayFabId = senderID,
                ProfileConstraints = new PlayerProfileViewConstraints {
                    ShowDisplayName = true
                }
            };

            var getProfileResult = await serverApi.GetPlayerProfileAsync(getProfileRequest);
            var senderName = getProfileResult.Result.PlayerProfile.DisplayName;

            getProfileRequest.PlayFabId = reciverID;
            getProfileResult = await serverApi.GetPlayerProfileAsync(getProfileRequest);
            var reciverName = getProfileResult.Result.PlayerProfile.DisplayName;

            var updateResult = await InsertOrUpdateMessageList(senderID, reciverID, reciverName, rowKey, lastMessage, false);
            updateResult = await InsertOrUpdateMessageList(reciverID, senderID, senderName, rowKey, lastMessage, true);

            return updateResult.RawResult;
        }

        private static async Task<AzureTableRequestResult> CreateTable(string tableID)
        {
            var secretKey = await GetInternalTitleData(AzureSecretDataKey);
            var storageKey = await GetInternalTitleData(AzureStorageDataKey);
            
            var requestClient = new HttpClient();

            var url = AzureUtils.GetTableSimpleURL(storageKey, secretKey);
            var httpRequestMessage = AzureUtils.GetHeader(AzureRequestType.CREATE_TABLE, url);
            var contentBody = JsonConvert.SerializeObject(new 
            { 
                TableName = tableID
            });

            httpRequestMessage.Content = new StringContent(contentBody, Encoding.UTF8, "application/json");

            var response = await requestClient.SendAsync(httpRequestMessage);
            var responseRawData = await response.Content.ReadAsStringAsync();

            var data = JsonConvert.DeserializeObject<dynamic>(responseRawData);
            return new AzureTableRequestResult(data);
        }

        private static async Task<AzureTableRequestResult> InsertOrUpdateMessageList(string userID, string reciverID, string userName, string rowKey, string lastMessage, bool inscreseCount)
        {
            var tableID = ChatListTablePrefix + userID;
            var serverTime = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();

            var secretKey = await GetInternalTitleData(AzureSecretDataKey);
            var storageKey = await GetInternalTitleData(AzureStorageDataKey);

            var unreadCount = 0;

            // check UnreadCount
            if (inscreseCount)
            {
                var urlCount = AzureUtils.GetTableQueryURL(storageKey, secretKey, tableID, reciverID, string.Empty, null);
                var httpRequestMessage = AzureUtils.GetHeader(AzureRequestType.GET_TABLE_DATA, urlCount);

                var requestClient = new HttpClient();
                var response = await requestClient.SendAsync(httpRequestMessage);

                var responseRawData = await response.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<dynamic>(responseRawData ?? "{}");
                var azureResult = new AzureTableRequestResult(data);
                if (azureResult.Error != null || data["UnreadCount"] == null)
                {
                    unreadCount++;
                }
                else
                {
                    var currentUnreadRaw = (string)data["UnreadCount"];
                    var currentUnread = 0;
                    int.TryParse(currentUnreadRaw, out currentUnread);
                    unreadCount = currentUnread + 1;
                }
            }

            var url = AzureUtils.GetTableQueryURL(storageKey, secretKey, tableID, reciverID, string.Empty, null);
            var updateRequestMessage = AzureUtils.GetHeader(AzureRequestType.UPDATE_TABLE, url);

            var contentBody = JsonConvert.SerializeObject(new 
            { 
                UserName = userName,
                UserID = reciverID,
                LastMessage = lastMessage,
                UpdateTime = serverTime.ToString(),
                UnreadCount = unreadCount.ToString()
            });
            updateRequestMessage.Content = new StringContent(contentBody, Encoding.UTF8, "application/json");

            var updateClient = new HttpClient();
            var updateResponse = await updateClient.SendAsync(updateRequestMessage);
            var updateRawData = updateResponse == null ? string.Empty : await updateResponse.Content.ReadAsStringAsync();

            var updateData = JsonConvert.DeserializeObject<dynamic>(string.IsNullOrEmpty(updateRawData) || updateRawData == "Null"  ? "{}" : updateRawData);

            var azureUpdateResult = new AzureTableRequestResult(updateData);

            // check table to exist

            if (azureUpdateResult.Error != null)
            {
                var errorBody = azureUpdateResult.Error;
                var errorCode = errorBody.code;
                if (errorBody.code == AzureUtils.TABLE_NOT_FOUND_CODE)
                {
                    var createTableResult = await CreateTable(tableID);
                    if (createTableResult.Error != null)
                    {
                        return createTableResult;
                    }
                    else
                    {
                        var updateRequestMessage2 = AzureUtils.GetHeader(AzureRequestType.UPDATE_TABLE, url);
                        updateRequestMessage2.Content = new StringContent(contentBody, Encoding.UTF8, "application/json");

                        var updateClient2 = new HttpClient();
                        var updateResponse2 = await updateClient2.SendAsync(updateRequestMessage2);
                    }
                }
            }
            return azureUpdateResult;
        }

        #endregion

        // Friends
        #region Friends API Methods

        [FunctionName("GetFriendsList")]
        public static async Task<dynamic> GetFriendsList([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var argsPlayerID = (string)args["ProfileID"] ?? string.Empty;
            var playerID = string.IsNullOrEmpty(argsPlayerID) ? context.CallerEntityProfile.Lineage.MasterPlayerAccountId : argsPlayerID;

            var serverApi = new PlayFabServerInstanceAPI(FabSettingAPI);

            var friendsRequest = new GetFriendsListRequest {
                PlayFabId = playerID,
                ProfileConstraints = new PlayerProfileViewConstraints {
                    ShowAvatarUrl = true,
                    ShowDisplayName = true,
                    ShowTags = true
                }
            };
            var friendResult = await serverApi.GetFriendsListAsync(friendsRequest);
            // some change
            return JsonConvert.SerializeObject(friendResult.Result);
        }

        [FunctionName("SendFriendRequest")]
        public static async Task<dynamic> SendFriendRequest([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var argsPlayerID = (string)args["ProfileID"] ?? string.Empty;
            var playerID = string.IsNullOrEmpty(argsPlayerID) ? context.CallerEntityProfile.Lineage.MasterPlayerAccountId : argsPlayerID;

            var friendID = (string)args["friendID"] ?? string.Empty;

            var serverApi = new PlayFabServerInstanceAPI(FabSettingAPI);

            var friendsRequest = new AddFriendRequest {
                PlayFabId = playerID,
                FriendPlayFabId = friendID
            };
            var addResult = await serverApi.AddFriendAsync(friendsRequest);

            friendsRequest.PlayFabId = friendID;
            friendsRequest.FriendPlayFabId = playerID;
            
            addResult = await serverApi.AddFriendAsync(friendsRequest);

            var setTagRequset = new SetFriendTagsRequest {
                PlayFabId = friendID,
                FriendPlayFabId = playerID,
                Tags = new List<string>() {FriendsRequestTag}
            };
            
            var tagResult = await serverApi.SetFriendTagsAsync(setTagRequset);
            
            return tagResult;
        }

        [FunctionName("RemoveFriend")]
        public static async Task<dynamic> RemoveFriend([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var argsPlayerID = (string)args["ProfileID"] ?? string.Empty;
            var playerID = string.IsNullOrEmpty(argsPlayerID) ? context.CallerEntityProfile.Lineage.MasterPlayerAccountId : argsPlayerID;

            var friendID = (string)args["friendID"] ?? string.Empty;

            var serverApi = new PlayFabServerInstanceAPI(FabSettingAPI);

            var request = new RemoveFriendRequest {
                PlayFabId = playerID,
                FriendPlayFabId = friendID
            };
            
            var removeResult = await serverApi.RemoveFriendAsync(request);
            
            request.PlayFabId = friendID;
            request.FriendPlayFabId = playerID;
            
            removeResult = await serverApi.RemoveFriendAsync(request);
            
            return removeResult;
        }

        [FunctionName("AcceptFriend")]
        public static async Task<dynamic> AcceptFriend([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var argsPlayerID = (string)args["ProfileID"] ?? string.Empty;
            var playerID = string.IsNullOrEmpty(argsPlayerID) ? context.CallerEntityProfile.Lineage.MasterPlayerAccountId : argsPlayerID;

            var friendID = (string)args["friendID"] ?? string.Empty;

            var serverApi = new PlayFabServerInstanceAPI(FabSettingAPI);

            var setTagRequset = new SetFriendTagsRequest {
                PlayFabId = friendID,
                FriendPlayFabId = playerID,
                Tags = new List<string> {FriendsAcceptTag}
            };
            
            var tagResult = await serverApi.SetFriendTagsAsync(setTagRequset);
            
            setTagRequset.PlayFabId = playerID;
            setTagRequset.FriendPlayFabId = friendID;
            
            tagResult = await serverApi.SetFriendTagsAsync(setTagRequset);
            
            return tagResult;
        }

        [FunctionName("ForceAddFriend")]
        public static async Task<dynamic> ForceAddFriend([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var argsPlayerID = (string)args["ProfileID"] ?? string.Empty;
            var playerID = string.IsNullOrEmpty(argsPlayerID) ? context.CallerEntityProfile.Lineage.MasterPlayerAccountId : argsPlayerID;

            var friendID = (string)args["friendID"] ?? string.Empty;

            var serverApi = new PlayFabServerInstanceAPI(FabSettingAPI);

            var request = new AddFriendRequest {
                PlayFabId = playerID,
                FriendPlayFabId = friendID
            };
            
            var addResult = await serverApi.AddFriendAsync(request);
            
            request.PlayFabId = friendID;
            request.FriendPlayFabId = playerID;
            
            addResult = await serverApi.AddFriendAsync(request);
            
            var setTagRequset = new SetFriendTagsRequest {
                PlayFabId = friendID,
                FriendPlayFabId = playerID,
                Tags = new List<string> {FriendsAcceptTag}
            };
            
            var tagResult = await serverApi.SetFriendTagsAsync(setTagRequset);
            
            setTagRequset.PlayFabId = playerID;
            setTagRequset.FriendPlayFabId = friendID;
            
            tagResult = await serverApi.SetFriendTagsAsync(setTagRequset);
            
            return tagResult;
        }

        #endregion

        // Clans
        #region Clans API Methods

        [FunctionName("GetUserClan")]
        public static async Task<dynamic> GetUserClan([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var argsPlayerID = (string)args["ProfileID"] ?? string.Empty;
            var playerID = string.IsNullOrEmpty(argsPlayerID) ? context.CallerEntityProfile.Lineage.MasterPlayerAccountId : argsPlayerID;
            var entityID = (string)args["EntityID"] ?? string.Empty;

            var authContext = context.GetAuthContext();
            var groupApi = new PlayFabGroupsInstanceAPI(FabSettingAPI, authContext);

            var clanID = string.Empty;
            var clanName = string.Empty;
            var exist = false;
            
            clanID = await GetUserClanID(entityID, authContext);

            if (!string.IsNullOrEmpty(clanID))
            {
                exist = true;
                
                var clanEntity = new PlayFab.GroupsModels.EntityKey {
                    Id =  clanID,
                    Type = "group"
                };
                
                var groupRequest = new GetGroupRequest {
                    Group = clanEntity
                };
                
                var groupResult = await groupApi.GetGroupAsync(groupRequest);
                
                clanName = groupResult.Result.GroupName;
            }
            
            var clanResult = new ExistInClanCallback {
                ExistInClan = exist,
                ClanID = clanID,
                ClanName = clanName
            };

            return JsonConvert.SerializeObject(clanResult);
        }

        [FunctionName("GetClanMemberships")]
        public static async Task<dynamic> GetClanMemberships([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var argsPlayerID = (string)args["ProfileID"] ?? string.Empty;
            var playerID = string.IsNullOrEmpty(argsPlayerID) ? context.CallerEntityProfile.Lineage.MasterPlayerAccountId : argsPlayerID;
            var clanID = (string)args["ClanID"] ?? string.Empty;

            var authContext = context.GetAuthContext();
            var groupApi = new PlayFabGroupsInstanceAPI(FabSettingAPI, authContext);

            var clanEntity = new PlayFab.GroupsModels.EntityKey {
                Id = clanID,
                Type = "group"
            };
            
            var groupRequest = new ListGroupMembersRequest {
                Group = clanEntity
            };
            
            var responseList = new List<ClanUser>();
            
            var adminID = string.Empty;
            var membersResult = await groupApi.ListGroupMembersAsync(groupRequest);
            var memberGroup = membersResult.Result.Members;
            
            foreach(var role in memberGroup) 
            {
                if (role.RoleId == "admins")
                {
                    adminID = role.Members.FirstOrDefault().Lineage["master_player_account"].Id;
                }
                var members = role.Members;
                foreach (var member in members) {
                    var entityID = member.Key.Id;
                    var profileID = member.Lineage["master_player_account"].Id;
                    
                    var newProfile = new ClanUser {
                        ClanAdminID = adminID,
                        ClanId = clanID, 
                        ProfileId = profileID,
                        EntityId = entityID,
                        RoleId = role.RoleId,
                        RoleName = role.RoleName
                    };
                    
                    responseList.Add(newProfile);
                };
            };
            
            var membersCallbackResult = new ClanMembersCallback{
                Profiles = responseList.ToArray()
            };

            return JsonConvert.SerializeObject(membersCallbackResult);
        }

        [FunctionName("CreateClan")]
        public static async Task<dynamic> CreateClan([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var argsPlayerID = (string)args["ProfileID"] ?? string.Empty;
            var playerID = string.IsNullOrEmpty(argsPlayerID) ? context.CallerEntityProfile.Lineage.MasterPlayerAccountId : argsPlayerID;
            var name = (string)args["Name"] ?? string.Empty;
            var description = (string)args["Description"] ?? string.Empty;
            var imageURL = (string)args["ImageURL"] ?? string.Empty;
            var entityID = (string)args["EntityID"] ?? string.Empty;
            var entityType = (string)args["EntityType"] ?? string.Empty;

            var authContext = context.GetAuthContext();
            var groupApi = new PlayFabGroupsInstanceAPI(FabSettingAPI, authContext);
            var dataApi = new PlayFabDataInstanceAPI(FabSettingAPI, authContext);
            var serverApi = new PlayFabServerInstanceAPI(FabSettingAPI);

            var entityKey = new PlayFab.GroupsModels.EntityKey {
                Id = entityID,
                Type = entityType
            };

            var createRequest = new CreateGroupRequest {
                GroupName = name,
                Entity = entityKey
            };

            var createResult = await groupApi.CreateGroupAsync(createRequest);
    
            var groupName = createResult.Result.GroupName;
            var groupID = createResult.Result.Group.Id;
            
            var registerCallback = await RegisterClanInAzure(groupName, groupID);
            
            var clanEntity = new PlayFab.DataModels.EntityKey {
                Id = groupID,
                Type = "group"
            };
            
            var apirequest = new SetObjectsRequest {
                Objects = new List<SetObject>() {
                    new SetObject {
                        ObjectName = "ImageURL",
                        DataObject = imageURL
                    },
                    new SetObject {
                        ObjectName = "Description",
                        DataObject = description
                    },
                    new SetObject {
                        ObjectName = "StatisticProfile",
                        DataObject = playerID
                    },
                    new SetObject {
                        ObjectName = "Tag",
                        DataObject = ClanGroupTag
                    }
                },
                Entity = clanEntity
            };
            
            await dataApi.SetObjectsAsync(apirequest);
            
            var addTagRequest = new AddPlayerTagRequest {
                PlayFabId = playerID,
                TagName = groupID
            };
            
            await serverApi.AddPlayerTagAsync(addTagRequest);
            
            addTagRequest.TagName = groupName;
            
            await serverApi.AddPlayerTagAsync(addTagRequest);
            
            return groupID;
        }

        [FunctionName("RemoveClan")]
        public static async Task<dynamic> RemoveClan([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var argsPlayerID = (string)args["ProfileID"] ?? string.Empty;
            var playerID = string.IsNullOrEmpty(argsPlayerID) ? context.CallerEntityProfile.Lineage.MasterPlayerAccountId : argsPlayerID;
            var clanID = (string)args["ClanID"] ?? string.Empty;
            var clanName = (string)args["ClanName"] ?? string.Empty;

            var authContext = await GetServerAuthContext();

            var groupApi = new PlayFabGroupsInstanceAPI(FabSettingAPI, authContext);
            var dataApi = new PlayFabDataInstanceAPI(FabSettingAPI, authContext);
            var serverApa = new PlayFabServerInstanceAPI(FabSettingAPI);

            await UnRegisterClanFromAzure(clanName);

            var clanEntity = new PlayFab.GroupsModels.EntityKey {
                Id = clanID,
                Type = "group"
            };

            var groupRequest = new DeleteGroupRequest {
                Group = clanEntity
            };
            
            var clanObjectEntity = new PlayFab.DataModels.EntityKey {
                Id = clanID,
                Type = "group"
            };
            
            var objectRequest = new GetObjectsRequest {
                Entity = clanObjectEntity
            };
            
            var statisticProfile = string.Empty;
            
            var objectsResult = await dataApi.GetObjectsAsync(objectRequest);
            if (objectsResult.Result.Objects.ContainsKey("StatisticProfile"))
            {
                statisticProfile = objectsResult.Result.Objects["StatisticProfile"].DataObject.ToString();
            }
            
            if (!string.IsNullOrEmpty(statisticProfile))
            {
                var statisticRequest = new UpdatePlayerStatisticsRequest {
                    PlayFabId = statisticProfile, 
                    Statistics = new List<StatisticUpdate>()
                    {
                        new StatisticUpdate{
                            StatisticName = "CBSClanRating",
                            Value = 0
                        }
                    }
                };
            
                var playerStatResult = await serverApa.UpdatePlayerStatisticsAsync(statisticRequest);
            }
            
            var removeResult = await groupApi.DeleteGroupAsync(groupRequest);
            
            var removeTagRequest = new RemovePlayerTagRequest {
                PlayFabId = playerID,
                TagName = clanID
            };
            
            await serverApa.RemovePlayerTagAsync(removeTagRequest);
            
            removeTagRequest.TagName = clanName;
            
            await serverApa.RemovePlayerTagAsync(removeTagRequest);
            
            return removeResult;
        }

        [FunctionName("GetClanInfo")]
        public static async Task<dynamic> GetClanInfo([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var argsPlayerID = (string)args["ProfileID"] ?? string.Empty;
            var playerID = string.IsNullOrEmpty(argsPlayerID) ? context.CallerEntityProfile.Lineage.MasterPlayerAccountId : argsPlayerID;
            var clanID = (string)args["ClanID"] ?? string.Empty;

            var authContext = await GetServerAuthContext();

            var groupApi = new PlayFabGroupsInstanceAPI(FabSettingAPI, authContext);
            var dataApi = new PlayFabDataInstanceAPI(FabSettingAPI, authContext);

            var clanEntity = new PlayFab.GroupsModels.EntityKey {
                Id = clanID,
                Type = "group"
            };

            var groupRequest = new GetGroupRequest {
                Group = clanEntity
            };
            
            var groupResult = await groupApi.GetGroupAsync(groupRequest);
            
            var objectRequest = new GetObjectsRequest {
                Entity = new PlayFab.DataModels.EntityKey{
                    Id = clanID,
                    Type = "group"
                }
            };
            
            var objectsResult = await dataApi.GetObjectsAsync(objectRequest);
            
            var groupMembersRequest = new ListGroupMembersRequest {
                Group = clanEntity
            };
            var membersResult = await groupApi.ListGroupMembersAsync(groupMembersRequest);
            
            var members = membersResult.Result.Members;
            
            var adminID = string.Empty;
            var membersCount = 0;
            
            foreach(var role in members) {
                if (role.RoleId == "admins")
                {
                    adminID = role.Members.FirstOrDefault().Key.Id;
                }
                membersCount += role.Members.Count;
            };
            
            var clanDataObject = objectsResult.Result.Objects ?? new Dictionary<string, PlayFab.DataModels.ObjectResult>();
            var imageURL = clanDataObject.ContainsKey("ImageURL") ? clanDataObject["ImageURL"].DataObject.ToString() : string.Empty;
            var description = clanDataObject.ContainsKey("Description") ? clanDataObject["Description"].DataObject.ToString() : string.Empty;

            var clanInfoObject = new ClanInfo {
                GroupId = clanID,
                GroupName = groupResult.Result.GroupName,
                AdminID = adminID,
                MembersCount = membersCount,
                MemberRoleId = groupResult.Result.MemberRoleId,
                AdminRoleId = groupResult.Result.AdminRoleId,
                Created = groupResult.Result.Created.ToString(),
                ImageURL = imageURL,
                Description = description
            };

            return JsonConvert.SerializeObject(clanInfoObject);
        }

        [FunctionName("GetClanAppications")]
        public static async Task<dynamic> GetClanAppications([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var argsPlayerID = (string)args["ProfileID"] ?? string.Empty;
            var playerID = string.IsNullOrEmpty(argsPlayerID) ? context.CallerEntityProfile.Lineage.MasterPlayerAccountId : argsPlayerID;
            var clanID = (string)args["ClanID"] ?? string.Empty;

            var authContext = await GetServerAuthContext();

            var groupApi = new PlayFabGroupsInstanceAPI(FabSettingAPI, authContext);

            var clanEntity = new PlayFab.GroupsModels.EntityKey {
                Id = clanID,
                Type = "group"
            };
            
            var request = new ListGroupApplicationsRequest {
                Group = clanEntity
            };
            
            var result = await groupApi.ListGroupApplicationsAsync(request);
            
            var applications = result.Result.Applications;
            var enities = applications.Select(a => a.Entity);
            var profileIds = enities.Select(a => a.Lineage["master_player_account"].Id);
            
            var responseList = new List<ClanRequestUser>();
            
            // get admin Id
            var groupRequest = new ListGroupMembersRequest {
                Group = clanEntity
            };
            
            var adminID = string.Empty;
            var membersResult = await groupApi.ListGroupMembersAsync(groupRequest);
            var members = membersResult.Result.Members;
            
            foreach (var role in members) {
                if (role.RoleId == "admins")
                {
                    adminID = role.Members.FirstOrDefault().Lineage["master_player_account"].Id;
                }
            };
            
            foreach (var apply in applications) {
                var expires = apply.Expires;
                var entityID = apply.Entity.Key.Id;
                var profileID = apply.Entity.Lineage["master_player_account"].Id;
                
                var newProfile = new ClanRequestUser {
                    ClanAdminID = adminID,
                    ClanIdToJoin = clanID, 
                    ProfileId = profileID,
                    EntityId = entityID,
                    Expires = expires
                };
                
                responseList.Add(newProfile);
            };
            
            var applicationResult = new ClanApplicationCallback{
                Profiles = responseList.ToArray()
            };

            return JsonConvert.SerializeObject(applicationResult);
        }

        [FunctionName("AcceptClanInvite")]
        public static async Task<dynamic> AcceptClanInvite([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var argsPlayerID = (string)args["ProfileID"] ?? string.Empty;
            var playerID = string.IsNullOrEmpty(argsPlayerID) ? context.CallerEntityProfile.Lineage.MasterPlayerAccountId : argsPlayerID;
            var clanID = (string)args["ClanID"] ?? string.Empty;
            var entityID = (string)args["EntityID"] ?? string.Empty;
            var authContext = context.GetAuthContext();

            var maxMembersRaw = await GetInternalTitleData(MaxClanMembersKey);
            maxMembersRaw = string.IsNullOrEmpty(maxMembersRaw) ? DefaultClanMembers : maxMembersRaw;
            var maxMembers = int.Parse(maxMembersRaw);

            var groupApi = new PlayFabGroupsInstanceAPI(FabSettingAPI, authContext);

            var clanMembers = await GetClanMemberCount(clanID, authContext);
    
            if (clanMembers >= maxMembers)
            {
                return MaxClanMembersError();
            }

            var clanEntity = new PlayFab.GroupsModels.EntityKey {
                Id = clanID,
                Type = "group"
            };
            
            var playerEntity = new PlayFab.GroupsModels.EntityKey {
                Id = entityID,
                Type = "title_player_account"
            };
            
            var request = new AcceptGroupInvitationRequest {
                Group = clanEntity,
                Entity = playerEntity
            };
            
            var result = await groupApi.AcceptGroupInvitationAsync(request);
            
            return result;
        }

        [FunctionName("AcceptGroupApplication")]
        public static async Task<dynamic> AcceptGroupApplication([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var argsPlayerID = (string)args["ProfileID"] ?? string.Empty;
            var playerID = string.IsNullOrEmpty(argsPlayerID) ? context.CallerEntityProfile.Lineage.MasterPlayerAccountId : argsPlayerID;
            var clanID = (string)args["ClanID"] ?? string.Empty;
            var entityID = (string)args["EntityID"] ?? string.Empty;
            var authContext = await GetServerAuthContext();

            var maxMembersRaw = await GetInternalTitleData(MaxClanMembersKey);
            maxMembersRaw = string.IsNullOrEmpty(maxMembersRaw) ? DefaultClanMembers : maxMembersRaw;
            var maxMembers = int.Parse(maxMembersRaw);

            var groupApi = new PlayFabGroupsInstanceAPI(FabSettingAPI, authContext);

            var clanMembers = await GetClanMemberCount(clanID, authContext);
    
            if (clanMembers >= maxMembers)
            {
                return MaxClanMembersError();
            }

            var clanEntity = new PlayFab.GroupsModels.EntityKey {
                Id = clanID,
                Type = "group"
            };

            var playerEntity = new PlayFab.GroupsModels.EntityKey {
                Id = entityID,
                Type = "title_player_account"
            };
            
            var request = new AcceptGroupApplicationRequest {
                Group = clanEntity,
                Entity = playerEntity
            };
            
            var result = await groupApi.AcceptGroupApplicationAsync(request);
            
            return result;
        }

        private static async Task<int> GetClanMemberCount(string clanID, PlayFabAuthenticationContext authContext)
        {
            var clanEntity = new PlayFab.GroupsModels.EntityKey {
                Id = clanID,
                Type = "group"
            };
            
            var groupRequest = new ListGroupMembersRequest {
                Group = clanEntity
            };
            
            authContext = await GetServerAuthContext();
            var groupApi = new PlayFabGroupsInstanceAPI(FabSettingAPI, authContext);
            
            var membersResult = await groupApi.ListGroupMembersAsync(groupRequest);

            if (membersResult.Error != null)
                return 0;
            
            var members = membersResult.Result.Members;
            
            var membersCount = 0;
            
            foreach (var role in members)
            {
                membersCount += role.Members.Count;
            } 
            
            return membersCount;
        }

        private static async Task<AzureTableRequestResult> RegisterClanInAzure(string clanName, string clanID)
        {        
            var secretKey = await GetInternalTitleData(AzureSecretDataKey);
            var storageKey = await GetInternalTitleData(AzureStorageDataKey);

            var url = AzureUtils.GetUpdateQueryURL(storageKey, secretKey, ClanTableID, clanName, string.Empty);

            var requestClient = new HttpClient();
            var httpRequestMessage = AzureUtils.GetHeader(AzureRequestType.UPDATE_TABLE, url);
            var contentBody = JsonConvert.SerializeObject(new 
            { 
                ClanID = clanID
            });

            httpRequestMessage.Content = new StringContent(contentBody, Encoding.UTF8, "application/json");
            var updateResult = await requestClient.SendAsync(httpRequestMessage);
            var updateRawData = updateResult == null ? string.Empty : await updateResult.Content.ReadAsStringAsync();

            var updateData = JsonConvert.DeserializeObject<dynamic>(string.IsNullOrEmpty(updateRawData) || updateRawData == "Null"  ? "{}" : updateRawData);
            var azureUpdateResult = new AzureTableRequestResult(updateData);

            if (azureUpdateResult.Error != null)
            {
                var errorCode = azureUpdateResult.Error.code;
                if (errorCode == AzureUtils.TABLE_NOT_FOUND_CODE)
                {
                    var createResult = await CreateTable(ClanTableID);
                    if (createResult.Error == null)
                    {
                        var updateRequestMessage2 = AzureUtils.GetHeader(AzureRequestType.UPDATE_TABLE, url);
                        updateRequestMessage2.Content = new StringContent(contentBody, Encoding.UTF8, "application/json");

                        var updateClient2 = new HttpClient();
                        var updateResponse2 = await updateClient2.SendAsync(updateRequestMessage2);
                    }
                    return createResult;
                }
                else
                {
                    return azureUpdateResult;
                }
            }
            else
            {
                return azureUpdateResult;
            }
        }

        private static async Task<AzureTableRequestResult> UnRegisterClanFromAzure(string clanName)
        {        
            var secretKey = await GetInternalTitleData(AzureSecretDataKey);
            var storageKey = await GetInternalTitleData(AzureStorageDataKey);

            var url = AzureUtils.GetDeleteQueryURL(storageKey, secretKey, ClanTableID, clanName, string.Empty);

            var requestClient = new HttpClient();
            var httpRequestMessage = AzureUtils.GetHeader(AzureRequestType.DELETE_TABLE, url);
    
            var updateResult = await requestClient.SendAsync(httpRequestMessage);
            var updateRawData = updateResult == null ? string.Empty : await updateResult.Content.ReadAsStringAsync();

            var updateData = JsonConvert.DeserializeObject<dynamic>(string.IsNullOrEmpty(updateRawData) || updateRawData == "Null"  ? "{}" : updateRawData);
            var azureUpdateResult = new AzureTableRequestResult(updateData);
            return azureUpdateResult;
        }

        #endregion

        // Tournament
        #region Tournament API Methods

        [FunctionName("GetTournamentState")]
        public static async Task<dynamic> GetTournamentState([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var argsPlayerID = (string)args["ProfileID"] ?? string.Empty;
            var playerID = string.IsNullOrEmpty(argsPlayerID) ? context.CallerEntityProfile.Lineage.MasterPlayerAccountId : argsPlayerID;

            var authContext = await GetServerAuthContext();

            var serverApi = new PlayFabServerInstanceAPI(FabSettingAPI);
            var dataApi = new PlayFabDataInstanceAPI(FabSettingAPI, authContext);

            var tournamentData = await GetTournamentData() as TournamentData;

            var allTournament = tournamentData.Tournaments;
            var duration = tournamentData.DateTimestamp;

            var playerTournamentID = await GetPlayerRawTitleData(playerID, PlayerTournamentKey);
            var playerTournamentTableID = await GetPlayerRawTitleData(playerID, PlayerTournamentTableKey);
            var joined = !string.IsNullOrEmpty(playerTournamentTableID);
            var playerTournamentData = await GetTournamentByID(playerTournamentID);

            if (string.IsNullOrEmpty(playerTournamentID) || playerTournamentData == null)
            {
                if (joined)
                {
                    var removeInternalDataRequest = new UpdateUserInternalDataRequest {
                        PlayFabId = playerID,
                        KeysToRemove = new List<string>() {PlayerTournamentTableKey}
                    };
                    await serverApi.UpdateUserInternalDataAsync(removeInternalDataRequest);
                    joined = false;
                    playerTournamentTableID = string.Empty;
                }
                var defaultTournament = GetDefaultTournament(tournamentData) as TournamentObject;
                playerTournamentID = defaultTournament.TounamentID;
            }
            
            var currentTournament = await GetTournamentByID(playerTournamentID) as TournamentObject;
            var tournamentName = currentTournament.TournamentName;

            var allResults = new List<PlayerTournamnetEntry>();
            var isFinished = false;
            long timeLeft = 0;
            
            if (joined)
            {
                allResults = await GetTournamentLeaderbaord(playerTournamentTableID, currentTournament);
                
                var tournamentEntity = new PlayFab.DataModels.EntityKey {
                    Id = playerTournamentTableID,
                    Type = "group"
                };
                
                var objectRequest = new GetObjectsRequest {
                    Entity = tournamentEntity
                };
                
                var objectsResult = await dataApi.GetObjectsAsync(objectRequest);
                var endDateObject = objectsResult.Result.Objects.ContainsKey("FinishTime") ? objectsResult.Result.Objects["FinishTime"].DataObject.ToString() : "0";
                var endDate = long.Parse(endDateObject);
                var time = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
                
                isFinished = time >= endDate;
                timeLeft = endDate - time;
            }
            
            var stateResult = new TournamentStateCallback {
                ProfileID = playerID,
                PlayerTournamentID = playerTournamentID,
                TournamentName = tournamentName,
                Joined = joined,
                Leaderboard = allResults,
                Finished = isFinished,
                TimeLeft = (int)timeLeft
            };

            return JsonConvert.SerializeObject(stateResult);
        }

        [FunctionName("FinishTournament")]
        public static async Task<dynamic> FinishTournament([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var argsPlayerID = (string)args["ProfileID"] ?? string.Empty;
            var ProfileID = string.IsNullOrEmpty(argsPlayerID) ? context.CallerEntityProfile.Lineage.MasterPlayerAccountId : argsPlayerID;
            var PlayerEntityID = (string)args["PlayerEntityID"] ?? string.Empty;

            var authContext = await GetServerAuthContext();

            var serverApi = new PlayFabServerInstanceAPI(FabSettingAPI);
            var dataApi = new PlayFabDataInstanceAPI(FabSettingAPI, authContext);

            var tournamentData = await GetTournamentData() as TournamentData;
            var playerTournamentID = await GetPlayerRawTitleData(ProfileID, PlayerTournamentKey);
            var playerTournamentTableID = await GetPlayerRawTitleData(ProfileID, PlayerTournamentTableKey);

            if (string.IsNullOrEmpty(playerTournamentTableID))
            {
                return NoTournamentExist();
            }

            if (string.IsNullOrEmpty(playerTournamentID))
            {
                var defaultTournament = GetDefaultTournament(tournamentData) as TournamentObject;
                playerTournamentID = defaultTournament.TounamentID;
            }

            var tournamentEntity = new PlayFab.DataModels.EntityKey {
                Id = playerTournamentTableID,
                Type = "group"
            };
            
            // check if tournament is not finished
            var objectRequest = new GetObjectsRequest {
                Entity = tournamentEntity
            };
            
            var objectsResult = await dataApi.GetObjectsAsync(objectRequest);
            var endDateObject = objectsResult.Result.Objects.ContainsKey("FinishTime") ? objectsResult.Result.Objects["FinishTime"].DataObject.ToString() : "0";
            var endDate = long.Parse(endDateObject);
            var time = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();

            if (time < endDate)
            {
                return NotFinishedTournamentError();
            }
            
            var currentTournament = await GetTournamentByID(playerTournamentID) as TournamentObject;
            
            var leaderboard = await GetTournamentLeaderbaord(playerTournamentTableID, currentTournament);
            
            var playerPosition = 0;
            var playerPoints = 0;
            var nextTournamentID = string.Empty;

            foreach (var leader in leaderboard) {
                if (leader.PlayFabId == ProfileID)
                {
                    playerPosition = leader.Position + 1;
                    playerPoints = leader.StatValue;
                }
            };
            
            var positions = currentTournament.Positions;
            var currentPosition = positions[playerPosition - 1];
            
            var prizeObject = currentPosition.Prizes;
            
            await AddPrizes(prizeObject, ProfileID);

            var goNext = currentPosition.NextTournament;
            var goDown = currentPosition.DowngradeTournament;

            var migration = TournamentMigration.NONE;
            if (goDown)
                migration = TournamentMigration.DOWN;
            if (goNext)
                migration = TournamentMigration.UP;
            
            if (goNext == false && goDown == false)
            {
                nextTournamentID = playerTournamentID;
            }
            else if (goNext == true)
            {
                nextTournamentID = currentTournament.NextTournamentID;
            }
            else if (goDown == true)
            {
                nextTournamentID = currentTournament.DowngradeTournamentID;
            }

            var newTurnamentData = await GetTournamentByID(nextTournamentID) as TournamentObject;
            var newTournamentName = newTurnamentData.TournamentName;

            // try remove tournament from search table

            tournamentEntity = new PlayFab.DataModels.EntityKey {
                Id = playerTournamentTableID,
                Type = "group"
            };
            
            objectRequest = new GetObjectsRequest {
                Entity = tournamentEntity
            };
            
            objectsResult = await dataApi.GetObjectsAsync(objectRequest);

            var connectedTableID = objectsResult.Result.Objects["TableID"].DataObject.ToString();
    
            var tableEntity = new PlayFab.DataModels.EntityKey {
                Id = connectedTableID,
                Type = "group"
            };
            
            var removeRequest = new SetObjectsRequest {
                Objects = new List<SetObject>() {
                    new SetObject {
                        ObjectName = playerTournamentTableID,
                        DeleteObject = true
                    }
                },
                Entity = tableEntity
            };
            try
            {
                await dataApi.SetObjectsAsync(removeRequest);
            }
            catch {};

            // clear internal data
    
            var removeInternalDataRequest = new UpdateUserInternalDataRequest {
                PlayFabId = ProfileID,
                KeysToRemove = new List<string>() {PlayerTournamentTableKey}
            };
            
            await serverApi.UpdateUserInternalDataAsync(removeInternalDataRequest);
            
            // update player tournament key
            var playerData = new Dictionary<string, string>();
            playerData[PlayerTournamentKey] = nextTournamentID;
            
            var updateDataRequest = new UpdateUserInternalDataRequest {
                PlayFabId = ProfileID,
                Data = playerData
            };
            
            await serverApi.UpdateUserInternalDataAsync(updateDataRequest);
            
            var finishResult = new TournamentFinishCallback {
                ProfileID = ProfileID,
                Position = playerPosition,
                Prize = prizeObject,
                NewTournamentID = nextTournamentID,
                NewTournamentName = newTournamentName,
                NewMigration = migration,
                PositionValue = playerPoints,
                FinishTournamentID = currentTournament.TounamentID,
                FinishTournamentName = currentTournament.TournamentName
            };

            return JsonConvert.SerializeObject(finishResult);
        }

        [FunctionName("FindAndJoinTournament")]
        public static async Task<dynamic> FindAndJoinTournament([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var argsPlayerID = (string)args["ProfileID"] ?? string.Empty;
            var ProfileID = string.IsNullOrEmpty(argsPlayerID) ? context.CallerEntityProfile.Lineage.MasterPlayerAccountId : argsPlayerID;
            var PlayerEntityID = (string)args["PlayerEntityID"] ?? string.Empty;

            var authContext = await GetServerAuthContext();

            var serverApi = new PlayFabServerInstanceAPI(FabSettingAPI);
            var dataApi = new PlayFabDataInstanceAPI(FabSettingAPI, authContext);
            var groupApi = new PlayFabGroupsInstanceAPI(FabSettingAPI, authContext);

            var playerTournamentID = await GetPlayerRawTitleData(ProfileID, PlayerTournamentKey);
            var playerTournamentTableID = await GetPlayerRawTitleData(ProfileID, PlayerTournamentTableKey);

            if (!string.IsNullOrEmpty(playerTournamentTableID))
            {
                return TournamentAllreadyExist();
            }
            
            if (string.IsNullOrEmpty(playerTournamentID) || await GetTournamentByID(playerTournamentID) == null)
            {
                var tournamentData = await GetTournamentData() as TournamentData;
                var defaultTournament = GetDefaultTournament(tournamentData);
                playerTournamentID = defaultTournament.TounamentID;
            }

            var currentTournament = await GetTournamentByID(playerTournamentID) as TournamentObject;
    
            var positions = currentTournament.Positions;
            var maxMembers = positions == null ? 0 : positions.Count;
            
            var now = DateTime.Now;
            var year = now.Year.ToString();
            var month = now.Month.ToString();
            var touramnentTableID = TournamentTablePrefix + year + month + playerTournamentID;
            
            var tableID = touramnentTableID;

            var groupRequest = new GetGroupRequest {
                GroupName = tableID
            };
            
            GetGroupResponse tableGroupResult = null;
            
            var tableGroupResultCache = await groupApi.GetGroupAsync(groupRequest);
            tableGroupResult = tableGroupResultCache.Result;
            
            if (tableGroupResultCache.Error != null)
            {
                // table not found
                var createGroupRequest = new CreateGroupRequest {
                    GroupName = tableID
                };
                var createResult = await groupApi.CreateGroupAsync(createGroupRequest);
                var tableGroupIDCache = createResult.Result.Group.Id;
                await AsignNewTournamentGroup(tableGroupIDCache, playerTournamentID);
                var tableGroupResultTask = await groupApi.GetGroupAsync(groupRequest);
                tableGroupResult = tableGroupResultTask.Result;
            }

            var tableGroupID = tableGroupResult.Group.Id;
    
            var tableEntity = new PlayFab.DataModels.EntityKey {
                Id = tableGroupID,
                Type = "group"
            };
            
            var tableMembersRequest = new GetObjectsRequest {
                Entity = tableEntity
            };
            
            var tableListResult = await dataApi.GetObjectsAsync(tableMembersRequest);
            
            var groupList = tableListResult.Result.Objects;
            var groupCount = groupList.Keys.Count;
            
            var groupIDToJoin = string.Empty;
            
            if (groupCount == 0)
            {
                // create new tournament group
                groupIDToJoin = await AsignNewTournamentGroup(tableGroupID, playerTournamentID);
            }
            else
            {
                // get random tournament
                var rnd = new Random();
                var keys = groupList.Keys.ToList();
                var randomKey = keys[rnd.Next(keys.Count)];
                groupIDToJoin = randomKey;
            }

            var tournamentEntity = new PlayFab.DataModels.EntityKey {
                Id = groupIDToJoin,
                Type = "group"
            };
            
            // check if tournament is not finished
            var objectRequest = new GetObjectsRequest {
                Entity = tournamentEntity
            };
            
            var objectsResult = await dataApi.GetObjectsAsync(objectRequest);
            var endDateObject = objectsResult.Result.Objects.ContainsKey("FinishTime") ? objectsResult.Result.Objects["FinishTime"].DataObject.ToString() : "0";
            var endDate = long.Parse(endDateObject);
            var time = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();

            if (time >= endDate)
            {
                //if tournament is finished - remove it from table
                var removeRequest = new SetObjectsRequest {
                    Objects = new List<SetObject>() {
                        new SetObject {
                            ObjectName = groupIDToJoin,
                            DeleteObject = true
                        }
                    },
                    Entity = tableEntity
                };
                try
                {
                    await dataApi.SetObjectsAsync(removeRequest);
                }
                catch{}
                
                groupIDToJoin = await AsignNewTournamentGroup(tableGroupID, playerTournamentID);
                
                tournamentEntity = new PlayFab.DataModels.EntityKey {
                    Id = groupIDToJoin,
                    Type = "group"
                };
            }

            var playerEntity = new PlayFab.GroupsModels.EntityKey {
                Id = PlayerEntityID,
                Type = "title_player_account"
            };
            
            // join player to tournamentID
            var addMemberRequest = new AddMembersRequest { 
                Group = new PlayFab.GroupsModels.EntityKey{
                    Id = tournamentEntity.Id,
                    Type = tournamentEntity.Type
                }, 
                Members = new List<PlayFab.GroupsModels.EntityKey>() {playerEntity} 
            };
            
            await groupApi.AddMembersAsync(addMemberRequest);

            var tournamentsMemberCount = await GetTournamentMemberCount(groupIDToJoin);
    
            // if tournament is full - remove it from table
            if (tournamentsMemberCount >= maxMembers)
            {
                var removeRequest = new SetObjectsRequest {
                    Objects = new List<SetObject>() {
                        new SetObject {
                            ObjectName = groupIDToJoin,
                            DeleteObject = true
                        }
                    },
                    Entity = tableEntity
                };
                try
                {
                    await dataApi.SetObjectsAsync(removeRequest);
                }
                catch{}
            }

            // finaly asign tournament id to player
            var playerData = new Dictionary<string, string>();
            playerData[PlayerTournamentTableKey] = groupIDToJoin;
            playerData[PlayerTournamentKey] = playerTournamentID;
            
            var updateDataRequest = new UpdateUserInternalDataRequest {
                PlayFabId = ProfileID,
                Data = playerData
            };
            
            await serverApi.UpdateUserInternalDataAsync(updateDataRequest);
            
            await SetTournamentPoint(ProfileID, groupIDToJoin, 0);
            
            return groupIDToJoin;
        }

        [FunctionName("AddPlayerTournamentPoint")]
        public static async Task<dynamic> AddPlayerTournamentPoint([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var argsPlayerID = (string)args["ProfileID"] ?? string.Empty;
            var ProfileID = string.IsNullOrEmpty(argsPlayerID) ? context.CallerEntityProfile.Lineage.MasterPlayerAccountId : argsPlayerID;
            var Point = (int)args["Point"];

            var authContext = await GetServerAuthContext();
            var serverApi = new PlayFabServerInstanceAPI(FabSettingAPI);

            var playerTournamentTableID = await GetPlayerRawTitleData(ProfileID, PlayerTournamentTableKey);
            if (string.IsNullOrEmpty(playerTournamentTableID))
            {
                return NoTournamentExist();
            }
            
            var getSharedDataRequest = new GetSharedGroupDataRequest {
                SharedGroupId = playerTournamentTableID
            };
            
            var dataResult = await serverApi.GetSharedGroupDataAsync(getSharedDataRequest);
            
            var tournamentData = dataResult.Result.Data;
            
            var playerData = tournamentData.ContainsKey(ProfileID) ? tournamentData[ProfileID] : null;

            if (playerData != null)
            {
                var data = JsonConvert.DeserializeObject<dynamic>(playerData.Value);
            
                var points = (int)data.Point;
                
                var newPoints = points + Point;
            
                await SetTournamentPoint (ProfileID, playerTournamentTableID, newPoints);
            }

            return new OkObjectResult("OK");
        }

        [FunctionName("LeaveTournament")]
        public static async Task<dynamic> LeaveTournament([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var argsPlayerID = (string)args["ProfileID"] ?? string.Empty;
            var ProfileID = string.IsNullOrEmpty(argsPlayerID) ? context.CallerEntityProfile.Lineage.MasterPlayerAccountId : argsPlayerID;
            var PlayerEntityID = (string)args["PlayerEntityID"] ?? string.Empty;

            var authContext = await GetServerAuthContext();

            var serverApi = new PlayFabServerInstanceAPI(FabSettingAPI);
            var groupApi = new PlayFabGroupsInstanceAPI(FabSettingAPI, authContext);

            var playerTournamentID = await GetPlayerRawTitleData(ProfileID, PlayerTournamentKey);
            var playerTournamentTableID = await GetPlayerRawTitleData(ProfileID, PlayerTournamentTableKey);
            if (string.IsNullOrEmpty(playerTournamentTableID))
            {
                return NoTournamentExist();
            }
            
            // remove from leaderboard
            var removeFromSharedGroupRequest = new UpdateSharedGroupDataRequest {
                SharedGroupId = playerTournamentTableID,
                KeysToRemove = new List<string>() {ProfileID}
            };
            
            await serverApi.UpdateSharedGroupDataAsync(removeFromSharedGroupRequest);

            // remove from group
            var playerEntity = new PlayFab.GroupsModels.EntityKey {
                Id = PlayerEntityID,
                Type = "title_player_account"
            };
            
            var tournamentEntity = new PlayFab.GroupsModels.EntityKey {
                Id = playerTournamentTableID,
                Type = "group"
            };
            
            var removeFromGroupRequest = new RemoveMembersRequest {
                Group = tournamentEntity,
                Members = new List<PlayFab.GroupsModels.EntityKey>() {playerEntity}
            };
            
            await groupApi.RemoveMembersAsync(removeFromGroupRequest);
            
            // clear internal data
            
            var removeInternalDataRequest = new UpdateUserInternalDataRequest {
                PlayFabId = ProfileID,
                KeysToRemove = new List<string>() {PlayerTournamentTableKey}
            };
            
            await serverApi.UpdateUserInternalDataAsync(removeInternalDataRequest);
            
            return ProfileID;
        }

        [FunctionName("GetTournament")]
        public static async Task<dynamic> GetTournament([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var argsPlayerID = (string)args["ProfileID"] ?? string.Empty;
            var ProfileID = string.IsNullOrEmpty(argsPlayerID) ? context.CallerEntityProfile.Lineage.MasterPlayerAccountId : argsPlayerID;
            var TournamentID = (string)args["TournamentID"] ?? string.Empty;

            var currentTournament = await GetTournamentByID(TournamentID) as TournamentObject;
            return JsonConvert.SerializeObject(currentTournament);
        }

        [FunctionName("GetAllTournament")]
        public static async Task<dynamic> GetAllTournament([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var tournamentData = await GetTournamentData() as TournamentData;
            return JsonConvert.SerializeObject(tournamentData);
        }

        private static async Task<string> AsignNewTournamentGroup(string tableID, string tournamentID)
        {
            var authContext = await GetServerAuthContext();

            var serverApi = new PlayFabServerInstanceAPI(FabSettingAPI);
            var dataApi = new PlayFabDataInstanceAPI(FabSettingAPI, authContext);
            var groupApi = new PlayFabGroupsInstanceAPI(FabSettingAPI, authContext);

            var tournamentData = await GetTournamentData() as TournamentData;
            var duration = tournamentData.DateTimestamp;
            var currentTournament = await GetTournamentByID(tournamentID) as TournamentObject;
            var tournamentName = currentTournament.TournamentName;
            
            var tableEntity = new PlayFab.GroupsModels.EntityKey {
                Id = tableID,
                Type = "group"
            };
            
            var createGroupRequest = new CreateGroupRequest {
                GroupName = System.Guid.NewGuid().ToString()
            };
            
            var createResult = await groupApi.CreateGroupAsync(createGroupRequest);
            
            var tournamentGroupID = createResult.Result.Group.Id;
            
            // calculate finish time
            var time = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
            var finishTime = time + duration;
            
            var tournamentEntity = new PlayFab.DataModels.EntityKey {
                Id = tournamentGroupID,
                Type = "group"
            };
            
            var apirequest = new SetObjectsRequest {
                Objects = new List<SetObject>() {
                    new SetObject {
                        ObjectName = "TournamentID",
                        DataObject = tournamentID
                    },
                    new SetObject {
                        ObjectName = "TournamentName",
                        DataObject = tournamentName
                    },
                    new SetObject {
                        ObjectName = "FinishTime",
                        DataObject = finishTime
                    },
                    new SetObject {
                        ObjectName = "TableID",
                        DataObject = tableID
                    },
                    new SetObject {
                        ObjectName = "Tag",
                        DataObject = TournamentGroupTag
                    }
                },
                Entity = tournamentEntity
            };
            
            await dataApi.SetObjectsAsync(apirequest);
            
            // insert tournament to table
            
            var insertRequest = new SetObjectsRequest {
                Objects = new List<SetObject>() {
                    new SetObject {
                        ObjectName = tournamentGroupID,
                        DataObject = tournamentGroupID
                    }
                },
                Entity = new PlayFab.DataModels.EntityKey{
                    Id = tableEntity.Id,
                    Type = tableEntity.Type
                }
            };
            
            await dataApi.SetObjectsAsync(insertRequest);
            
            // create group shared group data 
            var sharedRequest = new CreateSharedGroupRequest {
                SharedGroupId = tournamentGroupID
            };
            
            await serverApi.CreateSharedGroupAsync(sharedRequest);
            
            return tournamentGroupID;
        }

        private static async Task<dynamic> SetTournamentPoint(string playerID,string tournamentGroupID,int point)
        {
            var authContext = await GetServerAuthContext();

            var serverApi = new PlayFabServerInstanceAPI(FabSettingAPI);
            var dataApi = new PlayFabDataInstanceAPI(FabSettingAPI, authContext);

            var tournamentEntity = new PlayFab.DataModels.EntityKey {
                Id = tournamentGroupID,
                Type = "group"
            };
            
            // check if tournament is not finished
            var objectRequest = new GetObjectsRequest {
                Entity = tournamentEntity
            };
            
            var objectsResult = await dataApi.GetObjectsAsync(objectRequest);
            var endDateObject = objectsResult.Result.Objects.ContainsKey("FinishTime") ? objectsResult.Result.Objects["FinishTime"].DataObject.ToString() : "0";
            var endDate = long.Parse(endDateObject);
            var time = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
            
            var getProfileRequest = new GetPlayerProfileRequest {
                PlayFabId = playerID,
                ProfileConstraints = new PlayerProfileViewConstraints {
                    ShowDisplayName = true
                }
            };
            
            if (time > endDate)
            {
                return NoActiveTournamentError();
            }
            
            var profileResult = await serverApi.GetPlayerProfileAsync(getProfileRequest);
            var playerName = profileResult.Result.PlayerProfile.DisplayName;
            
            var playerData = new {
                PlayerID = playerID,
                PlayerName = playerName,
                Point = point
            };
            
            var rawData = JsonConvert.SerializeObject(playerData);
            
            var updateData = new Dictionary<string, string>();
            updateData[playerID] = rawData;
            
            var updateRequest = new UpdateSharedGroupDataRequest {
                SharedGroupId = tournamentGroupID,
                Data = updateData
            };
            
            var updateResult = await serverApi.UpdateSharedGroupDataAsync(updateRequest);
            return updateRequest;
        }

        private static async Task<int> GetTournamentMemberCount(string tournamentGroupID)
        {
            var authContext = await GetServerAuthContext();

            var groupApi = new PlayFabGroupsInstanceAPI(FabSettingAPI, authContext);

            var tournamentEntity = new PlayFab.GroupsModels.EntityKey {
                Id = tournamentGroupID,
                Type = "group"
            };
            
            var groupRequest = new ListGroupMembersRequest {
                Group = tournamentEntity
            };
            
            var membersResult = await groupApi.ListGroupMembersAsync(groupRequest);
            
            var members = membersResult.Result.Members;
            
            var membersCount = 0;
            
            foreach (var role in members) {
                if (role.RoleId != "admins")
                    membersCount += role.Members.Count;
            };
            
            return membersCount;
        }

        private static async Task<dynamic> GetTournamentData()
        {
            var rawData = await GetInternalTitleData(TournamentDataKey);
            if (string.IsNullOrEmpty(rawData))
            {
                return TournamentNotConfiguredError();
            }
            else
            {
                var data = JsonConvert.DeserializeObject<TournamentData>(rawData);
                if (data.Tournaments == null || data.Tournaments.Count == 0)
                {
                    return TournamentNotConfiguredError();
                }
                else
                {
                    return data;
                }
            }
        }

        private static dynamic GetDefaultTournament(TournamentData data)
        {
            if (data == null || data.Tournaments == null || data.Tournaments.Count == 0)
            {
                return TournamentNotConfiguredError();
            }
            else
            {
                var allTournament = data.Tournaments;
                var defaultTournament = allTournament.FirstOrDefault(x=>x.Value.IsDefault == true).Value;
                
                if (defaultTournament == null)
                {
                    return TournamentNotConfiguredError();
                }
                else
                {
                    return defaultTournament;
                }
            }
        }

        private static async Task<dynamic> GetTournamentByID(string tournamentID)
        {
            var data = await GetTournamentData() as TournamentData;
    
            if (data == null || data.Tournaments == null || data.Tournaments.Count == 0)
            {
                return TournamentNotConfiguredError();
            }
            else
            {
                var allTournament = data.Tournaments;
                if (allTournament.ContainsKey(tournamentID))
                    return allTournament[tournamentID];
                else
                    return null;
            }
        }

        private static async Task<List<PlayerTournamnetEntry>> GetTournamentLeaderbaord(string tournamentGroupID, TournamentObject tournament)
        {
            var authContext = await GetServerAuthContext();
            var groupApi = new PlayFabGroupsInstanceAPI(FabSettingAPI, authContext);
            var serverApi = new PlayFabServerInstanceAPI(FabSettingAPI);

            var tournamentEntity = new PlayFab.GroupsModels.EntityKey {
                Id = tournamentGroupID,
                Type = "group"
            };
            
            var getMembersRequest = new ListGroupMembersRequest {
                Group = tournamentEntity
            };

            var membersResult = await groupApi.ListGroupMembersAsync(getMembersRequest);
            var members = membersResult.Result.Members[1].Members;

            var profilesIDs = members.Select(a => a.Lineage["master_player_account"].Id);

            var getSharedDataRequest = new GetSharedGroupDataRequest {
                SharedGroupId = tournamentGroupID
            };
            
            var dataResult = await serverApi.GetSharedGroupDataAsync(getSharedDataRequest);
            
            var tournamentData = dataResult.Result.Data;

            var allResults = new List<PlayerTournamnetEntry>();
    
            foreach (var playerID in profilesIDs){
                var playerData = tournamentData.ContainsKey(playerID) ? tournamentData[playerID] : null;
                if (playerData == null)
                {
                    var playerResult = new PlayerTournamnetEntry {
                        PlayFabId = playerID,
                        DisplayName = "Unknown",
                        StatValue = 0
                    };
                    allResults.Add(playerResult);
                }
                else
                {
                    var data = JsonConvert.DeserializeObject<dynamic>(playerData.Value);
                    var playerResult = new PlayerTournamnetEntry {
                        PlayFabId = playerID,
                        DisplayName = data["PlayerName"],
                        StatValue = data["Point"],
                        Position = 0
                    };
                    allResults.Add(playerResult);
                    
                }
            };

            if (allResults.Count > 0)
            {
                allResults = allResults.OrderBy(x=>x.StatValue).ToList();
                
                allResults.Reverse();
                
                foreach (var leader in allResults) {
                    leader.Position = allResults.IndexOf(leader);
                };
            }

            // apply reward
            for (int i = 0; i < allResults.Count; i++)
            {
                allResults[i].Reward = tournament.Positions[i].Prizes;
            }
            
            return allResults;
        }

        #endregion

        // Leaderboad
        #region Leaderboard method API

        [FunctionName("AddClanStatisticPoint")]
        public static async Task<dynamic> AddClanStatisticPoint([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var clanID = (string)args["clanID"] ?? string.Empty;
            var statisticName = (string)args["statisticName"] ?? string.Empty;
            var value = (int)args["value"];

            var authContext = await GetServerAuthContext();
            var dataApi = new PlayFabDataInstanceAPI(FabSettingAPI, authContext);
            var serverApi = new PlayFabServerInstanceAPI(FabSettingAPI);

            var clanEntity = new PlayFab.DataModels.EntityKey {
                Id = clanID,
                Type = "group"
            };
            
            var objectRequest = new GetObjectsRequest {
                Entity = clanEntity
            };
            
            var objectsResult = await dataApi.GetObjectsAsync(objectRequest);
            var statisticProfile = objectsResult.Result.Objects["StatisticProfile"].DataObject.ToString();

            var oldValue = await GetPlayerStatisticValue(statisticProfile, statisticName);
            var newValue = oldValue + value;
            
            var request = new UpdatePlayerStatisticsRequest {
                PlayFabId = statisticProfile, 
                Statistics = new List<StatisticUpdate>()
                {
                    new StatisticUpdate{
                        StatisticName= statisticName,
                        Value= newValue
                    }
                }
            };
            
            var playerStatResult = await serverApi.UpdatePlayerStatisticsAsync(request);
            return playerStatResult;
        }

        [FunctionName("UpdateClanStatisticPoint")]
        public static async Task<dynamic> UpdateClanStatisticPoint([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var clanID = (string)args["clanID"] ?? string.Empty;
            var statisticName = (string)args["statisticName"] ?? string.Empty;
            var value = (int)args["value"];

            var authContext = await GetServerAuthContext();
            var dataApi = new PlayFabDataInstanceAPI(FabSettingAPI, authContext);
            var serverApi = new PlayFabServerInstanceAPI(FabSettingAPI);

            var clanEntity = new PlayFab.DataModels.EntityKey {
                Id = clanID,
                Type = "group"
            };
            
            var objectRequest = new GetObjectsRequest {
                Entity = clanEntity
            };
            
            var objectsResult = await dataApi.GetObjectsAsync(objectRequest);
            var statisticProfile = objectsResult.Result.Objects["StatisticProfile"].DataObject.ToString();
            
            var request = new UpdatePlayerStatisticsRequest {
                PlayFabId = statisticProfile, 
                Statistics = new List<StatisticUpdate>()
                {
                    new StatisticUpdate{
                        StatisticName = statisticName,
                        Value = value
                    }
                }
            };
            
            var playerStatResult = await serverApi.UpdatePlayerStatisticsAsync(request);
            return playerStatResult;
        }

        [FunctionName("UpdateStatisticPoint")]
        public static async Task<dynamic> UpdateStatisticPoint([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var argsPlayerID = (string)args["profileID"] ?? string.Empty;
            var profileID = string.IsNullOrEmpty(argsPlayerID) ? context.CallerEntityProfile.Lineage.MasterPlayerAccountId : argsPlayerID;
            var statisticName = (string)args["statisticName"] ?? string.Empty;
            var value = (int)args["value"];

            var serverApi = new PlayFabServerInstanceAPI(FabSettingAPI);

            var request = new UpdatePlayerStatisticsRequest {
                PlayFabId = profileID, 
                Statistics = new List<StatisticUpdate>()
                { 
                   new StatisticUpdate {
                    StatisticName = statisticName,
                    Value = value
                }}
            };
            
            var playerStatResult = await serverApi.UpdatePlayerStatisticsAsync(request);
            return JsonConvert.SerializeObject(playerStatResult);
        }

        [FunctionName("AddStatisticPoint")]
        public static async Task<dynamic> AddStatisticPoint([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var argsPlayerID = (string)args["profileID"] ?? string.Empty;
            var profileID = string.IsNullOrEmpty(argsPlayerID) ? context.CallerEntityProfile.Lineage.MasterPlayerAccountId : argsPlayerID;
            var statisticName = (string)args["statisticName"] ?? string.Empty;
            var value = (int)args["value"];

            var oldValue = await GetPlayerStatisticValue(profileID, statisticName);
            var newValue = oldValue + value;

            var serverApi = new PlayFabServerInstanceAPI(FabSettingAPI);

            var request = new UpdatePlayerStatisticsRequest {
                PlayFabId = profileID, 
                Statistics = new List<StatisticUpdate>()
                { 
                   new StatisticUpdate {
                    StatisticName = statisticName,
                    Value = newValue
                }}
            };
            
            var playerStatResult = await serverApi.UpdatePlayerStatisticsAsync(request);
            return JsonConvert.SerializeObject(playerStatResult);
        }

        [FunctionName("GetLeaderboardAroundPlayer")]
        public static async Task<dynamic> GetLeaderboardAroundPlayer([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var argsPlayerID = (string)args["profileID"] ?? string.Empty;
            var profileID = string.IsNullOrEmpty(argsPlayerID) ? context.CallerEntityProfile.Lineage.MasterPlayerAccountId : argsPlayerID;
            var statisticName = (string)args["statisticName"] ?? string.Empty;
            var maxCount = (int)args["maxCount"];

            var serverApi = new PlayFabServerInstanceAPI(FabSettingAPI);

            var request = new GetLeaderboardAroundUserRequest {
                PlayFabId = profileID,
                MaxResultsCount = maxCount,
                StatisticName = statisticName,
                ProfileConstraints = new PlayerProfileViewConstraints {
                    ShowAvatarUrl = true,
                    ShowDisplayName = true
                }
            };
            
            var result = await serverApi.GetLeaderboardAroundUserAsync(request);

            var leaderboardList = new List<PlayerLeaderboardEntry>();
    
            var fabLeaderboard = result.Result.Leaderboard;
            
            foreach (var member in fabLeaderboard) {
                var newProfile = new PlayerLeaderboardEntry {
                    PlayFabId = member.PlayFabId,
                    DisplayName = member.DisplayName,
                    StatValue = member.StatValue, 
                    Position = member.Position,
                    AvatarUrl = member.Profile.AvatarUrl
                };
                
                leaderboardList.Add(newProfile);
            };

            var playerRequest = new GetLeaderboardAroundUserRequest {
                MaxResultsCount = 1,
                StatisticName = statisticName,
                PlayFabId = profileID,
                ProfileConstraints = new PlayerProfileViewConstraints {
                    ShowAvatarUrl = true,
                    ShowDisplayName = true
                }
            };
            
            var player = fabLeaderboard.FirstOrDefault(x=>x.Profile.PlayerId == profileID);
            
            var playerProfile = new PlayerLeaderboardEntry {
                PlayFabId = player.PlayFabId,
                DisplayName = player.DisplayName,
                StatValue = player.StatValue, 
                Position = player.Position,
                AvatarUrl = player.Profile.AvatarUrl
            };
            
            var leaderboarResult = new GetProfileCallback {
                ProfileResult = playerProfile,
                Leaderboards = leaderboardList
            };

            return JsonConvert.SerializeObject(leaderboarResult);
        }

        [FunctionName("GetLeaderboard")]
        public static async Task<dynamic> GetLeaderboard([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var argsPlayerID = (string)args["profileID"] ?? string.Empty;
            var profileID = string.IsNullOrEmpty(argsPlayerID) ? context.CallerEntityProfile.Lineage.MasterPlayerAccountId : argsPlayerID;
            var statisticName = (string)args["statisticName"] ?? string.Empty;
            var maxCount = (int)args["maxCount"];

            var serverApi = new PlayFabServerInstanceAPI(FabSettingAPI);

            var request = new GetLeaderboardRequest {
                MaxResultsCount = maxCount,
                StatisticName = statisticName,
                ProfileConstraints = new PlayerProfileViewConstraints {
                    ShowAvatarUrl = true,
                    ShowDisplayName = true
                }
            };
            
            var result = await serverApi.GetLeaderboardAsync(request);

            var leaderboardList = new List<PlayerLeaderboardEntry>();
    
            var fabLeaderboard = result.Result.Leaderboard;
            
            foreach (var member in fabLeaderboard) {
                var newProfile = new PlayerLeaderboardEntry {
                    PlayFabId = member.PlayFabId,
                    DisplayName = member.DisplayName,
                    StatValue = member.StatValue, 
                    Position = member.Position,
                    AvatarUrl = member.Profile.AvatarUrl
                };
                
                leaderboardList.Add(newProfile);
            };

            var playerRequest = new GetLeaderboardAroundUserRequest {
                MaxResultsCount = 1,
                StatisticName = statisticName,
                PlayFabId = profileID,
                ProfileConstraints = new PlayerProfileViewConstraints {
                    ShowAvatarUrl = true,
                    ShowDisplayName = true
                }
            };
            
            var playerResult = await serverApi.GetLeaderboardAroundUserAsync(playerRequest);
            var playerPosition = playerResult.Result.Leaderboard;
            var player = playerPosition[0];
            
            var playerProfile = new PlayerLeaderboardEntry {
                PlayFabId = player.PlayFabId,
                DisplayName = player.DisplayName,
                StatValue = player.StatValue, 
                Position = player.Position,
                AvatarUrl = player.Profile.AvatarUrl
            };
            
            var leaderboarResult = new GetProfileCallback {
                ProfileResult = playerProfile,
                Leaderboards = leaderboardList
            };

            return JsonConvert.SerializeObject(leaderboarResult);
        }

        [FunctionName("GetFriendsLeaderboard")]
        public static async Task<dynamic> GetFriendsLeaderboard([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var argsPlayerID = (string)args["profileID"] ?? string.Empty;
            var profileID = string.IsNullOrEmpty(argsPlayerID) ? context.CallerEntityProfile.Lineage.MasterPlayerAccountId : argsPlayerID;
            var statisticName = (string)args["statisticName"] ?? string.Empty;
            var maxCount = (int)args["maxCount"];

            var serverApi = new PlayFabServerInstanceAPI(FabSettingAPI);

            var request = new GetFriendLeaderboardRequest {
                MaxResultsCount = maxCount,
                StatisticName = statisticName,
                PlayFabId = profileID,
                ProfileConstraints = new PlayerProfileViewConstraints {
                    ShowAvatarUrl = true,
                    ShowDisplayName = true
                }
            };
            
            var result = await serverApi.GetFriendLeaderboardAsync(request);

            var leaderboardList = new List<PlayerLeaderboardEntry>();

            PlayerLeaderboardEntry playerProfile = null;
    
            var fabLeaderboard = result.Result.Leaderboard;

            foreach (var member in fabLeaderboard) {
                var newProfile = new PlayerLeaderboardEntry {
                    PlayFabId = member.PlayFabId,
                    DisplayName = member.DisplayName,
                    StatValue = member.StatValue, 
                    Position = member.Position,
                    AvatarUrl = member.Profile.AvatarUrl
                };
                
                if (member.PlayFabId == profileID)
                {
                    playerProfile = newProfile;
                }
                
                leaderboardList.Add(newProfile);
            };
            
            var leaderboarResult = new GetProfileCallback {
                ProfileResult = playerProfile,
                Leaderboards = leaderboardList
            };

            return JsonConvert.SerializeObject(leaderboarResult);
        }

        [FunctionName("GetClanLeaders")]
        public static async Task<dynamic> GetClanLeaders([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var viewerEntityID = (string)args["viewerEntityID"] ?? string.Empty;
            var statisticName = (string)args["statisticName"] ?? string.Empty;
            var maxCount = (int)args["maxCount"];

            var authContext = await GetServerAuthContext();
            var dataApi = new PlayFabDataInstanceAPI(FabSettingAPI, authContext);
            var serverApi = new PlayFabServerInstanceAPI(FabSettingAPI);

            var connectedClanID = string.Empty;
            var statisticProfile = string.Empty;
            
            connectedClanID = await GetUserClanID(viewerEntityID, authContext);

            if (!string.IsNullOrEmpty(connectedClanID))
            {
                var clanEntity = new PlayFab.DataModels.EntityKey {
                    Id = connectedClanID,
                    Type = "group"
                };
                
                var objectRequest = new GetObjectsRequest {
                    Entity = clanEntity
                };
                
                var objectsResult = await dataApi.GetObjectsAsync(objectRequest);
                statisticProfile = objectsResult.Result.Objects["StatisticProfile"].DataObject.ToString();
            }

            ClanLeaderboardEntry playerProfile = null;
    
            var request = new GetLeaderboardRequest {
                MaxResultsCount = maxCount,
                StatisticName = statisticName,
                ProfileConstraints = new PlayerProfileViewConstraints {
                    ShowTags = true
                }
            };
            
            var result = await serverApi.GetLeaderboardAsync(request);
            
            var leaderboardList = new List<ClanLeaderboardEntry>();
            
            var fabLeaderboard = result.Result.Leaderboard;

            foreach (var member in fabLeaderboard) {
                var clanID = string.Empty;
                var clanName = string.Empty;
                
                var titleID = member.Profile.TitleId;
                var partToReplace = string.Format("title.{0}.", titleID);
                
                var tags = member.Profile.Tags;
                if (tags.Count > 0)
                {
                    clanID = tags[0].TagValue;
                    clanName = tags[1].TagValue;
                }
                
                clanID = clanID.Replace(partToReplace, string.Empty);
                clanName = clanName.Replace(partToReplace, string.Empty);
                
                var newProfile = new ClanLeaderboardEntry {
                    ClanId = clanID,
                    ClanName = clanName,
                    StatValue = member.StatValue, 
                    Position = member.Position,
                    CurrentClanId = connectedClanID
                };
                if (newProfile.StatValue != 0)
                    leaderboardList.Add(newProfile);
            };

            if (!string.IsNullOrEmpty(statisticProfile))
            {
                var playerRequest = new GetLeaderboardAroundUserRequest {
                    MaxResultsCount = 1,
                    StatisticName = statisticName,
                    PlayFabId = statisticProfile,
                    ProfileConstraints = new PlayerProfileViewConstraints {
                        ShowTags = true
                    }
                };
                
                var playerResult = await serverApi.GetLeaderboardAroundUserAsync(playerRequest);
                var playerPosition = playerResult.Result.Leaderboard;
                var player = playerPosition.FirstOrDefault();
                
                var clanID = string.Empty;
                var clanName = string.Empty;
                
                var titleID = player.Profile.TitleId;
                var partToReplace = string.Format("title.{0}.", titleID);
                
                var tags = player.Profile.Tags;
                if (tags.Count > 0)
                {
                    clanID = tags[0].TagValue;
                    clanName = tags[1].TagValue;
                }
                
                clanID = clanID.Replace(partToReplace, string.Empty);
                clanName = clanName.Replace(partToReplace, string.Empty);
                
                playerProfile = new ClanLeaderboardEntry {
                    ClanId = clanID,
                    ClanName = clanName,
                    StatValue = player.StatValue, 
                    Position = player.Position,
                    CurrentClanId = connectedClanID
                };
            }
            
            var leaderboardResult = new GetClanProfileCallback {
                ProfileResult = playerProfile,
                Leaderboards = leaderboardList
            };

            return JsonConvert.SerializeObject(leaderboardResult);
        }

        [FunctionName("ResetPlayerStatistics")]
        public static async Task<dynamic> ResetPlayerStatistics([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var argsPlayerID = (string)args["ProfileID"] ?? string.Empty;
            var profileID = string.IsNullOrEmpty(argsPlayerID) ? context.CallerEntityProfile.Lineage.MasterPlayerAccountId : argsPlayerID;
            
            var authContext = await GetServerAuthContext();
            var adminApi = new PlayFabAdminInstanceAPI(FabSettingAPI, authContext);

            var resetRequest = new PlayFab.AdminModels.ResetUserStatisticsRequest{
                PlayFabId = profileID
            };
            var resetResult = await adminApi.ResetUserStatisticsAsync(resetRequest);
            return JsonConvert.SerializeObject(resetResult.Result);
        }

        #endregion

        #region Daily Rewards Method API

        [FunctionName("GetDailyBonus")]
        public static async Task<dynamic> GetDailyBonus([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var argsPlayerID = (string)args["ProfileID"] ?? string.Empty;
            var ProfileID = string.IsNullOrEmpty(argsPlayerID) ? context.CallerEntityProfile.Lineage.MasterPlayerAccountId : argsPlayerID;
            var ZoneOfset = (long)args["ZoneOfset"];

            var serverApi = new PlayFabServerInstanceAPI(FabSettingAPI);

            var userDailydata = await GetPlayerDailyBonusData(ProfileID);
            var fullDailyInformation = userDailydata.GetFullInformation();

            var picked = fullDailyInformation.HasActivityToday;
            var currentDailyIndex = fullDailyInformation.CurrentIndex;
            var data = await GetDailyBonusData();

            var daysCount = data.DaliyPrizes.Count;
            var endCondition = currentDailyIndex > daysCount && !picked;
            currentDailyIndex = picked ? userDailydata.Index - 1 : userDailydata.Index;
            var resetTrigger = fullDailyInformation.TotalDayPassed - userDailydata.Day >= 2 && userDailydata.Day != 0 || endCondition;

            if (resetTrigger)
            {
                await SavePlayerDailyIndexData<DailyIndexObject>(ProfileID, PlayerDailyBonusDataKey, new DailyIndexObject());
                currentDailyIndex = 0;
            }
            
            var result = new DailyBonusResultData {
                CurrentDailyIndex = currentDailyIndex,
                Picked = picked,
                DailyData = data
            };
            
            return JsonConvert.SerializeObject(result);
        }

        [FunctionName("CollectDailyBonus")]
        public static async Task<dynamic> CollectDailyBonus([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var argsPlayerID = (string)args["ProfileID"] ?? string.Empty;
            var ProfileID = string.IsNullOrEmpty(argsPlayerID) ? context.CallerEntityProfile.Lineage.MasterPlayerAccountId : argsPlayerID;
            var ZoneOfset = (long)args["ZoneOfset"];

            var serverApi = new PlayFabServerInstanceAPI(FabSettingAPI);

            var userDailydata = await GetPlayerDailyBonusData(ProfileID);
            var fullDailyInformation = userDailydata.GetFullInformation();
            
            var picked = fullDailyInformation.HasActivityToday;
            if (picked)
            {
                return DailyBonusCollectedError();
            }
            
            var currentDailyIndex = fullDailyInformation.CurrentIndex;
            var newDailyIndex = fullDailyInformation.NextIndex;
            var newSavedDay = fullDailyInformation.TotalDayPassed;
            
            var data = await GetDailyBonusData();

            // get prize
            var prizes = data.DaliyPrizes;
            var prizeNotFound = currentDailyIndex >= prizes.Count;
            var prizeIndex = currentDailyIndex == 0 ? currentDailyIndex : currentDailyIndex - 1;
            var currentPrize = prizeNotFound == true ? prizes[prizes.Count - 1] : prizes[prizeIndex];

            var newIndexObject = new DailyIndexObject{
                Day = newSavedDay,
                Index = newDailyIndex,
            };

            await SavePlayerDailyIndexData<DailyIndexObject>(ProfileID, PlayerDailyBonusDataKey, newIndexObject);
            
            await AddPrizes(currentPrize, ProfileID);
            
            return JsonConvert.SerializeObject(currentPrize);
        }

        [FunctionName("ResetDailyBonus")]
        public static async Task<dynamic> ResetDailyBonus([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var argsPlayerID = (string)args["ProfileID"] ?? string.Empty;
            var ProfileID = string.IsNullOrEmpty(argsPlayerID) ? context.CallerEntityProfile.Lineage.MasterPlayerAccountId : argsPlayerID;

            var serverApi = new PlayFabServerInstanceAPI(FabSettingAPI);

            await SavePlayerDailyIndexData<DailyIndexObject>(ProfileID, PlayerDailyBonusDataKey, new DailyIndexObject());

            return new OkObjectResult("Ok");
        }

        private static async Task<DailyBonusTable> GetDailyBonusData()
        {
            var rawData = await GetTitleData(DailyBonusDataKey);
            if (string.IsNullOrEmpty(rawData))
            {
                DailyBonusNotConfiguredError();
            }
            else
            {
                var data = JsonConvert.DeserializeObject<DailyBonusTable>(rawData);
                if (data.DaliyPrizes == null || data.DaliyPrizes.Count == 0)
                {
                    DailyBonusNotConfiguredError();
                }
                else
                {
                    return data;
                }
            }
            return null;
        }

        private static async Task<DailyIndexObject> GetPlayerDailyBonusData(string profileID)
        {
            return await GetPlayerDailyIndexData<DailyIndexObject>(profileID, PlayerDailyBonusDataKey);
        }

        #endregion

        #region Roulette API Methods

        [FunctionName("GetRouletteTable")]
        public static async Task<dynamic> GetRouletteTable([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var argsPlayerID = (string)args["ProfileID"] ?? string.Empty;
            var ProfileID = string.IsNullOrEmpty(argsPlayerID) ? context.CallerEntityProfile.Lineage.MasterPlayerAccountId : argsPlayerID;

            var data = await GetRouletteData();
    
            return JsonConvert.SerializeObject(data);
        }

        [FunctionName("SpinRoulette")]
        public static async Task<dynamic> SpinRoulette([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var argsPlayerID = (string)args["ProfileID"] ?? string.Empty;
            var ProfileID = string.IsNullOrEmpty(argsPlayerID) ? context.CallerEntityProfile.Lineage.MasterPlayerAccountId : argsPlayerID;

            var data = await GetRouletteData();

            var positions = data.Positions;
    
            var items = new List<string>();
            
            foreach (var position in positions) {
                var weight = position.Weight;
                var id = position.ID;
                for (int i = 0; i < weight; i++) {
                    items.Add(id);
                }
            };

            var rnd = new Random();
            var itemID = items[rnd.Next(items.Count)];

            var foundItem = positions.FirstOrDefault(element => element.ID == itemID);
    
            var currentPrize = foundItem.Prize;
            
            await AddPrizes(currentPrize, ProfileID);
            
            return JsonConvert.SerializeObject(foundItem);
        }

        private static async Task<RouletteTable> GetRouletteData()
        {
            var rawData = await GetTitleData(RouletteDataKey);
            if (string.IsNullOrEmpty(rawData))
            {
                RoulettNotConfiguredError();
            }
            else
            {
                var data = JsonConvert.DeserializeObject<RouletteTable>(rawData);
                if (data.Positions == null || data.Positions.Count == 0)
                {
                    RoulettNotConfiguredError();
                }
                else
                {
                    return data;
                }
            }
            return null;
        }

        private static async Task<BattlePassData> GetBattlePassData()
        {
            var rawData = await GetInternalTitleData(BattlePassDataKey);
            if (string.IsNullOrEmpty(rawData))
            {
                BattlePassNotConfiguredError();
            }
            else
            {
                var data = JsonConvert.DeserializeObject<BattlePassData>(rawData);
                if (data.Instances == null || data.Instances.Count == 0)
                {
                    BattlePassNotConfiguredError();
                }
                else
                {
                    return data;
                }
            }
            return null;
        }

        #endregion

        #region Matchmaking API Methods

        [FunctionName("GetMatchmakingQueue")]
        public static async Task<dynamic> GetMatchmakingQueue([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var authContext = await GetServerAuthContext();

            var queueName = (string)args["QueueName"] ?? string.Empty;
            var multiplayerApi = new PlayFabMultiplayerInstanceAPI(FabSettingAPI, authContext);

            var request = new GetMatchmakingQueueRequest {
                QueueName = queueName
            };
            
            var data = await multiplayerApi.GetMatchmakingQueueAsync(request);
            return JsonConvert.SerializeObject(data.Result);
        }

        [FunctionName("GetMatchmakingList")]
        public static async Task<dynamic> GetMatchmakingList([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var authContext = await GetServerAuthContext();

            var request = new ListMatchmakingQueuesRequest();
            var multiplayerApi = new PlayFabMultiplayerInstanceAPI(FabSettingAPI, authContext);
	
            var data = await multiplayerApi.ListMatchmakingQueuesAsync(request);
            return JsonConvert.SerializeObject(data.Result);
        }

        [FunctionName("UpdateMatchmakingQueue")]
        public static async Task<dynamic> UpdateMatchmakingQueue([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var authContext = await GetServerAuthContext();

            var RawQueue = (string)args["Queue"] ?? "{}";
            var Queue = JsonConvert.DeserializeObject<MatchmakingQueueConfig>(RawQueue);
            var multiplayerApi = new PlayFabMultiplayerInstanceAPI(FabSettingAPI, authContext);

            var request = new SetMatchmakingQueueRequest {
                MatchmakingQueue = Queue
            };
            
            var data = await multiplayerApi.SetMatchmakingQueueAsync(request);
            return JsonConvert.SerializeObject(data.Result);
        }

        [FunctionName("RemoveMatchmakingQueue")]
        public static async Task<dynamic> RemoveMatchmakingQueue([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var authContext = await GetServerAuthContext();

            var QueueName = (string)args["QueueName"] ?? string.Empty;
            var multiplayerApi = new PlayFabMultiplayerInstanceAPI(FabSettingAPI, authContext);

            var request = new RemoveMatchmakingQueueRequest {
                QueueName = QueueName
            };
            
            var data = await multiplayerApi.RemoveMatchmakingQueueAsync(request);
            return JsonConvert.SerializeObject(data.Result);
        }
             
        #endregion

        #region Daily Tasks Method API

        [FunctionName("GetDailyTasksTable")]
        public static async Task<dynamic> GetDailyTasksTable([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var tasksData = await GetDailyTasksData();	
	        return JsonConvert.SerializeObject(tasksData);
        }

        private static async Task<DailyTasksData> GetDailyTasksData()
        {
            var rawData = await GetTitleData(DailyTasksDataKey);
            if (string.IsNullOrEmpty(rawData))
            {
                DailyTasksNotConfiguredError();
            }
            else
            {
                var data = JsonConvert.DeserializeObject<DailyTasksData>(rawData);
                if (data.DailyTasks == null || data.DailyTasks.Count == 0)
                {
                    DailyTasksNotConfiguredError();
                }
                else
                {
                    return data;
                }
            }
            return null;
        }

        private static async Task<DailyTasksIndexObject> GetPlayerDailyTasksData(string profileID)
        {
            return await GetPlayerDailyIndexData<DailyTasksIndexObject>(profileID, PlayerDailyTasksIndexDataKey);
        }

        private static List<CBSTask> GetRandomTasksFromList(List<CBSTask> taskList, int taskCount, int level = 0)
        {
            var availableTasks = taskList;
            
            var cleanTaskList = new List<CBSTask>();
            
            // sort tasks by level
            foreach(var task in availableTasks) 
            {
                var isLockedByLevel = task.IsLockedByLevel;
                var lockLevel = task.LockLevel;
                
                if (isLockedByLevel)
                {
                    if (level >= lockLevel)
                    {
                        cleanTaskList.Add(task);
                    }
                }
                else
                {
                    cleanTaskList.Add(task);
                }
            };
            
            // get random tasks
            if (taskCount > cleanTaskList.Count)
            {
                taskCount = cleanTaskList.Count;
            }
            
            cleanTaskList.Shuffle();
            
            var randomTasks = new List<CBSTask>();
            
            for (var i = 0;i<taskCount;i++)
            {
                randomTasks.Add(cleanTaskList[i]);
            }
            
            return randomTasks;
        }

        [FunctionName("PickupDailyTaskTaskReward")]
        public static async Task<dynamic> PickupDailyTaskTaskReward([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var argsPlayerID = (string)args["ProfileID"] ?? string.Empty;
            var ProfileID = string.IsNullOrEmpty(argsPlayerID) ? context.CallerEntityProfile.Lineage.MasterPlayerAccountId : argsPlayerID;
            var TaskID = (string)args["TaskID"] ?? string.Empty;

            var tasksData = await GetDailyTasksData();
            var tasksList = tasksData.DailyTasks;
            var autoReward = tasksData.AutomaticReward;

            var serverApi = new PlayFabServerInstanceAPI(FabSettingAPI);
	
            // find achievement by id
            var currentTask = tasksList.FirstOrDefault(a => a.ID == TaskID);
            if (currentTask == null)
                return TaskNotFoundError();
            
            var isLockedByLevel = currentTask.IsLockedByLevel;
            var type = currentTask.Type;
            var id = currentTask.ID;
            var lockLevel = currentTask.LockLevel;

            // get player level 
            var currentExp = await GetPlayerStatisticValue(ProfileID, StaticsticExpKey);
            var levelData = await GetTitleData(LevelTitleKey);
            var levelDetail = ParseLevelDetail(levelData, currentExp);
            var level = levelDetail.CurrentLevel;
            
            // get player achievements data
            var playerData = await GetPlayerTasksState(ProfileID, PlayerDailyTasksDataKey);
            var taskState = currentTask.ApplyPlayerState(playerData, (int)level);

            // check complete
            var isComplete = taskState.IsComplete;
            if (isComplete == false)
            {
                return TaskNotCompletedError();
            }
            
            var available = true;

            // check available
            if (isLockedByLevel)
            {
                available = level >= lockLevel;
            }
            // add reward 
            var grandResult = await GrandTaskRewardToPlayer(currentTask, ProfileID);

            // save state
            taskState.IsComplete = isComplete;
            taskState.Rewarded = true;
            taskState.IsAvailable = available;
            currentTask.UpdateState(taskState);

            await SavePlayerTaskState(currentTask, ProfileID, PlayerDailyTasksDataKey);

            var achievementResult = new AddTaskPointCallbackData {
                Task = currentTask,
                ReceivedReward = grandResult.ReceivedReward
            };
            
            return JsonConvert.SerializeObject(achievementResult);
        }

        [FunctionName("AddDailyPointsToDailyTask")]
        public static async Task<dynamic> AddDailyPointsToDailyTask([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var argsPlayerID = (string)args["ProfileID"] ?? string.Empty;
            var ProfileID = string.IsNullOrEmpty(argsPlayerID) ? context.CallerEntityProfile.Lineage.MasterPlayerAccountId : argsPlayerID;
            var Points = (int)args["Points"];
            var Method = (ModifyMethod)args["Method"];
            var TaskID = (string)args["TaskID"] ?? string.Empty;

            var serverApi = new PlayFabServerInstanceAPI(FabSettingAPI);

            var tasksData = await GetDailyTasksData();
            var tasksList = tasksData.DailyTasks;
            var autoReward = tasksData.AutomaticReward;
            
            // find achievement by id
            var currentTask = tasksList.FirstOrDefault(a => a.ID == TaskID);
            if (currentTask == null)
                return TaskNotFoundError();
            
            // get player level 
            var currentExp = await GetPlayerStatisticValue(ProfileID, StaticsticExpKey);
            var levelData = await GetTitleData(LevelTitleKey);
            var levelDetail = ParseLevelDetail(levelData, currentExp);
            var level = levelDetail.CurrentLevel;

            var addPointResult = await ModifyTaskPoint(currentTask, ProfileID, PlayerDailyTasksDataKey, Points, Method, autoReward, level);
            return JsonConvert.SerializeObject(addPointResult);
        }

        [FunctionName("GetDailyTasksState")]
        public static async Task<dynamic> GetDailyTasksState([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var argsPlayerID = (string)args["ProfileID"] ?? string.Empty;
            var ProfileID = string.IsNullOrEmpty(argsPlayerID) ? context.CallerEntityProfile.Lineage.MasterPlayerAccountId : argsPlayerID;
            var ZoneOfset = (long)args["ZoneOfset"];

            var serverApi = new PlayFabServerInstanceAPI(FabSettingAPI);

            var tasksData = await GetDailyTasksData();
            var taslsList = tasksData.DailyTasks;
            
            // get player level 
            var currentExp = await GetPlayerStatisticValue(ProfileID, StaticsticExpKey);
            var levelData = await GetTitleData(LevelTitleKey);
            var levelDetail = ParseLevelDetail(levelData, currentExp);
            var level = levelDetail.CurrentLevel;

            // get player tasks data
            var playerObject = await GetPlayerDailyTasksData(ProfileID);
            var fullDailyInformation = playerObject.GetFullInformation();
            var lastSavedDay = playerObject.Day;

            // check player tasks state
            var currentTasks = playerObject.Tasks;
            var newDay = lastSavedDay != fullDailyInformation.TotalDayPassed;
            var needToRenerateNewTask = newDay == true || currentTasks == null || currentTasks.Count == 0;

            if (needToRenerateNewTask)
            {
                var taskCount = tasksData.DailyTasksCount;
                currentTasks = GetRandomTasksFromList(taslsList, taskCount, level);
                // save tasks for player
                playerObject.Tasks = currentTasks;
		        playerObject.Day = fullDailyInformation.TotalDayPassed;

                await SavePlayerDailyIndexData<DailyTasksIndexObject>(ProfileID, PlayerDailyTasksIndexDataKey, playerObject);
            }

            // get player tasks data
            var playerTasksData = await GetPlayerTasksState(ProfileID, PlayerDailyTasksDataKey);

            // set tasks data
            foreach (var task in currentTasks) {
                var newTasksState = task.ApplyPlayerState(playerTasksData, (int)level);
                task.UpdateState(newTasksState);
            };

            var resultObject = new PlayerTasksResponeData{
                Tasks = currentTasks
            };

            return JsonConvert.SerializeObject(resultObject);
        }

        [FunctionName("ResetDailyTasksState")]
        public static async Task<dynamic> ResetDailyTasksState([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var argsPlayerID = (string)args["ProfileID"] ?? string.Empty;
            var ProfileID = string.IsNullOrEmpty(argsPlayerID) ? context.CallerEntityProfile.Lineage.MasterPlayerAccountId : argsPlayerID;
            var ZoneOfset = (long)args["ZoneOfset"];

            var serverApi = new PlayFabServerInstanceAPI(FabSettingAPI);

            var tasksData = await GetDailyTasksData();
            var taslsList = tasksData.DailyTasks;
            
            // get player level 
            var currentExp = await GetPlayerStatisticValue(ProfileID, StaticsticExpKey);
            var levelData = await GetTitleData(LevelTitleKey);
            var levelDetail = ParseLevelDetail(levelData, currentExp);
            var level = levelDetail.CurrentLevel;

            // get player tasks data
            var playerObject = await GetPlayerDailyTasksData(ProfileID);
            var fullDailyInformation = playerObject.GetFullInformation();

            // get random tasks
            var taskCount = tasksData.DailyTasksCount;
            var currentTasks = GetRandomTasksFromList(taslsList, taskCount, level);

            // save tasks for player
            playerObject.Tasks = currentTasks;
            playerObject.Day = fullDailyInformation.TotalDayPassed;
            await SavePlayerDailyIndexData<DailyTasksIndexObject>(ProfileID, PlayerDailyTasksIndexDataKey, playerObject);

            // clear player tasks state
            var playerData = new Dictionary<string,string>();
            playerData[PlayerDailyTasksDataKey] = string.Empty;
            
            var updateDataRequest = new UpdateUserInternalDataRequest {
                PlayFabId = ProfileID,
                Data = playerData
            };
            
            await serverApi.UpdateUserInternalDataAsync(updateDataRequest);

            var resultObject = new PlayerTasksResponeData{
                Tasks = currentTasks
            };

            return JsonConvert.SerializeObject(resultObject);
        }

        #endregion

        #region Achievements API Methods

        [FunctionName("ResetAchievement")]
        public static async Task<dynamic> ResetAchievement([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var argsPlayerID = (string)args["ProfileID"] ?? string.Empty;
            var ProfileID = string.IsNullOrEmpty(argsPlayerID) ? context.CallerEntityProfile.Lineage.MasterPlayerAccountId : argsPlayerID;
            var AchievementID = (string)args["AchievementID"] ?? string.Empty;

            var achievementsData = await GetAchievementsData();
            var achievementsList = achievementsData.Achievements;
            var autoReward = achievementsData.AutomaticReward;

            var serverApi = new PlayFabServerInstanceAPI(FabSettingAPI);
	
            // find achievement by id
            var currentAchievement = achievementsList.FirstOrDefault(a => a.ID == AchievementID);
            if (currentAchievement == null)
                return TaskNotFoundError();
            
            var isLockedByLevel = currentAchievement.IsLockedByLevel;
            var type = currentAchievement.Type;
            var id = currentAchievement.ID;
            var lockLevel = currentAchievement.LockLevel;

            // get player level 
            var currentExp = await GetPlayerStatisticValue(ProfileID, StaticsticExpKey);
            var levelData = await GetTitleData(LevelTitleKey);
            var levelDetail = ParseLevelDetail(levelData, currentExp);
            var level = levelDetail.CurrentLevel;
            
            var available = true;
            // check available
            if (isLockedByLevel)
            {
                available = level >= lockLevel;
            }
            
            currentAchievement.UpdateState(new BaseTaskState{
                IsAvailable = available,
                IsComplete = false,
                Rewarded = false,
                CurrentStep = 0
            });

            await SavePlayerTaskState(currentAchievement, ProfileID, PlayerAchievementsDataKey);
                  
            return JsonConvert.SerializeObject(currentAchievement);
        }

        [FunctionName("PickupAchievementReward")]
        public static async Task<dynamic> PickupAchievementReward([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var argsPlayerID = (string)args["ProfileID"] ?? string.Empty;
            var ProfileID = string.IsNullOrEmpty(argsPlayerID) ? context.CallerEntityProfile.Lineage.MasterPlayerAccountId : argsPlayerID;
            var AchievementID = (string)args["AchievementID"] ?? string.Empty;

            var achievementsData = await GetAchievementsData();
            var achievementsList = achievementsData.Achievements;
            var autoReward = achievementsData.AutomaticReward;

            var serverApi = new PlayFabServerInstanceAPI(FabSettingAPI);
	
            // find achievement by id
            var currentAchievement = achievementsList.FirstOrDefault(a => a.ID == AchievementID);
            if (currentAchievement == null)
                return TaskNotFoundError();
            
            var isLockedByLevel = currentAchievement.IsLockedByLevel;
            var type = currentAchievement.Type;
            var id = currentAchievement.ID;
            var lockLevel = currentAchievement.LockLevel;

            // get player level 
            var currentExp = await GetPlayerStatisticValue(ProfileID, StaticsticExpKey);
            var levelData = await GetTitleData(LevelTitleKey);
            var levelDetail = ParseLevelDetail(levelData, currentExp);
            var level = levelDetail.CurrentLevel;
            
            // get player achievements data
            var playerData = await GetPlayerTasksState(ProfileID, PlayerAchievementsDataKey);
            var achievementState = currentAchievement.ApplyPlayerState(playerData, (int)level);

            // check complete
            var isComplete = achievementState.IsComplete;
            if (isComplete == false)
            {
                return TaskNotCompletedError();
            }
            
            var available = true;

            // check available
            if (isLockedByLevel)
            {
                available = level >= lockLevel;
            }
            // add reward 
            var grandResult = await GrandTaskRewardToPlayer(currentAchievement, ProfileID);

            // save state
            achievementState.IsComplete = isComplete;
            achievementState.Rewarded = true;
            achievementState.IsAvailable = available;
            currentAchievement.UpdateState(achievementState);

            await SavePlayerTaskState(currentAchievement, ProfileID, PlayerAchievementsDataKey);

            var achievementResult = new AddTaskPointCallbackData {
                Task = currentAchievement,
                ReceivedReward = grandResult.ReceivedReward
            };
            
            return JsonConvert.SerializeObject(achievementResult);
        }

        [FunctionName("AddAchievementPoints")]
        public static async Task<dynamic> AddAchievementPoints([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var argsPlayerID = (string)args["ProfileID"] ?? string.Empty;
            var ProfileID = string.IsNullOrEmpty(argsPlayerID) ? context.CallerEntityProfile.Lineage.MasterPlayerAccountId : argsPlayerID;
            var Points = (int)args["Points"];
            var Method = (ModifyMethod)args["Method"];
            var AchievementID = (string)args["AchievementID"] ?? string.Empty;

            var serverApi = new PlayFabServerInstanceAPI(FabSettingAPI);

            var achievementsData = await GetAchievementsData();
            var achievementsList = achievementsData.Achievements;
            var autoReward = achievementsData.AutomaticReward;
            
            // find achievement by id
            var currentAchievement = achievementsList.FirstOrDefault(a => a.ID == AchievementID);
            if (currentAchievement == null)
                return TaskNotFoundError();
            
            // get player level 
            var currentExp = await GetPlayerStatisticValue(ProfileID, StaticsticExpKey);
            var levelData = await GetTitleData(LevelTitleKey);
            var levelDetail = ParseLevelDetail(levelData, currentExp);
            var level = levelDetail.CurrentLevel;

            var addPointResult = await ModifyTaskPoint(currentAchievement, ProfileID, PlayerAchievementsDataKey, Points, Method, autoReward, level);
            return JsonConvert.SerializeObject(addPointResult);
        }

        [FunctionName("GetAchievementsTable")]
        public static async Task<dynamic> GetAchievementsTable([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            var args = context.FunctionArgument;

            var argsPlayerID = (string)args["ProfileID"] ?? string.Empty;
            var ProfileID = string.IsNullOrEmpty(argsPlayerID) ? context.CallerEntityProfile.Lineage.MasterPlayerAccountId : argsPlayerID;

            var achievementsData = await GetAchievementsData();
            var achievementsList = achievementsData.Achievements;
            
            // get player level 
            var currentExp = await GetPlayerStatisticValue(ProfileID, StaticsticExpKey);
            var levelData = await GetTitleData(LevelTitleKey);
            var levelDetail = ParseLevelDetail(levelData, currentExp);
            var level = levelDetail.CurrentLevel;
            
            // get player achievements data
            var playerAchievementsData = await GetPlayerTasksState(ProfileID, PlayerAchievementsDataKey);

            // set achievements data
            foreach (var achievement in achievementsList) {
                var newAchievementState = achievement.ApplyPlayerState(playerAchievementsData, (int)level);
                achievement.UpdateState(newAchievementState);
            };

            achievementsData.Achievements = achievementsList;
	
            return JsonConvert.SerializeObject(achievementsData);
        }

        private static async Task<AchievementsData> GetAchievementsData()
        {
            var rawData = await GetTitleData(AchievementsDataKey);
            if (string.IsNullOrEmpty(rawData))
            {
                AchievementsNotConfiguredError();
            }
            else
            {
                var data = JsonConvert.DeserializeObject<AchievementsData>(rawData);
                if (data.Achievements == null || data.Achievements.Count == 0)
                {
                    AchievementsNotConfiguredError();
                }
                else
                {
                    return data;
                }
            }
            return null;
        }

        #endregion

        // tasks
        #region Tasks Methods

        private static async Task<GrandTaskRewardResult> GrandTaskRewardToPlayer(CBSTask task, string profileID)
        {
            var taskState = task.TaskState;
            var rewarded = taskState.Rewarded;
            var justRewarded = false;
            // reward
            if (rewarded == false)
            {
                var prize = task.Reward;
                await AddPrizes(prize, profileID);
                rewarded = true;
                justRewarded = true;
            }
            else
            {
                TaskAlreadyRewardedError();
            }
            var receivedReward = justRewarded == true ? task.Reward : null;
            var rewardResult = new GrandTaskRewardResult{
                Rewarded = true,
                ReceivedReward = receivedReward
            };
            return rewardResult;
        }

        private static async Task<AddTaskPointCallbackData> ModifyTaskPoint(CBSTask task, string profileID, string playerDataKey, int points, ModifyMethod method, bool autoReward = false, int playerLevel = 0)
        {
            var isLockedByLevel = task.IsLockedByLevel;
            var id = task.ID;
            var lockLevel = task.LockLevel;

            var serverApi = new PlayFabServerInstanceAPI(FabSettingAPI);

            // get player achievements data
            var playerObject = await GetPlayerTasksState(profileID, playerDataKey);
            var taskState = task.ApplyPlayerState(playerObject, playerLevel);
            
            // check complete
            var isComplete = task.IsComplete;
            if (isComplete)
            {
                TaskAlreadyCompletedError();
            }

            var rewarded = task.Rewarded;
            var available = true;

            taskState = task.AddPoints(points, method);

            // check available
            if (isLockedByLevel)
            {
                available = playerLevel >= lockLevel;
            }
            
            // reward
            var justRewarded = false;
            if (autoReward && !rewarded)
            {
                var grandResult = await GrandTaskRewardToPlayer(task, profileID);
                taskState.Rewarded = true;
                justRewarded = true;
            }
            var revicedReward = justRewarded ? task.Reward : null;

            // save state
            taskState.IsAvailable = available;

            task.UpdateState(taskState);
            
            var saveResult = await SavePlayerTaskState(task, profileID, playerDataKey);
            
            var taskResult = new AddTaskPointCallbackData {
                Task = task,
                ReceivedReward = revicedReward
            };

            return taskResult;
        }

        private static async Task<Dictionary<string, BaseTaskState>> GetPlayerTasksState(string profileID, string playerDataKey)
        {
            var stateData = await GetPlayerTitleData<Dictionary<string, BaseTaskState>>(profileID, playerDataKey);
            return stateData;
        }

        private static async Task<UpdateUserDataResult> SavePlayerTaskState(CBSTask task, string profileID, string playerDataKey)
        {
            var serverApi = new PlayFabServerInstanceAPI(FabSettingAPI);
            var taskID = task.ID;

            var state = task.TaskState ?? new BaseTaskState();
            var taskPlayersData = await GetPlayerTasksState(profileID, playerDataKey);
            taskPlayersData[taskID] = state;
            var playerRawData = JsonConvert.SerializeObject(taskPlayersData);
            var playerData = new Dictionary<string,string>();
            playerData[playerDataKey] = playerRawData;
            
            var updateDataRequest = new UpdateUserInternalDataRequest {
                PlayFabId = profileID,
                Data = playerData
            };
            
            var updateResult = await serverApi.UpdateUserInternalDataAsync(updateDataRequest);
            return updateResult.Result;
        }

        #endregion

        // internal
        #region Internal Functions    

        private static bool IsPeriodActive(Period period)
        {
            if (period == null)
                return false;
            var startServerTime = DateToTimestamp(period.Start);
            var endServerTime = DateToTimestamp(period.End);
            return ServerTimestamp >= startServerTime && ServerTimestamp <= endServerTime;
        }

        private static long DateToTimestamp(DateTime data)
        {
            return new DateTimeOffset(data).ToUnixTimeMilliseconds();
        }

        private static async Task<string> GetUserClanID(string entityID, PlayFabAuthenticationContext authContext)
        {
            var viewerEntity = new PlayFab.GroupsModels.EntityKey {
                Id = entityID,
                Type = "title_player_account"
            };
            
            var memberRequest = new ListMembershipRequest {
                Entity = viewerEntity
            };

            var groupApi = new PlayFabGroupsInstanceAPI(FabSettingAPI, authContext);
            var entityAPI = new PlayFabDataInstanceAPI(FabSettingAPI, authContext);

            var memberResult = await groupApi.ListMembershipAsync(memberRequest);
            
            var memberGroups = memberResult.Result.Groups;

            if (memberGroups != null && memberGroups.Count > 0)
            {
                var clanId = string.Empty;
                foreach (var groupRole in memberGroups) {
                    var group = groupRole.Group;
                    var currentGroupID = group.Id;

                    var groupEntity = new PlayFab.DataModels.EntityKey {
                        Id = currentGroupID,
                        Type = "group"
                    };
                    
                    var objectRequest = new PlayFab.DataModels.GetObjectsRequest {
                        Entity = groupEntity
                    };
                    
                    var objectsResult = await entityAPI.GetObjectsAsync(objectRequest);
                    
                    var objects = objectsResult.Result.Objects;
                    if (objects != null && objects.ContainsKey("Tag"))
                    {
                        var tag = objects["Tag"].DataObject.ToString();
                        if (tag == ClanGroupTag)
                        {
                            clanId = currentGroupID;
                            break;
                        }
                    }
                }
                return clanId;
            }
            else
            {
                return string.Empty;
            }
        }

        private static async Task<dynamic> GetPlayerStatisticValue(string playerID, string statisticName)
        {
            var serverApi = new PlayFabServerInstanceAPI(FabSettingAPI);

            var request = new GetPlayerStatisticsRequest {
                PlayFabId = playerID,
                StatisticNames = new List<string>() {statisticName}
            };

            var result = await serverApi.GetPlayerStatisticsAsync(request);

            if (result.Result.Statistics.Count > 0)
            {
                var experienceStats = result.Result.Statistics.FirstOrDefault();
                var experienceValue = experienceStats.Value;
                return experienceValue;
            }
            else
            {
                return 0;
            }
        }

        private static async Task<string> GetTitleData(string titleKey)
        {
            var serverApi = new PlayFabServerInstanceAPI(FabSettingAPI);

            var request = new GetTitleDataRequest{
                Keys = new List<string>() {titleKey}
            };

            var resultData = await serverApi.GetTitleDataAsync(request);
            var data = resultData.Result.Data;
            return data.ContainsKey(titleKey) ? data[titleKey] : string.Empty;
        }

        private static async Task<string> GetInternalTitleData(string titleKey)
        {
            var serverApi = new PlayFabServerInstanceAPI(FabSettingAPI);

            var request = new GetTitleDataRequest{
                Keys = new List<string>() {titleKey}
            };

            var resultData = await serverApi.GetTitleInternalDataAsync(request);
            var data = resultData.Result.Data;
            return data.ContainsKey(titleKey) ? data[titleKey] : string.Empty;
        }

        private static dynamic ParseLevelDetail(string levelData, int currentExp)
        {
            int? prevLevelExp = 0;
            int? nextLevelExp = 0;
            int? currentLevel = 0;

            var resultObject = new {
                CurrentLevel = 0,
                PrevLevelExp = 0,
                NextLevelExp = 0,
                CurrentExp = 0,
                NewLevelReached = false,
                NewLevelPrize = new PrizeObject()
            };

            if (!string.IsNullOrEmpty(levelData))
            {
                var levels = JsonConvert.DeserializeObject<LevelTable>(levelData);
                if (levels.Table != null)
                {
                    var objectArray = levels.Table;
                    var experienceArray = objectArray.Select(a => a.Expirience).ToList();
                    experienceArray.Sort();
                    experienceArray.Reverse();
                    prevLevelExp = experienceArray.FirstOrDefault(a => a <= currentExp);
                    experienceArray.Reverse();
                    
                    if (prevLevelExp != null)
                    {
                        currentLevel = experienceArray.IndexOf((int)prevLevelExp) + 1;
                    }
                    else
                    {
                        prevLevelExp = 0;
                    }
                    
                    var nextLevel = currentLevel + 1;
                    
                    if (nextLevel > experienceArray.Count)
                    {
                        nextLevelExp = prevLevelExp;
                    }
                    else
                    {
                        nextLevelExp = experienceArray[(int)nextLevel - 1];
                    }

                    var result = new {
                        CurrentLevel = currentLevel,
                        PrevLevelExp = prevLevelExp,
                        NextLevelExp = nextLevelExp,
                        CurrentExp = currentExp,
                        NewLevelReached = false,
                        NewLevelPrize = new PrizeObject()
                    };

                    return result;
                }
            }
            
            return resultObject;
        }

        private static async Task<LevelInfo> AsignNewLevelResult(dynamic result, string levelData, int newLevel, string playerID)
        {
            var newLevelPrize = new PrizeObject();
            if (!string.IsNullOrEmpty(levelData))
            {
                var levels = JsonConvert.DeserializeObject<LevelTable>(levelData);
                var levelTable = levels.Table;
                if (levelTable != null)
                {
                    var levelDetail = levelTable[newLevel - 1];
                    var prizeObject = levelDetail.Prizes;
                    
                    await AddPrizes(prizeObject, playerID);
                    
                    newLevelPrize = prizeObject;
                }
            }
            return new LevelInfo {
                CurrentLevel = result.CurrentLevel,
                PrevLevelExp = result.PrevLevelExp,
                NextLevelExp = result.NextLevelExp,
                CurrentExp = result.CurrentExp,
                NewLevelReached = true,
                NewLevelPrize = newLevelPrize
            };
        }

        private static async Task AddPrizes(PrizeObject prizeObject, string playerID)
        {
            if (prizeObject != null)
            {
                // grand items
                var items = prizeObject.BundledItems;
                if (items != null && items.Count > 0)
                {
                    var grandResult = await InternalGrandItems(items, playerID);
                }
                // grand lutboxes
                var lutboxes = prizeObject.Lootboxes;
                if (lutboxes != null && lutboxes.Count > 0)
                {
                    var grandResult = await InternalGrandItems(lutboxes, playerID);
                }
                // grand currenices
                var currencies = prizeObject.BundledVirtualCurrencies;
                if (currencies != null && currencies.Keys.Count > 0)
                {
                    var serverApi = new PlayFabServerInstanceAPI(FabSettingAPI);

                    foreach (var currency in currencies)
                    {
                        var code = currency.Key;
                        var val = currency.Value;
                        await serverApi.AddUserVirtualCurrencyAsync(new AddUserVirtualCurrencyRequest { 
                            PlayFabId = playerID, 
                            VirtualCurrency = code, 
                            Amount = (int)val 
                        });
                    }
                }
            }
        }

        private static async Task<string> GetPlayerRawTitleData(string profileID, string titleKey)
        {
            var serverApi = new PlayFabServerInstanceAPI(FabSettingAPI);

            var resultData = await serverApi.GetUserInternalDataAsync(new GetUserDataRequest { 
                PlayFabId = profileID,
                Keys = new List<string>() {titleKey} 
            });
            var data = resultData.Result.Data;
            if (data.ContainsKey(titleKey))
            {
                return data[titleKey].Value;
            }
            else
            {
                return "";
            }
        }

        private static async Task<T> GetPlayerTitleData<T>(string profileID, string titleKey) where T : class, new()
        {
            var rawData = await GetPlayerRawTitleData(profileID, titleKey);
            if (string.IsNullOrEmpty(rawData))
                rawData = "{}";
            try
            {
                var data = JsonConvert.DeserializeObject<T>(rawData);
                return data;
            }
            catch
            {
                return new T();
            }
        }

        private static async Task<dynamic> InternalGrandItems(List<string> ItemIds, string playerID)
        {
            var serverApi = new PlayFabServerInstanceAPI(FabSettingAPI);

            var request = new GrantItemsToUserRequest {
                PlayFabId = playerID,
                ItemIds = ItemIds,
                CatalogVersion = ItemDefaultCatalog
            };
            
            var result = await serverApi.GrantItemsToUserAsync(request);
            var grandResult = result.Result.ItemGrantResults;
            foreach (var element in grandResult)
            {
                if (element == null || element.Result == false)
                {
                    return DefaultError();
                }
            }
            
            return result;
        }

        private static async Task<BattlePassUserStatesData> GetPlayerBattlePassData(string profileID)
        {
            var passRawData = await GetPlayerRawTitleData(profileID, PlayerBattleDataKey);
            try
            {
                var passData = JsonConvert.DeserializeObject<BattlePassUserStatesData>(passRawData);
                passData = passData ?? new BattlePassUserStatesData();
                return passData;
            }
            catch
            {
                return new BattlePassUserStatesData();
            }
        }

        private static async Task<UpdateUserDataResult> SavePlayerBattlePassData(string profileID, BattlePassUserStatesData newData)
        {
            var serverApi = new PlayFabServerInstanceAPI(FabSettingAPI);
            var userRawData = JsonConvert.SerializeObject(newData);

            var playerData = new Dictionary<string, string>();
            playerData[PlayerBattleDataKey] = userRawData;
            
            var updateDataRequest = new UpdateUserInternalDataRequest {
                PlayFabId = profileID,
                Data = playerData
            };
            
            var result = await serverApi.UpdateUserInternalDataAsync(updateDataRequest);
            return result.Result;
        }

        private static async Task<T> GetPlayerDailyIndexData<T>(string profileID, string playerDataKey) where T : DailyIndexObject, new()
        {
            var dailyRawData = await GetPlayerRawTitleData(profileID, playerDataKey);
            try
            {
                var daiyData = JsonConvert.DeserializeObject<T>(dailyRawData);
                daiyData = daiyData ?? new T();
                return daiyData;
            }
            catch
            {
                return new T();
            }
        }

        private static async Task<UpdateUserDataResult> SavePlayerDailyIndexData<T>(string profileID, string playerDataKey, T dailyObject) where T : DailyIndexObject
        {
            var serverApi = new PlayFabServerInstanceAPI(FabSettingAPI);
            var indexRawData = JsonConvert.SerializeObject(dailyObject);

            var playerData = new Dictionary<string, string>();
            playerData[playerDataKey] = indexRawData;
            
            var updateDataRequest = new UpdateUserInternalDataRequest {
                PlayFabId = profileID,
                Data = playerData
            };
            
            var result = await serverApi.UpdateUserInternalDataAsync(updateDataRequest);
            return result.Result;
        }

        #endregion

        private static BadRequestObjectResult DefaultError()
        {
            return new BadRequestObjectResult("The script called a PlayFab API, which returned an error. See the Error logs for details.");
        }

        private static BadRequestObjectResult MaxClanMembersError()
        {
            return new BadRequestObjectResult("Failed. Clan reached max members count.");
        }

        private static BadRequestObjectResult TournamentNotConfiguredError()
        {
            return new BadRequestObjectResult("Failed. Tournament Data not configured.");
        }

        private static BadRequestObjectResult DailyBonusNotConfiguredError()
        {
            return new BadRequestObjectResult("Failed. Daily Bonus Data not configured.");
        }

        private static BadRequestObjectResult DailyBonusCollectedError()
        {
            return new BadRequestObjectResult("Failed. You already collected daily bonus.");
        }

        private static BadRequestObjectResult TournamentAllreadyExist()
        {
            return new BadRequestObjectResult("Failed. You are already exist in the tournament.");
        }

        private static BadRequestObjectResult NoTournamentExist()
        {
            return new BadRequestObjectResult("Failed. You are not participating in any tournament.");
        }

        private static BadRequestObjectResult NoActiveTournamentError()
        {
            return new BadRequestObjectResult("Failed. Tournament is no longer active.");
        }

        private static BadRequestObjectResult NotFinishedTournamentError()
        {
            return new BadRequestObjectResult("Failed. Tournament is not over yet.");
        }

        private static BadRequestObjectResult RoulettNotConfiguredError()
        {
            return new BadRequestObjectResult("Failed. Roulette Data not configured.");
        }

        private static BadRequestObjectResult BattlePassNotConfiguredError()
        {
            return new BadRequestObjectResult("Failed. Battle Pass Data not configured.");
        }

        private static BadRequestObjectResult AchievementsNotConfiguredError()
        {
            return new BadRequestObjectResult("Failed. Achievements Data not configured.");
        }

        private static BadRequestObjectResult TaskNotFoundError()
        {
            return new BadRequestObjectResult("Failed. Achievement with this id was not found.");
        }

        private static BadRequestObjectResult TaskAlreadyCompletedError()
        {
            return new BadRequestObjectResult("Failed. The task has already been completed");
        }

        private static BadRequestObjectResult TaskAlreadyRewardedError()
        {
            return new BadRequestObjectResult("Failed. The achievement has already been rewarded");
        }

        private static BadRequestObjectResult TaskNotCompletedError()
        {
            return new BadRequestObjectResult("Failed. The achievement has been not completed");
        }

        private static BadRequestObjectResult DailyTasksNotConfiguredError()
        {
            return new BadRequestObjectResult("Failed. Daily Tasks Data not configured.");
        }

        private static BadRequestObjectResult BattlePassNotFound()
        {
            return new BadRequestObjectResult("Failed. Batlle Pass Not Found.");
        }

        private static BadRequestObjectResult RewardNotAvailable()
        {
            return new BadRequestObjectResult("Failed. Reward Not Available.");
        }

        private static BadRequestObjectResult RewardNotFound()
        {
            return new BadRequestObjectResult("Failed. Reward Not Found.");
        }

        private static BadRequestObjectResult RewardAlreadyReceived()
        {
            return new BadRequestObjectResult("Failed. Award Already Received.");
        }

        private static BadRequestObjectResult PremiumAccessAlreadyGranted()
        {
            return new BadRequestObjectResult("Failed. Premium Access Already Granted.");
        }
    }
}
