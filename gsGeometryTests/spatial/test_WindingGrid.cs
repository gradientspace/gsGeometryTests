using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using g3;
using gs;

namespace gsGeometryTests
{
    public static class test_WindingGrid
    {

        public class WindingGridImplicit : ImplicitFunction3d
        {
            DenseGridTrilinearImplicit WindingGrid;
            float WindingIsoValue;

            public WindingGridImplicit(MeshWindingNumberGrid mwnGrid)
            {
                Initialize(mwnGrid);
            }

            public void Initialize(MeshWindingNumberGrid mwnGrid)
            {
                WindingGrid = new DenseGridTrilinearImplicit(mwnGrid.Grid, mwnGrid.GridOrigin, mwnGrid.CellSize);
                WindingIsoValue = mwnGrid.WindingIsoValue;
            }

            public double Value(ref Vector3d pt)
            {
                double winding = WindingGrid.Value(ref pt);
                if (winding == WindingGrid.Outside)
                    winding = 0;

                // kind of ugly binary field
                //return Math.Abs(winding) > WindingIsoValue ? -Math.Abs(winding) : 1;

                // shift zero-isocontour to winding isovalue, and then flip sign
                return -(winding - WindingIsoValue);

            }
        }




        public class SampledGridImplicit : ImplicitFunction3d
        {
            DenseGridTrilinearImplicit WindingGrid;
            float IsoValue;

            public SampledGridImplicit(MeshScalarSamplingGrid scalarGrid)
            {
                Initialize(scalarGrid);
            }

            public void Initialize(MeshScalarSamplingGrid scalarGrid)
            {
                WindingGrid = new DenseGridTrilinearImplicit(scalarGrid.Grid, scalarGrid.GridOrigin, scalarGrid.CellSize);
                IsoValue = scalarGrid.IsoValue;
            }

            public double Value(ref Vector3d pt)
            {
                double winding = WindingGrid.Value(ref pt);
                if (winding == WindingGrid.Outside)
                    winding = 0;

                // shift zero-isocontour to winding isovalue, and then flip sign
                return -(winding - IsoValue);

            }
        }





        public class FastWindingImplicit : ImplicitFunction3d
        {
            DMeshAABBTreePro Spatial;
            double IsoValue;

            public FastWindingImplicit(DMeshAABBTreePro spatial, double isoValue = 0.5)
            {
                Spatial = spatial;
                spatial.FastWindingNumber(Vector3d.Zero);
                IsoValue = isoValue;
            }

            public double Value(ref Vector3d pt)
            {
                double winding = Spatial.FastWindingNumber(pt);

                // shift zero-isocontour to winding isovalue, and then flip sign
                return -(winding - IsoValue);
            }
        }




        public static void test_winding_grid()
        {
            //Sphere3Generator_NormalizedCube gen = new Sphere3Generator_NormalizedCube() { EdgeVertices = 15, Radius = 5 };
            //CappedCylinderGenerator gen = new CappedCylinderGenerator() { };
            //TrivialBox3Generator gen = new TrivialBox3Generator() { Box = new Box3d(Vector3d.Zero, 5 * Vector3d.One) };
            //DMesh3 mesh = gen.Generate().MakeDMesh();
            //MeshTransforms.Translate(mesh, 5 * Vector3d.One);
            //DMesh3 mesh = TestUtil.LoadTestInputMesh("bunny_solid.obj");
            //DMesh3 mesh = TestUtil.LoadTestInputMesh("holey_bunny.obj");
            DMesh3 mesh = TestUtil.LoadTestInputMesh("holey_bunny_2.obj");
            //DMesh3 mesh = TestUtil.LoadTestMesh("C:\\git\\gsOrthoVRApp\\sample_files\\scan_1_raw.obj");   // use iso 0.25
            //DMesh3 mesh = TestUtil.LoadTestMesh("C:\\scratch\\irongiant.stl");
            //DMesh3 mesh = TestUtil.LoadTestMesh("C:\\Users\\rms\\Dropbox\\meshes\\cars\\beetle.obj");
            //DMesh3 mesh = TestUtil.LoadTestMesh("c:\\scratch\\PigHead_rot90.obj");
            //DMesh3 mesh = TestUtil.LoadTestMesh("C:\\Projects\\nia_files\\testscan2.obj");

            //TestUtil.WriteTestOutputMesh(mesh, "xxx.obj");

            AxisAlignedBox3d meshBounds = mesh.CachedBounds;
            int num_cells = 128;
            double cell_size = meshBounds.MaxDim / num_cells;

            float winding_iso = 0.35f;

            DMeshAABBTree3 spatial = new DMeshAABBTree3(mesh, true);
            MeshWindingNumberGrid mwnGrid = new MeshWindingNumberGrid(mesh, spatial, cell_size) { WindingIsoValue = winding_iso };
            mwnGrid.DebugPrint = true;

            LocalProfiler p = new LocalProfiler();
            p.Start("grid");
            mwnGrid.Compute();
            p.Stop("grid");
            System.Console.WriteLine(p.AllTimes());


            MarchingCubes c = new MarchingCubes();
            c.Implicit = new WindingGridImplicit(mwnGrid);

            c.Bounds = mesh.CachedBounds;
            c.CubeSize = c.Bounds.MaxDim / 128;

            //c.Bounds = mesh.CachedBounds;
            //c.Bounds.Expand(c.Bounds.MaxDim * 0.1);
            //c.CubeSize = cell_size * 0.5;
            //c.IsoValue = mwnGrid.WindingIsoValue;

            c.Generate();

            // reproject
            foreach (int vid in c.Mesh.VertexIndices()) {
                Vector3d v = c.Mesh.GetVertex(vid);

                int tid = spatial.FindNearestTriangle(v, cell_size * MathUtil.SqrtTwo);
                if (tid != DMesh3.InvalidID) {
                    var query = MeshQueries.TriangleDistance(mesh, tid, v);
                    if (v.Distance(query.TriangleClosest) < cell_size)
                        c.Mesh.SetVertex(vid, query.TriangleClosest);
                }
            }

            //MeshNormals.QuickCompute(c.Mesh);
            TestUtil.WriteTestOutputMesh(c.Mesh, "mwn_implicit.obj");
        }





