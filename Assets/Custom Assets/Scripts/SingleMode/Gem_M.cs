using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PhantomBetrayal
{
    public class Gem_M : MonoBehaviour
    {

        public enum GameState_En
        {
            Nothing,
            InBox, OnGround, Taken, Dropped, Arrived,
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Fields
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Fields

        [SerializeField] public GemValue gemValue = new GemValue();

        [SerializeField] GameObject groundEff_Pf, takenEff_Pf, arriveEff_Pf;

        [SerializeField] Transform effTriPoint_Tf;

        [SerializeField] AudioSource audioS_Cp;

        [SerializeField] float takenEffDur = 3f;

        [ReadOnly] GameObject groundEff_GO, takenEff_GO;

        [ReadOnly] public Character_M ownCha_Cp;

        [ReadOnly] public GameState_En mainGameState = GameState_En.Nothing;

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Methods
        /// </summary>
        //////////////////////////////////////////////////////////////////////

        // Start is called before the first frame update
        void Start()
        {
            OnGenerateInBox();
        }

        // Update is called once per frame
        void Update()
        {

        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Handle show/hide gem
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Handle show/hide gem

        //--------------------------------------------------
        void SetShowGem(bool flag)
        {
            foreach (MeshRenderer meshR_Cp_tp in GetComponentsInChildren<MeshRenderer>())
            {
                meshR_Cp_tp.enabled = flag;
            }

            foreach (SkinnedMeshRenderer meshR_Cp_tp in GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                meshR_Cp_tp.enabled = flag;
            }

            foreach (Collider coll_Cp_tp in GetComponentsInChildren<Collider>())
            {
                coll_Cp_tp.enabled = flag;
            }
        }

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Interface methods
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region interface methods

        public void OnGenerateInBox()
        {
            mainGameState = GameState_En.InBox;
        }

        public void OnUnhide()
        {
            mainGameState = GameState_En.OnGround;

            groundEff_GO = Instantiate(groundEff_Pf, effTriPoint_Tf);
        }

        public void OnTakenByCha(Character_M cha_Cp_tp)
        {
            mainGameState = GameState_En.Taken;

            if (groundEff_GO)
            {
                Destroy(groundEff_GO);
            }

            takenEff_GO = Instantiate(takenEff_Pf, cha_Cp_tp.chaValue.effTriPoint_middle_Tf);
            Destroy(takenEff_GO, takenEffDur);

            //
            SetShowGem(false);
        }

        public void OnDropped(Character_M cha_Cp_tp)
        {
            mainGameState = GameState_En.Dropped;

            if (takenEff_GO)
            {
                Destroy(takenEff_GO);
            }

            groundEff_GO = Instantiate(groundEff_Pf, effTriPoint_Tf);

            //
            transform.position = cha_Cp_tp.transform.position;
            SetShowGem(true);
        }

        public void OnArriveToFinal()
        {
            mainGameState = GameState_En.Arrived;

            if (takenEff_GO)
            {
                Destroy(takenEff_GO);
            }

            Instantiate(arriveEff_Pf, effTriPoint_Tf);

            SetShowGem(true);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.transform.root == transform.root)
            {
                return;
            }

            if (!other.CompareTag("Character"))
            {
                return;
            }

            if (mainGameState == GameState_En.Arrived)
            {
                return;
            }

            Character_M cha_Cp_tp = other.gameObject.GetComponent<Character_M>();
            cha_Cp_tp.TakeGem(this);
        }

        #endregion
    }

}
