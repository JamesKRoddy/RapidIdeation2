using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThirdPersonCharacterController : MonoBehaviour
{
    private CharacterController controller;

    public float speed = 4.0f;

    public float turnSpeed = 0.1f;

    public Transform camera;

    public float playerGravity = 9.81f;

    public float jumpForce = 1.0f;

    public Animator anim;

    private bool groundedPlayer;

    private Vector3 moveDir;

    private bool rootMotionUsed = false;

    // Start is called before the first frame update
    void Start()
    {
        controller = GetComponent<CharacterController>();

        rootMotionUsed = anim.applyRootMotion;
    }

    // Update is called once per frame
    void Update()
    {
        groundedPlayer = controller.isGrounded;

        if(groundedPlayer && moveDir.y < 0)
        {
            moveDir.y = 0;
        }

        float yMovement = moveDir.y;

        moveDir.x = 0.0f;
        moveDir.z = 0.0f;

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 dir = new Vector3(horizontal, 0, vertical);

        if(dir.magnitude > 0.1f)
        {
            float targetAngle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg + camera.eulerAngles.y;

            Quaternion targetQuaternion = Quaternion.Euler(0f, targetAngle, 0f);

            transform.rotation = Quaternion.Slerp(transform.rotation, targetQuaternion, Time.deltaTime * turnSpeed);

            if(rootMotionUsed == false || groundedPlayer == false)
            {
                moveDir = targetQuaternion * Vector3.forward;

                moveDir = (moveDir.normalized * dir.magnitude) + (Vector3.up * yMovement);
            }
                                    
        }


        anim.SetFloat("Speed", dir.magnitude);

        if (Input.GetButtonDown("Jump") && groundedPlayer)
        {
            moveDir.y += Mathf.Sqrt(jumpForce * -3.0f * playerGravity);
        }

        //anim.SetBool("IsGrounded", groundedPlayer);

        if (moveDir.y > 0.1f)
        {
            anim.SetBool("IsGrounded", false);
        }
        else
        {
            anim.SetBool("IsGrounded", true);
        }

        moveDir.y += playerGravity * Time.deltaTime;

        controller.Move(moveDir * speed * Time.deltaTime);
    }

    private void FixedUpdate()
    {
        transform.parent = null;
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if(hit.gameObject.tag == "Platform")
        {
            transform.parent = hit.gameObject.transform;
        }
        else
        {
            transform.parent = null;
        }
    }
}
