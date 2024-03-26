using Photon.Pun;
using Photon.Pun.Demo.Cockpit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Realtime;

namespace PhantomBetrayal
{
    public class OnlineHandler : MonoBehaviourPun
    {

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Types
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Types

        public enum GameState_En
        {
            Nothing, Inited, Playing,
            ClientConnected, AllClientConnected,
        }

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Fields
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Fields

        //-------------------------------------------------- serialize fields

        //-------------------------------------------------- public fields
        [SerializeField][ReadOnly] List<GameState_En> gameStates = new List<GameState_En>();

        //-------------------------------------------------- private fields
        Controller_Main_MM controller_Cp;
        PlayerManager_MM pManager_Cp;
        Data_Main_MM data_Cp;

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Properties
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Properties

        //-------------------------------------------------- public properties
        public GameState_En mainGameState
        {
            get { return gameStates[0]; }
            set { gameStates[0] = value; }
        }

        public bool isServer { get { return PhotonNetwork.IsMasterClient && PhotonNetwork.IsConnectedAndReady; } }
        public bool isOnline { get { return PhotonNetwork.IsConnectedAndReady; } }
        public bool isMultiMode
        {
            get
            {
                return PhotonNetwork.IsConnectedAndReady &&
                    PhotonNetwork.PlayerList.Length >= 1;
            }
        }

        //-------------------------------------------------- private properties
        PrepareData prepareData { get { return controller_Cp.data_Cp.prepareData; } }
        CharactersData chaData { get { return controller_Cp.pManager_Cp_temp.chsData; } }
        Player[] photonPlayers { get { return PhotonNetwork.PlayerList; } }
        List<GameObject> cha_Pfs { get { return pManager_Cp.cha_Pfs; } }
        List<GameObject> ghost_Pfs { get { return pManager_Cp.ghost_Pfs; } }

        #endregion

        //////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Methods
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////

