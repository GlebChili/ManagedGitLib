using System;
using System.IO;
using Xunit;
using LibGit2Sharp;
using System.Diagnostics;

namespace ManagedGitLib.ExtendedTests
{
    public class MonoRepoProvider : IDisposable
    {
        readonly DirectoryInfo repoDirectory;
        readonly Repository repo;

        public DirectoryInfo RepoDirectory => repoDirectory;
        public Repository Repo => repo;

        public MonoRepoProvider()
        {
            repoDirectory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));

            using Process gitProcess = new Process();
            gitProcess.StartInfo.WorkingDirectory = repoDirectory.FullName;
            gitProcess.StartInfo.CreateNoWindow = true;
            gitProcess.StartInfo.FileName = "git";
            gitProcess.StartInfo.ArgumentList.Add("clone");
            gitProcess.StartInfo.ArgumentList.Add("https://github.com/mono/mono.git");
            gitProcess.StartInfo.ArgumentList.Add(".");
            gitProcess.Start();
            gitProcess.WaitForExit();

            repo = new Repository(repoDirectory.FullName);
        }

        public void Dispose()
        {
            repo.Dispose();

            repoDirectory.SetFilesAttributesToNormal();
            repoDirectory.Delete(true);
        }
    }

    public class TestAgainstRealRepo : IClassFixture<MonoRepoProvider>
    {
        readonly MonoRepoProvider repoProvider;

        public TestAgainstRealRepo(MonoRepoProvider repoProvider)
        {
            this.repoProvider = repoProvider;
        }

        [Fact]
        public void GetCommitById1()
        {
            using GitRepository? repo = GitRepository.Create(repoProvider.RepoDirectory.FullName);

            Assert.NotNull(repo);
        }
    }
}