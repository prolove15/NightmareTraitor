using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
[ExecuteInEditMode]
public class ButtonName : MonoBehaviour
{
    public Text[] buttontexts;
    public string[] buttonnames;
    int num;
    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < buttontexts.Length; i++)
        {
            changename(i);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void changename(int n)
    {
        buttontexts[n].text = buttonnames[n].ToString();
    }
}
