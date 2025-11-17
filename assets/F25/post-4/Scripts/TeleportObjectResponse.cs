using UnityEngine;

public class TeleportObjectResponse : InteractionResponse
{
    [SerializeField] public GameObject objectToTeleport;
    [SerializeField] public Transform teleportPosition;

    protected override void TriggerResponse()
    {
        Rigidbody rb = objectToTeleport.GetComponent<Rigidbody>();
        
        //if (rb == null)
            objectToTeleport.transform.position = teleportPosition.position;
        //else 
            //rb.MovePosition(objectToTeleport.transform.position);
    }
}
