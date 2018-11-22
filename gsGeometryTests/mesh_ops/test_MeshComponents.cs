using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using gs;
using g3;

namespace gsGeometryTests
{
    public static class test_MeshComponents
    {
        public static void test_sort_mesh_components()
        {
            DMesh3 mesh = TestUtil.LoadTestInputMesh("bunny_nested_spheres.obj");

            MeshConnectedComponents components = new MeshConnectedComponents(mesh);
            components.FindConnectedT();
            DSubmesh3Set componentMeshes = new DSubmesh3Set(mesh, components);

            LocalProfiler p = new LocalProfiler();
            p.Start("sort");

            MeshSpatialSort sorter = new MeshSpatialSort();
            foreach (DSubmesh3 submesh in componentMeshes)
                sorter.AddMesh(submesh.SubMesh, submesh);
            sorter.Sort();

            p.Stop("sort");
            System.Console.WriteLine(p.AllTimes());

            DMesh3 resultMesh = new DMesh3();
            foreach (var solid in sorter.Solids) {
                if ( solid.Outer.InsideOf.Count == 0 )
                    MeshEditor.Append(resultMesh, solid.Outer.Mesh);
            }

            TestUtil.WriteTestOutputMesh(resultMesh, "mesh_components.obj");
        }
    }
}
