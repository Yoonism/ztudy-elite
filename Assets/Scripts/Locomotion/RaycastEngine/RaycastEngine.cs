using UnityEngine;
using System.Collections;

namespace Elite.Locomotion
{
     public class RaycastEngine : MonoBehaviour
     {

          public enum JumpState
          {
               None = 0,
               Holding
          }

          public enum MoveState
          {
               Full = 0,
               Reduced,
               Freeze
          }

          #region --------------- [ Locomotion value setup ]

          [Space(10)]
          [Header("Collision")]

          [SerializeField]
          private LayerMask platformMask;
          [SerializeField]
          private float _parallelInsetLength;
          [SerializeField]
          private float _perpendicularInsetLength;
          [SerializeField]
          private float _groundCheckThreshold;
          [SerializeField]
          private float _hCollider;
          [SerializeField]
          private float _vCollider;

          [Space(10)]
          [Header("Velocity & Knockback")]

          [SerializeField]
          private float _gravity;
          [SerializeField]
          private float _hAcceleration;
          [SerializeField]
          private float _hFriction;
          [SerializeField]
          private float _hSpeedSnap;
          [SerializeField]
          private float _hSpeedMax;
          [SerializeField]
          private float _hSpeedReducedMultiplier;
          [SerializeField]
          private float knockbackDecay;

          [Space(10)]
          [Header("Input & Jumping")]

          [SerializeField]
          private float _jumpBufferThreshold;
          [SerializeField]
          private float _jumpSpeedStart;
          [SerializeField]
          private float _jumpMaxHoldDuration;
          [SerializeField]
          private float _jumpSpeedMin;

          [SerializeField]
          private bool _isGrounded;

          #endregion

          #region --------------- [ References ]

          [SerializeField]
          private AudioSource _jumpSFX, _landSFX, _startMoveSFX;

          private Animator _animator;

          #endregion

          #region --------------- [ Motion variables ]

          [HideInInspector]
          public Vector2 velocity;
          [HideInInspector]
          public Vector2 knockback;

          private RaycastMoveDirection _raycastDown;
          private RaycastMoveDirection _raycastLeft;
          private RaycastMoveDirection _raycastRight;
          private RaycastMoveDirection _raycastUp;

          private RaycastCheckTouch _raycastGroundCheck;

          private Vector2 _lastStandingOnPos;
          private Vector2 _lastStandingOnVel;
          private Collider2D _lastStandingOn;

          #endregion

          #region --------------- [ Input / buffer system ]

          private float _jumpStartTimer;
          private float _jumpHoldTimer;
          private bool _jumpInputDown;
          private JumpState _jumpState;
          private bool _lastGrounded;

          [HideInInspector]
          public Vector2 inputAxis = new Vector2(0f, 0f);
          public MoveState moveState = MoveState.Full;

          [SerializeField]
          private float _hDeadzone = 0.1f;
          [SerializeField]
          private float _vDeadzone = 0.2f;

          #endregion

          void Start()
          {
               _animator = GetComponent<Animator>();

               float hCl = _hCollider * 0.5f;
               float vCl = _vCollider * 0.5f;

               _raycastDown = new RaycastMoveDirection(new Vector2(-_hCollider, -_vCollider), new Vector2(_hCollider, -_vCollider), Vector2.down, platformMask,
                    Vector2.right * _parallelInsetLength, Vector2.up * _perpendicularInsetLength);
               _raycastLeft = new RaycastMoveDirection(new Vector2(-_hCollider, -_vCollider), new Vector2(-_hCollider, _vCollider), Vector2.left, platformMask,
                    Vector2.up * _parallelInsetLength, Vector2.right * _perpendicularInsetLength);
               _raycastUp = new RaycastMoveDirection(new Vector2(-_hCollider, _vCollider), new Vector2(_hCollider, _vCollider), Vector2.up, platformMask,
                    Vector2.right * _parallelInsetLength, Vector2.down * _perpendicularInsetLength);
               _raycastRight = new RaycastMoveDirection(new Vector2(_hCollider, -_vCollider), new Vector2(_hCollider, _vCollider), Vector2.right, platformMask,
                    Vector2.up * _parallelInsetLength, Vector2.left * _perpendicularInsetLength);

               _raycastGroundCheck = new RaycastCheckTouch(new Vector2(-_hCollider, -_vCollider), new Vector2(_hCollider, -_vCollider), Vector2.down, platformMask,
                    Vector2.right * _parallelInsetLength, Vector2.up * _perpendicularInsetLength, _groundCheckThreshold);
          }

