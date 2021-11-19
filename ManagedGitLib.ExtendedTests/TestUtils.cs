using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ManagedGitLib.ExtendedTests
{
    public class TestUtils
    {
    }

    public static class HelperExtensions
    {
        public static void SetFilesAttributesToNormal(this DirectoryInfo directory)
        {
            foreach (FileInfo file in directory.GetFiles())
            {
                File.SetAttributes(file.FullName, FileAttributes.Normal);
                File.Delete(file.FullName);
            }

            foreach (DirectoryInfo subdirectory in directory.GetDirectories())
            {
                subdirectory.SetFilesAttributesToNormal();
            }
        }
    }
}
