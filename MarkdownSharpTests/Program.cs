﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Text.RegularExpressions;

namespace MarkdownSharpTests
{
    class Program
    {

        static void Main(string[] args)
        {

            //UnitTests();

            //
            // this is the closest thing to a set of Markdown reference tests I could find
            //
            // see http://six.pairlist.net/pipermail/markdown-discuss/2009-February/001526.html
            // and http://michelf.com/docs/projets/mdtest-1.1.zip
            // and http://git.michelf.com/mdtest/
            //
            GenerateTestOutput(@"mdtest-1.1");

            //
            // same as mdtest-1.1, but:
            //
            // * remove any non-significant whitespace or extra paragraph differences
            //   (there are only two as I recall; not sure why, but frankly who cares)
            // * comment out any edge conditions we aren't dealing with yet; search for
            //   the string "omitted:" to see what those are
            //
            GenerateTestOutput(@"mdtest-1.1-alt");

            //
            // a few additional random "Hello World" type tests
            //
            GenerateTestOutput(@"test-input");

            Benchmark();

            //AdHocTest();
            
            Console.ReadKey();
        }

        /// <summary>
        /// mini test harness for one-liner Markdown bug repros 
        /// for anything larger, I recommend using the folder based approach and GenerateTestOutput()
        /// </summary>
        private static void AdHocTest()
        {
            var m = new MarkdownSharp.Markdown();

            string input = "mail me at <username@example.com> please";
            string output = m.Transform(input);

            Console.WriteLine("input:");
            Console.WriteLine(input);
            Console.WriteLine("output:");
            Console.WriteLine(output);
        }

        /// <summary>
        /// iterates through all the test files in a given folder and generates file-based output 
        /// this is essentially the same as running the unit tests, but with diff-able results
        /// </summary>
        /// <remarks>
        /// two files should be present for each test:
        /// 
        /// test_name.text         -- input (raw markdown)
        /// test_name.html         -- output (expected cooked html output from reference markdown engine)
        /// 
        /// this file will be generated if, and ONLY IF, the expected output does not match the actual output:
        /// 
        /// test_name.xxxx.actual.html  -- actual output (actual cooked html output from our markdown c# engine)
        ///                             -- xxxx is the 16-bit CRC checksum of the file contents; this is included
        ///                                so you can tell if the contents of a failing test have changed
        /// </remarks>
        static void GenerateTestOutput(string testfolder)
        {
            var m = new MarkdownSharp.Markdown();

            Console.WriteLine();
            Console.WriteLine(@"MarkdownSharp v" + m.Version + @" test run on " + Path.DirectorySeparatorChar + testfolder);
            Console.WriteLine();

            string path = Path.Combine(ExecutingAssemblyPath, testfolder);
            string output;
            string expected;
            string actual;

            int ok = 0;
            int err = 0;
            int errnew = 0;
            int total = 0;            

            foreach (var file in Directory.GetFiles(path, "*.text"))
            {

                expected = FileContents(Path.ChangeExtension(file, "html"));
                output = m.Transform(FileContents(file));
                
                total++;

                Console.Write(String.Format("{0:000} {1,-55}", total, Path.GetFileNameWithoutExtension(file)));

                if (output == expected)
                {
                    ok++;
                    Console.WriteLine("OK");
                }
                else
                {

                    actual = Path.ChangeExtension(file, GetCrc16(output) + ".actual.html");

                    err++;
                    if (File.Exists(actual))
                    {
                        Console.WriteLine("Mismatch");
                    }
                    else
                    {
                        errnew++;
                        Console.WriteLine("Mismatch *NEW*");                        
                        File.WriteAllText(actual, output);
                    }
                }
            }

            Console.WriteLine();
            Console.WriteLine("Tests        : " + total);
            Console.WriteLine("OK           : " + ok);
            Console.Write("Mismatch     : " + err);
            if (errnew > 0)
                Console.WriteLine(" (" + errnew + " *NEW*)");
            else
                Console.WriteLine();

            if (errnew > 0)
            {
                Console.WriteLine();
                Console.WriteLine("for each mismatch, an *.actual.html file was generated in");
                Console.WriteLine(path);
                Console.WriteLine("to troubleshoot mismatches, use a diff tool on *.html and *.actual.html");
            }

        }

