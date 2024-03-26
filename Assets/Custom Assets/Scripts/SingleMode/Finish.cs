using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PhantomBetrayal
{
    
    public class Finish : MonoBehaviour
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
            ChaDefeated, ChaVictory,
        }

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Fields
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Fields

        //-------------------------------------------------- static fields
        public static Finish instance;

        //-------------------------------------------------- serialize fields
        [SerializeField] Animator winAnim_Cp;

        [SerializeField] Animator loseAnim_Cp;

        //-------------------------------------------------- public fields
        public GameState_En mainGameState = GameState_En.Nothing;

        //-------------------------------------------------- private fields
        Controller_Gp controller_Cp;
        PlayerManager pManager_Cp;
        UIManager_Gp uiManager_Cp;
        FinalArea finalArea_Cp;
        EnvManager envManager_Cp;

        //--------------------------------------------------
        List<Character_M> cha_Cps { get { return pManager_Cp.cha_Cps; } }

        Ghost_M ghost_Cp { get { return pManager_Cp.ghost_Cp; } }

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Methods
        /// </summary>
        //////////////////////////////////////////////////////////////////////

        private void Awake()
        {
            instance = this;
        }

        // Start is called before the first frame update
        void Start()
        {

        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Init
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Init

        //--------------------------------------------------
        public void Init()
        {
            SetComponents();

            mainGameState = GameState_En.Inited;
        }

        //--------------------------------------------------
        void SetComponents()
        {
            controller_Cp = Controller_Gp.instance;
            pManager_Cp = controller_Cp.pManager_Cp;
            uiManager_Cp = controller_Cp.uiManager_Cp;
            finalArea_Cp = controller_Cp.finalArea_Cp;
            envManager_Cp = controller_Cp.envManager_Cp;
        }

        #endregion

        //--------------------------------------------------
        public void OnFinish()
        {
            if (mainGameState == GameState_En.ChaDefeated)
            {
                ghost_Cp.OnFinish(true);

                foreach (Character_M cha_Cp_tp in cha_Cps)
                {
                    cha_Cp_tp.OnFinish(false);
                }
            }
            else
            {
                ghost_Cp.OnFinish(false);

                foreach (Character_M cha_Cp_tp in cha_Cps)
                {
                    cha_Cp_tp.OnFinish(true);
                }
            }

            controller_Cp.OnFinish();
            uiManager_Cp.OnFinish();
            pManager_Cp.OnFinish();
            controller_Cp.envManager_Cp.OnFinish();
            controller_Cp.mapManager_Cp.OnFinish();
            controller_Cp.traitorHandler_Cp.OnFinish();

            //
            bool isVictory = false;
            if (pManager_Cp.localPlType == PlayerType.Cha)
            {
                if (pManager_Cp.localCha_Cp.isTraitor)
                {
                    if (mainGameState == GameState_En.ChaVictory)
                    {
                        isVictory = false;
                    }
                    else if (mainGameState == GameState_En.ChaDefeated)
                    {
                        isVictory = true;
                    }
                }
                else
                {
                    if (mainGameState == GameState_En.ChaVictory)
                    {
                        isVictory = true;
                    }
                    else if (mainGameState == GameState_En.ChaDefeated)
                    {
                        isVictory = false;
                    }
                }
            }
            else if (pManager_Cp.localPlType == PlayerType.Ghost)
            {
                if (mainGameState == GameState_En.ChaVictory)
                {
                    isVictory = false;
                }
                else if (mainGameState == GameState_En.ChaDefeated)
                {
                    isVictory = true;
                }
            }

            if (isVictory)
            {
                VictoryAction();
            }
            else
            {
                DefeatAction();
            }
        }

        //--------------------------------------------------
        void VictoryAction()
        {
            StartCoroutine(Corou_VictoryAction());
        }

        IEnumerator Corou_VictoryAction()
        {
            yield return new WaitForSeconds(3f);

            winAnim_Cp.SetTrigger("open");
        }

        //--------------------------------------------------
        void DefeatAction()
        {
            StartCoroutine(Corou_DefeatAction());
        }

        IEnumerator Corou_DefeatAction()
        {
            yield return new WaitForSeconds(3f);

            loseAnim_Cp.SetTrigger("open");
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Events from external
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Events from external

        //--------------------------------------------------
        public void OnCharacterDead()
        {
            if (mainGameState == GameState_En.ChaVictory || mainGameState == GameState_En.ChaDefeated)
            {
                return;
            }

            bool isDefeated = true;
            foreach (Character_M cha_Cp_tp in pManager_Cp.cha_Cps)
            {
                if (!cha_Cp_tp.isTraitor && cha_Cp_tp.isAlive)
                {
                    isDefeated = false;
                    break;
                }
            }

            if (!isDefeated)
            {
                return;
            }

            mainGameState = GameState_En.ChaDefeated;

            OnFinish();
        }

        //--------------------------------------------------
        public void OnGemArrivedToFinalArea()
        {
            if (mainGameState == GameState_En.ChaVictory || mainGameState == GameState_En.ChaDefeated)
            {
                return;
            }

            bool gemCompleted = false;
            if (finalArea_Cp.takenGem_Cps.Count == envManager_Cp.gemData.maxGemCount)
            {
                gemCompleted = true;
            }

            if (!gemCompleted)
            {
                return;
            }

            mainGameState = GameState_En.ChaVictory;
            OnFinish();
        }

        //--------------------------------------------------
        public void OnGhostDead()
        {
            if (mainGameState == GameState_En.ChaVictory || mainGameState == GameState_En.ChaDefeated)
            {
                return;
            }

            mainGameState = GameState_En.ChaVictory;
            OnFinish();
        }

        //--------------------------------------------------
        public void OnTimerEnd()
        {
            //if ()
            print("OnTimerEnd called");

        }

        #endregion

    }

}