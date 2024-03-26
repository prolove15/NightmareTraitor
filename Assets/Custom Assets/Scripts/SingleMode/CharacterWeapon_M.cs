using PhantomBetrayal;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CharacterWeapon_M : MonoBehaviour
{

    public UnityAction<GameObject> onCollideWithGhostAction;

    public UnityAction<Character_M, CharacterWeapon_M> onCollideWithChaAction;

    public float validDur;

    [ReadOnly] public bool isHitEnable;

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

        if (other.CompareTag("Character") && !isHitEnable)
        {
            onCollideWithChaAction.Invoke(other.gameObject.GetComponent<Character_M>(), this);
        }
        else if (other.CompareTag("Ghost") && isHitEnable)
        {
            onCollideWithGhostAction.Invoke(other.gameObject);
        }
    }

    #endregion

}