          private void Update()
          {
               _jumpStartTimer -= Time.deltaTime;

               bool jumpBtn;
               if (inputAxis.y > _vDeadzone)
               {
                    jumpBtn = true;
               }
               else
               {
                    jumpBtn = false;
               }

               if (jumpBtn && _jumpInputDown == false)
               {
                    _jumpStartTimer = _jumpBufferThreshold;
               }
               _jumpInputDown = jumpBtn;

               CalculatePhysics();
          }

          private void CalculatePhysics()
          {

               Collider2D standingOn = _raycastGroundCheck.DoRaycast(transform.position);
               _isGrounded = standingOn != null;
               if (_isGrounded && _lastGrounded == false)
               {
                    _jumpState = JumpState.None;
                    //landSFX.Play();
               }
               _lastGrounded = _isGrounded;

               #region --------------- [ CALC : Y velocity control ]

               switch (_jumpState)
               {
                    case JumpState.None:
                         if (_isGrounded && _jumpStartTimer > 0)
                         {
                              _jumpStartTimer = 0;
                              _jumpState = JumpState.Holding;
                              _jumpHoldTimer = 0;
                              velocity.y = _jumpSpeedStart;
                              //jumpSFX.Play();
                         }
                         else
                         {
                              velocity.y -= _gravity * Time.deltaTime;
                         }
                         break;
                    case JumpState.Holding:
                         _jumpHoldTimer += Time.deltaTime;
                         if (_jumpInputDown == false || _jumpHoldTimer >= _jumpMaxHoldDuration)
                         {
                              _jumpState = JumpState.None;
                              velocity.y = Mathf.Lerp(_jumpSpeedMin, _jumpSpeedStart, _jumpHoldTimer / _jumpMaxHoldDuration);
                         }
                         break;
               }

               #endregion

               #region --------------- [ CALC : X velocity control ]

               float hInput = 0f;
               if (Mathf.Abs(inputAxis.x) >= _hDeadzone)
               {
                    hInput = inputAxis.x;
               }

               int targetDirection = UtilitiesMath.ApproximateSign(hInput);
               int velocityDirection = UtilitiesMath.ApproximateSign(velocity.x);

               if (targetDirection != 0)
               {
                    if (velocityDirection != targetDirection)
                    {
                         velocity.x = _hSpeedSnap * targetDirection;
                         //startMoveSFX.Play();
                    }
                    else
                    {
                         // move state calculation
                         float finalHspeedMax = _hSpeedMax;
                         switch (moveState)
                         {
                              case MoveState.Reduced:
                                   finalHspeedMax *= _hSpeedReducedMultiplier;
                                   break;
                              case MoveState.Freeze:
                                   finalHspeedMax = 0f;
                                   break;
                              default:
                                   break;
                         }

                         velocity.x = Mathf.MoveTowards(velocity.x, finalHspeedMax * targetDirection, _hAcceleration * Time.deltaTime);
                    }
               }
               else
               {
                    velocity.x = Mathf.MoveTowards(velocity.x, 0, _hFriction * Time.deltaTime);
               }

               #endregion

               #region --------------- [ CALC : Knockback ]

               knockback = Vector2.MoveTowards(knockback, Vector2.zero, knockbackDecay * Time.deltaTime);

               #endregion

               #region --------------- [ CALC : Pre-movement calcualtions ]

               Vector2 displacement = Vector2.zero;
               Vector2 targetDisplacement = (velocity + knockback) * Time.deltaTime;

               if (standingOn != null)
               {
                    if (_lastStandingOn == standingOn)
                    {
                         _lastStandingOnVel = (Vector2)standingOn.transform.position - _lastStandingOnPos;
                         targetDisplacement += _lastStandingOnVel;
                    }
                    else if (standingOn == null)
                    {
                         velocity += _lastStandingOnVel / Time.deltaTime;
                         targetDisplacement += _lastStandingOnVel;
                    }
                    _lastStandingOnPos = standingOn.transform.position;
               }
               _lastStandingOn = standingOn;

               #endregion

               #region --------------- [ CALC : Check collision & stop ]

               // X displacement check
               if (targetDisplacement.x > 0)
               {
                    displacement.x = _raycastRight.DoRaycast(transform.position, targetDisplacement.x);
               }
               else if (targetDisplacement.x < 0)
               {
                    displacement.x = -_raycastLeft.DoRaycast(transform.position, -targetDisplacement.x);
               }

               // Y displacement check
               if (targetDisplacement.y > 0)
               {
                    displacement.y = _raycastUp.DoRaycast(transform.position, targetDisplacement.y);
               }
               else if (targetDisplacement.y < 0)
               {
                    displacement.y = -_raycastDown.DoRaycast(transform.position, -targetDisplacement.y);
               }

               // check for solid
               if (Mathf.Approximately(displacement.x, targetDisplacement.x) == false)
               {
                    velocity.x = 0;
                    knockback = Vector2.zero;
               }
               if (Mathf.Approximately(displacement.y, targetDisplacement.y) == false)
               {
                    velocity.y = 0;
                    _jumpState = JumpState.None;
                    knockback = Vector2.zero;
               }

               #endregion

               #region --------------- [ CALC : Final movement ]

               transform.Translate(displacement);

               #endregion

               #region --------------- [ CALC : Animation ]
               /*
          bool lookBack = false;

          if(!_playerController.IsGamepad())
          {
               if(_playerController.aimPosition.x < transform.position.x && targetDirection == 1)
               {
                    lookBack = true;
               }
               else if(_playerController.aimPosition.x > transform.position.x && targetDirection == -1)
               {
                     lookBack = true;
               }
          }
          else
          {
               if(_playerController.aimPosition.x <= 0f && targetDirection == 1)
               {
                     lookBack = true;
               }
               else if(_playerController.aimPosition.x > 0f && targetDirection == -1)
               {
                     lookBack = true;
               }
          }


               if(engineFreeze)
               {
                    _animator.Play("Character" + _animationType.ToString() + "Skill");
               }
               else
               {
                    if(_jumpState == JumpState.Holding)
                    {
                         _animator.Play("Character" + _animationType.ToString() + "JumpRunStart");
                    }
                    else
                    {
                         if(grounded)
                         {
                              if(targetDirection == 0)
                              {
                                   _animator.Play("Character" + _animationType.ToString() + "Idle");
                              }
                              else
                              {
                                   if(lookBack) //targetDirection != 0)
                                   {
                                        _animator.Play("Character" + _animationType.ToString() + "Run_rev");
                                   }
                                   else
                                   {
                                        _animator.Play("Character" + _animationType.ToString() + "Run");
                                   }
                                   // _animator.Play("Character" + _animationType.ToString() + "Run");
                              }
                         }
                         else
                         {
                              if(velocity.y < 0)
                              {
                                   _animator.Play("Character" + _animationType.ToString() + "JumpRunFall");
                              }
                              else
                              {
                                   _animator.Play("Character" + _animationType.ToString() + "JumpRunStart");
                              }
                         }
                    }
               }

               if(_playerController.aimPosition.x < transform.position.x) //targetDirection != 0)
               {
                    _spriteRenderer.flipX = true;
               }
               else
               {
                    _spriteRenderer.flipX = false;
               }
          */
               #endregion
          }

          #region --------------- [ Parsing ]

          public bool GetLastGrounded()
          {
               return _lastGrounded;
          }

          public bool GetCurrentGrounded()
          {
               return _isGrounded;
          }

          #endregion
     }
}