using System;
using UnityEngine;
using UnityEngine.UI;

public class CameraScript : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private MapTilegen2 tileGen;
    [SerializeField] private GameManager gameMan;
    [SerializeField] private LayerMask uiMask;
    private RaycastHit info;

    [SerializeField] private Vector2 rangeX, rangeZ, distRange;
    [SerializeField] private Vector3 targetPos, offsetVec;
    [SerializeField] private float distance, scrollSens;
    [SerializeField] private Transform centerTransform;

    [SerializeField] private Transform canvas;
    [SerializeField] private Vector3 canvasOffset;
    
    private Vector3 ogPos;
    private bool mode = false;

    public void Reset()
    {
        //targetPos = centerTransform.position;
        //transform.position = ogPos;
        mode = false;
    }

    private void Start()
    {
        ogPos = transform.position;
    }

    public void SetTarget(Vector3 target, float dist = -1)
    {
        targetPos = target;

        if (dist > 0)
        {
            distance = dist;
        }

        mode = true;
    }
    
    // Update is called once per frame
    void Update()
    {
        if (gameMan.leader != null)
        {
            Vector3 pos = tileGen.GetCenter(tileGen.GetLeaderIndex(gameMan.leader)) + offsetVec;
            targetPos = pos;
            canvas.localScale = Vector3.Lerp(canvas.localScale, Vector3.one, Time.deltaTime * 20f);
            canvas.position = pos;
        }
        else
        {
            canvas.localScale = Vector3.Lerp(canvas.localScale, Vector3.zero, Time.deltaTime * 20f);
        }
        
        if (mode)
        {
            targetPos = new Vector3(Mathf.Clamp(targetPos.x, rangeX.x, rangeX.y), 0,
                 Mathf.Clamp(targetPos.z, rangeZ.x, rangeZ.y));
            transform.position = Vector3.Lerp(transform.position, targetPos + offsetVec * distance, Time.deltaTime * 4.92f);
            transform.rotation = Quaternion.LookRotation(-offsetVec);
            //transform.rotation = Quaternion.Slerp(Quaternion.LookRotation(-offsetVec), Quaternion.LookRotation(new Vector3((rangeX.x + rangeX.y) / 2f, 0f, (rangeY.x + rangeY.y) / 2f) - transform.position), 0.3f);
            distance += Input.mouseScrollDelta.y * scrollSens;
            distance = Mathf.Clamp(distance, distRange.x, distRange.y);

        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, ogPos, Time.deltaTime * 3.92f);
            transform.rotation = Quaternion.LookRotation(-offsetVec);
        }
        
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
