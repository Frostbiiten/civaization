using UnityEngine;
using UnityEngine.UI;

public class CameraScript : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private MapTilegen2 tileGen;
    [SerializeField] private LayerMask uiMask;
    private RaycastHit info;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);

            bool isOverUI = UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
            if (!isOverUI && Physics.Raycast(ray, out info, Mathf.Infinity))
            {
                if (info.collider.gameObject.layer == LayerMask.NameToLayer("Tile"))
                {
                    tileGen.SelectTile(int.Parse(info.collider.gameObject.name));
                }
            }
        }
    }
}
