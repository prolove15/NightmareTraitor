using Cinemachine;
using PhantomBetrayal;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CameraHandler : MonoBehaviour
{

    [SerializeField] CinemachineVirtualCamera cam_Cp;

    [SerializeField]
    [ReadOnly] Player_M tarPlayer_Cp;

    [SerializeField]
    [ReadOnly] Transform lookPoint_Tf, followPoint_Tf;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void SetTarget()
    {
        Controller_Gp controller_Cp = GameObject.FindWithTag("GameController").GetComponent<Controller_Gp>();
        tarPlayer_Cp = controller_Cp.localPlayer_Cp;
        if (tarPlayer_Cp != null )
        {
            Debug.Log("CamearHandler, tarPlayer is null");
            return;
        }

        lookPoint_Tf = tarPlayer_Cp.lookPoint_Tf;
        followPoint_Tf = tarPlayer_Cp.followPoint_Tf;

        cam_Cp.Follow = followPoint_Tf;
        cam_Cp.LookAt = lookPoint_Tf;
    }

}
