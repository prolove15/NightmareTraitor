using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PhantomBetrayal;
using Photon.Pun;

namespace PhantomBetrayal
{
    public class DataManager : MonoBehaviourPun
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

        //-------------------------------------------------- static fields
        public static DataManager instance;

        //-------------------------------------------------- serialize fields

        //-------------------------------------------------- public fields
        [SerializeField] public PrepareData pList = new PrepareData();

        //-------------------------------------------------- private fields
        Controller_Gp controller_Cp;
        OnlineHandler onlineHandler_Cp;

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Properties
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Properties

        //-------------------------------------------------- public properties

        //-------------------------------------------------- private properties

        #endregion

        //////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Methods
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////

        //-------------------------------------------------- Awake
        private void Awake()
        {
            instance = this;
        }

        //-------------------------------------------------- Start is called before the first frame update
        void Start()
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

        }

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// External interface
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region External interface

        //--------------------------------------------------
        public void GenRandPlayerList()
        {

        }

        #endregion

        //////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Internal methods
        /// </summary>
        //////////////////////////////////////////////////////////////////////
        #region Internal method

        #endregion

    }
}