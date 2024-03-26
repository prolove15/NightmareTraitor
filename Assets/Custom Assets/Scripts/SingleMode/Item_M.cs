using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace PhantomBetrayal
{
    public class Item_M : MonoBehaviour
    {

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Fields
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Fields

        //--------------------------------------------------
        [SerializeField] List<GameObject> parEff_GOs = new List<GameObject>();

        [SerializeField] Collider coll_Cp;

        //--------------------------------------------------
        [ReadOnly] public ItemValue itemValue = new ItemValue();

        public UnityAction<GameObject, Item_M> onTakeAction;

        [ReadOnly] public bool isTaken;

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Methods
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        
        // Start is called before the first frame update
        void Start()
        {
            
        }

        //--------------------------------------------------
        public void SetActiveParEff(bool flag)
        {
            for (int i = 0; i < parEff_GOs.Count; i++)
            {
                parEff_GOs[i].SetActive(flag);
            }
        }

        //--------------------------------------------------
        public void SetActiveCollider(bool flag)
        {
            coll_Cp.enabled = flag;
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Callback from internal
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Callback from internal

        //--------------------------------------------------
        private void OnTriggerEnter(Collider other)
        {
            if (isTaken)
            {
                return;
            }
                        
            onTakeAction.Invoke(other.gameObject, this);
        }

        #endregion

    }

}