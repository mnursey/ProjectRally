﻿using System;
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

    public List<Color> chunkColorPalette = new List<Color>();

    private bool generateTerrain = false;
    private bool finishedGeneration = false;
    private int genX = 0;
    private int genY = 0;
    private int genZ = 0;

    // Start is called before the first frame update
    void Start()
    {
        BeginTerrainGeneration();
    }

    // Update is called once per frame
    void Update()
    {
        //if (!EditorApplication.isPlaying && updateInEditor)
        //{
        //    updateInEditor = false;
        //    GenerateTerrain();
        //}
        
        if(generateTerrain && !finishedGeneration)
        {
            GenerateTerrain();
        }

        if(reset)
        {
            reset = false;
            chunks = new List<List<Chunk>>();
        }
    }

    int Generator(int x, int y, int z)
    {
        Vector3[] generatorOctaves = new Vector3[] {
            // Frequency, Amplitude, Offset
            new Vector3(0.01f, 10f, 0f),
            new Vector3(0.05f, 3f, 0f),
            new Vector3(0.1f, 1f, 0f),
        };

        float height = 0.0f;

        foreach(Vector3 v in generatorOctaves)
        {
            height += Mathf.PerlinNoise((x + v.z) * v.x, (z + v.z) * v.x) * v.y;
        }

        if (y <= height)
        {
            if (height < 4f)
            {
                return 2;
            }

            return 1;

        } else
        {
            return 0;
        }
    }

    int[,,] GetChunkVoxels(int chunkX, int chunkY, int chunkZ)
    {
        int[,,] voxels = new int[Chunk.chunkSize, Chunk.chunkSize, Chunk.chunkSize];

        for (int x = 0; x < voxels.GetLength(0); x++)
        {
            for (int y = 0; y < voxels.GetLength(1); y++)
            {
                for (int z = 0; z < voxels.GetLength(2); z++)
                {
                    voxels[x, y, z] = Generator(x + chunkX * Chunk.chunkSize, y + chunkY * Chunk.chunkSize, z + chunkZ * Chunk.chunkSize);
                }
            }
        }

        return voxels;
    }

    public void BeginTerrainGeneration()
    {
        generateTerrain = true;
        finishedGeneration = false;


        foreach (List<Chunk> l in chunks.ToArray())
        {
            foreach (Chunk c in l.ToArray())
            {
                DestroyImmediate(c.gameObject);
            }
        }

        chunks = new List<List<Chunk>>();

        chunks.Add(new List<Chunk>());

        genX = 0;
        genY = 0;
        genZ = 0;
    }

    public void GenerateTerrain()
    {
        // Create chunks
        // Set each chunks terrain -> use terrain generator
        // Render each chunk

        if(genX < numberOfChunksX && genZ < numberOfChunksZ)
        {
            float chunkOffset = (Chunk.chunkSize * voxelSize / 2f);
            GameObject newChunkGameObject = Instantiate(chunkPrefab, new Vector3(genX * chunkOffset - (Chunk.chunkSize * voxelSize / 2f * numberOfChunksX / 2f) - this.transform.position.x, this.transform.position.y, genZ * chunkOffset - (Chunk.chunkSize * voxelSize / 2f * numberOfChunksZ / 2f) - this.transform.position.z), Quaternion.identity);
            newChunkGameObject.transform.parent = this.transform;

            Chunk chunk = newChunkGameObject.GetComponent<Chunk>();

            chunks[genX].Add(chunk);

            chunk.SetVoxels(GetChunkVoxels(genX, 0, genZ));
            chunk.SetColorPalette(chunkColorPalette);
            chunk.EnableHightColorCylce(true);
            chunk.GenerateChunk();

            genZ++;

            if(genZ == numberOfChunksZ)
            {
                genX++;
                genZ = 0;

                if(genX < numberOfChunksX)
                {
                    chunks.Add(new List<Chunk>());
                }
            }
        }
    }
}