        public static void test_fast_winding_implicit()
        {
            //Sphere3Generator_NormalizedCube gen = new Sphere3Generator_NormalizedCube() { EdgeVertices = 15, Radius = 5 };
            //CappedCylinderGenerator gen = new CappedCylinderGenerator() { };
            //TrivialBox3Generator gen = new TrivialBox3Generator() { Box = new Box3d(Vector3d.Zero, 5 * Vector3d.One) };
            //DMesh3 mesh = gen.Generate().MakeDMesh();
            //MeshTransforms.Translate(mesh, 5 * Vector3d.One);
            DMesh3 mesh = TestUtil.LoadTestInputMesh("bunny_solid.obj");
            //DMesh3 mesh = TestUtil.LoadTestInputMesh("holey_bunny.obj");
            //DMesh3 mesh = TestUtil.LoadTestInputMesh("holey_bunny_2.obj");
            //DMesh3 mesh = TestUtil.LoadTestMesh("C:\\git\\gsOrthoVRApp\\sample_files\\scan_1_raw.obj");   // use iso 0.25
            //DMesh3 mesh = TestUtil.LoadTestMesh("C:\\scratch\\irongiant.stl");
            //DMesh3 mesh = TestUtil.LoadTestMesh("C:\\Users\\rms\\Dropbox\\meshes\\cars\\beetle.obj");
            //DMesh3 mesh = TestUtil.LoadTestMesh("c:\\scratch\\PigHead_rot90.obj");
            //DMesh3 mesh = TestUtil.LoadTestMesh("C:\\Projects\\nia_files\\testscan2.obj");

            //TestUtil.WriteTestOutputMesh(mesh, "xxx.obj");

            int mesh_cells = 128;

            AxisAlignedBox3d meshBounds = mesh.CachedBounds;
            float winding_iso = 0.5f;

            DMeshAABBTreePro spatialPro = new DMeshAABBTreePro(mesh, true);
            spatialPro.FastWindingNumber(Vector3d.Zero);
            //spatialPro.FWNBeta = 1.0;

            FastWindingImplicit fwnImplicit = new FastWindingImplicit(spatialPro, winding_iso);

            MarchingCubes c = new MarchingCubes();
            c.Implicit = fwnImplicit;
            c.Bounds = mesh.CachedBounds;
            c.CubeSize = c.Bounds.MaxDim / mesh_cells;
            c.Bounds.Expand(c.CubeSize * 3);
            c.RootMode = MarchingCubes.RootfindingModes.Bisection;
            c.RootModeSteps = 10;

            c.Generate();

            // reproject
            //foreach (int vid in c.Mesh.VertexIndices()) {
            //    Vector3d v = c.Mesh.GetVertex(vid);

            //    int tid = spatial.FindNearestTriangle(v, cell_size * MathUtil.SqrtTwo);
            //    if (tid != DMesh3.InvalidID) {
            //        var query = MeshQueries.TriangleDistance(mesh, tid, v);
            //        if (v.Distance(query.TriangleClosest) < cell_size * 1.5)
            //            c.Mesh.SetVertex(vid, query.TriangleClosest);
            //    }
            //}

            //MeshNormals.QuickCompute(c.Mesh);
            TestUtil.WriteTestOutputMesh(c.Mesh, "mwn_implicit.obj");
        }









