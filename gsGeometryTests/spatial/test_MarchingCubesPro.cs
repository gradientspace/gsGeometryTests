using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using g3;
using gs;

namespace gsGeometryTests
{
    public static class test_MarchingCubesPro
    {
        public static void test_1()
        {
            DMesh3 mesh = TestUtil.LoadTestInputMesh("bunny_solid.obj");

            int num_cells = 64;
            double cell_size = mesh.CachedBounds.MaxDim / num_cells;
            MeshSignedDistanceGrid sdf = new MeshSignedDistanceGrid(mesh, cell_size) {
                ExactBandWidth = 5
            };
            sdf.Compute();

            var iso = new DenseGridTrilinearImplicit(sdf.Grid, sdf.GridOrigin, sdf.CellSize);
            var skel_field = new DistanceFieldToSkeletalField() {
                DistanceField = iso, FalloffDistance = 5 * cell_size
            };
            var offset_field = new ImplicitOffset3d() {
                A = skel_field, Offset = DistanceFieldToSkeletalField.ZeroIsocontour
            };


            MarchingCubesPro c = new MarchingCubesPro();
            //c.Implicit = iso;
            //c.Implicit = skel_field;
            //c.IsoValue = DistanceFieldToSkeletalField.ZeroIsocontour;
            c.Implicit = offset_field;

            c.Bounds = mesh.CachedBounds;
            c.CubeSize = c.Bounds.MaxDim / 128;
            c.Bounds.Expand(3 * c.CubeSize);
            //c.RootMode = MarchingCubesPro.RootfindingModes.Bisection;
            c.ParallelCompute = false;

            c.Generate();
            //c.GenerateContinuation(mesh.Vertices());

            TestUtil.WriteTestOutputMesh(c.Mesh, "mcpro_output.obj");
        }




        public static void test_2()
        {
            DMesh3 mesh = TestUtil.LoadTestInputMesh("bunny_solid.obj");
            DMeshAABBTree3 spatial = new DMeshAABBTree3(mesh, true);
            int num_cells = 64;
            double cell_size = mesh.CachedBounds.MaxDim / num_cells;

            CachingMeshSDF sdf = new CachingMeshSDF(mesh, cell_size, spatial);
            sdf.MaxOffsetDistance = (float)(4 * cell_size);
            sdf.Initialize();

            gParallel.ForEach(sdf.Grid.Indices(), (idx) => {
                sdf.GetValue(idx);
            });

            CachingMeshSDFImplicit sdf_iso = new CachingMeshSDFImplicit(sdf);
            var skel_field = new DistanceFieldToSkeletalField() {
                DistanceField = sdf_iso, FalloffDistance = 5*cell_size
            };

            MarchingCubesPro c = new MarchingCubesPro();
            //c.Implicit = sdf_iso;
            c.Implicit = skel_field;
            //c.IsoValue = DistanceFieldToSkeletalField.ZeroIsocontour;
            c.Bounds = mesh.CachedBounds;
            c.CubeSize = c.Bounds.MaxDim / 128;
            c.Bounds.Expand(3 * c.CubeSize);
            c.RootMode = MarchingCubesPro.RootfindingModes.Bisection;
            c.ParallelCompute = false;

            c.Generate();
            //c.GenerateContinuation(mesh.Vertices());

            c.Mesh.ReverseOrientation();

            TestUtil.WriteTestOutputMesh(c.Mesh, "mcpro_output.obj");
        }

    }
}
