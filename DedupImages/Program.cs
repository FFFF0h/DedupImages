using CommandLine;
using DupImageLib;
using ImageMagick;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace DedupImages
{
    class Program
    {
        /// <summary>Command line arguments options.</summary>
        public class Options
        {
            [Option('s', "source", Required = true, HelpText = "The source folder to scan.")]
            public string SourceFolder { get; set; }

            [Option('d', "destination", Required = true, HelpText = "The destionation folder where to move duplicate images.")]
            public string DestinationFolder { get; set; }

            [Option('t', "threshold", Default = 1.0f, Required = false, HelpText = "The threshold sensitivity. A value of 1.0 is a exact match.")]
            public float Threshold { get; set; }
        }

        private static Dictionary<ulong, string> _hashList = new Dictionary<ulong, string>();


        /// <summary>The startup call.</summary>
        /// <param name="args">The CLI arguments.</param>
        /// <returns></returns>
        static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<Options>(args).MapResult(opts => RunOptionsAndReturnExitCode(opts),  errs => HandleParseError(errs));
        }

        /// <summary>Handles CLI parse errors.</summary>
        /// <param name="errs">The errs.</param>
        /// <returns></returns>
        private static int HandleParseError(IEnumerable<Error> errs)
        {
            return 1;
        }

        /// <summary> Handles the process if no CLI parse errors.</summary>
        /// <param name="opts">The opts.</param>
        /// <returns></returns>
        private static int RunOptionsAndReturnExitCode(Options opts)
        {
            Console.WriteLine("Searching for image perceptual similarities...");
            var defaultColor = Console.ForegroundColor;

            try
            {
                var files = Directory.EnumerateFiles(opts.SourceFolder, "*.*", SearchOption.AllDirectories)
                            .Where(s => s.EndsWith(".jpg") || s.EndsWith(".jpeg") || s.EndsWith(".png") || s.EndsWith(".bmp") || s.EndsWith(".gif") || s.EndsWith(".tif") || s.EndsWith(".tiff"));

                var imageHasher = new ImageHashes(new ImageMagickTransformer());
                foreach (var file in files)
                {
                    var currentImage = new MagickImage(file);
                    var currentSize = currentImage.Width * currentImage.Height;

                    Console.WriteLine("{0} ({1}x{2})", file, currentImage.Width, currentImage.Height);
                    var currentHash = imageHasher.CalculateDifferenceHash64(file);

                    var sim = 0f;
                    foreach (var hash in _hashList)
                    {
                        sim = ImageHashes.CompareHashes(hash.Key, currentHash);
                        if (sim >= opts.Threshold)
                        {
                            var existingImage = new MagickImage(hash.Value);
                            var existingSize = existingImage.Width * existingImage.Height;
                            
                            if (currentImage > existingImage)
                            {
                                Console.ForegroundColor = ConsoleColor.Magenta;
                                Console.WriteLine("\t=> Found ! (sim = {0}) {1} ({2}x{3})", sim, hash.Value, existingImage.Width, existingImage.Height);
                                Console.ForegroundColor = defaultColor;

                                File.Move(file, opts.DestinationFolder.TrimEnd('\\') + "\\" + Path.GetFileName(file));
                            }
                            else if (currentImage == existingImage)
                            {
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine("\t=> Found ! (sim = {0}) {1} ({2}x{3})", sim, hash.Value, existingImage.Width, existingImage.Height);
                                Console.ForegroundColor = defaultColor;

                                File.Move(file, opts.DestinationFolder.TrimEnd('\\') + "\\" + Path.GetFileName(file));
                            }
                            else
                            {
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine("\t=> Found ! (sim = {0}) {1} ({2}x{3})", sim, hash.Value, existingImage.Width, existingImage.Height);
                                Console.ForegroundColor = defaultColor;

                                File.Move(hash.Value, opts.DestinationFolder.TrimEnd('\\') + "\\" + Path.GetFileName(hash.Value));
                            }
                            break;
                        }
                    }

                    if (sim != 1)
                    {
                        _hashList.Add(currentHash, file);
                    }
                }
            }
            catch(Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error : " + ex.Message);
                Console.ForegroundColor = defaultColor;
                return 1;
            }

            return 0;
        }
    }
}
