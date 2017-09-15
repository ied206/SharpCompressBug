using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpCompress.Compressors.Deflate;
using SharpCompress.Compressors;
using System.IO;
using System.Security.Cryptography;
using System.Linq;

namespace SharpCompressBug
{
    /* 
     * [Tested Environment]
     * OS : Windows 10 x64 v1703
     *      (pigz executed in Windows Subsystem for Linux)
     * IDE : Visual Studio 2017
     * DotNet : .Net Framework 4.6.2
     * Package : SharpCompress 0.18.1
     * 
     * [Behavior]
     * All of this test should be passed in theory.
     * However, SharpCompress_Zlib_1 and SharpCompress_Zlib_2 failes.
     * 
     * [Detailed Information]
     * ex1_zlib_stream.jpg.zz, ex2_zlib_stream.jpg.zz is two sample zlib stream.
     * ex1_pigz_zlib.jpg, ex2_pigz_zlib.jpg is raw files, decompressed by pigz 2.3.1 (with --zlib flag).
     *   Ex) $ pigz --zlib -d ex1_zlib_stream.jpg.zz
     * (I used pigz as a reference since it has been written by Mark Adler who is coauthor of zlib.)
     * 
     * Since pigz can decompress samples with zlib compatible mode, 
     * SharpCompress' ZlibStream should be able to decompress it, too.
     * Instead, it fails, and throws ZlibException with 'invalid data check' message.
     * 
     * If I remove zlib magic number (First 2B) and Adler32 checksum (Last 4B) and put them into DeflateStream,
     * Both SharpCompress' and System.IO.Compression's DeflateStream successfully decompresses samples.
     */
    [TestClass]
    public class ZlibStreamTest
    {
        private static string dirPath = $@"..\..\Samples";
        private static string ex1_pigz;
        private static string ex2_pigz;
        private static byte[] ex1_sha256;
        private static byte[] ex2_sha256;

        [TestInitialize]
        public void Init()
        {
            ex1_pigz = Path.Combine(dirPath, "ex1_pigz_zlib.jpg");
            ex2_pigz = Path.Combine(dirPath, "ex2_pigz_zlib.jpg");

            using (FileStream fs = new FileStream(ex1_pigz, FileMode.Open))
            {
                SHA256 hash = new SHA256Managed();
                ex1_sha256 = hash.ComputeHash(fs);
            }

            using (FileStream fs = new FileStream(ex2_pigz, FileMode.Open))
            {
                SHA256 hash = new SHA256Managed();
                ex2_sha256 = hash.ComputeHash(fs);
            }
        }

        #region SharpCompress_Zlib
        [TestMethod]
        public void SharpCompress_Zlib_1()
        {
            string readFilePath = Path.Combine(dirPath, "ex1_zlib_stream.jpg.zz");
            string writeFilePath = Path.Combine(dirPath, "ex1_zlib_sharpcompress.jpg");
            try
            {
                using (FileStream rfs = new FileStream(readFilePath, FileMode.Open))
                using (FileStream wfs = new FileStream(writeFilePath, FileMode.Create))
                using (ZlibStream zs = new ZlibStream(wfs, CompressionMode.Decompress))
                {
                    rfs.CopyTo(zs);
                    zs.Close();
                }
            }
            catch (ZlibException e)
            {
                Console.WriteLine($"ZlibException: [{e.Message}]");
                Assert.Fail();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception: [{e.Message}]");
                Assert.Fail();
            }

            byte[] digest;
            using (FileStream fs = new FileStream(writeFilePath, FileMode.Open))
            {
                SHA256 hash = new SHA256Managed();
                digest = hash.ComputeHash(fs);
            }

            Console.Write("Comparing Hash... ");
            Assert.IsTrue(ex1_sha256.SequenceEqual(digest));
            Console.WriteLine("Success!");
        }

        [TestMethod]
        public void SharpCompress_Zlib_2()
        {
            string readFilePath = Path.Combine(dirPath, "ex2_zlib_stream.jpg.zz");
            string writeFilePath = Path.Combine(dirPath, "ex2_zlib_sharpcompress.jpg");

            try
            {    
                using (FileStream rfs = new FileStream(readFilePath, FileMode.Open))
                using (FileStream wfs = new FileStream(writeFilePath, FileMode.Create))
                using (ZlibStream zs = new ZlibStream(wfs, CompressionMode.Decompress))
                {
                    rfs.CopyTo(zs);
                    zs.Close();
                }
            }
            catch (ZlibException e)
            {
                Console.WriteLine($"ZlibException: [{e.Message}]");
                Assert.Fail();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception: [{e.Message}]");
                Assert.Fail();
            }

            byte[] digest;
            using (FileStream fs = new FileStream(writeFilePath, FileMode.Open))
            {
                SHA256 hash = new SHA256Managed();
                digest = hash.ComputeHash(fs);
            }

            Console.Write("Comparing Hash... ");
            Assert.IsTrue(ex2_sha256.SequenceEqual(digest));
            Console.WriteLine("Success!");
        }
        #endregion

