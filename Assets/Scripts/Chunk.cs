using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

//[ExecuteInEditMode]
[Serializable]
public class Chunk : MonoBehaviour
{
    public float voxelSize = 1.0f;
    public static int chunkSize = 16;

    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();
    List<Vector2> uv = new List<Vector2>();
    List<Color> colors = new List<Color>();

    MeshFilter mf;

    int[,,] voxels = new int[chunkSize, chunkSize, chunkSize];

    public Color topColor = new Color(96 / 256f, 128 / 256f, 56 / 256f);
    public Color remainingColor = Color.gray;
    public bool updateInEditor = false;

    // Start is called before the first frame update
    void Start()
    {
        Display();
    }

    void Awake()
    {
        mf = GetComponent<MeshFilter>();
    }

    // Update is called once per frame
    void Update()
    {
        //if (!EditorApplication.isPlaying && updateInEditor)
        //{
        //    Display();
        //    updateInEditor = false;
        //}   
    }

    void UpdateVoxel(int x, int y, int z, int value)
    {
        voxels[x, y, z] = value;
    }

    void SetVoxels(int[,,] voxels)
    {
        this.voxels = voxels;
    }

    void Display()
    {
        for (int x = 0; x < voxels.GetLength(0); x++)
        {
            for (int y = 0; y < voxels.GetLength(1); y++)
            {
                for (int z = 0; z < voxels.GetLength(2); z++)
                {
                    float height = ((Mathf.Cos(x * 0.5f) * 3f + 7f) + (Mathf.Cos(z * 0.5f) * 3f + 7f)) / 2;

                    if(y <= height)
                    {
                        voxels[x, y, z] = 1;
                    }
                }
            }
        }

        GenerateChunk();
    }

    void GenerateChunk()
    {
        Reset();

        for (int x = 0; x < voxels.GetLength(0); x++)
        {
            for (int y = 0; y < voxels.GetLength(1); y++)
            {
                for (int z = 0; z < voxels.GetLength(2); z++)
                {
                    ComputeVoxel(x, y, z, voxels[x, y, z]);
                }
            }
        }

        Render();
    }

    void Reset()
    {
        vertices = new List<Vector3>();
        triangles = new List<int>();
        uv = new List<Vector2>();
        colors = new List<Color>();
    }

    bool[] GetOpenFaces(int x, int y, int z)
    {
        // F Bk L R T B
        bool[] faces = new bool[] { true, true, true, true, true, true};

        if (x < chunkSize - 2)
        {
            if (voxels[x + 1, y, z] != 0)
            {
                faces[3] = false;
            }
        }

        if (y < chunkSize - 2)
        {
            if (voxels[x, y + 1, z] != 0)
            {
                faces[4] = false;
            }
        }

        if (z < chunkSize - 2)
        {
            if(voxels[x, y, z + 1] != 0)
            {
                faces[1] = false;
            }
        }

        if (x > 0)
        {
            if (voxels[x - 1, y, z] != 0)
            {
                faces[2] = false;
            }
        }

        if (y > 0)
        {
            if (voxels[x, y - 1, z] != 0)
            {
                faces[5] = false;
            }
        }

        if (z > 0)
        {
            if (voxels[x, y, z - 1] != 0)
            {
                faces[0] = false;
            }
        }

        return faces;
    }

