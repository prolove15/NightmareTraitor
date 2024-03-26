using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationChange : MonoBehaviour {

    public Animator anim;

	
	// Update is called once per frame
	void Update () {
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            anim.Play ( "Attack" );
        }

        if ( Input.GetKeyDown ( KeyCode.Alpha1 ) )
        {
            anim.Play ( "Attack Vertical" );
        }

        if ( Input.GetKeyDown ( KeyCode.Alpha2 ) )
        {
            anim.Play ( "Dodge Right" );
        }

        if ( Input.GetKeyDown ( KeyCode.Alpha3 ) )
        {
            anim.Play ( "Dodge Left" );
        }

        if ( Input.GetKeyDown ( KeyCode.Alpha4 ) )
        {
            anim.Play ( "Dodge Back" );
        }

        if ( Input.GetKeyDown ( KeyCode.Alpha5 ) )
        {
            anim.Play ( "Scream" );
        }

        if ( Input.GetKeyDown ( KeyCode.Alpha6 ) )
        {
            anim.Play ( "Shake" );
        }

        if ( Input.GetKeyDown ( KeyCode.Alpha7 ) )
        {
            anim.Play ( "Death" );
        }

        if ( Input.GetKeyDown ( KeyCode.Alpha8 ) )
        {
            anim.Play ( "Ressurection" );
        }

        if ( Input.GetKeyDown ( KeyCode.Alpha9 ) )
        {
            anim.Play ( "Damage Taken" );
        }

    }
}
