using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

[ExecuteAlways]
[Serializable]
public class Chunk : MonoBehaviour
{
    public int x, y, z;
    public string chunkName;

    public float voxelSize = 1.0f;
    public static int chunkSize = 16;

    public static string chunkSaveExtension = ".cnk";

    [SerializeField]
    List<Vector3> vertices = new List<Vector3>();
    [SerializeField]
    List<int> triangles = new List<int>();
    List<Vector2> uv = new List<Vector2>();
    [SerializeField]
    List<Color> colors = new List<Color>();

    [SerializeField]
    List<Color> colorPalette = new List<Color>(); 

    MeshFilter mf;

    int[,,] voxels = new int[chunkSize, chunkSize, chunkSize];

    public float colorHeightDelta = 0.02f;
    public int heightColorCycle = 3;
    private bool enableHightColorCylce = false;

    public bool updateInEditor = false;

    // front back left right up down
    private Chunk[] surroundingChunks = new Chunk[6];

    public bool loadChunk = false;
    public bool saveChunk = false;
    public bool loadOnStart = false;
    public bool renderOnStart = false;

    // Start is called before the first frame update
    void Start()
    {
        if(loadOnStart)
        {
            loadChunk = true;
        }

        if(renderOnStart)
        {
            Render();
            renderOnStart = false;
        }
    }

    void Awake()
    {
        mf = GetComponent<MeshFilter>();
    }

    public string GetChunkSaveLocation()
    {
        return Application.persistentDataPath + "/ChunkSaves";
    }

    // Update is called once per frame
    void Update()
    {
        if (updateInEditor)
        {
            GenerateChunk();
            updateInEditor = false;
        }
        
        if(loadChunk)
        {
            LoadChunk();
            GenerateChunk();
            loadChunk = false;
        }

        if (saveChunk)
        {
            SaveChunk();
            saveChunk = false;
        }
    }

    public void SetXYZ(int x, int y, int z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public void SetChunkName(string chunkName)
    {
        this.chunkName = chunkName;
    }

    public int[] GetXYX()
    {
        return new int[] { x, y, z};
    }

    public void EnableHightColorCylce(bool enable) {
        enableHightColorCylce = enable;
    }

    public void SetColorPalette(List<Color> palette)
    {
        colorPalette = palette;
    }

    public int GetVoxel(int x, int y, int z)
    {
        return voxels[x, y, z];
    }

    public void SetVoxel(int x, int y, int z, int value)
    {
        voxels[x, y, z] = value;
    }

    public void SetVoxels(int[,,] voxels)
    {
        this.voxels = voxels;
    }

    public void GenerateChunk()
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

        // TESTING END
        Render();
    }

    public void SetSurroundingChunks(Chunk[] surroundingChunks)
    {
        this.surroundingChunks = surroundingChunks;
    }

