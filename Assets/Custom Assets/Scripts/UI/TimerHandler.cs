using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PhantomBetrayal
{
    public class TimerHandler : MonoBehaviour
    {

        public static TimerHandler instance;

        [SerializeField] Animator timerAnim_Cp;

        [SerializeField] Text timeText_Cp;

        [SerializeField] int totalTime, timeoutPulse, interval;

        Controller_Gp controller_Cp;

        int restTime;

        private void Awake()
        {
            instance = this;
        }

        // Start is called before the first frame update
        void Start()
        {

        }

        public void Init()
        {
            controller_Cp = Controller_Gp.instance;

            //
            restTime = totalTime;
        }

        public void StartTimer()
        {
            StartCoroutine(Corou_StartTimer());
        }

        IEnumerator Corou_StartTimer()
        {
            do
            {
                timeText_Cp.text = restTime.ToString();
                yield return new WaitForSeconds((float)interval);

                restTime -= interval;
                if (restTime <= timeoutPulse)
                {
                    timerAnim_Cp.SetTrigger("pulse");
                }
            }
            while (restTime >= 0);

            controller_Cp.finish_Cp.OnTimerEnd();
        }
    }
}

