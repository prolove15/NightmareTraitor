using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PhantomBetrayal;

namespace PhantomBetrayal
{
    public class Controller_Main_MM : MonoBehaviour
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
        }

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Fields
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Fields

        //-------------------------------------------------- static fields
        public static Controller_Main_MM instance;

        //-------------------------------------------------- serialize fields
        [SerializeField] public OnlineHandler onlineHandler_Cp;
        [SerializeField] public CameraHandler camHandler_Cp;
        [SerializeField] public Data_Main_MM data_Cp;
        [SerializeField] public PlayerManager_MM pManager_Cp;
        [ReadOnly] public UIManager_Gp uiManager_Cp;
        [ReadOnly] public DataManager dataManager_Cp;
        [ReadOnly] public PlayerManager pManager_Cp_temp;
        [ReadOnly] public EnvManager envManager_Cp;
        [ReadOnly] public MapManager mapManager_Cp;
        [ReadOnly] public TraitorHandler traitorHandler_Cp;
        [ReadOnly] public FinalArea finalArea_Cp;
        [ReadOnly] public TimerHandler timer_Cp;
        [ReadOnly] public InputActionPanel inputActionPanel_Cp;
        [ReadOnly] public Finish finish_Cp;

        //-------------------------------------------------- public fields
        [ReadOnly]
        public List<GameState_En> gameStates = new List<GameState_En>();

        //-------------------------------------------------- private fields

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

        public Player_M localPlayer_Cp { get { return pManager_Cp_temp.localPlayer_Cp; } }

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
            Init();
        }

        //-------------------------------------------------- Update is called once per frame
        void Update()
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
            AddMainGameState();

            SetComponents();

            Hash128 hash_tp = HashHandler.RegRandHash();
            InitVariables(hash_tp);
            yield return new WaitUntil(() => !HashHandler.ContainsHash(hash_tp));

            mainGameState = GameState_En.Inited;

            //
            ReadyToPlay();
        }

        //--------------------------------------------------
        void SetComponents()
        {
            uiManager_Cp = UIManager_Gp.instance;
            dataManager_Cp = DataManager.instance;
            pManager_Cp_temp = PlayerManager.instance;
            envManager_Cp = EnvManager.instance;
            mapManager_Cp = MapManager.instance;
            traitorHandler_Cp = TraitorHandler.instance;
            finalArea_Cp = FinalArea.instance;
            timer_Cp = TimerHandler.instance;
            inputActionPanel_Cp = InputActionPanel.instance;
            finish_Cp = Finish.instance;
        }

        //--------------------------------------------------
        void InitVariables(Hash128 hash_pr)
        {
            StartCoroutine(Corou_InitVariables(hash_pr));
        }

        IEnumerator Corou_InitVariables(Hash128 hash_pr)
        {
            onlineHandler_Cp.Init();
            yield return new WaitUntil(() => onlineHandler_Cp.mainGameState == OnlineHandler.GameState_En.Inited);

            camHandler_Cp.SetTarget();

            pManager_Cp_temp.Init();
            yield return new WaitUntil(() => pManager_Cp_temp.mainGameState == PlayerManager.GameState_En.Inited);

            envManager_Cp.Init();
            yield return new WaitUntil(() => envManager_Cp.mainGameState == EnvManager.GameState_En.Inited);

            mapManager_Cp.Init();
            yield return new WaitUntil(() => mapManager_Cp.mainGameState == MapManager.GameState_En.Inited);

            traitorHandler_Cp.Init();
            yield return new WaitUntil(() => traitorHandler_Cp.mainGameState
                == TraitorHandler.GameState_En.Inited);

            uiManager_Cp.Init();

            finalArea_Cp.Init();

            timer_Cp.Init();

            inputActionPanel_Cp.Init();

            finish_Cp.Init();

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
        void ReadyToPlay()
        {
            pManager_Cp_temp.ReadyToPlay();

            //
            Play();
        }

        //--------------------------------------------------
        void Play()
        {
            mainGameState = GameState_En.Playing;

            pManager_Cp_temp.Play();

            envManager_Cp.Play();

            mapManager_Cp.Play();

            traitorHandler_Cp.GenTraitorRandomly();

            timer_Cp.StartTimer();
        }

        #endregion

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