    public int[] WorldSpaceToVoxelXYZ(Vector3 worldSpace, out Chunk chunk)
    {
        Vector3 posDelta = -transform.position + worldSpace;

        float xHat = posDelta.x / voxelSize;
        float yHat = posDelta.y / voxelSize;
        float zHat = posDelta.z / voxelSize;

        int x = Mathf.RoundToInt(xHat) + chunkSize / 2;
        int y = Mathf.RoundToInt(yHat) + chunkSize / 2;
        int z = Mathf.RoundToInt(zHat) + chunkSize / 2;

        chunk = null;

        int[] xyz = { x, y , z};

        bool thisChunk = true;

        if(x < 0)
        {
            if(surroundingChunks[2] != null)
            {
                xyz = surroundingChunks[2].WorldSpaceToVoxelXYZ(worldSpace, out chunk);
            }

            thisChunk = false;
        }

        if (x >= chunkSize)
        {
            if (surroundingChunks[3] != null)
            {
                xyz = surroundingChunks[3].WorldSpaceToVoxelXYZ(worldSpace, out chunk);
            }

            thisChunk = false;
        }

        if (y < 0)
        {
            if (surroundingChunks[5] != null)
            {
                xyz = surroundingChunks[5].WorldSpaceToVoxelXYZ(worldSpace, out chunk);
            }

            thisChunk = false;
        }

        if (y >= chunkSize)
        {
            if (surroundingChunks[4] != null)
            {
                xyz = surroundingChunks[4].WorldSpaceToVoxelXYZ(worldSpace, out chunk);
            }

            thisChunk = false;
        }

        if (z < 0)
        {
            if (surroundingChunks[1] != null)
            {
                xyz = surroundingChunks[1].WorldSpaceToVoxelXYZ(worldSpace, out chunk);
            }

            thisChunk = false;
        }

        if (z >= chunkSize)
        {
            if (surroundingChunks[0] != null)
            {
                xyz = surroundingChunks[0].WorldSpaceToVoxelXYZ(worldSpace, out chunk);
            }

            thisChunk = false;
        }

        if (thisChunk)
        {
            chunk = this;
        }

        return xyz;
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
        if(value > 0)
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
                    Color adjColor;

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

                            adjColor = colorPalette[value * 6 - 6];

                            if (enableHightColorCylce)
                            {
                                adjColor.r += (y % heightColorCycle) * colorHeightDelta;
                                adjColor.g += (y % heightColorCycle) * colorHeightDelta;
                                adjColor.b += (y % heightColorCycle) * colorHeightDelta;
                            }

                            c.AddRange(new List<Color> {
                                adjColor,
                                adjColor,
                                adjColor,
                                adjColor,
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

                            adjColor = colorPalette[value * 6 - 5];

                            if (enableHightColorCylce)
                            {
                                adjColor.r += (y % heightColorCycle) * colorHeightDelta;
                                adjColor.g += (y % heightColorCycle) * colorHeightDelta;
                                adjColor.b += (y % heightColorCycle) * colorHeightDelta;
                            }

                            c.AddRange(new List<Color> {
                                adjColor,
                                adjColor,
                                adjColor,
                                adjColor,
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

                            adjColor = colorPalette[value * 6 - 4];

                            if (enableHightColorCylce)
                            {
                                adjColor.r += (y % heightColorCycle) * colorHeightDelta;
                                adjColor.g += (y % heightColorCycle) * colorHeightDelta;
                                adjColor.b += (y % heightColorCycle) * colorHeightDelta;
                            }

                            c.AddRange(new List<Color> {
                                adjColor,
                                adjColor,
                                adjColor,
                                adjColor,
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

                            adjColor = colorPalette[value * 6 - 3];

                            if (enableHightColorCylce)
                            {
                                adjColor.r += (y % heightColorCycle) * colorHeightDelta;
                                adjColor.g += (y % heightColorCycle) * colorHeightDelta;
                                adjColor.b += (y % heightColorCycle) * colorHeightDelta;
                            }

                            c.AddRange(new List<Color> {
                                adjColor,
                                adjColor,
                                adjColor,
                                adjColor,
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

                            adjColor = colorPalette[value * 6 - 2];

                            if(enableHightColorCylce)
                            {
                                adjColor.r += (y % heightColorCycle) * colorHeightDelta;
                                adjColor.g += (y % heightColorCycle) * colorHeightDelta;
                                adjColor.b += (y % heightColorCycle) * colorHeightDelta;
                            }

                            c.AddRange(new List<Color> {
                                adjColor,
                                adjColor,
                                adjColor,
                                adjColor,
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

                            adjColor = colorPalette[value * 6 - 1];

                            if (enableHightColorCylce)
                            {
                                adjColor.r += (y % heightColorCycle) * colorHeightDelta;
                                adjColor.g += (y % heightColorCycle) * colorHeightDelta;
                                adjColor.b += (y % heightColorCycle) * colorHeightDelta;
                            }

                            c.AddRange(new List<Color> {
                                adjColor,
                                adjColor,
                                adjColor,
                                adjColor,
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

        GetComponent<MeshCollider>().sharedMesh = mesh;
    }

    string GetSavePath()
    {
        return GetChunkSaveLocation() + "/ " + chunkName + chunkSaveExtension;
    }

    public void SaveChunk()
    {
        string serializedChunk = SerializeChunk();
        Directory.CreateDirectory(GetChunkSaveLocation());
        File.WriteAllText(GetSavePath(), serializedChunk, System.Text.Encoding.ASCII);
    }

    public bool LoadChunk()
    {
        if(File.Exists(GetSavePath()))
        {
            string serializedChunk = File.ReadAllText(GetSavePath(), System.Text.Encoding.ASCII);
            SetToSerializedChunk(serializedChunk);
            return true;
        } else
        {
            return false;
        }
    }

    public string SerializeChunk()
    {
        ChunkTransferObject transferObj = new ChunkTransferObject(this.voxels, this.colorPalette);

        return JsonUtility.ToJson(transferObj);
    }

    void SetToSerializedChunk(String str)
    {
        ChunkTransferObject transferObj = JsonUtility.FromJson<ChunkTransferObject>(str);

        transferObj.GetData(out voxels, out colorPalette);
    }
}

[System.Serializable]
public class ChunkTransferObject
{
    public int[] flattenedVoxels = new int[Chunk.chunkSize * Chunk.chunkSize * Chunk.chunkSize];
    public List<SerializableColor> colorPalette;

    public ChunkTransferObject()
    {

    }

    public ChunkTransferObject(int[,,] voxels, List<Color> palette)
    {
        //this.voxels = voxels;

        for(int xVoxel = 0; xVoxel < Chunk.chunkSize; xVoxel++)
        {
            for (int yVoxel = 0; yVoxel < Chunk.chunkSize; yVoxel++)
            {
                for (int zVoxel = 0; zVoxel < Chunk.chunkSize; zVoxel++)
                {
                    flattenedVoxels[(xVoxel * Chunk.chunkSize * Chunk.chunkSize) + (yVoxel * Chunk.chunkSize) + zVoxel] = voxels[xVoxel, yVoxel, zVoxel];
                }
            }
        }

        colorPalette = new List<SerializableColor>();

        foreach (Color c in palette)
        {
            colorPalette.Add(new SerializableColor(c));
        }
    }

    public void GetData(out int[,,] voxels, out List<Color> palette)
    {
        voxels = new int[Chunk.chunkSize, Chunk.chunkSize, Chunk.chunkSize];

        for(int i = 0; i < flattenedVoxels.Length; i++)
        {
            int x = Mathf.FloorToInt(i / (Chunk.chunkSize * Chunk.chunkSize));
            int y = Mathf.FloorToInt((i - (x * Chunk.chunkSize * Chunk.chunkSize)) / Chunk.chunkSize);
            int z = i - (y * Chunk.chunkSize) - (x * Chunk.chunkSize * Chunk.chunkSize);

            voxels[x, y, z] = flattenedVoxels[i];
        }

        palette = new List<Color>();

        foreach (SerializableColor c in colorPalette)
        {
            palette.Add(c.GetColor());
        }
    }
}

[System.Serializable]
public class SerializableColor
{
    public float r;
    public float g;
    public float b;
    public float a;

    public SerializableColor(Color color)
    {
        r = color.r;
        g = color.g;
        b = color.b;
        a = color.a;
    }
    public Color GetColor()
    {
        return new Color(r, g, b, a);
    }
}
