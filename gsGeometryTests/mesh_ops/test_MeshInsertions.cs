using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using gs;
using g3;

namespace gsGeometryTests
{
    public static class test_MeshInsertions
    {
        public static void testInsertPolygon_PlanarProj()
        {
            double dscale = 1.0;
            DMesh3 mesh = TestUtil.LoadTestInputMesh("bunny_solid.obj");  dscale = 0.3;
            //DMesh3 mesh = TestUtil.LoadTestInputMesh("cylinder.obj");
            //DMesh3 mesh = TestUtil.LoadTestInputMesh("cube.obj");
            double size = mesh.CachedBounds.MaxDim;
            Vector3d c = mesh.CachedBounds.Center;
            Vector3d fw = c + mesh.CachedBounds.DiagonalLength * 2 * Vector3d.AxisZ;
            Ray3d ray = new Ray3d(fw, (c - fw).Normalized);

            // projection frame and polygon that lives in this frame
            Frame3f projectFrame = new Frame3f(ray.Origin, ray.Direction);
            Polygon2d circle = Polygon2d.MakeCircle(dscale * size * 0.1, 6);


            DMeshAABBTree3 spatial = new DMeshAABBTree3(mesh, true);
            List<int> hitTris = new List<int>();
            spatial.FindAllHitTriangles(ray, hitTris);
            while ( hitTris.Count != 2 ) {
                ray.Origin += 100 * MathUtil.Epsilon * Vector3d.One;
                hitTris.Clear();
                spatial.FindAllHitTriangles(ray, hitTris);
            }

            // insert polygons but don't simplify the result

            DMesh3 noTrimMesh = new DMesh3(mesh);
            List<int[]> noTrimPolyVerts = new List<int[]>();
            List<EdgeLoop> noTrimLoops = new List<EdgeLoop>();
            foreach (int hit_tid in hitTris) {
                MeshInsertProjectedPolygon insert = new MeshInsertProjectedPolygon(noTrimMesh, circle, projectFrame, hit_tid);
                insert.SimplifyInsertion = false;
                if (insert.Insert()) {
                    noTrimPolyVerts.Add(insert.InsertedPolygonVerts);
                    noTrimLoops.Add(insert.InsertedLoop);
                } else
                    System.Console.WriteLine("testInsertPolygon_PlanarProj: no-trim Insert() failed");
            }
            TestUtil.WriteTestOutputMesh(noTrimMesh, "insert_polygon_notrim.obj");

            // do different-vtx-count stitch
            if (noTrimLoops.Count == 2) {
                noTrimLoops[1].Reverse();
                MeshStitchLoops stitcher = new MeshStitchLoops(noTrimMesh, noTrimLoops[0], noTrimLoops[1]);
                stitcher.TrustLoopOrientations = false;
                stitcher.AddKnownCorrespondences(noTrimPolyVerts[0], noTrimPolyVerts[1]);
                stitcher.Stitch();
            }
            TestUtil.WriteTestOutputMesh(noTrimMesh, "insert_polygon_notrim_joined.obj");


            // now do simplified version, which we can trivially stitch

            List<EdgeLoop> edgeLoops = new List<EdgeLoop>();
            foreach (int hit_tid in hitTris) {
                MeshInsertProjectedPolygon insert = new MeshInsertProjectedPolygon(mesh, circle, projectFrame, hit_tid);
                if (insert.Insert()) {
                    edgeLoops.Add(insert.InsertedLoop);
                } else {
                    System.Console.WriteLine("testInsertPolygon_PlanarProj: Insert() failed");
                }
            }

            //TestUtil.WriteTestOutputMesh(mesh, "insert_polygon_before_stitch.obj");

            // do stitch
            if ( edgeLoops.Count == 2 ) {
                MeshEditor editor = new MeshEditor(mesh);
                EdgeLoop l0 = edgeLoops[0];
                EdgeLoop l1 = edgeLoops[1];
                l1.Reverse();
                editor.StitchLoop(l0.Vertices, l1.Vertices);
            }

            TestUtil.WriteTestOutputMesh(mesh, "insert_polygon_joined.obj");
        }
    }
}
