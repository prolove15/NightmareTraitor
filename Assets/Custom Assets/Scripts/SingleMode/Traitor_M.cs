using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PhantomBetrayal;

namespace PhantomBetrayal
{
    public class Traitor_M : MonoBehaviour
    {

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Types
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Types

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Fields
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Fields

        //--------------------------------------------------
        Controller_Gp controller_Cp;

        TraitorHandler traitorHandler_Cp;

        Character_M cha_Cp;

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Properties
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Properties

        #endregion

        //////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Methods
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////

        //-------------------------------------------------- Start is called before the first frame update
        void Start()
        {

        }

        //-------------------------------------------------- Update is called once per frame
        void Update()
        {

        }

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Initialize
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Initialize

        //--------------------------------------------------
        public void Init()
        {
            SetComponents();

            InitVariables();
        }

        //--------------------------------------------------
        void SetComponents()
        {
            controller_Cp = Controller_Gp.instance;
            traitorHandler_Cp = controller_Cp.traitorHandler_Cp;
            cha_Cp = gameObject.GetComponent<Character_M>();
        }

        //--------------------------------------------------
        void InitVariables()
        {
            cha_Cp.isTraitor = true;
        }

        #endregion

    }
}