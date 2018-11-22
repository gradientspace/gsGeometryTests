using System;
using System.Collections.Generic;
using System.Linq;
using g3;
using gs;

namespace gsGeometryTests
{
	class Program
	{
		public static string TEST_FILES_PATH {
			get { return Util.IsRunningOnMono() ? "../../../test_files/" : "..\\..\\..\\test_files\\"; }
		}
		public static string TEST_OUTPUT_PATH {
			get { return Util.IsRunningOnMono() ? "../../../test_output/" : "..\\..\\..\\test_output\\"; }
		}

		public static void Main(string[] args)
		{
            //test_MergeCoincidentEdges.basic_tests();
            //test_MergeCoincidentEdges.hard_test();

            //test_RemesherPro.basic_test();
            //test_RemesherPro.boundary_test();
            //test_RemesherPro.crease_test();
            //test_RemesherPro.upres_test();

            //test_Reproject.basic_test();
            //test_Reproject.mask_test();

            //test_MeshInsertions.testInsertPolygon_PlanarProj();

            //test_WindingGrid.test_winding_grid();
            //test_WindingGrid.test_fast_winding_grid();
            //test_WindingGrid.test_fast_winding_implicit();

            //test_MeshComponents.test_sort_mesh_components();

            //test_HoleFilling.test_smooth_fill();
            //test_HoleFilling.test_auto_fill();

            //test_AutoRepair.test_repair_sample();
            //test_AutoRepair.test_repair_file();
            //test_AutoRepair.test_autorepair_thingi10k();

            test_MarchingCubesPro.test_1();
            //test_MarchingCubesPro.test_2();

            //quick_test_2();

            //test_Thing10k.test_repair_all();
            //test_Thing10k.test_specific_file();
            //test_Thing10k.test_write_solids();

            //DGTest.test(Console.WriteLine);

            //quick_test();
            //test_compact_in_place();

            System.Console.WriteLine("Hit enter to exit");
            System.Console.ReadLine();
		}



		public static void test_orientation_repair()
		{
			DMesh3 mesh = TestUtil.LoadTestInputMesh("bunny_orientation1.obj");

			MeshRepairOrientation orient = new MeshRepairOrientation(mesh);
			orient.OrientComponents();

			orient.SolveGlobalOrientation();

			TestUtil.WriteTestOutputMesh(mesh, "bunny_oriented.obj");
		}



        public static void quick_test_2()
        {
            DMesh3 target = TestUtil.LoadTestInputMesh("cylinder_orig.obj");
            DMeshAABBTree3 targetSpatial = new DMeshAABBTree3(target, true);

            DMesh3 mesh = TestUtil.LoadTestInputMesh("cylinder_approx.obj");
            DMeshAABBTree3 meshSpatial = new DMeshAABBTree3(mesh, true);

            double search_dist = 10.0;

            MeshTopology topo = new MeshTopology(target);
            topo.Compute();

            RemesherPro r = new RemesherPro(mesh);
            r.SetTargetEdgeLength(2.0);
            r.SmoothSpeedT = 0.5;
            r.SetProjectionTarget(MeshProjectionTarget.Auto(target));
            MeshConstraints cons = new MeshConstraints();
            r.SetExternalConstraints(cons);


            int set_id = 1;
            foreach (var loop in topo.Loops) {
                DCurveProjectionTarget curveTarget = new DCurveProjectionTarget(loop.ToCurve(target));
                set_id++;

                // pick a set of points we will find paths between. We will chain
                // up those paths and constrain them to target loops.
                // (this part is the hack!)
                List<int> target_verts = new List<int>();
                List<int> mesh_verts = new List<int>();
                for (int k = 0; k < loop.VertexCount; k += 5) {
                    target_verts.Add(loop.Vertices[k]);

                    Vector3d vCurve = target.GetVertex(loop.Vertices[k]);
                    int mesh_vid = meshSpatial.FindNearestVertex(vCurve, search_dist);
                    mesh_verts.Add(mesh_vid);
                }
                int NT = target_verts.Count;

                // find the paths to assemble the edge chain
                // [TODO] need to filter out junction vertices? or will they just handle themselves
                //   because they can be collapsed away?
                List<int> vert_seq = new List<int>();
                for ( int k = 0; k < NT; k++) {
                    EdgeSpan e = find_edge_path(mesh, mesh_verts[k], mesh_verts[(k + 1) % NT]);
                    int n = e.Vertices.Length;
                    for (int i = 0; i < n - 1; ++i)
                        vert_seq.Add(e.Vertices[i]);
                }

                // now it's easy, just add the loop constraint
                EdgeLoop full_loop = EdgeLoop.FromVertices(mesh, vert_seq);
                MeshConstraintUtil.ConstrainVtxLoopTo(cons, mesh, full_loop.Vertices, curveTarget, set_id);
            }


            r.FastestRemesh();

            TestUtil.WriteTestOutputMesh(mesh, "curves_test_out.obj");

        }



