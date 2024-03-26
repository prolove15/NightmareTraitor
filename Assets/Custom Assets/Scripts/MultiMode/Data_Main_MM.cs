using PhantomBetrayal;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Realtime;

public class Data_Main_MM : MonoBehaviour
{

    [SerializeField] public bool isDataCorr;

    [SerializeField] public PrepareData prepareData = new PrepareData();

    Player[] photonPlayers { get { return PhotonNetwork.PlayerList; } }

    public void ModifyPrepareData()
    {
        if (!isDataCorr)
        {
            for (int i = 0; i < photonPlayers.Length; i++)
            {
                prepareData.values[i].uId = photonPlayers[i].ActorNumber;
            }
        }
    }

}
