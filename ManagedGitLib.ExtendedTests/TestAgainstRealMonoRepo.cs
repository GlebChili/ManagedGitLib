using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;
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

            // For some unknown issue, git pack files can't be deleted, when tests are running on GitHub Windows runners
            if (OperatingSystem.IsWindows() && Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true")
            {
                Console.WriteLine($"{nameof(MonoRepoProvider)}: Tests are running on GitHub Windows runners. " +
                                       $"Test repository cleanup won't be performed");
            }
            else
            {
                repoDirectory.SetFilesAttributesToNormal();
                repoDirectory.Delete(true);
            }
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

        [Fact]
        public void GetCommitBySha1()
        {
            using GitRepository repo = GitRepository.Create(repoProvider.RepoDirectory.FullName)!;

            Assert.NotNull(repo);

            GitCommit testCommit = repo.GetCommit(GitObjectId.Parse("1e2d68b92c9df9147b6c21de4e96c392f6c2ea00"));

            GitSignature author = testCommit.Author;
            GitSignature committer = testCommit.Committer;

            Assert.Equal("Alexander Köplinger", author.Name);
            Assert.Equal("alex.koeplinger@outlook.com", author.Email);
            Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1580213767), author.Date);

            Assert.Equal("Alexander Köplinger", committer.Name);
            Assert.Equal("alex.koeplinger@outlook.com", committer.Email);
            Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1580213767), committer.Date);

            Assert.Null(testCommit.GpgSignature);

            string expectedCommitMessage = "Bump bockbuild for Pango patch\n\n" +
                                           "Fixes bug #1048838, correct SF font not loading in VSMac\n\n" +
                                           "Backport of https://github.com/mono/mono/pull/18566";

            Assert.Equal(expectedCommitMessage, testCommit.Message);

            Assert.Equal("3ad9ae0c77b19e8f4ac5904cfd03f282f189aad1", testCommit.Tree.ToString());

            Assert.NotNull(testCommit.FirstParent);
            Assert.Equal("e55302cb080450bbb9e71ff3a6b4ecf3ce52183e", testCommit.FirstParent!.Value.ToString());

            Assert.Null(testCommit.SecondParent);
        }
    }
}