using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] private float distance = 3f;
    [SerializeField] private Camera cam;
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private Transform holdPoint;

    private PickupItem currentTarget;

    void Update()
    {
        CheckInteractable();
    }

    void CheckInteractable()
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        if (Physics.Raycast(ray, out RaycastHit hit, distance))
        {
            PickupItem pickup = hit.collider.GetComponent<PickupItem>();

            if (pickup != null)
            {
                // Nếu đang nhìn vào item mới
                if (currentTarget != pickup)
                {
                    currentTarget = pickup;
                    //UIManager.Instance.ShowInteract(pickup.itemName);
                    UIManager.Instance.ShowTextItem(pickup.itemName);
                }

                return;
            }
        }

        // Nếu không còn nhìn vào item nữa
        if (currentTarget != null)
        {
            currentTarget = null;
            //UIManager.Instance.HideInteract();
            UIManager.Instance.HideTextItem();
        }
    }

    // ==============================
    // PICKUP / USE
    // ==============================

    public void OnPickupButton()
    {
        if (inventory.HasItem())
        {
            GameObject heldObject = inventory.CurrentObject;

            ItemUse useComponent = heldObject.GetComponent<ItemUse>();
            if (useComponent != null)
                useComponent.Use();

            return;
        }

        if (currentTarget == null) return;

        GameObject targetObject = currentTarget.gameObject;

        Rigidbody rb = targetObject.GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;

        Collider col = targetObject.GetComponent<Collider>();
        if (col) col.enabled = false;

        targetObject.transform.SetParent(holdPoint);
        targetObject.transform.localPosition = Vector3.zero;
        targetObject.transform.localRotation = Quaternion.identity;

        inventory.Pick(targetObject);
        UIManager.Instance.ShowDropButton();
        UIManager.Instance.HideTextItem();
        currentTarget = null;
    }

    // ==============================
    // DROP
    // ==============================

    public void OnDropButton()
    {
        if (!inventory.HasItem()) return;

        GameObject obj = inventory.Drop();
        UIManager.Instance.HideDropButton();

        obj.transform.SetParent(null);
        obj.transform.position = transform.position + transform.forward * 1.5f;

        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = false;

        Collider col = obj.GetComponent<Collider>();
        if (col) col.enabled = true;
    }
}