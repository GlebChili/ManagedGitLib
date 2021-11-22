using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;
using LibGit2Sharp;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;

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

            Stream expectedAuthorNameStream = TestUtils.GetEmbeddedResource("commit1-author-name");
            byte[] expectedAuthorNameBuffer = new byte[expectedAuthorNameStream.Length];
            expectedAuthorNameStream.ReadAll(expectedAuthorNameBuffer);
            string expectedAuthorName = GitRepository.Encoding.GetString(expectedAuthorNameBuffer);

            Assert.Equal(expectedAuthorName, author.Name);
            Assert.Equal("alex.koeplinger@outlook.com", author.Email);
            Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1580213767), author.Date);

            Assert.Equal(expectedAuthorName, committer.Name);
            Assert.Equal("alex.koeplinger@outlook.com", committer.Email);
            Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1580213767), committer.Date);

            string expectedCommitMessage = "Bump bockbuild for Pango patch\n\n" +
                                           "Fixes bug #1048838, correct SF font not loading in VSMac\n\n" +
                                           "Backport of https://github.com/mono/mono/pull/18566";

            Assert.Equal(expectedCommitMessage, testCommit.Message);

            Assert.Equal("3ad9ae0c77b19e8f4ac5904cfd03f282f189aad1", testCommit.Tree.ToString());

            Assert.NotNull(testCommit.FirstParent);
            Assert.Equal("e55302cb080450bbb9e71ff3a6b4ecf3ce52183e", testCommit.FirstParent!.Value.ToString());

            Assert.Null(testCommit.SecondParent);

            Assert.Single(testCommit.Parents);
        }

        [Fact]
        public void GetCommitBySha2()
        {
            using GitRepository repo = GitRepository.Create(repoProvider.RepoDirectory.FullName)!;

            Assert.NotNull(repo);

            GitCommit testCommit = repo.GetCommit(GitObjectId.Parse("3cf59ad33daa57120ec2d3ca97cfdff4c89ca372"));

            GitSignature author = testCommit.Author;
            GitSignature committer = testCommit.Committer;

            Assert.Equal("Marius Ungureanu", author.Name);
            Assert.Equal("marius.ungureanu@xamarin.com", author.Email);
            Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1629381061), author.Date);

            Assert.Equal("GitHub", committer.Name);
            Assert.Equal("noreply@github.com", committer.Email);
            Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1629381061), author.Date);

            Stream commitMessageStream = TestUtils.GetEmbeddedResource("commit2-message");
            byte[] commitMessageBuffer = new byte[commitMessageStream.Length];
            commitMessageStream.ReadAll(commitMessageBuffer);
            string expectedCommitMessage = GitRepository.Encoding.GetString(commitMessageBuffer);

            Assert.Equal(expectedCommitMessage, testCommit.Message);

            Assert.Equal("b92897da2b6e46a512c8c19e7132c33a662542e9", testCommit.Tree.ToString());

            Assert.NotNull(testCommit.FirstParent);
            Assert.Equal("1e649b6338669d32c92bc882af968f68e4c14896", testCommit.FirstParent!.Value.ToString());

            Assert.Null(testCommit.SecondParent);

            Assert.Single(testCommit.Parents);
        }

        static int CountCommits(GitRepository repo)
        {
            int counter = 0;
            Queue<GitCommit> queue = new Queue<GitCommit>();
            HashSet<GitObjectId> setOfCommitIds = new HashSet<GitObjectId>();

            GitCommit? headCommit = repo.GetHeadCommit();

            if (headCommit is not null)
            {
                queue.Enqueue(headCommit.Value);
            }

            while (queue.Count != 0)
            {
                GitCommit commit = queue.Dequeue();

                counter++;

                setOfCommitIds.Add(commit.Sha);

                foreach (GitObjectId parentId in commit.Parents)
                {
                    if (!setOfCommitIds.Contains(parentId))
                    {
                        setOfCommitIds.Add(parentId);
                        queue.Enqueue(repo.GetCommit(parentId));
                    }
                }
            }

            return counter;
        }

        [Fact]
        public void CountCommitsFromHead()
        {
            using GitRepository repo = GitRepository.Create(repoProvider.RepoDirectory.FullName)!;

            int expectedNumber = repoProvider.Repo.Commits.Count();

            Assert.Equal(expectedNumber, CountCommits(repo));
        }

        [Fact]
        public void GetAnnotatedTag()
        {
            using GitRepository repo = GitRepository.Create(repoProvider.RepoDirectory.FullName)!;

            GitTag tag = repo.GetAnnotatedTag(GitObjectId.Parse("b148bbd1583868db22eb52f06985bed671ca3d9a"));

            Assert.Equal(GitObjectId.Parse("b148bbd1583868db22eb52f06985bed671ca3d9a"), tag.Sha);

            Assert.Equal(GitObjectId.Parse("6bf3922f3fdf8587302a8f7b1b6cbb4fad78a42c"), tag.Target);

            Assert.Equal("commit", tag.TargetType);

            Assert.Equal("mono-5.8.1.0", tag.Name);

            GitSignature tagger = tag.Tagger!.Value;

            Assert.Equal("Xamarin Release Manager", tagger.Name);
            Assert.Equal("builder@xamarin.com", tagger.Email);
            Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1522083303), tagger.Date);

            Assert.Equal(@"Mono - 5.8.1.0", tag.Message);
        }
    }
}