using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PhantomBetrayal
{
    public class PlayerIconArea : MonoBehaviour
    {

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Fields
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Fields

        [SerializeField] Image avatar_Cp;

        [SerializeField] GameObject darkness_GO;

        [SerializeField] Animator msgAreaAnim_Cp;

        [SerializeField] GameObject gemTakenStatus_GO;

        [SerializeField] TextMeshProUGUI msgText_Cp;

        [SerializeField] Text goldText_Cp;

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Methods
        /// </summary>
        //////////////////////////////////////////////////////////////////////

        // Start is called before the first frame update
        void Start()
        {
            Init();
        }

        //--------------------------------------------------
        void Init()
        {
            darkness_GO.SetActive(false);
            gemTakenStatus_GO.SetActive(false);
        }

        //-------------------------------------------------- 
        public void SetIcon(Sprite sprite)
        {
            avatar_Cp.sprite = sprite;
        }

        //-------------------------------------------------- 
        public void OnCharacterDead()
        {
            darkness_GO.SetActive(false);
        }

        //-------------------------------------------------- 
        public void OnTakeGem()
        {
            gemTakenStatus_GO.SetActive(true);
        }

        //-------------------------------------------------- 
        public void OnDropGem()
        {
            gemTakenStatus_GO.SetActive(false);
        }

        //-------------------------------------------------- 
        public void SetMessage(string msg)
        {
            CancelInvoke("Invoke_HideMsg");

            msgText_Cp.text = msg;
            msgAreaAnim_Cp.SetTrigger("open");
            Invoke("Invoke_HideMsg", 5f);
        }

        void Invoke_HideMsg()
        {
            msgAreaAnim_Cp.SetTrigger("close");
        }

        //--------------------------------------------------
        public void SetGold(int gold_tp)
        {
            goldText_Cp.text = gold_tp.ToString();
        }

    }

}
