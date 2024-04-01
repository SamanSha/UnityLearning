using System.Collections;
using System.Collections.Generic;
using ProceduralMeshes;
using ProceduralMeshes.Generators;
using ProceduralMeshes.Streams;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ProceduralMesh : MonoBehaviour {

    static MeshJobScheduleDelegate[] jobs = {
        MeshJob<SquareGrid, SingleStream>.ScheduleParallel, 
        MeshJob<SharedSquareGrid, SingleStream>.ScheduleParallel
    };

    public enum MeshType {
        SquareGrid, SharedSquareGrid
    };

    [SerializeField]
    MeshType meshType;

    Mesh mesh;

    [SerializeField, Range(1, 10)]
    int resolution = 1;

    void Awake () {
        mesh = new Mesh {
            name = "Procedural Mesh"
        };
        //GenerateMesh();
        GetComponent<MeshFilter>().mesh = mesh;
    }

    void OnValidate () => enabled = true;

    void Update () {
        GenerateMesh();
        enabled = false;
    }

    void GenerateMesh () {
        Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
        Mesh.MeshData meshData = meshDataArray[0];

        jobs[(int)meshType](mesh, meshData, resolution, default).Complete();

        Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);
    }
}