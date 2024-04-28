using System.Collections;
using System.Collections.Generic;
using ProceduralMeshes;
using ProceduralMeshes.Generators;
using ProceduralMeshes.Streams;
using UnityEngine;
using UnityEngine.Rendering;

using static Noise;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ProceduralSurface : MonoBehaviour {

    static AdvancedMeshJobScheduleDelegate[] meshJobs = {
        MeshJob<SquareGrid, SingleStream>.ScheduleParallel, 
        MeshJob<SharedSquareGrid, SingleStream>.ScheduleParallel, 
        MeshJob<SharedTriangleGrid, SingleStream>.ScheduleParallel,
        MeshJob<FlatHexagonGrid, SingleStream>.ScheduleParallel,
        MeshJob<PointyHexagonGrid, SingleStream>.ScheduleParallel, 
        MeshJob<CubeSphere, SingleStream>.ScheduleParallel, 
        MeshJob<SharedCubeSphere, PositionStream>.ScheduleParallel, 
        MeshJob<Icosphere, PositionStream>.ScheduleParallel, 
        MeshJob<GeoIcosphere, PositionStream>.ScheduleParallel, 
        MeshJob<Octasphere, SingleStream>.ScheduleParallel, 
        MeshJob<GeoOctasphere, SingleStream>.ScheduleParallel, 
        MeshJob<UVSphere, SingleStream>.ScheduleParallel
    };

    public enum MeshType {
        SquareGrid, SharedSquareGrid, SharedTriangleGrid, 
        FlatHexagonGrid, PointyHexagonGrid, CubeSphere, SharedCubeSphere, 
        Icosphere, GeoIcosphere, Octasphere, GeoOctasphere, UVSphere
    };

    static SurfaceJobScheduleDelegate[,] surfaceJobs = {
        {
            SurfaceJob<Lattice1D<LatticeNormal, Perlin>>.ScheduleParallel,
            SurfaceJob<Lattice2D<LatticeNormal, Perlin>>.ScheduleParallel,
            SurfaceJob<Lattice3D<LatticeNormal, Perlin>>.ScheduleParallel
        },
        {
            SurfaceJob<Lattice1D<LatticeNormal, Smoothstep<Turbulence<Perlin>>>>
                .ScheduleParallel,
            SurfaceJob<Lattice2D<LatticeNormal, Smoothstep<Turbulence<Perlin>>>>
                .ScheduleParallel,
            SurfaceJob<Lattice3D<LatticeNormal, Smoothstep<Turbulence<Perlin>>>>
                .ScheduleParallel
        }, 
        {
            SurfaceJob<Lattice1D<LatticeNormal, Value>>.ScheduleParallel,
            SurfaceJob<Lattice2D<LatticeNormal, Value>>.ScheduleParallel,
            SurfaceJob<Lattice3D<LatticeNormal, Value>>.ScheduleParallel
        }, 
        {
            SurfaceJob<Simplex1D<Simplex>>.ScheduleParallel,
            SurfaceJob<Simplex2D<Simplex>>.ScheduleParallel,
            SurfaceJob<Simplex3D<Simplex>>.ScheduleParallel
        },
        {
            SurfaceJob<Simplex1D<Smoothstep<Turbulence<Simplex>>>>.ScheduleParallel,
            SurfaceJob<Simplex2D<Smoothstep<Turbulence<Simplex>>>>.ScheduleParallel,
            SurfaceJob<Simplex3D<Smoothstep<Turbulence<Simplex>>>>.ScheduleParallel
        }, 
        {
            SurfaceJob<Simplex1D<Value>>.ScheduleParallel,
            SurfaceJob<Simplex2D<Value>>.ScheduleParallel,
            SurfaceJob<Simplex3D<Value>>.ScheduleParallel
        },
        {
            SurfaceJob<Voronoi1D<LatticeNormal, Worley, F1>>.ScheduleParallel,
            SurfaceJob<Voronoi2D<LatticeNormal, Worley, F1>>.ScheduleParallel,
            SurfaceJob<Voronoi3D<LatticeNormal, Worley, F1>>.ScheduleParallel
        },
        {
            SurfaceJob<Voronoi1D<LatticeNormal, Worley, F2>>.ScheduleParallel,
            SurfaceJob<Voronoi2D<LatticeNormal, Worley, F2>>.ScheduleParallel,
            SurfaceJob<Voronoi3D<LatticeNormal, Worley, F2>>.ScheduleParallel
        },
        {
            SurfaceJob<Voronoi1D<LatticeNormal, Worley, F2MinusF1>>.ScheduleParallel,
            SurfaceJob<Voronoi2D<LatticeNormal, Worley, F2MinusF1>>.ScheduleParallel,
            SurfaceJob<Voronoi3D<LatticeNormal, Worley, F2MinusF1>>.ScheduleParallel
        }
    };

    public enum NoiseType {
        Perlin, PerlinSmoothTurbulence, PerlinValue, 
        Simplex, SimplexSmoothTurbulence, SimplexValue, 
        VoronoiWorleyF1, VoronoiWorleyF2, VoronoiWorleyF2MinusF1
    }

    [SerializeField]
    NoiseType noiseType;

    [SerializeField, Range(1, 3)]
    int dimensions = 1;

    [SerializeField]
    MeshType meshType;

    [SerializeField]
    bool recalculateNormals, recalculateTangents;

    Mesh mesh;

    [SerializeField, Range(1, 50)]
    int resolution = 1;

    [SerializeField, Range(-1f, 1f)]
    float displacement = 0.5f;

    [SerializeField]
    Settings noiseSettings = Settings.Default;

    [SerializeField]
    SpaceTRS domain = new SpaceTRS {
        scale = 1f
    };

    [System.NonSerialized]
    Vector3[] vertices, normals;

    [System.NonSerialized]
    Vector4[] tangents;

    [System.Flags]
    public enum GizmoMode { 
        Nothing = 0, Vertices = 1, Normals = 0b10, Tangents = 0b100, Triangles = 0b1000 
    }

    [SerializeField]
    GizmoMode gizmos;

    public enum MaterialMode { Displacement, Flat, LatLonMap, CubeMap }

    [SerializeField]
    MaterialMode material;

    [SerializeField]
    Material[] materials;

    [System.NonSerialized]
    int[] triangles;

    [System.Flags]
    public enum MeshOptimizationMode {
        Nothing = 0, ReorderIndices = 1, ReorderVertices = 0b10
    }

    [SerializeField]
    MeshOptimizationMode meshOptimization;

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

        vertices = null;
        normals = null;
        tangents = null;
        triangles = null;

        GetComponent<MeshRenderer>().material = materials[(int)material];
    }

    void GenerateMesh () {
        Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
        Mesh.MeshData meshData = meshDataArray[0];

        
        surfaceJobs[(int)noiseType, dimensions - 1](
            meshData, resolution, noiseSettings, domain, displacement, 
            meshJobs[(int)meshType](
                mesh, meshData, resolution, default, 
                new Vector3(0f, Mathf.Abs(displacement)), true
            )
        ).Complete();

        Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);

        if (recalculateNormals) {
            mesh.RecalculateNormals();
        }
        if (recalculateTangents) {
            mesh.RecalculateTangents();
        }

        if (meshOptimization == MeshOptimizationMode.ReorderIndices) {
            mesh.OptimizeIndexBuffers();
        }
        else if (meshOptimization == MeshOptimizationMode.ReorderVertices) {
            mesh.OptimizeReorderVertexBuffer();
        }
        else if (meshOptimization != MeshOptimizationMode.Nothing) {
            mesh.Optimize();
        }
    }

    void OnDrawGizmos () {
        if (gizmos == GizmoMode.Nothing || mesh == null) {
            return;
        }

        bool drawVertices = (gizmos & GizmoMode.Vertices) != 0;
        bool drawNormals = (gizmos & GizmoMode.Normals) != 0;
        bool drawTangents = (gizmos & GizmoMode.Tangents) != 0;
        bool drawTriangles = (gizmos & GizmoMode.Triangles) != 0;

        if (vertices == null) {
            vertices = mesh.vertices;
        }
        if (drawNormals && normals == null) {
            drawNormals = mesh.HasVertexAttribute(VertexAttribute.Normal);
            if (drawNormals) {
                normals = mesh.normals;
            }
        }
        if (drawTangents && tangents == null) {
            drawTangents = mesh.HasVertexAttribute(VertexAttribute.Tangent);
            if (drawTangents) {
                tangents = mesh.tangents;
            }
        }
        if (drawTriangles && triangles == null) {
            triangles = mesh.triangles;
        }

        //Gizmos.color = Color.cyan;
        Transform t = transform;
        for (int i = 0; i < vertices.Length; i++) {
            Vector3 position = t.TransformPoint(vertices[i]);
            if (drawVertices) {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(position, 0.02f);
            }
            if (drawNormals) {
                Gizmos.color = Color.green;
                Gizmos.DrawRay(position, t.TransformDirection(normals[i]) * 0.2f);
            }
            if (drawTangents) {
                Gizmos.color = Color.red;
                Gizmos.DrawRay(position, t.TransformDirection(tangents[i]) * 0.2f);
            }
        }

        if (drawTriangles) {
            float colorStep = 1f / (triangles.Length - 3);
            for (int i = 0; i < triangles.Length; i += 3) {
                float c = i * colorStep;
                Gizmos.color = new Color(c, 0f, c);
                Gizmos.DrawSphere(
                    t.TransformPoint((
                        vertices[triangles[i]] +
                        vertices[triangles[i + 1]] +
                        vertices[triangles[i + 2]]
                    ) * (1f / 3f)), 
                    0.02f
                );
            }
        }
    }
}
