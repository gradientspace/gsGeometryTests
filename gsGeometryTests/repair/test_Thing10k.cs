using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using g3;
using gs;

namespace gsGeometryTests
{
    public static class test_Thing10k
    {

        public static void test_repair_all()
        {
            //const string THINGIROOT = "D:\\meshes\\Thingi10K\\raw_meshes\\";
            const string THINGIROOT = "E:\\Thingi10K\\raw_meshes\\";
            string[] files = Directory.GetFiles(THINGIROOT);
            //files = File.ReadAllLines("C:\\git\\geometry3SharpDemos\\geometry3Test\\test_output\\thingi10k_open.txt");
            SafeListBuilder<string> failures = new SafeListBuilder<string>();

            SafeListBuilder<string> empty = new SafeListBuilder<string>();
            SafeListBuilder<string> closed = new SafeListBuilder<string>();
            SafeListBuilder<string> open = new SafeListBuilder<string>();
            SafeListBuilder<string> boundaries_failed = new SafeListBuilder<string>();
            SafeListBuilder<string> boundaries_spans_failed = new SafeListBuilder<string>();
            SafeListBuilder<string> slow = new SafeListBuilder<string>();
            SafeListBuilder<string> veryslow = new SafeListBuilder<string>();

            int k = 0;
            int MAX_NUM_FILES = 10000;

            gParallel.ForEach(files, (filename) => {
                if (k > MAX_NUM_FILES)
                    return;

                int i = k;
                Interlocked.Increment(ref k);
                System.Console.WriteLine("{0} : {1}", i, filename);

                long start_ticks = DateTime.Now.Ticks;

                DMesh3Builder builder = new DMesh3Builder();
                StandardMeshReader reader = new StandardMeshReader() { MeshBuilder = builder };
                IOReadResult result = reader.Read(filename, ReadOptions.Defaults);
                if (result.code != IOCode.Ok) {
                    System.Console.WriteLine("{0} FAILED!", filename);
                    failures.SafeAdd(filename);
                    return;
                }

                bool is_open = false;
                bool loops_failed = false;
                bool loops_spans_failed = false;
                bool is_empty = true;
                foreach (DMesh3 mesh in builder.Meshes) {
                    if (mesh.TriangleCount > 0)
                        is_empty = false;

                    if (mesh.IsClosed() == false) {
                        MergeCoincidentEdges closeCracks = new MergeCoincidentEdges(mesh);
                        closeCracks.Apply();
                    }

                    if (mesh.IsClosed() == false) {
                        is_open = true;
                        try {
                            MeshBoundaryLoops loops = new MeshBoundaryLoops(mesh, false) {
                                SpanBehavior = MeshBoundaryLoops.SpanBehaviors.ThrowException,
                                FailureBehavior = MeshBoundaryLoops.FailureBehaviors.ThrowException
                            };
                            loops.Compute();
                        } catch {
                            loops_failed = true;
                        }

                        if ( loops_failed ) {
                            try {
                                MeshBoundaryLoops loops = new MeshBoundaryLoops(mesh, false) {
                                    SpanBehavior = MeshBoundaryLoops.SpanBehaviors.Compute,
                                    FailureBehavior = MeshBoundaryLoops.FailureBehaviors.ConvertToOpenSpan
                                };
                                loops.Compute();
                            } catch {
                                loops_spans_failed = true;
                            }
                        } 
                    }
                }


                TimeSpan elapsed = new TimeSpan(DateTime.Now.Ticks - start_ticks);
                if (elapsed.TotalSeconds > 60)
                    veryslow.SafeAdd(filename);
                else if (elapsed.TotalSeconds > 10)
                    slow.SafeAdd(filename);

                if (is_empty) {
                    empty.SafeAdd(filename);
                } else if (is_open) {
                    open.SafeAdd(filename);
                    if (loops_failed)
                        boundaries_failed.SafeAdd(filename);
                    if (loops_spans_failed)
                        boundaries_spans_failed.SafeAdd(filename);

                } else {
                    closed.SafeAdd(filename);
                }

            });


            foreach (string failure in failures.Result) {
                System.Console.WriteLine("FAIL: {0}", failure);
            }

            TestUtil.WriteTestOutputStrings(make_strings(failures), "thingi10k_failures.txt");
            TestUtil.WriteTestOutputStrings(make_strings(empty), "thingi10k_empty.txt");
            TestUtil.WriteTestOutputStrings(make_strings(closed), "thingi10k_closed.txt");
            TestUtil.WriteTestOutputStrings(make_strings(open), "thingi10k_open.txt");
            TestUtil.WriteTestOutputStrings(make_strings(boundaries_failed), "thingi10k_boundaries_failed.txt");
            TestUtil.WriteTestOutputStrings(make_strings(boundaries_spans_failed), "thingi10k_boundaries_spans_failed.txt");

            TestUtil.WriteTestOutputStrings(make_strings(slow), "thingi10k_slow.txt");
            TestUtil.WriteTestOutputStrings(make_strings(veryslow), "thingi10k_veryslow.txt");
        }

        
        static string[] make_strings(SafeListBuilder<string> s)
        {
            s.List.Sort();
            return s.List.ToArray();
        }




