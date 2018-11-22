using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using gs;
using g3;

namespace gsGeometryTests
{
    public static class test_AutoRepair
    {
        public static void test_repair_1()
        {
            string filename = "swat.obj";
            //string filename = "chainsaw.obj";

            DMesh3 mesh = TestUtil.LoadTestInputMesh(filename);

            MeshAutoRepair repair = new MeshAutoRepair(mesh);
            repair.Apply();

            TestUtil.WriteTestOutputMesh(mesh, filename.Replace(".obj", ".repaired.obj"));
        }


        public static void test_autorepair_thingi10k()
        {
            //const string THINGIROOT = "E:\\Thingi10K\\";
            string WRITEPATH = "E:\\Thingi10K\\repair_fails\\";
            //string[] files = File.ReadAllLines("E:\\Thingi10K\\current\\thingi10k_open.txt");
            string[] files = File.ReadAllLines("C:\\git\\gsGeometryTests\\test_output\\thingi10k_autorepair_failures.txt");
            //string[] files = new string[] {
            //    "E:\\Thingi10K\\raw_meshes\\37011.stl"
            //};
            SafeListBuilder<string> failures = new SafeListBuilder<string>();

            int count = 0;
            int MAX_NUM_FILES = 10000;

            gParallel.ForEach(files, (filename) => {
                if (count > MAX_NUM_FILES)
                    return;

                int i = count;
                Interlocked.Increment(ref count);
                if ( i % 10 == 0 )
                    System.Console.WriteLine("{0} / {1}", i, files.Length);

                long start_ticks = DateTime.Now.Ticks;

                DMesh3Builder builder = new DMesh3Builder();
                StandardMeshReader reader = new StandardMeshReader() { MeshBuilder = builder };
                IOReadResult result = reader.Read(filename, ReadOptions.Defaults);
                if (result.code != IOCode.Ok) {
                    System.Console.WriteLine("{0} FAILED TO READ!", filename);
                    failures.SafeAdd(filename);
                    return;
                }

                DMesh3 mesh = builder.Meshes[0];
                for (int k = 1; k < builder.Meshes.Count; ++k)
                    MeshEditor.Append(mesh, builder.Meshes[k]);
                DMesh3 before = new DMesh3(mesh);


                try {
                    MeshAutoRepair repair = new MeshAutoRepair(mesh);
                    repair.Apply();
                } catch (Exception e) {
                    System.Console.WriteLine("EXCEPTION {0} : {1}", filename, e.Message);
                    failures.SafeAdd(filename);
                    return;
                }

                if ( mesh.IsClosed() == false ) {
                    failures.SafeAdd(filename);
                    Util.WriteDebugMesh(before, WRITEPATH + Path.GetFileNameWithoutExtension(filename) + ".obj");
                    Util.WriteDebugMesh(mesh, WRITEPATH + Path.GetFileNameWithoutExtension(filename) + ".failed.obj");
                    return;
                } else {
                    if (mesh.CheckValidity(false, FailMode.ReturnOnly) == false ) {
                        System.Console.WriteLine("INVALID {0}", filename);
                        failures.SafeAdd(filename);

                        Util.WriteDebugMesh(before, WRITEPATH + Path.GetFileNameWithoutExtension(filename) + ".obj");
                        Util.WriteDebugMesh(mesh, WRITEPATH + Path.GetFileNameWithoutExtension(filename) + ".invalid.obj");

                        return;
                    }
                }

            });


            //foreach (string failure in failures.Result) {
            //    System.Console.WriteLine("FAIL: {0}", failure);
            //}
            System.Console.WriteLine("repaired {0} of {1}", files.Length - failures.Result.Count, files.Length);

            TestUtil.WriteTestOutputStrings(make_strings(failures), "thingi10k_autorepair_failures_new.txt");
        }
        static string[] make_strings(SafeListBuilder<string> s)
        {
            s.List.Sort();
            return s.List.ToArray();
        }
    }
}
