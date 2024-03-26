using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PhantomBetrayal
{

    public class InputActionPanel : MonoBehaviour
    {

        public static InputActionPanel instance;

        [SerializeField] GameObject chaInputPanel_GO;

        [SerializeField] GameObject ghostInputPanel_GO;

        [SerializeField] Button cha_rollBtn_Cp, cha_atkBtn_Cp, cha_breakBtn_Cp, cha_openBtn_Cp;

        [SerializeField] Button ghost_atkBtn_Cp, ghost_openBtn_Cp;

        Controller_Gp controller_Cp;

        PlayerManager pManager_Cp;

        PlayerType localPlType { get { return pManager_Cp.localPlType; } }

        Character_M localCha_Cp { get { return pManager_Cp.localCha_Cp; } }

        Ghost_M ghost_Cp { get { return ghost_Cp; } }

        //--------------------------------------------------
        private void Awake()
        {
            instance = this;
        }

        // Start is called before the first frame update
        void Start()
        {

        }

        //--------------------------------------------------
        public void Init()
        {
            controller_Cp = Controller_Gp.instance;
            pManager_Cp = controller_Cp.pManager_Cp;
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Events from external
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Events from external

        //--------------------------------------------------
        public void SetActiveChaInputPanel(bool flag)
        {
            chaInputPanel_GO.SetActive(flag);
            ghostInputPanel_GO.SetActive(!flag);
        }

        //--------------------------------------------------
        public void SetActiveGhostInputPanel(bool flag)
        {
            ghostInputPanel_GO.SetActive(flag);
            chaInputPanel_GO.SetActive(!flag);
        }

        //--------------------------------------------------
        public void EnableChaAtkBtn(bool flag)
        {
            cha_atkBtn_Cp.interactable = flag;
        }

        //--------------------------------------------------
        public void EnableChaOpenBtn(bool flag)
        {
            cha_openBtn_Cp.interactable = flag;
        }

        //--------------------------------------------------
        public void EnableGhostOpenBtn(bool flag)
        {
            ghost_openBtn_Cp.interactable = flag;
        }

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Events from UI
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Events from UI

        //--------------------------------------------------
        public void OnClickRollBtn_Cha()
        {
            if (localPlType == PlayerType.Cha)
            {
                if (localCha_Cp.ExistGameStates(Character_M.GameState_En.Playing))
                {
                    localCha_Cp.Handle_Roll();
                }
            }
        }

        //--------------------------------------------------
        public void OnClickBreakBoxBtn_Cha()
        {
            if (localPlType == PlayerType.Cha)
            {
                if (localCha_Cp.ExistGameStates(Character_M.GameState_En.Playing))
                {
                    localCha_Cp.Handle_BreakBox();
                }
            }
        }

        //--------------------------------------------------
        public void OnClickAtkBtn_Cha()
        {
            if (localPlType == PlayerType.Cha)
            {
                if (localCha_Cp.ExistGameStates(Character_M.GameState_En.Playing))
                {
                    localCha_Cp.Handle_Atk();
                }
            }
        }

        //--------------------------------------------------
        public void OnClickMoveDoorBtn_Cha()
        {
            if (localPlType == PlayerType.Cha)
            {
                if (localCha_Cp.ExistGameStates(Character_M.GameState_En.Playing))
                {
                    localCha_Cp.Handle_MoveDoor();
                }
            }
        }

        //--------------------------------------------------
        public void OnClickAtkBtn_Ghost()
        {

        }

        //--------------------------------------------------
        public void OnClickMoveDoorBtn_Ghost()
        {

        }

        #endregion

    }
}
