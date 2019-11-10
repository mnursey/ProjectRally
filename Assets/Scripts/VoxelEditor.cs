using System.Collections;
using System.Collections.Generic;
using UnityEngine;

enum VoxelEditorMode {Add, Remove, Color};

public class VoxelEditor : MonoBehaviour
{
    private VoxelEditorMode mode = VoxelEditorMode.Add;
    public int selectedValue = 1;
    public int selectedValueMax = 2;
    public float pointDelta = 0.001f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Tab))
        {
            switch(mode)
            {
                case VoxelEditorMode.Add:
                    mode = VoxelEditorMode.Remove;
                    break;
                case VoxelEditorMode.Remove:
                    mode = VoxelEditorMode.Add;
                    break;

                default:
                    break;
            }
        }

        if(Input.GetKeyDown(KeyCode.R))
        {
            selectedValue++;

            if(selectedValue > selectedValueMax)
            {
                selectedValue = 1;
            }
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            selectedValue--;

            if(selectedValue < 1)
            {
                selectedValue = selectedValueMax;
            }
        }

        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            switch (mode)
            {
                case VoxelEditorMode.Add:
                    AddVoxel();
                    break;
                case VoxelEditorMode.Remove:
                    RemoveVoxel();
                    break;

                default:
                    break;
            }
        }
    }

    void AddVoxel()
    {
        Vector3 fwd = transform.TransformDirection(Vector3.forward);
        RaycastHit hit;

        if (Physics.Raycast(transform.position, fwd, out hit, Mathf.Infinity))
        {
           
            Debug.DrawRay(transform.position, hit.point, Color.yellow);

            Chunk chunk = hit.collider.GetComponent<Chunk>();

            if(chunk != null)
            {
                Vector3 nudge = new Vector3(0f, 0f, 0f);

                if (this.transform.position.x < hit.point.x)
                {
                    nudge.x += chunk.voxelSize * pointDelta;
                }

                if (this.transform.position.x > hit.point.x)
                {
                    nudge.x += -chunk.voxelSize * pointDelta;
                }

                if (this.transform.position.y < hit.point.y)
                {
                    nudge.y += chunk.voxelSize * pointDelta;
                }

                if (this.transform.position.y > hit.point.y)
                {
                    nudge.y += -chunk.voxelSize * pointDelta;
                }

                if (this.transform.position.z < hit.point.z)
                {
                    nudge.z += chunk.voxelSize * pointDelta;
                }

                if (this.transform.position.z > hit.point.z)
                {
                    nudge.z += -chunk.voxelSize * pointDelta;
                }

                int[] voxelPos = chunk.WorldSpaceToVoxelXYZ(hit.point - nudge, out Chunk target);

                if (target != null)
                {
                    int newValue = selectedValue;
                    int oldValue = target.GetVoxel(voxelPos[0], voxelPos[1], voxelPos[2]);

                    if (oldValue != newValue)
                    {
                        target.SetVoxel(voxelPos[0], voxelPos[1], voxelPos[2], newValue);
                        target.GenerateChunk();
                    }
                }
            }
        }
    }

    void RemoveVoxel()
    {
        Vector3 fwd = transform.TransformDirection(Vector3.forward);
        RaycastHit hit;

        if (Physics.Raycast(transform.position, fwd, out hit, Mathf.Infinity))
        {

            Debug.DrawRay(transform.position, hit.point, Color.yellow);

            Chunk chunk = hit.collider.GetComponent<Chunk>();

            if (chunk != null)
            {
                Vector3 nudge = new Vector3(0f, 0f, 0f);

                if (this.transform.position.x < hit.point.x)
                {
                    nudge.x += chunk.voxelSize * pointDelta;
                }

                if (this.transform.position.x > hit.point.x)
                {
                    nudge.x += -chunk.voxelSize * pointDelta;
                }

                if (this.transform.position.y < hit.point.y)
                {
                    nudge.y += chunk.voxelSize * pointDelta;
                }

                if (this.transform.position.y > hit.point.y)
                {
                    nudge.y += -chunk.voxelSize * pointDelta;
                }

                if (this.transform.position.z < hit.point.z)
                {
                    nudge.z += chunk.voxelSize * pointDelta;
                }

                if (this.transform.position.z > hit.point.z)
                {
                    nudge.z += -chunk.voxelSize * pointDelta;
                }

                int[] voxelPos = chunk.WorldSpaceToVoxelXYZ(hit.point + nudge, out Chunk target);

                if (target != null)
                {
                    int newValue = 0;
                    int oldValue = target.GetVoxel(voxelPos[0], voxelPos[1], voxelPos[2]);

                    if (oldValue != newValue)
                    {
                        target.SetVoxel(voxelPos[0], voxelPos[1], voxelPos[2], newValue);
                        target.GenerateChunk();
                    }
                }
            }
        }
    }
}
