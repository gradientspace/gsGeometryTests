using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using g3;
using gs;

namespace gsGeometryTests
{
    public static class test_Reproject
    {


        public static void basic_test()
        {
            DMesh3 target = TestUtil.LoadTestInputMesh("twocylinder_orig.obj");
            DMesh3 mesh = TestUtil.LoadTestInputMesh("twocylinder_approx.obj");

            DMeshAABBTree3 targetSpatial = new DMeshAABBTree3(target, true);

            ConstantMeshSourceOp sourceOp = new ConstantMeshSourceOp(mesh, true, true);
            ConstantMeshSourceOp targetOp = new ConstantMeshSourceOp(target, true, true);

            RemesherPro remesher = new RemesherPro(mesh);
            remesher.SetTargetEdgeLength(0.5f);
            //remesher.MinEdgeLength = 0.25;
            remesher.SmoothSpeedT = 0.5f;
            var ProjTarget = new MeshProjectionTarget(target, targetSpatial);
            remesher.SetProjectionTarget(ProjTarget);

            remesher.SharpEdgeReprojectionRemesh(20, 40);

            TestUtil.WriteTestOutputMesh(mesh, "reproject_cylinder.obj");
        }




        public static void mask_test()
        {
            DMesh3 input = TestUtil.LoadTestMesh("C:\\meshes\\user_bugs\\mask_solid.obj");
            //DMesh3 input = TestUtil.LoadTestMesh("C:\\meshes\\user_bugs\\mask_highres_region_1.obj");
            ConstantMeshSourceOp sourceOp = new ConstantMeshSourceOp(input, false, true);

            System.Console.WriteLine("finished reading...");

            RemeshOp remesh = new RemeshOp();
            remesh.MeshSource = sourceOp;
            remesh.PreserveCreases = true;
            //remesh.CreaseAngle = 60;
            //remesh.RemeshRounds = 1;
            //remesh.EnableSplits = false;
            //remesh.EnableCollapses = true;
            //remesh.EnableFlips = false;
            //remesh.EnableSmoothing = false;

            remesh.TargetEdgeLength = 0.5;

            DMesh3 result = remesh.ExtractDMesh();

            System.Console.WriteLine("finished remeshing...");


            Util.WriteDebugMesh(result, "c:\\meshes\\user_bugs\\mask_solid_remesh.obj");
        }




    }
}
