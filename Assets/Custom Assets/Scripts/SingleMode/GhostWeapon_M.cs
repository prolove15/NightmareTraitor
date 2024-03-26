using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using PhantomBetrayal;

namespace PhantomBetrayal
{
    public class GhostWeapon_M : MonoBehaviour
    {

        public UnityAction<GameObject> onCollideWithChaAction;

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
        /// Default callback methods
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Default callback methods

        //--------------------------------------------------
        private void OnTriggerEnter(Collider other)
        {
            if (other.transform.root.tag == transform.root.tag)
            {
                return;
            }

            if (other.CompareTag("Character"))
            {
                onCollideWithChaAction.Invoke(other.gameObject);
            }
        }

        #endregion

    }
}