        public static void test_specific_file()
        {
            //string filename = "F:\\Thingi10K\\raw_meshes\\1423009.stl";
            //string filename = "E:\\Thingi10K\\raw_meshes\\99944.stl";
            string filename = "E:\\Thingi10K\\raw_meshes\\57356.stl";

            System.Console.WriteLine("reading {0}", filename);

            DMesh3Builder builder = new DMesh3Builder();
            StandardMeshReader reader = new StandardMeshReader() { MeshBuilder = builder };
            IOReadResult result = reader.Read(filename, ReadOptions.Defaults);
            if (result.code != IOCode.Ok) {
                System.Console.WriteLine("{0} FAILED!", filename);
                return;
            }

            System.Console.WriteLine("got {0} meshes", builder.Meshes.Count);

            bool is_open = false;
            bool loops_failed = false;
            bool is_empty = true;
            foreach (DMesh3 mesh in builder.Meshes) {
                if (mesh.TriangleCount > 0)
                    is_empty = false;

                TestUtil.WriteTestOutputMesh(mesh, "thingi10k_specific_file_in.obj");

                if (mesh.IsClosed() == false) {
                    MergeCoincidentEdges closeCracks = new MergeCoincidentEdges(mesh);
                    closeCracks.Apply();
                }

                if (mesh.IsClosed() == false) {
                    is_open = true;
                    try {
                        MeshBoundaryLoops loops = new MeshBoundaryLoops(mesh, false) {
                            SpanBehavior = MeshBoundaryLoops.SpanBehaviors.Compute,
                            FailureBehavior = MeshBoundaryLoops.FailureBehaviors.ConvertToOpenSpan
                        };
                        loops.Compute();
                    } catch (Exception e) {
                        System.Console.WriteLine("EXCEPTION: " + e.Message);
                        loops_failed = true;
                    }
                }

                System.Console.WriteLine("open: {0}  loopsFailed: {1}  empty: {2}", is_open, loops_failed, is_empty);
                TestUtil.WriteTestOutputMesh(mesh, "thingi10k_specific_file_out.obj");
            }

        }











        public static void test_write_solids()
        {
            //string FORMAT = ".obj";
            string FORMAT = ".g3mesh";
            string WRITEPATH = "E:\\Thingi10K\\closed\\";
            string[] files = File.ReadAllLines("E:\\Thingi10K\\current\\thingi10k_closed.txt");
            SafeListBuilder<string> failures = new SafeListBuilder<string>();

            if ( ! Directory.Exists(WRITEPATH) )
                Directory.CreateDirectory(WRITEPATH);

            int k = 0;

            gParallel.ForEach(files, (filename) => {
                int i = k;
                Interlocked.Increment(ref k);
                if ( i % 500 == 0 )
                    System.Console.WriteLine("{0} : {1}", i, files.Length);

                long start_ticks = DateTime.Now.Ticks;

                DMesh3Builder builder = new DMesh3Builder();
                StandardMeshReader reader = new StandardMeshReader() { MeshBuilder = builder };
                IOReadResult result = reader.Read(filename, ReadOptions.Defaults);
                if (result.code != IOCode.Ok) {
                    System.Console.WriteLine("{0} FAILED!", filename);
                    failures.SafeAdd(filename);
                    return;
                }

                DMesh3 combineMesh = new DMesh3();
                if (builder.Meshes.Count == 1) {
                    combineMesh = builder.Meshes[0];
                } else {
                    foreach (DMesh3 mesh in builder.Meshes)
                        MeshEditor.Append(combineMesh, mesh);
                }


                if (combineMesh.IsClosed() == false) {
                    MergeCoincidentEdges closeCracks = new MergeCoincidentEdges(combineMesh);
                    closeCracks.Apply();
                }

                if (combineMesh.IsClosed() == false) {
                    System.Console.WriteLine("NOT CLOSED: {0}", filename);
                    return;
                }

                string outPath = Path.Combine(WRITEPATH, Path.GetFileNameWithoutExtension(filename) + FORMAT);
                StandardMeshWriter.WriteMesh(outPath, combineMesh, WriteOptions.Defaults);

            });


        }



    }
}
