using PhantomBetrayal;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PhantomBetrayal
{
    public class FinalArea : MonoBehaviour
    {

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Fields
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Fields

        //-------------------------------------------------- static fields
        public static FinalArea instance;

        //-------------------------------------------------- serialize fields
        [SerializeField] public Transform gemPointGroup_Tf;

        //-------------------------------------------------- public fields
        public List<Gem_M> takenGem_Cps = new List<Gem_M>();

        //-------------------------------------------------- private fields
        Controller_Gp controller_Cp;

        List<Transform> gemPlacePoint_Tfs = new List<Transform>();

        Finish finish_Cp;

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// methods
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

        //--------------------------------------------------
        public void Init()
        {
            for (int i = 0; i < gemPointGroup_Tf.childCount; i++)
            {
                gemPlacePoint_Tfs.Add(gemPointGroup_Tf.GetChild(i));
            }

            controller_Cp = Controller_Gp.instance;
            finish_Cp = controller_Cp.finish_Cp;

            // init variables
            controller_Cp.uiManager_Cp.SetGemCount(takenGem_Cps.Count,
                controller_Cp.envManager_Cp.gemData.maxGemCount);
        }

        //--------------------------------------------------
        public void TakeGem(Character_M cha_Cp_tp, Gem_M gem_Cp_tp)
        {
            gem_Cp_tp.transform.SetPositionAndRotation(gemPlacePoint_Tfs[takenGem_Cps.Count].position,
                Quaternion.identity);
            gem_Cp_tp.transform.SetParent(gemPlacePoint_Tfs[takenGem_Cps.Count]);
            gem_Cp_tp.OnArriveToFinal();

            cha_Cp_tp.takenGem_Cp = null;
            cha_Cp_tp.OnDropGemToFinalArea();

            takenGem_Cps.Add(gem_Cp_tp);

            controller_Cp.uiManager_Cp.SetGemCount(takenGem_Cps.Count,
                controller_Cp.envManager_Cp.gemData.maxGemCount);

            //
            finish_Cp.OnGemArrivedToFinalArea();
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Events
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Events

        //--------------------------------------------------
        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Character"))
            {
                return;
            }

            Character_M cha_Cp_tp = other.GetComponent<Character_M>();
            Gem_M gem_Cp_tp = cha_Cp_tp.takenGem_Cp;
            if (gem_Cp_tp == null)
            {
                return;
            }

            if (cha_Cp_tp.isTraitor)
            {
                return;
            }

            TakeGem(cha_Cp_tp, gem_Cp_tp);
        }

        #endregion
    }


}
