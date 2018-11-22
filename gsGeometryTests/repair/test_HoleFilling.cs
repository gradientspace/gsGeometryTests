using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using gs;
using g3;

namespace gsGeometryTests
{
    public static class test_HoleFilling
    {
        public static void test_smooth_fill()
        {
            //DMesh3 mesh = TestUtil.LoadTestInputMesh("n_holed_bunny.obj");
            DMesh3 mesh = TestUtil.LoadTestInputMesh("crazyhole.obj");
            double mine, maxe, avge;
            MeshQueries.EdgeLengthStats(mesh, out mine, out maxe, out avge);

            MeshBoundaryLoops loops = new MeshBoundaryLoops(mesh);
            foreach ( EdgeLoop loop in loops ) {
                SmoothedHoleFill fill = new SmoothedHoleFill(mesh, loop);
                fill.TargetEdgeLength = avge;
                fill.SmoothAlpha = 1.0f;
                fill.InitialRemeshPasses = 50;
                fill.EnableLaplacianSmooth = true;
                fill.ConstrainToHoleInterior = true;
                fill.Apply();
            }

            TestUtil.WriteTestOutputMesh(mesh, "smooth_fill_result.obj");
        }



        public static void test_auto_fill()
        {
            DMesh3 mesh = TestUtil.LoadTestInputMesh("autofill_cases.obj");
            //DMesh3 mesh = TestUtil.LoadTestInputMesh("autofill_tempcase1.obj");
            //DMesh3 mesh = TestUtil.LoadTestInputMesh("autofill_planarcase.obj");
            //DMesh3 mesh = TestUtil.LoadTestInputMesh("autofill_box_edge_strip.obj");
            //DMesh3 mesh = TestUtil.LoadTestInputMesh("autofill_box_edge_strip_2.obj");
            //DMesh3 mesh = TestUtil.LoadTestInputMesh("crazyhole.obj");

            double mine, maxe, avge;
            MeshQueries.EdgeLengthStats(mesh, out mine, out maxe, out avge);

            int ROUNDS = 1;
            for (int k = 0; k < ROUNDS; ++k) {
                MeshBoundaryLoops loops = new MeshBoundaryLoops(mesh);
                foreach (EdgeLoop loop in loops) {
                    AutoHoleFill fill = new AutoHoleFill(mesh, loop);
                    fill.TargetEdgeLength = avge;
                    fill.Apply();
                }
                if (k == ROUNDS - 1)
                    continue;

                RemesherPro r = new RemesherPro(mesh);
                r.SetTargetEdgeLength(avge);
                for ( int j = 0; j < 10; ++j )
                    r.FastSplitIteration();

                MergeCoincidentEdges merge = new MergeCoincidentEdges(mesh);
                merge.Apply();
            }

            TestUtil.WriteTestOutputMesh(mesh, "autofill_result.obj");

            //DMesh3 mesh = new DMesh3();
            //SphericalFibonacciPointSet ps = new SphericalFibonacciPointSet();
            //for (int k = 0; k < ps.N; ++k) {
            //    MeshEditor.AppendBox(mesh, ps[k], ps[k], 0.05f);
            //}
            //Random r = new Random(31337);
            //Vector3d[] pts = TestUtil.RandomPoints3(10000, r, Vector3d.Zero);
            //foreach ( Vector3d pt in pts ) {
            //    pt.Normalize();
            //    int nearest = ps.NearestPoint(pt, true);
            //    Vector3d p = ps[nearest];
            //    MeshEditor.AppendLine(mesh, new Segment3d(p, p + 0.25f * pt), 0.01f);
            //}
            //TestUtil.WriteTestOutputMesh(mesh, "fibonacci.obj");
        }

    }
}
