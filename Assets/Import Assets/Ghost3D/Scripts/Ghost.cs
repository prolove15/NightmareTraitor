using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class Ghost : MonoBehaviour
{
    
    public Animator[] myanimator;
    
    // Start is called before the first frame update
    void Start()
    {

        
       }

    // Update is called once per frame
    public void Idle()
    {
        foreach (Animator x in myanimator)
        {
            x.Play("Idle2");
        }
    }
    public void Hitanim()
    {
        foreach(Animator x in myanimator)
        {
            x.Play("Hit");
        }
    }
    public void Lookrightanim()
    {
        foreach (Animator x in myanimator)
        {
            x.Play("LookRight");
        }
    }
    public void Lookleftanim()
    {
        foreach (Animator x in myanimator)
        {
            x.Play("LookLeft");
        }
    }
    public void Rotateleft()
    {
        foreach (Animator x in myanimator)
        {
            x.Play("RoatetLeft");
        }
    }
    public void Rotateright()
    {
        foreach (Animator x in myanimator)
        {
            x.Play("RotateRight");
        }
    }
    public void run()
    {
        foreach (Animator x in myanimator)
        {
            x.Play("Run");
        }
    }
    public void attack()
    {
        foreach (Animator x in myanimator)
        {
            x.Play("Attack");
        }
    }
    public void wierd()
    {
        foreach (Animator x in myanimator)
        {
            x.Play("Weird");
        }
    }




}
