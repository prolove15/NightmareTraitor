using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace EasyMobileInput.Demo
{
    public class Demo : MonoBehaviour
    {
        [SerializeField] 
        private Joystick movementJoystick;

        [SerializeField] 
        private Joystick heightJoystick;
        
        [FormerlySerializedAs("sideJoystick")] [SerializeField] 
        private Joystick sizeJoystick;

        [SerializeField] 
        private Button flipButton;
        
        [SerializeField] 
        private Button flipBackButton;

        private float currentAngle = 0.0f;
        private float targetAngle = 0.0f;

        private void Start()
        {
            flipButton.OnPressed += () =>
                {
                    targetAngle += 45.0f;
                };
            
            flipBackButton.OnPressed += () =>
                {
                    targetAngle -= 45.0f;
                };
        }

        private void Update()
        {
            transform.position += movementJoystick.CurrentProcessedValue * Time.deltaTime * 10.0f;
            transform.position = new Vector3(transform.position.x, heightJoystick.CurrentProcessedValue.y, transform.position.z);
            
            transform.localScale = new Vector3(1, 1, 1.1f + sizeJoystick.CurrentProcessedValue.x);
            
            transform.localRotation = Quaternion.AngleAxis(currentAngle, Vector3.right);
            currentAngle = Mathf.Lerp(currentAngle, targetAngle, Time.deltaTime * 10.0f);
        }
    }
}