        public static void test_fast_winding_grid()
        {
            //Sphere3Generator_NormalizedCube gen = new Sphere3Generator_NormalizedCube() { EdgeVertices = 15, Radius = 5 };
            //CappedCylinderGenerator gen = new CappedCylinderGenerator() { };
            //TrivialBox3Generator gen = new TrivialBox3Generator() { Box = new Box3d(Vector3d.Zero, 5 * Vector3d.One) };
            //DMesh3 mesh = gen.Generate().MakeDMesh();
            //MeshTransforms.Translate(mesh, 5 * Vector3d.One);
            DMesh3 mesh = TestUtil.LoadTestInputMesh("bunny_solid.obj");
            //DMesh3 mesh = TestUtil.LoadTestInputMesh("holey_bunny.obj");
            //DMesh3 mesh = TestUtil.LoadTestInputMesh("holey_bunny_2.obj");
            //DMesh3 mesh = TestUtil.LoadTestMesh("C:\\git\\gsOrthoVRApp\\sample_files\\scan_1_raw.obj");   // use iso 0.25
            //DMesh3 mesh = TestUtil.LoadTestMesh("C:\\scratch\\irongiant.stl");
            //DMesh3 mesh = TestUtil.LoadTestMesh("C:\\Users\\rms\\Dropbox\\meshes\\cars\\beetle.obj");
            //DMesh3 mesh = TestUtil.LoadTestMesh("c:\\scratch\\PigHead_rot90.obj");
            //DMesh3 mesh = TestUtil.LoadTestMesh("C:\\Projects\\nia_files\\testscan2.obj");

            //TestUtil.WriteTestOutputMesh(mesh, "xxx.obj");

            AxisAlignedBox3d meshBounds = mesh.CachedBounds;
            int num_cells = 256;
            double cell_size = meshBounds.MaxDim / num_cells;

            float winding_iso = 0.35f;

            DMeshAABBTree3 spatial = new DMeshAABBTree3(mesh, true);
            spatial.WindingNumber(Vector3d.Zero);

            DMeshAABBTreePro spatialPro = new DMeshAABBTreePro(mesh, true);
            spatialPro.FastWindingNumber(Vector3d.Zero);

            Func<Vector3d, double> exactWN = (q) => { return spatial.WindingNumber(q); };
            Func<Vector3d, double> PerFacePtWN = (q) => { return eval_point_wn(mesh, q); };
            Func<Vector3d, double> fastWN = (q) => { return spatialPro.FastWindingNumber(q); };

            //MeshScalarSamplingGrid mwnGrid = new MeshScalarSamplingGrid(mesh, cell_size, exactWN);
            //MeshScalarSamplingGrid mwnGrid = new MeshScalarSamplingGrid(mesh, cell_size, PerFacePtWN );
            MeshScalarSamplingGrid mwnGrid = new MeshScalarSamplingGrid(mesh, cell_size, fastWN);

            mwnGrid.IsoValue = winding_iso;
            mwnGrid.DebugPrint = true;

            LocalProfiler p = new LocalProfiler();
            p.Start("grid");
            mwnGrid.Compute();
            p.Stop("grid");
            System.Console.WriteLine(p.AllTimes());


            MarchingCubes c = new MarchingCubes();
            c.Implicit = new SampledGridImplicit(mwnGrid);

            c.Bounds = mesh.CachedBounds;
            c.CubeSize = c.Bounds.MaxDim / 128;

            //c.Bounds = mesh.CachedBounds;
            c.Bounds.Expand(c.CubeSize * 3);
            //c.CubeSize = cell_size * 0.5;
            //c.IsoValue = mwnGrid.WindingIsoValue;

            c.Generate();

            // reproject
            foreach (int vid in c.Mesh.VertexIndices()) {
                Vector3d v = c.Mesh.GetVertex(vid);

                int tid = spatial.FindNearestTriangle(v, cell_size * MathUtil.SqrtTwo);
                if (tid != DMesh3.InvalidID) {
                    var query = MeshQueries.TriangleDistance(mesh, tid, v);
                    if (v.Distance(query.TriangleClosest) < cell_size * 1.5)
                        c.Mesh.SetVertex(vid, query.TriangleClosest);
                }
            }

            //MeshNormals.QuickCompute(c.Mesh);
            TestUtil.WriteTestOutputMesh(c.Mesh, "mwn_implicit.obj");
        }









