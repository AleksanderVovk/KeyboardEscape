using UnityEngine;

public class WorldUIClick : MonoBehaviour
{
    public LayerMask worldUILayer;

    void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, worldUILayer))
        {
            var wb = hit.collider.GetComponent<WorldButton>();
            if (wb != null) wb.OnWorldClick();
        }
    }
}
