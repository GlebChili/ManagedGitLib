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

    public class TestAgainstRealMonoRepo : IClassFixture<MonoRepoProvider>
    {
        readonly MonoRepoProvider repoProvider;

        public TestAgainstRealMonoRepo(MonoRepoProvider repoProvider)
        {
            this.repoProvider = repoProvider;
        }

        [Fact]
        public void OpenRepoAndCheckHead()
        {
            using GitRepository? repo = GitRepository.Create(repoProvider.RepoDirectory.FullName);

            Assert.NotNull(repo);

            string? headName = repo!.GetHeadAsReferenceOrSha() as string;

            Assert.NotNull(headName);
            Assert.Equal(repoProvider.Repo.Head.ToString(), headName);

            GitCommit? headCommit = repo.GetHeadCommit();

            Assert.NotNull(headCommit);
            Assert.Equal(repoProvider.Repo.Head.Tip.Sha, headCommit!.Value.Sha.ToString());
            Assert.Equal(repoProvider.Repo.Head.Tip.Message, headCommit!.Value.Message);

            Assert.Equal(repoProvider.Repo.Head.Tip.Author.Name, headCommit!.Value.Author.Name);
            Assert.Equal(repoProvider.Repo.Head.Tip.Author.Email, headCommit!.Value.Author.Email);
            Assert.Equal(repoProvider.Repo.Head.Tip.Author.When, headCommit!.Value.Author.Date);

            Assert.Equal(repoProvider.Repo.Head.Tip.Committer.Name, headCommit!.Value.Committer.Name);
            Assert.Equal(repoProvider.Repo.Head.Tip.Committer.Email, headCommit!.Value.Committer.Email);
            Assert.Equal(repoProvider.Repo.Head.Tip.Committer.When, headCommit!.Value.Committer.Date);
        }
    }
}