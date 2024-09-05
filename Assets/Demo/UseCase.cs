using System;
using UnityEngine;

namespace UseCases
{
    public class UseCase : MonoBehaviour
    {
        private Demo m_Demo;
        
        protected virtual void Awake()
        {
            m_Demo = GetComponent<Demo>();
        }

        protected Demo demo => m_Demo;
        
        protected void Quit()
        {
            if (Application.isEditor)
                UnityEditor.EditorApplication.isPlaying = false;
            else
                Application.Quit();
            Debug.Log("Quit application");
        }

        private bool m_IsFiring;
        protected bool isFiring
        {
            get => m_IsFiring;
            set
            {
                if (value && !m_IsFiring)
                    Debug.Log("Started to fire");
                else if (!value && m_IsFiring)
                    Debug.Log("Stopped firing");

                m_IsFiring = value;
            }
        }

        private void Update()
        {
            if (isFiring)
            {
                m_Demo.target.GetComponent<MeshRenderer>().material.color = Color.red;
            }
            else
            {
                m_Demo.target.GetComponent<MeshRenderer>().material.color = Color.white;
            }

            if (moveDirection.sqrMagnitude > float.Epsilon)
            {
                var scaled = moveDirection * (Time.deltaTime * demo.movementSpeed);
                m_Demo.target.transform.Translate(scaled.x, scaled.y, 0, Space.World);
            }
        }

        protected Vector2 moveDirection
        {
            get;
            set;
        }
    }
}
