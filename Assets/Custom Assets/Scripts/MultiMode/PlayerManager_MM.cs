using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PhantomBetrayal
{
    public class PlayerManager_MM : MonoBehaviour
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
            GenCharacterEnabled,
            GhostDetectEnabled,
        }

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Fields
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Fields

        //-------------------------------------------------- static fields
        public static PlayerManager_MM instance;

        //-------------------------------------------------- serialize fields
        [SerializeField] public CharactersData chsData = new CharactersData();
        [SerializeField] public List<GameObject> cha_Pfs = new List<GameObject>();
        [SerializeField] public GameObject ghost_Pf;
        [SerializeField] public List<GameObject> ghost_Pfs = new List<GameObject>();

        //-------------------------------------------------- public fields
        [ReadOnly] public List<GameState_En> gameStates = new List<GameState_En>();

        [ReadOnly] public PlayersData psData = new PlayersData();

        [ReadOnly] public Player_M localPlayer_Cp;

        [ReadOnly] public Character_M localCha_Cp;

        [SerializeField]
        //[ReadOnly]
        public List<Character_M> cha_Cps = new List<Character_M>();

        [ReadOnly] public Ghost_M ghost_Cp = new Ghost_M();

        [ReadOnly] public PlayerType localPlType = new PlayerType();

        [ReadOnly] public int localPlId;

        [ReadOnly] public bool isGhostDetected;

        //-------------------------------------------------- private fields
        Controller_Gp controller_Cp;

        MapManager mapManager_Cp;

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

        public List<Player_M> player_Cps
        {
            get { return psData.player_Cps; }
        }

        public bool isServer { get { return localPlayer_Cp.pData.isServer; } }


        public int aliveChaCount
        {
            get
            {
                int result = 0;

                for (int i = 0; i < cha_Cps.Count; i++)
                {
                    if (cha_Cps[i].isAlive)
                    {
                        result++;
                    }
                }

                return result;
            }
        }

        public int chaCount { get { return cha_Cps.Count; } }

        //-------------------------------------------------- private properties

        #endregion

        //////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Methods
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////

        //-------------------------------------------------- Awake
        private void Awake()
        {
            instance = this;
        }

        //-------------------------------------------------- Start is called before the first frame update
        void Start()
        {

        }

        //-------------------------------------------------- Update is called once per frame
        void Update()
        {
            if (ExistGameStates(GameState_En.GhostDetectEnabled))
            {
                Handle_GhostDetect();
            }
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
            AddMainGameState();

            chsData.Init();
            
            SetComponents();

            Hash128 hash3_tp = HashHandler.RegRandHash();
            GenCharacters(hash3_tp);
            yield return new WaitUntil(() => !HashHandler.ContainsHash(hash3_tp));

            SetPlayerComponents();

            InitPlayersVariable();

            Hash128 hash_tp = HashHandler.RegRandHash();
            InitPlayers(hash_tp);
            yield return new WaitUntil(() => !HashHandler.ContainsHash(hash_tp));

            Hash128 hash2_tp = HashHandler.RegRandHash();
            InitCharacters(hash2_tp);
            yield return new WaitUntil(() => !HashHandler.ContainsHash(hash2_tp));

            Hash128 hash4_tp = HashHandler.RegRandHash();
            InitGhost(hash4_tp);
            yield return new WaitUntil(() => !HashHandler.ContainsHash(hash4_tp));

            mainGameState = GameState_En.Inited;
        }

        //--------------------------------------------------
        void SetComponents()
        {
            controller_Cp = Controller_Gp.instance;
            mapManager_Cp = controller_Cp.mapManager_Cp;
        }

        //--------------------------------------------------
        void SetPlayerComponents()
        {
            psData.player_Cps = FindObjectsOfType<Player_M>().ToList();

            ghost_Cp = FindObjectOfType<Ghost_M>();
        }

        //--------------------------------------------------
        void InitPlayersVariable()
        {
            foreach (Player_M player_Cp_tp in player_Cps)
            {
                if (player_Cp_tp.isLocalPlayer)
                {
                    localPlayer_Cp = player_Cp_tp;
                    break;
                }
            }

            localPlId = localPlayer_Cp.pData.id;
            localPlType = localPlayer_Cp.pData.type;
        }

        //--------------------------------------------------
        void InitPlayers(Hash128 hash_pr)
        {
            StartCoroutine(Corou_InitPlayers(hash_pr));
        }

        IEnumerator Corou_InitPlayers(Hash128 hash_pr)
        {
            foreach (Player_M player_Cp_tp in player_Cps)
            {
                player_Cp_tp.Init();
                yield return new WaitUntil(() => player_Cp_tp);
            }

            HashHandler.RemoveHash(hash_pr);
        }

        //--------------------------------------------------
        void InitCharacters(Hash128 hash_pr)
        {
            StartCoroutine(Corou_InitCharacters(hash_pr));
        }

        IEnumerator Corou_InitCharacters(Hash128 hash_pr)
        {
            // init characters
            foreach (Character_M cha_Cp_tp in cha_Cps)
            {
                cha_Cp_tp.Init();
                yield return new WaitUntil(() => cha_Cp_tp.mainGameState == Character_M.GameState_En.Inited);
            }

            HashHandler.RemoveHash(hash_pr);

            // set local character
            foreach (Character_M cha_Cp_tp in cha_Cps)
            {
                if (cha_Cp_tp.isLocalPlayer)
                {
                    localCha_Cp = cha_Cp_tp;
                    break;
                }
            }
        }

        //--------------------------------------------------
        void InitGhost(Hash128 hash_pr)
        {
            StartCoroutine(Corou_InitGhost(hash_pr));
        }

        IEnumerator Corou_InitGhost(Hash128 hash_pr)
        {
            ghost_Cp.Init();
            yield return new WaitUntil(() => ghost_Cp.mainGameState == Ghost_M.GameState_En.Inited);

            HashHandler.RemoveHash(hash_pr);
        }

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Play
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Play

        //--------------------------------------------------
        public void ReadyToPlay()
        {
            foreach (Character_M chat_Cp_tp in cha_Cps)
            {
                chat_Cp_tp.ReadyToPlay();
            }
        }

        //--------------------------------------------------
        public void Play()
        {
            mainGameState = GameState_En.Playing;

            foreach (Player_M player_Cp_tp in player_Cps)
            {
                player_Cp_tp.Play();
            }

            foreach (Character_M chat_Cp_tp in cha_Cps)
            {
                chat_Cp_tp.Play();
            }

            ghost_Cp.Play();

            AddGameStates(GameState_En.GhostDetectEnabled);
        }

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Handle fight
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Handle fight

        //--------------------------------------------------
        public void HandleFight(Ghost_M atker_Cp_pr, Character_M victim_Cp_pr)
        {
            victim_Cp_pr.Handle_TakeAtk(atker_Cp_pr);
        }

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Handle character
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Handle characters

        //--------------------------------------------------
        void GenCharacters(Hash128 hash_pr)
        {
            // proceed exception
            for (int i = 0; i < cha_Cps.Count; i++)
            {
                chsData.data.Add(cha_Cps[i].chaData);
            }

            //
            while (cha_Cps.Count < chsData.chsMaxCount)
            {
                int randChaId = -1;
                do
                {
                    randChaId = Random.Range(0, chsData.chsMaxCount);
                }
                while (chsData.ContainsId(randChaId));

                Transform spawnPoint_Tf = chsData.spawnPoint_Tfs[randChaId];

                Character_M cha_Cp_tp = Instantiate(chsData.cha_Pfs[randChaId],
                    spawnPoint_Tf.position, spawnPoint_Tf.rotation).GetComponent<Character_M>();
                cha_Cps.Add(cha_Cp_tp);
                chsData.data.Add(cha_Cp_tp.chaData);
            }

            HashHandler.RemoveHash(hash_pr);
        }

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Handle ghost detection
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Handle ghost detection

        //--------------------------------------------------
        public void Handle_GhostDetect()
        {
            bool isGhostDetected_tp = false;

            foreach (Character_M cha_Cp_tp in cha_Cps)
            {
                if (!cha_Cp_tp.isTraitor)
                {
                    RaycastHit ray;
                    if (Physics.Raycast(cha_Cp_tp.transform.position, ghost_Cp.transform.position
                        - cha_Cp_tp.transform.position, out ray))
                    {
                        if (ray.transform.root.CompareTag("Ghost"))
                        {
                            isGhostDetected_tp = true;
                            break;
                        }
                    }
                }
            }

            isGhostDetected = isGhostDetected_tp;

            if (localPlType == PlayerType.Cha)
            {
                mapManager_Cp.OnGhostDetectedByCha(isGhostDetected);
            }
        }

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Functionalities
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Functionalities

        //--------------------------------------------------
        public static PlayerType GetPlayerType(GameObject tar_GO_pr)
        {
            GameObject root_GO_tp = tar_GO_pr.transform.root.gameObject;

            Player_M player_Cp_tp = root_GO_tp.GetComponent<Player_M>();
            if (player_Cp_tp == null)
            {
                return PlayerType.Null;
            }

            return player_Cp_tp.pType;
        }

        //--------------------------------------------------
        public Character_M GetChaFromId(int id)
        {
            Character_M cha_Cp = null;

            foreach (Character_M cha_Cp_tp in cha_Cps)
            {
                if (cha_Cp_tp.chaData.id == id)
                {
                    cha_Cp = cha_Cp_tp;
                    break;
                }
            }

            return cha_Cp;
        }

        #endregion

        //--------------------------------------------------
        public void OnTraitorAppear()
        {

        }

        //--------------------------------------------------
        public void SetChatMsg(string msg)
        {
            if (localPlType == PlayerType.Cha)
            {
                localCha_Cp.SetChatMsg(msg);
            }
            else if (localPlType == PlayerType.Ghost)
            {
                //pManager_Cp.ghost_Cp.Set
            }
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Finish
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Finish

        //--------------------------------------------------
        public void OnFinish()
        {

        }

        #endregion
    }
}