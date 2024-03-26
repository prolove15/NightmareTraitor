using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PhantomBetrayal;

namespace PhantomBetrayal
{
    public class Door_M : MonoBehaviour
    {

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Fields
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Fields

        //-------------------------------------------------- serialize fields
        [SerializeField]
        public DoorValue doorValue = new DoorValue();

        //-------------------------------------------------- private fields

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Properties
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Properties

        //-------------------------------------------------- private properties
        Controller_Gp controller_Cp
        {
            get { return Controller_Gp.instance; }
        }

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
        public void MoveDoor(Hash128 hash_pr)
        {
            DoorOpenStatus stat_pr = DoorOpenStatus.Null;

            if (doorValue.openStat == DoorOpenStatus.Open)
            {
                stat_pr = DoorOpenStatus.Close;
            }
            else if (doorValue.openStat == DoorOpenStatus.Close || doorValue.openStat == DoorOpenStatus.Null)
            {
                stat_pr = DoorOpenStatus.Open;
            }

            MoveDoor(stat_pr, hash_pr);
        }

        public void MoveDoor(DoorOpenStatus tarStat_pr, Hash128 hash_pr)
        {
            StartCoroutine(Corou_MoveDoor(tarStat_pr, hash_pr));
        }

        IEnumerator Corou_MoveDoor(DoorOpenStatus tarStat_pr, Hash128 hash_pr)
        {
            //
            if (tarStat_pr == DoorOpenStatus.Null || tarStat_pr == doorValue.openStat)
            {
                HashHandler.RemoveHash(hash_pr);
                yield break;
            }

            if (doorValue.openStat == DoorOpenStatus.Opening ||
                doorValue.openStat == DoorOpenStatus.Closing)
            {
                HashHandler.RemoveHash(hash_pr);
                yield break;
            }

            //
            if (tarStat_pr == DoorOpenStatus.Open)
            {
                doorValue.anim_Cp.SetTrigger("open");

                doorValue.openStat = DoorOpenStatus.Opening;
            }
            else if (tarStat_pr == DoorOpenStatus.Close)
            {
                doorValue.anim_Cp.SetTrigger("close");

                doorValue.openStat = DoorOpenStatus.Closing;
            }

            yield return new WaitForSeconds(doorValue.animDur);

            if (doorValue.openStat == DoorOpenStatus.Opening)
            {
                doorValue.openStat = DoorOpenStatus.Open;
            }
            else if (doorValue.openStat == DoorOpenStatus.Closing)
            {
                doorValue.openStat = DoorOpenStatus.Close;
            }

            HashHandler.RemoveHash(hash_pr);
        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Internal callback methods
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Internal callback methods

        #endregion

    }
}