        //-------------------------------------------------- Start is called before the first frame update
        void Start()
        {

        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Manage gameStates
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region ManageGameStates

        //--------------------------------------------------
        public void AddMainGameState(GameState_En value = GameState_En.Nothing)
        {
            if (gameStates.Count == 0)
            {
                gameStates.Add(value);
            }
        }

        //--------------------------------------------------
        public void AddGameStates(params GameState_En[] values)
        {
            foreach (GameState_En value_tp in values)
            {
                gameStates.Add(value_tp);
            }
        }

        //--------------------------------------------------
        public bool ExistGameStates(params GameState_En[] values)
        {
            bool result = true;
            foreach (GameState_En value in values)
            {
                if (!gameStates.Contains(value))
                {
                    result = false;
                    break;
                }
            }

            return result;
        }

        //--------------------------------------------------
        public bool ExistAnyGameStates(params GameState_En[] values)
        {
            bool result = false;
            foreach (GameState_En value in values)
            {
                if (gameStates.Contains(value))
                {
                    result = true;
                    break;
                }
            }

            return result;
        }

        //--------------------------------------------------
        public int GetExistGameStatesCount(GameState_En value)
        {
            int result = 0;

            for (int i = 0; i < gameStates.Count; i++)
            {
                if (gameStates[i] == value)
                {
                    result++;
                }
            }

            return result;
        }

        //--------------------------------------------------
        public void RemoveGameStates(params GameState_En[] values)
        {
            foreach (GameState_En value in values)
            {
                gameStates.RemoveAll(gameState_tp => gameState_tp == value);
            }
        }

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Initialize
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Initialize

        //--------------------------------------------------
        public void Init()
        {
            StartCoroutine(Corou_Init());
        }

        IEnumerator Corou_Init()
        {
            AddMainGameState(GameState_En.Nothing);

            if (!isMultiMode)
            {
                Escape();
                yield break;
            }

            SetComponents();

            Hash128 hash_tp = HashHandler.RegRandHash();
            CheckNetworkIsReady(hash_tp);
            yield return new WaitUntil(() => !HashHandler.ContainsHash(hash_tp));

            if (!data_Cp.isDataCorr)
            {
                data_Cp.ModifyPrepareData();
            }

            Hash128 hash2_tp = HashHandler.RegRandHash();
            InitPlayers(hash2_tp);
            yield return new WaitUntil(() => !HashHandler.ContainsHash(hash2_tp));

            mainGameState = GameState_En.Inited;
        }

        //--------------------------------------------------
        void SetComponents()
        {
            controller_Cp = Controller_Main_MM.instance;
            pManager_Cp = controller_Cp.pManager_Cp;
            data_Cp = controller_Cp.data_Cp;
        }

        //--------------------------------------------------
        void CheckNetworkIsReady(Hash128 hash_tp)
        {
            StartCoroutine (Corou_CheckNetworkIsReady(hash_tp));
        }

        IEnumerator Corou_CheckNetworkIsReady(Hash128 hash_tp)
        {
            float curTime = Time.time;
            float waitDur = 10f;
            
            photonView.RPC("Master_TriggerReadyMsg", RpcTarget.MasterClient);
            yield return new WaitUntil(() => ExistGameStates(GameState_En.AllClientConnected) ||
                (Time.time - curTime > waitDur));

            if (!ExistGameStates(GameState_En.AllClientConnected))
            {
                Escape();
                yield break;
            }
            RemoveGameStates(GameState_En.AllClientConnected);

            HashHandler.RemoveHash(hash_tp);
        }

        [PunRPC]
        void Master_TriggerReadyMsg()
        {
            AddGameStates(GameState_En.ClientConnected);
            if (GetExistGameStatesCount(GameState_En.ClientConnected) == PhotonNetwork.PlayerList.Length)
            {
                RemoveGameStates(GameState_En.ClientConnected);
                photonView.RPC("Rpc_ToNextStep", RpcTarget.All);
            }
        }

        [PunRPC]
        void Rpc_ToNextStep()
        {
            AddGameStates(GameState_En.AllClientConnected);
        }

        //--------------------------------------------------
        void InitPlayers(Hash128 hash2_tp)
        {
            StartCoroutine(Corou_InitPlayers(hash2_tp));
        }

        IEnumerator Corou_InitPlayers(Hash128 hash2_tp)
        {
            if (isServer)
            {
                InstPlayers();
                InstComPlayers();
            }

            // wait all players are instantiated
            Hash128 hash_tp = HashHandler.RegRandHash();
            CheckAllPlayersReady(hash_tp);
            yield return new WaitUntil(() => !HashHandler.ContainsHash(hash_tp));

            //
            HashHandler.RemoveHash(hash2_tp);
        }

        //--------------------------------------------------
        void InstPlayers()
        {
            for (int i = 0; i < photonPlayers.Length; i++)
            {
                // get prepareValue
                PrepareValue prepareValue_tp = new PrepareValue();
                prepareValue_tp.uId = -1;

                int actorNumber = photonPlayers[i].ActorNumber;
                for (int j = 0; j < prepareData.values.Count; j++)
                {
                    if (prepareData.values[j].uId == actorNumber)
                    {
                        prepareValue_tp = prepareData.values[j];
                        break;
                    }
                }
                if (prepareValue_tp.uId == -1)
                {
                    Debug.LogError("OnlineHandler, InstPlayers, uId is -1");
                    continue;
                }

                // get player prefab
                GameObject player_Pf_tp = null;
                if (prepareValue_tp.isCha)
                {
                    for (int j = 0; j < cha_Pfs.Count; j++)
                    {
                        if (cha_Pfs[j].GetComponent<Character_MM>().chaData.id == prepareValue_tp.pId)
                        {
                            player_Pf_tp = cha_Pfs[j];
                            break;
                        }
                    }
                }
                else
                {
                    for (int j = 0; j < ghost_Pfs.Count; j++)
                    {
                        if (ghost_Pfs[j].GetComponent<Ghost_M>().id == prepareValue_tp.pId)
                        {
                            player_Pf_tp = ghost_Pfs[j];
                            break;
                        }
                    }
                }
                if (player_Pf_tp == null)
                {
                    Debug.LogError("OnlineHandler, player_Pf_tp is null");
                    continue;
                }

                // inst player
                GameObject player_GO_tp = PhotonNetwork.Instantiate(player_Pf_tp.name, player_Pf_tp.transform.position,
                    player_Pf_tp.transform.rotation);
            }
        }

        //--------------------------------------------------
        void InstComPlayers()
        {

        }

        //--------------------------------------------------
        void CheckAllPlayersReady(Hash128 hash_tp)
        {

        }

        #endregion

        //--------------------------------------------------
        void Escape()
        {

        }

    }

}