        public static EdgeSpan find_edge_path(DMesh3 mesh, int v0, int v1)
        {
            if (v0 == v1)
                throw new Exception("same vertices!");

            int eid = mesh.FindEdge(v0, v1);
            if ( eid >= 0 ) {
                return EdgeSpan.FromEdges(mesh, new List<int>() { eid });
            }

            DijkstraGraphDistance dist = DijkstraGraphDistance.MeshVertices(mesh);
            //DijkstraGraphDistance dist = DijkstraGraphDistance.MeshVerticesSparse(mesh);
            dist.AddSeed(v0, 0);
            dist.ComputeToNode(v1);
            List<int> vpath = new List<int>();
            bool bOK = dist.GetPathToSeed(v1, vpath);
            Util.gDevAssert(bOK);
            vpath.Reverse();
            return EdgeSpan.FromVertices(mesh, vpath);
        }




        public static void quick_test()
        {
            DMesh3 mesh = StandardMeshReader.ReadMesh("c:\\scratch\\block.obj");
            DMeshAABBTree3 spatial = new DMeshAABBTree3(mesh, true);

            Vector3d rayCenter = new Vector3d(0, 0, 1);
            Frame3f rayFrame = new Frame3f(rayCenter, Vector3d.AxisZ);

            List<Frame3f> frames = new List<Frame3f>();

            // how far into surface we will inset
            float SurfaceOffset = 0.01f;

            double step = 2.5f;
            for ( double angle = 0; angle < 360; angle += step ) {
                double dx = Math.Cos(angle*MathUtil.Deg2Rad), 
                       dy = Math.Sin(angle * MathUtil.Deg2Rad);
                Vector3d dir = dx*(Vector3d)rayFrame.X + dy* (Vector3d)rayFrame.Y;
                Ray3d ray = new Ray3d(rayFrame.Origin, dir.Normalized);

                Frame3f hitFrame;
                if (MeshQueries.RayHitPointFrame(mesh, spatial, ray, out hitFrame))
                    frames.Add(hitFrame);

            }

            int N = frames.Count;
            for ( int k = 0; k < N; ++k ) {
                Frame3f f = frames[k];
                int prev = (k == 0) ? N - 1 : k - 1;
                int next = (k + 1) % N;
                //Vector3f dv = frames[(k + 1) % frames.Count].Origin - f.Origin;
                Vector3f dv = frames[next].Origin - frames[prev].Origin;
                dv.Normalize();
                f.ConstrainedAlignAxis(0, dv, f.Z);

                f.Origin = f.Origin + SurfaceOffset * f.Z;

                frames[k] = f;
            }

            //Frame3f f = frames[0];
            //Vector3f dv = (frames[1].Origin - frames[0].Origin).Normalized;
            //f.ConstrainedAlignAxis(1, dv, f.Z);
            //for (int k = 1; k < frames.Count; ++k) {
            //    f.Origin = frames[k].Origin;
            //    f.AlignAxis(2, frames[k].Z);
            //    frames[k] = f;
            //}


            List<Vector3d> vertices = frames.ConvertAll((ff) => { return (Vector3d)ff.Origin; });

            TubeGenerator tubegen = new TubeGenerator() {
                Vertices = vertices,
                Polygon = Polygon2d.MakeCircle(0.05, 16),
                NoSharedVertices = false
            };
            DMesh3 tubeMesh = tubegen.Generate().MakeDMesh();

            TestUtil.WriteTestOutputMeshes(new List<IMesh>() { mesh, tubeMesh }, "curve_tube.obj");


            SimpleQuadMesh stripMeshY = new SimpleQuadMesh();
            double w = 0.1;
            int preva = -1, prevb = -1;
            for ( int k = 0; k < N; ++k ) {
                Vector3d pa = frames[k].Origin + w * (Vector3d)frames[k].Y;
                Vector3d pb = frames[k].Origin - w * (Vector3d)frames[k].Y;
                int a = stripMeshY.AppendVertex(pa);
                int b = stripMeshY.AppendVertex(pb);
                if ( preva != -1 ) {
                    stripMeshY.AppendQuad(preva, prevb, b, a);
                }
                preva = a; prevb = b;
            }
            stripMeshY.AppendQuad(preva, prevb, 1, 0);
            SimpleQuadMesh.WriteOBJ(stripMeshY, TEST_OUTPUT_PATH + "quadstripy.obj", WriteOptions.Defaults);



            SimpleQuadMesh stripMeshZ = new SimpleQuadMesh();
            preva = -1; prevb = -1;
            double wz = 0.1;
            for (int k = 0; k < N; ++k) {
                Vector3d pa = frames[k].Origin + wz * (Vector3d)frames[k].Z;
                Vector3d pb = frames[k].Origin - wz * (Vector3d)frames[k].Z;
                int a = stripMeshZ.AppendVertex(pa);
                int b = stripMeshZ.AppendVertex(pb);
                if (preva != -1) {
                    stripMeshZ.AppendQuad(preva, prevb, b, a);
                }
                preva = a; prevb = b;
            }
            stripMeshZ.AppendQuad(preva, prevb, 1, 0);
            SimpleQuadMesh.WriteOBJ(stripMeshZ, TEST_OUTPUT_PATH + "quadstripz.obj", WriteOptions.Defaults);

        }

    }
}