        /// <summary>
        /// returns CRC-16 of string as 4 hex characters
        /// </summary>
        private static string GetCrc16(string s)
        {            
            if (String.IsNullOrEmpty(s)) return "";
            byte[] b = new Crc16().ComputeChecksumBytes(Encoding.UTF8.GetBytes(s));
            return b[0].ToString("x2") + b[1].ToString("x2");
        }


        /// <summary>
        /// returns the contents of the specified file as a string  
        /// assumes the file is relative to the root of the project
        /// </summary>
        static string FileContents(string filename)
        {
            try
            {
                return File.ReadAllText(Path.Combine(ExecutingAssemblyPath, filename));
            }
            catch (FileNotFoundException)
            {
                return "";
            }
            
        }

        /// <summary>
        /// returns the root path of the currently executing assembly
        /// </summary>
        static private string ExecutingAssemblyPath
        {
            get
            {
                string path = System.Reflection.Assembly.GetExecutingAssembly().Location;
                // removes executable part
                path = Path.GetDirectoryName(path);
                // we're typically in \bin\debug or bin\release so move up two folders
                path = Path.Combine(path, "..");
                path = Path.Combine(path, "..");
                return path;
            }
        }


        /// <summary>
        /// executes a standard benchmark on short, medium, and long markdown samples  
        /// use this to verify any impacts of code changes on performance  
        /// please DO NOT MODIFY the input samples or the benchmark itself as this will invalidate previous 
        /// benchmark runs!
        /// </summary>
        static void Benchmark()
        {
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine(@"MarkdownSharp v" + new MarkdownSharp.Markdown().Version + " benchmark, takes 10 ~ 30 seconds...");
            Console.WriteLine();

            Benchmark(FileContents(Path.Combine("benchmark", "markdown-example-short-1.text")), 4000);
            Benchmark(FileContents(Path.Combine("benchmark", "markdown-example-medium-1.text")), 1000);
            Benchmark(FileContents(Path.Combine("benchmark", "markdown-example-long-2.text")), 100);
            Benchmark(FileContents(Path.Combine("benchmark", "markdown-readme.text")), 1);
            Benchmark(FileContents(Path.Combine("benchmark", "markdown-readme.8.text")), 1);
            Benchmark(FileContents(Path.Combine("benchmark", "markdown-readme.32.text")), 1);
        }

        /// <summary>
        /// performs a rough benchmark of the Markdown engine using small, medium, and large input samples 
        /// please DO NOT MODIFY the input samples or the benchmark itself as this will invalidate previous 
        /// benchmark runs!
        /// </summary>
        static void Benchmark(string text, int iterations)
        {
            var m = new MarkdownSharp.Markdown();

            var sw = new Stopwatch();
            sw.Start();
            for (int i = 0; i < iterations; i++)
                m.Transform(text);
            sw.Stop();

            Console.WriteLine("input string length: " + text.Length);
            Console.Write(iterations + " iteration" + (iterations == 1 ? "" : "s") + " in " + sw.ElapsedMilliseconds + " ms");
            if (iterations == 1)
                Console.WriteLine();
            else
                Console.WriteLine(" (" + Convert.ToDouble(sw.ElapsedMilliseconds) / Convert.ToDouble(iterations) + " ms per iteration)");
        }


        /// <summary>
        /// executes nunit-console.exe to run all the tests in this assembly
        /// </summary>
        static void UnitTests()
        {
            log4net.Config.XmlConfigurator.Configure();

            string testAssemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;

            Console.WriteLine("Running tests in {0}\n", testAssemblyLocation);

            var p = new Process();

            string path = Path.Combine(Path.GetDirectoryName(testAssemblyLocation), @"nunit-console\nunit-console.exe");
            path = path.Replace(@"\bin\Debug", "");
            path = path.Replace(@"\bin\Release", "");
            p.StartInfo.FileName = path;
            p.StartInfo.Arguments = "\"" + testAssemblyLocation + "\" /labels /nologo";

            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.UseShellExecute = false;

            p.StartInfo.RedirectStandardOutput = true;
            p.OutputDataReceived += new DataReceivedEventHandler(p_DataReceived);

            p.StartInfo.RedirectStandardError = true;
            p.ErrorDataReceived += new DataReceivedEventHandler(p_DataReceived);

            p.Start();

            p.BeginOutputReadLine();
            p.BeginErrorReadLine();

            while (!p.HasExited)
                System.Threading.Thread.Sleep(500);

            Console.WriteLine();
        }

        private static void p_DataReceived(object sender, DataReceivedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Data)) return;
            Console.WriteLine(e.Data);
        }

    }
}
