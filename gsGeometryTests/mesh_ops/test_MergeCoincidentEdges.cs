using System;
using System.Collections.Generic;
using System.Linq;
using g3;
using gs;

namespace gsGeometryTests
{
	public static class test_MergeCoincidentEdges
	{
		public static void basic_tests() {

			DMesh3 mesh = TestUtil.LoadTestInputMesh("three_edge_crack.obj");
			MergeCoincidentEdges merge = new MergeCoincidentEdges(mesh);
			merge.Apply();
			Util.gDevAssert(mesh.BoundaryEdgeIndices().Count() == 0);
            mesh.CheckValidity(true, FailMode.DebugAssert);
			TestUtil.WriteTestOutputMesh(mesh, "three_edge_crack_merged.obj");

			DMesh3 mesh2 = TestUtil.LoadTestInputMesh("crack_loop.obj");
			MergeCoincidentEdges merge2 = new MergeCoincidentEdges(mesh2);
			merge2.Apply();
			Util.gDevAssert(mesh2.BoundaryEdgeIndices().Count() == 0);
            mesh2.CheckValidity(true, FailMode.DebugAssert);
            TestUtil.WriteTestOutputMesh(mesh2, "crack_loop_merged.obj");

			DMesh3 mesh3 = TestUtil.LoadTestInputMesh("cracks_many.obj");
			MergeCoincidentEdges merge3 = new MergeCoincidentEdges(mesh3);
			merge3.Apply();
			Util.gDevAssert(mesh3.BoundaryEdgeIndices().Count() == 0);
            mesh3.CheckValidity(true, FailMode.DebugAssert);
            TestUtil.WriteTestOutputMesh(mesh3, "cracks_many_merged.obj");

			DMesh3 mesh4 = TestUtil.LoadTestInputMesh("cracks_duplicate_edge.obj");
			MergeCoincidentEdges merge4 = new MergeCoincidentEdges(mesh4);
			merge4.Apply();
			Util.gDevAssert(mesh4.BoundaryEdgeIndices().Count() == 0);
            mesh4.CheckValidity(true, FailMode.DebugAssert);
            TestUtil.WriteTestOutputMesh(mesh4, "cracks_duplicate_edge_merged.obj");
		}



        public static void hard_test()
        {
            //DMesh3 mesh = TestUtil.LoadTestInputMesh("three_edge_crack.obj");
            DMesh3 mesh = TestUtil.LoadTestMesh("c:\\scratch\\VTX_Scan_removeocc.obj");
            //MeshEditor editor = new MeshEditor(mesh);
            //editor.DisconnectAllBowties(100);

            mesh.CheckValidity(true, FailMode.DebugAssert);
            MergeCoincidentEdges merge = new MergeCoincidentEdges(mesh);
            merge.Apply();
            mesh.CheckValidity(true, FailMode.DebugAssert);
            TestUtil.WriteTestOutputMesh(mesh, "vtx_scan_merged.obj");
        }


	}
}
