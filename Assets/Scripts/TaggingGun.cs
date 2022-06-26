using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class TaggingGun : Gun
{
    [SerializeField] GameObject raycast;

    PhotonView PV;

    void Awake()
    {
        PV = GetComponent<PhotonView>(); 
    }

    public override void Use()
    {
        Shoot();
    }

    void Shoot()
    {
        RaycastHit hit;

        if (Physics.Raycast(raycast.transform.position, raycast.transform.TransformDirection(Vector3.forward), out hit, 10.0f))
        {
            hit.collider.gameObject.GetComponent<IDamageable>()?.TakeDamage(((GunInfo)itemInfo).damage);
            PV.RPC("RPC_Shoot", RpcTarget.All, hit.point, hit.normal);
        }
    }

    [PunRPC]
    void RPC_Shoot(Vector3 hitPosition, Vector3 hitNormal)
    {
        Collider[] colliders = Physics.OverlapSphere(hitPosition, 0.3f);
        if(colliders.Length != 0)
        {
            GameObject bulletImpactObj = Instantiate(bulletImpactPrefab, hitPosition + hitNormal * 0.001f, Quaternion.LookRotation(hitNormal, Vector3.up) * bulletImpactPrefab.transform.rotation);
            Destroy(bulletImpactObj, 10f);
            bulletImpactObj.transform.SetParent(colliders[0].transform);
        }
    }
}
