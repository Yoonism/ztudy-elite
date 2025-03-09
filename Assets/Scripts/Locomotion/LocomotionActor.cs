using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Elite.Locomotion
{
    public class LocomotionActor : MonoBehaviour
    {
        public enum ActorState
        {
            Standing = 0,
            Walking,
            Air
        }

        [SerializeField]
        private RaycastEngine _raycastEngine;
        [SerializeField]
        private Transform _spriteContainer;

        [SerializeField]
        private ActorState _actorState = ActorState.Standing;

        // sprite control variables
        [SerializeField]
        private Transform[] _legTransform = new Transform[2];
        [SerializeField]
        private Vector3[] _legPositionBuffer = new Vector3[6];

        private void Start()
        {
            StartCoroutine(UpdateLegsFinal());
            StartCoroutine(UpdateLegsWalking());
        }

        private void Update()
        {
            _raycastEngine.inputAxis.x = Input.GetAxis("Horizontal");

            if(Input.GetKey(KeyCode.Space)) _raycastEngine.inputAxis.y = 1f;
            else _raycastEngine.inputAxis.y = 0f;

            if(_raycastEngine.GetCurrentGrounded())
            {
                if(Mathf.Abs(_raycastEngine.inputAxis.x) != 0f) _actorState = ActorState.Walking;
                else _actorState = ActorState.Standing;
            }
            else _actorState = ActorState.Air;

            if(_raycastEngine.inputAxis.x < 0f) _spriteContainer.localScale = new Vector3(-0.1f, 1f, 0.1f);//.Set(-1f, 0f, 0f);
            if(_raycastEngine.inputAxis.x > 0f) _spriteContainer.localScale = new Vector3(0.1f, 1f, 0.1f);//.Set(1f, 0f, 0f);
        }

        private IEnumerator UpdateLegsFinal()
        {
            Vector3[] legsPostLerp = new Vector3[2];
            int[] legsIndex = new int[2];
            float lerpMultiplier = 1f;

            while(true)
            {
                switch (_actorState)
                {
                    case ActorState.Standing:
                        legsIndex[0] = 0;
                        legsIndex[1] = 1;
                        lerpMultiplier = 40f;
                        break;
                    case ActorState.Walking:
                        legsIndex[0] = 2;
                        legsIndex[1] = 3;
                        lerpMultiplier = 10000f;
                        break;
                    case ActorState.Air:
                        legsIndex[0] = 4;
                        legsIndex[1] = 5;
                        lerpMultiplier = 10f;
                        break;
                }

                legsPostLerp[0] = Vector3.Lerp(legsPostLerp[0], _legPositionBuffer[legsIndex[0]], Time.deltaTime * lerpMultiplier);
                legsPostLerp[1] = Vector3.Lerp(legsPostLerp[1], _legPositionBuffer[legsIndex[1]], Time.deltaTime * lerpMultiplier);

                _legTransform[0].localPosition = legsPostLerp[0];
                _legTransform[1].localPosition = legsPostLerp[1];

                yield return null;
            }
        }

        private IEnumerator UpdateLegsWalking()
        {
            float pingPongDirection = 1f;
            float pingPongValue = 0f;

            while(true)
            {
                pingPongValue += pingPongDirection * (Time.deltaTime * 15f);
                if(Mathf.Abs(pingPongValue) > 1f)
                {
                    pingPongDirection *= -1f;
                    pingPongValue += (Mathf.Abs(pingPongValue) - 1f) * pingPongDirection;
                }

                _legPositionBuffer[2].z = (pingPongValue - 1f) * 0.25f;
                _legPositionBuffer[3].z = (-pingPongValue - 1f) * 0.25f;

                //_legTransform[0].localPosition = legPositionBuffer[0];
                //_legTransform[1].localPosition = legPositionBuffer[1];

                yield return null;
            }
        }
    }
}