// Copyright Epic Games, Inc. All Rights Reserved.

using System;
using System.IO;
using LoreVcs.Types;
using LoreVcs.Types.Args;
using LoreVcs;
using LoreVcs.Types.Events;
using LoreVcs.Types.Enums;

// If a remote URL is provided as the first CLI arg, run in online mode (push
// the revision and clone the repository back). Otherwise run a fully offline
// example that only creates a local repository and commits a file.
// Authentication is not handled by this example; if the remote requires it,
// run `lore auth` before invoking this program.
string? REMOTE_URL = args.Length > 0 ? args[0] : null;
bool ONLINE = REMOTE_URL != null;

if (ONLINE)
{
    Console.WriteLine($"Running in online mode against: {REMOTE_URL}");
}
else
{
    Console.WriteLine("Running in offline mode (pass a remote URL as the first arg to enable push/clone)");
}

string REPOSITORY_NAME = "EpicRepo" + Guid.NewGuid().ToString();
string REPOSITORY_PATH = $"./LoreRepositories/{REPOSITORY_NAME}";
string REPOSITORY_URL = ONLINE ? $"{REMOTE_URL}/{REPOSITORY_NAME}" : REPOSITORY_NAME;

static void EventHandler(LoreEventFFI loreEvent, ulong userContext)
{
    if (loreEvent.Tag == LoreEventTag.REPOSITORY_CREATE)
    {
        var createEvent = loreEvent.GetData<LoreRepositoryCreateEventDataFFI>();
        Console.WriteLine($"{createEvent.Name}");
    }
}

static void createFiles(string repository_name)
{
    string[] files = {
        $"./LoreRepositories/{repository_name}/file.txt",
        $"./LoreRepositories/{repository_name}/log.txt",
    };
    foreach (string file in files)
    {
        File.WriteAllText(file, "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et");
    }
}

// Lore operations throw a LoreError when they finish with a non-zero return
// code; the exception message carries the underlying error detail. A single
// try/catch around the workflow is all that is needed to handle failures.
try
{
    var logConfig = new LoreLogConfig { FilePath = "./LoreRepositories", File = true };
    Lore.LogConfigure(logConfig);

    // Register a global logger. Disposed automatically when out of scope, or when globalLogger.Dispose() is called manually
    using var globalLogger = Lore.GlobalCallback(
        LoreEventTag.LOG,
        (loreEvent, userContext) =>
        {
            var logEvent = loreEvent.GetData<LoreLogEventDataFFI>();
            if (logEvent.Level > LoreLogLevel.DEBUG)
            {
                Console.WriteLine($"{logEvent.Message}");
            }
        }
    );

    using var globalArgs = new LoreGlobalArgs { RepositoryPath = REPOSITORY_PATH, Offline = !ONLINE };

    using var repoArgs = new LoreRepositoryCreateArgs { RepositoryUrl = REPOSITORY_URL };
    Lore.RepositoryCreate(globalArgs, repoArgs).Callback(EventHandler).Wait();
    Console.WriteLine("Repository created.");

    createFiles(REPOSITORY_NAME);

    using var stageArgs = new LoreFileStageArgs
    {
        Paths = new string[] { $"./LoreRepositories/{REPOSITORY_NAME}/file.txt", $"./LoreRepositories/{REPOSITORY_NAME}/log.txt" }
    };
    Lore.FileStage(globalArgs, stageArgs).Callback(EventHandler).Wait();
    Console.WriteLine("Files staged.");

    using var commitArgs = new LoreRevisionCommitArgs { Message = "Initial Commit" };
    Lore.RevisionCommit(globalArgs, commitArgs).Callback(EventHandler).Wait();
    Console.WriteLine("Revision committed.");

    if (ONLINE)
    {
        using var pushArgs = new LoreBranchPushArgs();
        Lore.BranchPush(globalArgs, pushArgs).Callback(EventHandler).Wait();
        Console.WriteLine("Branch pushed.");

        using var globalArgsClone = new LoreGlobalArgs { RepositoryPath = REPOSITORY_PATH + "_clone" };
        using var cloneArgs = new LoreRepositoryCloneArgs { RepositoryUrl = REPOSITORY_URL };
        Lore.RepositoryClone(globalArgsClone, cloneArgs).Callback(EventHandler).Wait();
        Console.WriteLine("Repository cloned.");
    }

    Lore.Shutdown();
    Console.WriteLine("Done.");
}
catch (LoreError error)
{
    Console.WriteLine($"Lore operation failed: {error.Message}");
    Environment.Exit(1);
}
