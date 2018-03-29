using UnityEngine;
using UnityEngine.UI;
using System.Text;

namespace ISX.XR
{
    public class GearVRDebugger : MonoBehaviour
    {
        void Draw()
        {
            StringBuilder sb = new StringBuilder(); ;
            GearVRTrackedController leftHand = GearVRTrackedController.leftHand;
            if (leftHand != null)
            {
                sb.Append("LeftHand Data");               
            }
        }        
    }
}