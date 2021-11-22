using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibGit2Sharp;
using Xunit;
using Xunit.Abstractions;

namespace ManagedGitLib.ExtendedTests
{
    public class Libgit2MonoRepoProvider : IDisposable
    {
        readonly DirectoryInfo repoDirectory;
        readonly Repository repo;

        public DirectoryInfo RepoDirectory => repoDirectory;
        public Repository Repo => repo;

        public Libgit2MonoRepoProvider()
        {
            repoDirectory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));

            Repository.Clone("https://github.com/mono/mono.git", repoDirectory.FullName);

            repo = new Repository(repoDirectory.FullName);
        }

        public void Dispose()
        {
            repo.Dispose();

            repoDirectory.SetFilesAttributesToNormal();
            repoDirectory.Delete(true);
        }
    }

    public class TestAgainstLibgit2MonoRepo : IClassFixture<Libgit2MonoRepoProvider>
    {
        readonly Libgit2MonoRepoProvider repoProvider;

        public TestAgainstLibgit2MonoRepo(Libgit2MonoRepoProvider repoProvider)
        {
            this.repoProvider = repoProvider;
        }

        [Fact]
        public void GetAnnotatedTag()
        {
            using GitRepository repo = GitRepository.Create(repoProvider.RepoDirectory.FullName)!;

            GitTag tag = repo.GetAnnotatedTag(GitObjectId.Parse("b148bbd1583868db22eb52f06985bed671ca3d9a"));

            Assert.Equal(GitObjectId.Parse("b148bbd1583868db22eb52f06985bed671ca3d9a"), tag.Sha);

            Assert.Equal(GitObjectId.Parse("6bf3922f3fdf8587302a8f7b1b6cbb4fad78a42c"), tag.Target);

            Assert.True(tag.IsAnnotated);

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
