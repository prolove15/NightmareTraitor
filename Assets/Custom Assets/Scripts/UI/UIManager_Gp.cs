using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PhantomBetrayal
{
    public class UIManager_Gp : MonoBehaviour
    {

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// fields
        /// </summary>
        //////////////////////////////////////////////////////////////////////

        //-------------------------------------------------- static fields
        public static UIManager_Gp instance;

        //-------------------------------------------------- serialize fields
        [SerializeField] Text hpText_Cp;

        [SerializeField] Text gemCountText_Cp;

        [SerializeField] Text goldText_Cp;

        [SerializeField] Transform psIconAreaGroup_Tf;

        [SerializeField] GameObject pIconArea_Pf;

        [SerializeField] InputField chatMsgInputF_Cp;

        [SerializeField] VariableJoystick joyInput_Cp;

        //-------------------------------------------------- private fields
        Controller_Gp controller_Cp;

        PlayerManager pManager_Cp;

        List<PlayerIconArea> pIconArea_Cps = new List<PlayerIconArea>();

        //--------------------------------------------------
        List<Character_M> cha_Cps { get { return pManager_Cp.cha_Cps; } }

        public float vertInput { get { return joyInput_Cp.Vertical; } }

        public float horInput { get { return joyInput_Cp.Horizontal; } }

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// methods
        /// </summary>
        //////////////////////////////////////////////////////////////////////

        //--------------------------------------------------
        private void Awake()
        {
            instance = this;
        }

        // Start is called before the first frame update
        void Start()
        {
            
        }

        // Update is called once per frame
        void Update()
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

            InitPlayersIconArea();
        }

        //--------------------------------------------------
        void SetComponents()
        {
            controller_Cp = Controller_Gp.instance;
            pManager_Cp = controller_Cp.pManager_Cp;
        }

        //--------------------------------------------------
        void InitPlayersIconArea()
        {
            for (int i = 0; i < cha_Cps.Count; i++)
            {
                PlayerIconArea pIconArea_Cp_tp = Instantiate(pIconArea_Pf, psIconAreaGroup_Tf)
                    .GetComponent<PlayerIconArea>();
                pIconArea_Cps.Add(pIconArea_Cp_tp);
                cha_Cps[i].SetPlayerIconArea(pIconArea_Cp_tp);
            }

            // add player icon to the ghost
        }

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Events form external
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region

        //--------------------------------------------------
        public void SetHp(int hp_pr)
        {
            hpText_Cp.text = hp_pr.ToString();
        }

        //--------------------------------------------------
        public void SetGold(int gold_tp)
        {
            goldText_Cp.text = gold_tp.ToString();
        }

        //--------------------------------------------------
        public void OnCharacterDead(int chaId_pr)
        {
            
        }

        //--------------------------------------------------
        public void SetNoticeText(string notice)
        {

        }

        //--------------------------------------------------
        public void SetChatMsg(string msg)
        {
            pManager_Cp.SetChatMsg(msg);
        }

        //--------------------------------------------------
        public void SetGemCount(int arrivedGemCount, int totalGemCount)
        {
            gemCountText_Cp.text = arrivedGemCount.ToString() + "/" + totalGemCount.ToString();
        }

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Events from UI
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Events from UI

        //--------------------------------------------------
        public void OnClickSubmitMsgBtn()
        {
            SetChatMsg(chatMsgInputF_Cp.text);
        }

        //--------------------------------------------------
        public void OnClickEscapeBtn()
        {
            PhotonNetwork.Disconnect();
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
