using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

using static Unity.Mathematics.math;

namespace ProceduralMeshes.Generators {
    public struct Octasphere : IMeshGenerator {

        struct Rhombus {
            public int id;
            public float3 uvOrigin, uVector, vVector;
            public int seamStep;

            public bool TouchesMinimumPole => (id & 1) == 0;
        }

        public int VertexCount => 4 * Resolution * Resolution + 2 * Resolution + 7;

        public int IndexCount => 6 * 4 * Resolution * Resolution;

        public int JobLength => 4 * Resolution + 1;

        public Bounds Bounds => new Bounds(Vector3.zero, new Vector3(2f, 2f, 2f));

        public int Resolution { get; set; }

        static Rhombus GetRhombus (int id) => id switch {
            0 => new Rhombus {
                id = id,
                uvOrigin = -1f,
                uVector = 2f * right(),
                vVector = 2f * up(), 
                seamStep = 4
            },
            1 => new Rhombus {
                id = id,
                uvOrigin = float3(1f, -1f, -1f),
                uVector = 2f * forward(),
                vVector = 2f * up(),
                seamStep = 4
            }, 
            2 => new Rhombus {
                id = id,
                uvOrigin = -1f,
                uVector = 2f * forward(),
                vVector = 2f * right(),
                seamStep = -2
            }, 
            3 => new Rhombus {
                id = id,
                uvOrigin = float3(-1f, -1f, 1f),
                uVector = 2f * up(),
                vVector = 2f * right(),
                seamStep = -2
            }, 
            4 => new Rhombus {
                id = id,
                uvOrigin = -1f,
                uVector = 2f * up(),
                vVector = 2f * forward(),
                seamStep = -2
            }, 
            _ => new Rhombus {
                id = id,
                uvOrigin = float3(-1f, 1f, -1f),
                uVector = 2f * right(),
                vVector = 2f * forward(),
                seamStep = -2
            }
        };

        public void Execute<S> (int i, S streams) where S : struct, IMeshStreams {
            int u = i / 4;
            Rhombus rhombus = GetRhombus(i - 4 * u);
            int vi = Resolution * (Resolution * rhombus.id + u + 2) + 7;
            int ti = 2 * Resolution * (Resolution * rhombus.id + u);
            bool firstColumn = u == 0;
            u += 1;

            var vertex = new Vertex();

            streams.SetVertex(vi, vertex);

            vi += 1;

            for (int v = 1; v < Resolution; v++, vi++, ti += 2) {
                streams.SetVertex(vi, vertex);

                streams.SetTriangle(ti + 0, 0);
                streams.SetTriangle(ti + 1, 0);
            }

            streams.SetTriangle(ti + 0, 0);
            streams.SetTriangle(ti + 1, 0);
        }
    }
}