    void ComputeVoxel(int x, int y, int z, int value)
    {
        if(value == 1)
        {
            float voxelRadius = voxelSize / 2f;
            float id_x = voxelSize * x - (chunkSize * voxelSize / 2f);
            float id_y = voxelSize * y - (chunkSize * voxelSize / 2f);
            float id_z = voxelSize * z - (chunkSize * voxelSize / 2f);

            Vector3[] corners = new Vector3[] {
                new Vector3(id_x - voxelRadius, id_y - voxelRadius, id_z - voxelRadius),
                new Vector3(id_x + voxelRadius, id_y - voxelRadius, id_z - voxelRadius),
                new Vector3(id_x - voxelRadius, id_y + voxelRadius, id_z - voxelRadius),
                new Vector3(id_x + voxelRadius, id_y + voxelRadius, id_z - voxelRadius),

                new Vector3(id_x - voxelRadius, id_y - voxelRadius, id_z + voxelRadius),
                new Vector3(id_x + voxelRadius, id_y - voxelRadius, id_z + voxelRadius),
                new Vector3(id_x - voxelRadius, id_y + voxelRadius, id_z + voxelRadius),
                new Vector3(id_x + voxelRadius, id_y + voxelRadius, id_z + voxelRadius),
            };

            bool[] openFaces = GetOpenFaces(x, y, z);

            List<Vector3> v = new List<Vector3>();
            List<int> t = new List<int>();
            List<Vector2> uvs = new List<Vector2>();
            List<Color> c = new List<Color>();

            for (int f = 0; f < openFaces.Length; f++)
            {
                if(openFaces[f])
                {
                    int tOffset = vertices.Count + v.Count;

                    switch (f)
                    {
                        case 0:
                            // Front

                            t.AddRange(new List<int> {
                                0 + tOffset, 2 + tOffset, 1 + tOffset,
                                2 + tOffset, 3 + tOffset, 1 + tOffset,
                            });

                            v.AddRange(new List<Vector3> {
                                corners[0],
                                corners[1],
                                corners[2],
                                corners[3],
                            });

                            uvs.AddRange(new List<Vector2> {
                                new Vector2(0, 0),
                                new Vector2(1, 0),
                                new Vector2(0, 1),
                                new Vector2(1, 1),
                            });

                            c.AddRange(new List<Color> {
                                remainingColor,
                                remainingColor,
                                remainingColor,
                                remainingColor,
                            });

                            break;

                        case 1:
                            // Back

                            t.AddRange(new List<int> {
                                0 + tOffset, 1 + tOffset, 2 + tOffset,
                                1 + tOffset, 3 + tOffset, 2 + tOffset,
                            });

                            v.AddRange(new List<Vector3> {
                                corners[4],
                                corners[5],
                                corners[6],
                                corners[7],
                            });

                            uvs.AddRange(new List<Vector2> {
                                new Vector2(0, 0),
                                new Vector2(1, 0),
                                new Vector2(0, 1),
                                new Vector2(1, 1),
                            });

                            c.AddRange(new List<Color> {
                                remainingColor,
                                remainingColor,
                                remainingColor,
                                remainingColor,
                            });

                            break;

                        case 2:
                            // Left

                            t.AddRange(new List<int> {
                                0 + tOffset, 2 + tOffset, 1 + tOffset,
                                2 + tOffset, 3 + tOffset, 1 + tOffset,
                            });

                            v.AddRange(new List<Vector3> {
                                corners[4],
                                corners[0],
                                corners[6],
                                corners[2],
                            });

                            uvs.AddRange(new List<Vector2> {
                                new Vector2(0, 0),
                                new Vector2(1, 0),
                                new Vector2(0, 1),
                                new Vector2(1, 1),
                            });

                            c.AddRange(new List<Color> {
                                remainingColor,
                                remainingColor,
                                remainingColor,
                                remainingColor,
                            });

                            break;

                        case 3:
                            // Right

                            t.AddRange(new List<int> {
                                0 + tOffset, 2 + tOffset, 1 + tOffset,
                                2 + tOffset, 3 + tOffset, 1 + tOffset,
                            });

                            v.AddRange(new List<Vector3> {
                                corners[1],
                                corners[5],
                                corners[3],
                                corners[7],
                            });

                            uvs.AddRange(new List<Vector2> {
                                new Vector2(0, 0),
                                new Vector2(1, 0),
                                new Vector2(0, 1),
                                new Vector2(1, 1),
                            });

                            c.AddRange(new List<Color> {
                                remainingColor,
                                remainingColor,
                                remainingColor,
                                remainingColor,
                            });

                            break;

                        case 4:
                            // Top

                            t.AddRange(new List<int> {
                                0 + tOffset, 2 + tOffset, 1 + tOffset,
                                2 + tOffset, 3 + tOffset, 1 + tOffset,
                            });

                            v.AddRange(new List<Vector3> {
                                corners[2],
                                corners[3],
                                corners[6],
                                corners[7],
                            });

                            uvs.AddRange(new List<Vector2> {
                                new Vector2(0, 0),
                                new Vector2(1, 0),
                                new Vector2(0, 1),
                                new Vector2(1, 1),
                            });

                            c.AddRange(new List<Color> {
                                topColor,
                                topColor,
                                topColor,
                                topColor,
                            });

                            break;
                        case 5:
                            // Bottom

                            t.AddRange(new List<int> {
                                0 + tOffset, 1 + tOffset, 2 + tOffset,
                                2 + tOffset, 1 + tOffset, 3 + tOffset,
                            });

                            v.AddRange(new List<Vector3> {
                                corners[0],
                                corners[1],
                                corners[4],
                                corners[5],
                            });

                            uvs.AddRange(new List<Vector2> {
                                new Vector2(0, 0),
                                new Vector2(1, 0),
                                new Vector2(0, 1),
                                new Vector2(1, 1),
                            });

                            c.AddRange(new List<Color> {
                                remainingColor,
                                remainingColor,
                                remainingColor,
                                remainingColor,
                            });

                            break;
                        default:
                            break;
                    }
                }
            }

            vertices.AddRange(v);
            triangles.AddRange(t);
            uv.AddRange(uvs);
            colors.AddRange(c);
        }
    }

    void Render()
    {
        Mesh mesh = new Mesh();
        mf.mesh = mesh;

        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.colors = colors.ToArray();
        mesh.Optimize();
        mesh.RecalculateNormals();
    }
}
