using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using PhantomBetrayal;
using UnityEngine.Events;
using Unity.VisualScripting;

namespace PhantomBetrayal
{
    public class Box_M : MonoBehaviour
    {

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Fields
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Fields

        //-------------------------------------------------- serialize fields
        [SerializeField] Animator anim_Cp;

        [SerializeField] NavMeshAgent navAgent_Cp;

        [SerializeField] List<Collider> colliders = new List<Collider>();

        [SerializeField] public Transform contentPoint_Tf;

        //-------------------------------------------------- public fields
        public UnityAction<GameObject, Box_M> onBreakAction;

        [SerializeField][ReadOnly] bool isBroken;

        //-------------------------------------------------- private fields
        EnvManager envManager_Cp;

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Properties
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Properties

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Methods
        /// </summary>
        //////////////////////////////////////////////////////////////////////

        // Start is called before the first frame update
        void Start()
        {
            envManager_Cp = EnvManager.instance;
        }

        // Update is called once per frame
        void Update()
        {

        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Functionalities
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Functionalities

        //--------------------------------------------------
        public void BreakBox()
        {
            StartCoroutine(Corou_BreakBox());
        }

        IEnumerator Corou_BreakBox()
        {
            isBroken = true;

            anim_Cp.SetTrigger("break");

            navAgent_Cp.enabled = false;

            foreach (Collider collider in colliders)
            {
                collider.enabled = false;
            }

            yield return new WaitForSeconds(envManager_Cp.boxData.disapDur);

            Destroy(gameObject);
        }

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Internal events
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Internal events

        //--------------------------------------------------
        private void OnTriggerEnter(Collider other)
        {
            if (other.transform.root == transform.root)
            {
                return;
            }

            if (isBroken)
            {
                return;
            }

            onBreakAction.Invoke(other.gameObject, this);
        }

        #endregion

    }

}