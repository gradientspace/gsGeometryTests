using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using g3;
using gs;

namespace gsGeometryTests
{
    public static class test_RemesherPro
    {
        public static void basic_test()
        {
            DMesh3 mesh = TestUtil.LoadTestInputMesh("bunny_solid.obj");

            double mine, maxe, avge;
            MeshQueries.EdgeLengthStats(mesh, out mine, out maxe, out avge);

            RemesherPro remesh = new RemesherPro(mesh);
            remesh.SetTargetEdgeLength(avge*0.25);
            remesh.SmoothSpeedT = 1.0;
            remesh.SetProjectionTarget(MeshProjectionTarget.Auto(mesh));

            LocalProfiler p = new LocalProfiler();
            p.Start("iters");

            remesh.ProjectionMode = Remesher.TargetProjectionMode.NoProjection;
            remesh.FastestRemesh();

            //for (int k = 0; k < 20; ++k)
            //    remesh.BasicRemeshPass();

            ////for (int k = 0; k < 20; ++k)
            ////    remesh.RemeshIteration();

            //while (remesh.FastSplitIteration() > 0) { };
            //remesh.ResetQueue();
            //for (int k = 0; k < 20; ++k)
            //    remesh.RemeshIteration();

            p.Stop("iters");
            System.Console.WriteLine(p.AllTimes());

            TestUtil.WriteTestOutputMesh(mesh, "remesh_pro.obj");

        }




        public static void boundary_test()
        {
            DMesh3 mesh = TestUtil.LoadTestInputMesh("bunny_open.obj");
            MeshBoundaryLoops startLoops = new MeshBoundaryLoops(mesh);

            double mine, maxe, avge;
            MeshQueries.EdgeLengthStats(mesh, out mine, out maxe, out avge);

            RemesherPro remesh = new RemesherPro(mesh);
            //remesh.SetTargetEdgeLength(avge * 0.25);
            remesh.SetTargetEdgeLength(3);
            remesh.SmoothSpeedT = 0.5;
            remesh.EnableSmoothing = true;
            remesh.SetProjectionTarget(MeshProjectionTarget.Auto(mesh));
            remesh.PreventNormalFlips = true;

            //MeshConstraintUtil.PreserveBoundaryLoops(remesh);
            MeshConstraintUtil.FixAllBoundaryEdges(remesh);

            LocalProfiler p = new LocalProfiler();
            p.Start("iters");

            //remesh.ProjectionMode = Remesher.TargetProjectionMode.NoProjection;
            remesh.FastestRemesh();

            p.Stop("iters");
            System.Console.WriteLine(p.AllTimes());

            MeshBoundaryLoops endLoops = new MeshBoundaryLoops(mesh);

            TestUtil.WriteTestOutputMesh(mesh, "remesh_boundary_test.obj");

        }





        public static void crease_test()
        {
            DMesh3 mesh = TestUtil.LoadTestInputMesh("cylinder_orig.obj");
            //DMesh3 mesh = TestUtil.LoadTestInputMesh("cube.obj");

            RemesherPro remesh = new RemesherPro(mesh);
            //remesh.SetTargetEdgeLength(avge * 0.25);
            remesh.SetTargetEdgeLength(0.5);
            remesh.SmoothSpeedT = 0.5;
            remesh.SetProjectionTarget(MeshProjectionTarget.Auto(mesh));
            remesh.PreventNormalFlips = true;

            remesh.SetExternalConstraints(new MeshConstraints());

            MeshTopology topo = new MeshTopology(mesh);
            topo.AddRemeshConstraints(remesh.Constraints);

            LocalProfiler p = new LocalProfiler();
            p.Start("iters");

            //remesh.ProjectionMode = Remesher.TargetProjectionMode.NoProjection;
            remesh.FastestRemesh();

            p.Stop("iters");
            System.Console.WriteLine(p.AllTimes());

            TestUtil.WriteTestOutputMesh(mesh, "remesh_crease_test.obj");

        }






        public static void upres_test()
        {
            DMesh3 mesh = StandardMeshReader.ReadMesh("C:\\scratch\\bust_on_pedestal_500k.obj");

            double target_e = 0.05;

            RemesherPro remesh = new RemesherPro(mesh);
            remesh.SetTargetEdgeLength(target_e);
            remesh.SmoothSpeedT = 0.5;
            remesh.SetProjectionTarget(MeshProjectionTarget.Auto(mesh));

            LocalProfiler p = new LocalProfiler();
            p.Start("iters");

            while (remesh.FastSplitIteration() > 0) { };
            remesh.ResetQueue();
            for (int k = 0; k < 20; ++k)
                remesh.RemeshIteration();

            p.Stop("iters");
            System.Console.WriteLine(p.AllTimes());

            TestUtil.WriteTestOutputMesh(mesh, string.Format("upres_bust_{0}.obj", mesh.TriangleCount));

        }

    }
}
