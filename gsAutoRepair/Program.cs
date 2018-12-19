using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using g3;
using gs;

namespace gsAutoRepairTool
{
	class Program
	{
        static void print_usage()
        {
            System.Console.WriteLine("gsAutoRepairTool v1.0 - Copyright gradientspace / Ryan Schmidt 2018");
            System.Console.WriteLine("usage: gsAutoRepairTool options <inputmesh>");
            System.Console.WriteLine("options:");
            System.Console.WriteLine("  -output <filename>  : output filename - default is inputmesh.repaired.fmt");
            //System.Console.WriteLine("  -v                  : verbose ");
        }

        public static void Main(string[] args)
		{
            CommandArgumentSet arguments = new CommandArgumentSet();
            //arguments.Register("-tcount", int.MaxValue);
            //arguments.Register("-percent", 50.0f);
            //arguments.Register("-v", false);
            arguments.Register("-output", "");
            if (arguments.Parse(args) == false) {
                return;
            }

            if (arguments.Filenames.Count != 1) {
                print_usage();
                return;
            }
            string inputFilename = arguments.Filenames[0];
            if (!File.Exists(inputFilename)) {
                System.Console.WriteLine("File {0} does not exist", inputFilename);
                return;
            }


            string outputFilename = Path.GetFileNameWithoutExtension(inputFilename);
            string format = Path.GetExtension(inputFilename);
            outputFilename = outputFilename + ".repaired" + format;
            if (arguments.Saw("-output")) {
                outputFilename = arguments.Strings["-output"];
            }


            //int triCount = int.MaxValue;
            //if (arguments.Saw("-tcount"))
            //    triCount = arguments.Integers["-tcount"];

            //float percent = 50.0f;
            //if (arguments.Saw("-percent"))
            //    percent = arguments.Floats["-percent"];

            bool verbose = true;
            //if (arguments.Saw("-v"))
            //    verbose = arguments.Flags["-v"];


            List<DMesh3> meshes;
            try {
                DMesh3Builder builder = new DMesh3Builder();
                IOReadResult result = StandardMeshReader.ReadFile(inputFilename, ReadOptions.Defaults, builder);
                if (result.code != IOCode.Ok) {
                    System.Console.WriteLine("Error reading {0} : {1}", inputFilename, result.message);
                    return;
                }
                meshes = builder.Meshes;
            } catch (Exception e) {
                System.Console.WriteLine("Exception reading {0} : {1}", inputFilename, e.Message);
                return;
            }
            if (meshes.Count == 0) {
                System.Console.WriteLine("file did not contain any valid meshes");
                return;
            }

            DMesh3 mesh = meshes[0];
            for (int k = 1; k < meshes.Count; ++k)
                MeshEditor.Append(mesh, meshes[k]);
            if (mesh.TriangleCount == 0) {
                System.Console.WriteLine("mesh does not contain any triangles");
                return;
            }

            if (verbose)
                System.Console.WriteLine("initial mesh contains {0} triangles", mesh.TriangleCount);

            if (verbose)
                System.Console.WriteLine("Repairing...", mesh.TriangleCount);

            MeshAutoRepair repair = new MeshAutoRepair(mesh);
            repair.RemoveMode = MeshAutoRepair.RemoveModes.None;
            bool bOK = repair.Apply();
            if (verbose) {
                if (bOK == false)
                    System.Console.WriteLine("repair failed!");
                else
                    System.Console.WriteLine("done! repaired mesh contains {0} triangles", mesh.TriangleCount);
            }

            try {
                IOWriteResult wresult =
                    StandardMeshWriter.WriteMesh(outputFilename, mesh, WriteOptions.Defaults);
                if (wresult.code != IOCode.Ok) {
                    System.Console.WriteLine("Error writing {0} : {1}", inputFilename, wresult.message);
                    return;
                }
            } catch (Exception e) {
                System.Console.WriteLine("Exception reading {0} : {1}", inputFilename, e.Message);
                return;
            }

            return;
        }




    }
}
