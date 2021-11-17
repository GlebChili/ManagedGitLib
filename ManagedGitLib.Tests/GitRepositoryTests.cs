using System;
using System.IO;
using System.Linq;
using System.Text;
using LibGit2Sharp;
using Xunit;
using Xunit.Abstractions;

namespace ManagedGitLib.Tests
{
    public class GitRepositoryTests : IDisposable
    {
        DirectoryInfo notARepo;

        DirectoryInfo repoWithOneFile;
        Signature repoWithOneFileSignature;
        DateTimeOffset repoWithOneFileTime;

        public GitRepositoryTests()
        {
            notARepo = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
            File.WriteAllText(Path.Combine(notARepo.FullName, "file.txt"), Guid.NewGuid().ToString());

            repoWithOneFile = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
            File.WriteAllText(Path.Combine(notARepo.FullName, "file.txt"), Guid.NewGuid().ToString());
            Repository.Init(repoWithOneFile.FullName);
            using Repository repoWithOneFileGit = new Repository(repoWithOneFile.FullName);
            repoWithOneFileTime = DateTimeOffset.Now;
            repoWithOneFileSignature = new Signature("TestRunner", "tests@tests.com", repoWithOneFileTime);
            Commands.Stage(repoWithOneFileGit, "*");
            repoWithOneFileGit.Commit("First commit", repoWithOneFileSignature, repoWithOneFileSignature);
            repoWithOneFileTime = repoWithOneFileGit.Head.Tip.Author.When;
        }

        public void Dispose()
        {
            notARepo.Delete(true);

            SetFilesAttributesToNormal(repoWithOneFile);
            repoWithOneFile.Delete(true);

            static void SetFilesAttributesToNormal(DirectoryInfo directory)
            {
                foreach (FileInfo file in directory.GetFiles())
                {
                    File.SetAttributes(file.FullName, FileAttributes.Normal);
                }

                foreach (DirectoryInfo subdirectory in directory.GetDirectories())
                {
                    SetFilesAttributesToNormal(subdirectory);
                }
            }
        }

        [Fact]
        public void CreateNotARepoTest()
        {
            Assert.Null(GitRepository.Create(null));
            Assert.Null(GitRepository.Create(""));
            Assert.Null(GitRepository.Create("/A/Path/To/A/Directory/Which/Does/Not/Exist"));
            Assert.True(notARepo.Exists);
            Assert.True(notARepo.GetFiles().Length > 0);
            Assert.Null(GitRepository.Create(notARepo.FullName));
        }

        [Fact]
        public void OpenRepoWithFile()
        {
            GitRepository managedRepo = GitRepository.Create(repoWithOneFile.FullName);
            
            Assert.NotNull(managedRepo);

            var commit = managedRepo.GetHeadCommit(true);

            Assert.NotNull(commit);

            var commitSignature = commit.Value.Author;

            Assert.NotNull(commitSignature);

            Assert.Equal("TestRunner", commitSignature.Value.Name);

            Assert.Equal("tests@tests.com", commitSignature.Value.Email);

            Assert.Equal<DateTimeOffset>(repoWithOneFileTime, commitSignature.Value.Date);

            Assert.Empty(commit.Value.Parents.ToList());
        }

        [Fact]
        public void ParseAlternates_SingleValue_Test()
        {
            var alternates = GitRepository.ParseAlternates(Encoding.UTF8.GetBytes("/home/git/nbgv/.git/objects\n"));
            Assert.Collection(
                alternates,
                a => Assert.Equal("/home/git/nbgv/.git/objects", a));
        }

        [Fact]
        public void ParseAlternates_SingleValue_NoTrailingNewline_Test()
        {
            var alternates = GitRepository.ParseAlternates(Encoding.UTF8.GetBytes("../repo/.git/objects"));
            Assert.Collection(
                alternates,
                a => Assert.Equal("../repo/.git/objects", a));
        }

        [Fact]
        public void ParseAlternates_TwoValues_Test()
        {
            var alternates = GitRepository.ParseAlternates(Encoding.UTF8.GetBytes("/home/git/nbgv/.git/objects:../../clone/.git/objects\n"));
            Assert.Collection(
                alternates,
                a => Assert.Equal("/home/git/nbgv/.git/objects", a),
                a => Assert.Equal("../../clone/.git/objects", a));
        }

        [Fact]
        public void ParseAlternates_PathWithColon_Test()
        {
            var alternates = GitRepository.ParseAlternates(
                Encoding.UTF8.GetBytes("C:/Users/nbgv/objects:C:/Users/nbgv2/objects/:../../clone/.git/objects\n"),
                2);
            Assert.Collection(
                alternates,
                a => Assert.Equal("C:/Users/nbgv/objects", a),
                a => Assert.Equal("C:/Users/nbgv2/objects/", a),
                a => Assert.Equal("../../clone/.git/objects", a));
        }
    }
}
