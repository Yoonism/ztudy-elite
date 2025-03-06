using UnityEngine;

namespace Elite.Locomotion
{
    public class LocomotionActor : MonoBehaviour
    {
        [SerializeField]
        RaycastEngine _raycastEngine;

        private void Update()
        {
            _raycastEngine.inputAxis.x = Input.GetAxis("Horizontal");

            if(Input.GetKey(KeyCode.Space))
            {
                _raycastEngine.inputAxis.y = 1f;
            }
            else
            {
                _raycastEngine.inputAxis.y = 0f;
            }
        }
    }
}