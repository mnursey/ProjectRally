using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

//[ExecuteInEditMode]
public class TerrainGenerator : MonoBehaviour
{
    public int numberOfChunksX = 6;
    public int numberOfChunksZ = 6;
    public float voxelSize = 1.0f;

    public int seed = 0;
    public GameObject chunkPrefab;
    public List<List<Chunk>> chunks = new List<List<Chunk>>();
    public bool updateInEditor = false;
    public bool reset = false;
    // Start is called before the first frame update
    void Start()
    {
        GenerateTerrain();
    }

    // Update is called once per frame
    void Update()
    {
        //if (!EditorApplication.isPlaying && updateInEditor)
        //{
        //    updateInEditor = false;
        //    GenerateTerrain();
        //}
        
        if(reset)
        {
            reset = false;
            chunks = new List<List<Chunk>>();
        }
    }

    float Generator(int x, int y, int z)
    {
        float height = ((Mathf.Cos(x * 0.5f) * 3f + 7f) + (Mathf.Cos(z * 0.5f) * 3f + 7f)) / 2;

        Debug.Log(height);
        if (y <= height)
        {
            return 1;
        } else
        {
            return 0;
        }
    }

    public void GenerateTerrain()
    {
        // Create chunks
        // Set each chunks terrain -> use terrain generator
        // Render each chunk

        foreach(List<Chunk> l in chunks.ToArray())
        {
            foreach (Chunk c in l.ToArray())
            {
                DestroyImmediate(c.gameObject);
            }
        }

        chunks = new List<List<Chunk>>();

        for (int chunkX = 0; chunkX < numberOfChunksX; chunkX++)
        {
            chunks.Add(new List<Chunk>());

            for (int chunkZ = 0; chunkZ < numberOfChunksZ; chunkZ++)
            {
                float chunkOffset = (Chunk.chunkSize * voxelSize / 2f);
                GameObject newChunk = Instantiate(chunkPrefab, new Vector3(chunkX * chunkOffset - (Chunk.chunkSize * voxelSize / 2f * numberOfChunksX / 2f) - this.transform.position.x, this.transform.position.y, chunkZ * chunkOffset - (Chunk.chunkSize * voxelSize / 2f * numberOfChunksZ / 2f) - this.transform.position.z), Quaternion.identity);
                newChunk.transform.parent = this.transform;
                chunks[chunkX].Add(newChunk.GetComponent<Chunk>());
            }
        }
    }
}