        #region SharpCompress_Deflate
        [TestMethod]
        public void SharpCompress_Deflate_1()
        {
            string readFilePath = Path.Combine(dirPath, "ex1_zlib_stream.jpg.zz");
            string writeFilePath = Path.Combine(dirPath, "ex1_deflate_sharpcompress.jpg");

            try
            {
                byte[] buffer;
                using (FileStream rfs = new FileStream(readFilePath, FileMode.Open))
                {
                    // First 2 byte : zlib magic number
                    // Last 4 byte : Adler32 checksum
                    buffer = new byte[rfs.Length - 6];
                    rfs.Position = 2;
                    rfs.Read(buffer, 0, buffer.Length);
                }

                using (FileStream wfs = new FileStream(writeFilePath, FileMode.Create))
                using (DeflateStream zs = new DeflateStream(wfs, CompressionMode.Decompress))
                {
                    zs.Write(buffer, 0, buffer.Length);
                    zs.Close();
                }
            }
            catch (ZlibException e)
            {
                Console.WriteLine($"ZlibException: [{e.Message}]");
                Assert.Fail();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception: [{e.Message}]");
                Assert.Fail();
            }

            byte[] digest;
            using (FileStream fs = new FileStream(writeFilePath, FileMode.Open))
            {
                SHA256 hash = new SHA256Managed();
                digest = hash.ComputeHash(fs);
            }

            Console.Write("Comparing Hash... ");
            Assert.IsTrue(ex1_sha256.SequenceEqual(digest));
            Console.WriteLine("Success!");
        }

        [TestMethod]
        public void SharpCompress_Deflate_2()
        {
            string readFilePath = Path.Combine(dirPath, "ex2_zlib_stream.jpg.zz");
            string writeFilePath = Path.Combine(dirPath, "ex2_deflate_sharpcompress.jpg");

            try
            {
                byte[] buffer;
                using (FileStream rfs = new FileStream(readFilePath, FileMode.Open))
                {
                    // First 2 byte : zlib magic number
                    // Last 4 byte : Adler32 checksum
                    buffer = new byte[rfs.Length - 6];
                    rfs.Position = 2;
                    rfs.Read(buffer, 0, buffer.Length);
                }
                
                using (FileStream rfs = new FileStream(readFilePath, FileMode.Open))
                using (FileStream wfs = new FileStream(writeFilePath, FileMode.Create))
                using (DeflateStream zs = new DeflateStream(wfs, CompressionMode.Decompress))
                {
                    zs.Write(buffer, 0, buffer.Length);
                    zs.Close();
                }
            }
            catch (ZlibException e)
            {
                Console.WriteLine($"ZlibException: [{e.Message}]");
                Assert.Fail();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception: [{e.Message}]");
                Assert.Fail();
            }

            byte[] digest;
            using (FileStream fs = new FileStream(writeFilePath, FileMode.Open))
            {
                SHA256 hash = new SHA256Managed();
                digest = hash.ComputeHash(fs);
            }

            Console.Write("Comparing Hash... ");
            Assert.IsTrue(ex2_sha256.SequenceEqual(digest));
            Console.WriteLine("Success!");
        }
        #endregion

        #region System_IO_Compression_Deflate
        [TestMethod]
        public void DotNetFramework_Deflate_1()
        {
            string readFilePath = Path.Combine(dirPath, "ex1_zlib_stream.jpg.zz");
            string writeFilePath = Path.Combine(dirPath, "ex1_deflate_dotnetframework.jpg");

            try
            {
                byte[] buffer;
                using (FileStream rfs = new FileStream(readFilePath, FileMode.Open))
                {
                    // First 2 byte : zlib magic number
                    // Last 4 byte : Adler32 checksum
                    buffer = new byte[rfs.Length - 6];
                    rfs.Position = 2;
                    rfs.Read(buffer, 0, buffer.Length);
                }

                using (FileStream wfs = new FileStream(writeFilePath, FileMode.Create))
                using (DeflateStream zs = new DeflateStream(wfs, CompressionMode.Decompress))
                {
                    zs.Write(buffer, 0, buffer.Length);
                    zs.Close();
                }
            }
            catch (ZlibException e)
            {
                Console.WriteLine($"ZlibException: [{e.Message}]");
                Assert.Fail();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception: [{e.Message}]");
                Assert.Fail();
            }

            byte[] digest;
            using (FileStream fs = new FileStream(writeFilePath, FileMode.Open))
            {
                SHA256 hash = new SHA256Managed();
                digest = hash.ComputeHash(fs);
            }

            Console.Write("Comparing Hash... ");
            Assert.IsTrue(ex1_sha256.SequenceEqual(digest));
            Console.WriteLine("Success!");
        }

        [TestMethod]
        public void DotNetFramework_Deflate_2()
        {
            string readFilePath = Path.Combine(dirPath, "ex2_zlib_stream.jpg.zz");
            string writeFilePath = Path.Combine(dirPath, "ex2_deflate_dotnetframework.jpg");

            try
            {
                
                byte[] buffer;
                using (FileStream rfs = new FileStream(readFilePath, FileMode.Open))
                {
                    // First 2 byte : zlib magic number
                    // Last 4 byte : Adler32 checksum
                    buffer = new byte[rfs.Length - 6];
                    rfs.Position = 2;
                    rfs.Read(buffer, 0, buffer.Length);
                }

                using (FileStream rfs = new FileStream(readFilePath, FileMode.Open))
                using (FileStream wfs = new FileStream(writeFilePath, FileMode.Create))
                using (DeflateStream zs = new DeflateStream(wfs, CompressionMode.Decompress))
                {
                    zs.Write(buffer, 0, buffer.Length);
                    zs.Close();
                }
            }
            catch (ZlibException e)
            {
                Console.WriteLine($"ZlibException: [{e.Message}]");
                Assert.Fail();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception: [{e.Message}]");
                Assert.Fail();
            }

            byte[] digest;
            using (FileStream fs = new FileStream(writeFilePath, FileMode.Open))
            {
                SHA256 hash = new SHA256Managed();
                digest = hash.ComputeHash(fs);
            }

            Console.Write("Comparing Hash... ");
            Assert.IsTrue(ex2_sha256.SequenceEqual(digest));
            Console.WriteLine("Success!");
        }
        #endregion
    }
}
