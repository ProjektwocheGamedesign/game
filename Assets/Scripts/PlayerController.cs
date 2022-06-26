using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;

public class PlayerController : MonoBehaviourPunCallbacks, IDamageable
{
    [SerializeField] GameObject cameraHolder;
    [SerializeField] GameObject raycast;

    [SerializeField] float sprintSpeed, walkSpeed, jumpForce, smoothTime;

    [SerializeField] Item[] items;

    [SerializeField] TextMeshProUGUI roleText;
    [SerializeField] GameObject ui;

    int itemIndex;

    public float lookSpeed = 2.0f;
    public float lookXLimit = 45.0f;
    Vector2 rotation = Vector2.zero;

    bool grounded;
    Vector3 smoothMoveVelocity;
    Vector3 moveAmount;

    Rigidbody rb;

    PhotonView PV;

    public bool isSearching;

    GameObject prop;

    const float maxHealth = 100f;
    float currentHealth = maxHealth;

    PlayerManager playerManager;


    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        PV = GetComponent<PhotonView>();

        playerManager = PhotonView.Find((int)PV.InstantiationData[0]).GetComponent<PlayerManager>();
    }

    void Start()
    {
        if (PV.IsMine && isSearching == true)
        {
            EquipItem(0);
        }
        else if(!PV.IsMine)
        {
            Destroy(GetComponentInChildren<Camera>().gameObject);
            Destroy(rb);
            Destroy(ui);
        }

        if (PhotonNetwork.IsMasterClient)
        {
            isSearching = true;
        }
        else
        {
            isSearching = false;
        }

        Vector3 randomPosition = new Vector3(Random.Range(-5, 5), 0, Random.Range(-5, 5));
        transform.position = randomPosition;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (!PV.IsMine)
            return;

        Look();
        Move();
        Jump();
        SetRole();
    }

    void SetRole()
    {
        if (isSearching == true)
        {
            PlayerShoot();
            roleText.text = "Suchen";
        }
        else if (isSearching == false)
        {
            PropChange();
            roleText.text = "Verstecken";
        }
    }

    void Move()
    {
        Vector3 moveDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;

        moveAmount = Vector3.SmoothDamp(moveAmount, moveDir * (Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed), ref smoothMoveVelocity, smoothTime);
    }

    void Jump()
    {
        if(!Physics.Raycast(transform.position, -Vector3.up, 2f + 0.1f))
        {
            grounded = false;
        } 
        else
        {
            grounded = true;
        }

        if (Input.GetKeyDown(KeyCode.Space) && grounded)
        {
            rb.velocity += jumpForce * Vector3.up;
        }
    }

    void EquipItem(int _index)
    {
        itemIndex = _index;

        items[itemIndex].itemGameObject.SetActive(true);
    }

    void Look()
    {
        rotation.y += Input.GetAxis("Mouse X") * lookSpeed;
        rotation.x += -Input.GetAxis("Mouse Y") * lookSpeed;

        rotation.x = Mathf.Clamp(rotation.x, -lookXLimit, lookXLimit);
        cameraHolder.transform.localRotation = Quaternion.Euler(rotation.x, 0, 0);

        transform.eulerAngles = new Vector2(0, rotation.y);
    }

    void PropChange()
    {

        if (Input.GetMouseButtonDown(0))
        {
            if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out RaycastHit hit, 6.0f))
            {
                GameObject tempHit = hit.collider.gameObject;

                if (tempHit.tag == "Prop")
                {
                    PV.RPC("RPC_PropChangeModel", RpcTarget.All, tempHit.GetPhotonView().ViewID);
                }
            }
        }
    }

    [PunRPC]
    void RPC_PropChangeModel(int targetPropID)
    {
        if (!PV.IsMine)
            return;

        PhotonView targetPV = PhotonView.Find(targetPropID);

        if (targetPV.gameObject == null)
            return;

        gameObject.GetComponent<MeshFilter>().mesh = targetPV.gameObject.GetComponent<MeshFilter>().mesh;
        gameObject.GetComponent<MeshRenderer>().material = targetPV.gameObject.GetComponent<MeshRenderer>().material;
        gameObject.transform.localScale = targetPV.transform.localScale;

        BoxCollider newCollider = targetPV.GetComponent<BoxCollider>();

        CapsuleCollider oldCollider = GetComponent<CapsuleCollider>();
        Destroy(oldCollider);

        BoxCollider oldColliderBox = GetComponent<BoxCollider>();
        Destroy(oldColliderBox);

        gameObject.AddComponent<BoxCollider>();
        GetComponent<BoxCollider>().size = newCollider.size;
    }

    void PlayerShoot()
    {
        if(Input.GetMouseButtonDown(0))
        {
            items[itemIndex].Use();
        }
    }

    void FixedUpdate()
    {
        if (!PV.IsMine)
            return;

        rb.MovePosition(rb.position + transform.TransformDirection(moveAmount) * Time.fixedDeltaTime);
    }

    public void TakeDamage(float damage)
    {
        PV.RPC("RPC_TakeDamage", RpcTarget.All, damage);
    }

    [PunRPC]
    void RPC_TakeDamage(float damage)
    {
        if (!PV.IsMine)
            return;

        currentHealth -= damage;

        if(currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        playerManager.Die();
        isSearching = true;
    }
}
