# ManagedGitLib

A lightweight read-only git library written entirely in .NET.
ManagedGitLib targets .NET Standard 2.0 (among other frameworks) and, unkile [Libgit2Sharp](https://github.com/libgit2/libgit2sharp) does not depend on any native library.
Therefore ManagedGitLib can be used in scenarios where taking dependency on native libraries is imposible or problematic,
like MSBuild Tasks and C# Source Generators.

ManagedGitLib is a read-only library and designed to be used for reading git metadata, like commit information, listing tags, and files change history.
It can't be used for cloning, managing, and modifying git repositories.
For a full-blown git experience check out [Libgit2Sharp](https://github.com/libgit2/libgit2sharp).

ManagedGitLib is derived from [Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning) `ManagedGit` implementation.

## Nightly builds

You can find the latest experemental builds of ManagedGitLib at my [nightly packages feed](https://dev.azure.com/glebchili-personal/glebchili-packages/_packaging?_a=feed&feed=glebchili-personal-public%40Local).