        static double eval_point_wn(DMesh3 mesh, Vector3d q)
        {
            SpinLock locker = new SpinLock();
            double sum = 0;
            gParallel.ForEach(mesh.TriangleIndices(), (tid) => {

                Vector3d n,c; double area;
                mesh.GetTriInfo(tid, out n, out area, out c);
                Triangle3d tri = new Triangle3d();
                mesh.GetTriVertices(tid, ref tri.V0, ref tri.V1, ref tri.V2);

                //double pt_wn = pointwn_order1(ref c, ref n, ref area, ref q);
                Vector3d evalPt = c - n * 0.001;
                //double pt_wn = pointwn_order2(ref c, ref evalPt, ref n, ref area, ref q);
                //double pt_wn = triwn_order1(ref tri, ref evalPt, ref n, ref area, ref q);
                double pt_wn = triwn_order2(ref tri, ref evalPt, ref n, ref area, ref q);

                bool entered = false;
                locker.Enter(ref entered);
                sum += pt_wn;
                locker.Exit();
            });

            return sum;
        }


        static double pointwn_exact(ref Vector3d x, ref Vector3d xn, ref double xA, ref Vector3d q)
        {
            Vector3d dv = (x - q);
            double len = dv.Length;
            return (xA / MathUtil.FourPI) * xn.Dot(dv / (len * len * len));
        }

        // point-winding-number first-order approximation. 
        // x is dipole point, p is 'center' of cluster of dipoles, q is evaluation point
        static double pointwn_order1(ref Vector3d x, ref Vector3d p, ref Vector3d xn, ref double xA, ref Vector3d q)
        {
            Vector3d dpq = (p - q);
            double len = dpq.Length;
            double len3 = len * len * len;

            return (xA / MathUtil.FourPI) * xn.Dot(dpq / (len * len * len));
        }


        // point-winding-number second-order approximation
        // x is dipole point, p is 'center' of cluster of dipoles, q is evaluation point
        static double pointwn_order2(ref Vector3d x, ref Vector3d p, ref Vector3d xn, ref double xA, ref Vector3d q)
        {
            Vector3d dpq = (p - q);
            Vector3d dxp = (x - p);

            double len = dpq.Length;
            double len3 = len*len*len;

            // first-order approximation - area*normal*\grad(G)
            double order1 = (xA / MathUtil.FourPI) * xn.Dot(dpq / len3);

            // second-order hessian \grad^2(G)
            Matrix3d xqxq = new Matrix3d(ref dpq, ref dpq);
            xqxq *= 3.0 / (MathUtil.FourPI * len3 * len * len);
            double diag = 1 / (MathUtil.FourPI * len3);
            Matrix3d hessian = new Matrix3d(diag,diag,diag) - xqxq;

            // second-order LHS area * \outer(x-p, normal)
            Matrix3d o2_lhs = new Matrix3d(ref dxp, ref xn);
            double order2 = xA * o2_lhs.InnerProduct(ref hessian);

            return order1 + order2;
        }




        // triangle-winding-number first-order approximation. 
        // t is triangle, p is 'center' of cluster of dipoles, q is evaluation point
        static double triwn_order1(ref Triangle3d t, ref Vector3d p, ref Vector3d xn, ref double xA, ref Vector3d q)
        {
            Vector3d at0 = xA * xn;

            Vector3d dpq = (p - q);
            double len = dpq.Length;
            double len3 = len * len * len;

            return (1.0 / MathUtil.FourPI) * at0.Dot(dpq / (len * len * len));
        }



        // triangle-winding-number second-order approximation
        // t is triangle, p is 'center' of cluster of dipoles, q is evaluation point
        static double triwn_order2(ref Triangle3d t, ref Vector3d p, ref Vector3d xn, ref double xA, ref Vector3d q)
        {
            Vector3d dpq = (p - q);

            double len = dpq.Length;
            double len3 = len * len * len;

            // first-order approximation - integrated_normal_area * \grad(G)
            double order1 = (xA / MathUtil.FourPI) * xn.Dot(dpq / len3);

            // second-order hessian \grad^2(G)
            Matrix3d xqxq = new Matrix3d(ref dpq, ref dpq);
            xqxq *= 3.0 / (MathUtil.FourPI * len3 * len * len);
            double diag = 1 / (MathUtil.FourPI * len3);
            Matrix3d hessian = new Matrix3d(diag, diag, diag) - xqxq;

            // second-order LHS - integrated second-order area matrix (formula 26)
            Vector3d centroid = new Vector3d(
                (t.V0.x+t.V1.x+t.V2.x)/3.0, (t.V0.y+t.V1.y+t.V2.y)/3.0, (t.V0.z+t.V1.z+t.V2.z)/3.0);
            Vector3d dcp = centroid - p;
            Matrix3d o2_lhs = new Matrix3d(ref dcp, ref xn);
            double order2 = xA * o2_lhs.InnerProduct(ref hessian);

            return order1 + order2;
        }




    }

}
