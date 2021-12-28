using Bob.Model;
using GoCommando;
using Semver;
using Spinnerino;
// ReSharper disable ArgumentsStyleLiteral

namespace Bob.Commands;

public abstract class BaseCommand
{
    [Parameter("verbose", "v", optional: true)]
    [Description("Toggles whether to print the full output when running the build")]
    public bool Verbose { get; set; }

    readonly ConcurrentQueue<string> _log = new();

    protected void Run(string script, string projectName, string currentDirectory, bool createTag)
    {
        try
        {
            InnerRun(script, projectName, currentDirectory, createTag);
        }
        catch (GoCommandoException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new GoCommandoException($@"Unhandled error: {exception}

Log:

{string.Join(Environment.NewLine, _log)}");
        }
    }

    void InnerRun(string script, string projectName, string currentDirectory, bool createTag)
    {
        if (!File.Exists(script))
        {
            throw new GoCommandoException($@"Could not find script: '{script}'.

Please create the script at the path shown above, and make it so that
it correctly accepts a project name and a version as its arguments.");
        }

        PrintIfVerbose($"Building '{projectName}' in '{currentDirectory}'");

        var changelog = ReadChangelog(currentDirectory);

        var versions = ParseChangelog(changelog).ToList();

        PrintIfVerbose($"Found {versions.Count} versions");

        var lastVersion = versions.Last();

        PrintIfVerbose($@"Building version:

{lastVersion}

");

        var arguments = $"{projectName} {lastVersion.Version}";

        Console.WriteLine($"EXEC> {script} {arguments}");

        using (Verbose ? (IDisposable)new DummyDisposable() : new IndefiniteSpinner())
        {
            ShellExecute(currentDirectory, script, arguments);

            if (createTag)
            {
                CreateTag(lastVersion, currentDirectory);
            }
        }

        Console.WriteLine("OK :)");
    }

    void PrintIfVerbose(string text)
    {
        if (!Verbose) return;
        Console.WriteLine(text);
    }

    void ShellExecute(string currentDirectory, string fileName, string arguments)
    {
        var buildStartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = currentDirectory,

            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        var process = Process.Start(buildStartInfo);

        if (process == null)
        {
            throw new GoCommandoException($"Could not start process with command {fileName} {arguments}");
        }

        using var outputTimer = new Timer(1000);

        var invoking = false;

        outputTimer.Elapsed += delegate
        {
            if (invoking) return;

            invoking = true;

            try
            {
                PumpOutput(process);
            }
            finally
            {
                invoking = false;
            }
        };
        outputTimer.Start();

        var executionTimeout = TimeSpan.FromMinutes(5);

        if (!process.WaitForExit(milliseconds: (int)executionTimeout.TotalMilliseconds))
        {
            throw new GoCommandoException($"Process for command {fileName} {arguments} did not exit within timeout of {executionTimeout}");
        }

        if (process.ExitCode != 0)
        {
            outputTimer.Stop();

            PumpOutput(process);

            throw new GoCommandoException($"Process for command {fileName} {arguments} exited with code {process.ExitCode}");
        }
    }

    void PumpOutput(Process process)
    {
        string line;

        while ((line = process.StandardOutput.ReadLine()) != null)
        {
            _log.Enqueue(line);

            if (Verbose)
            {
                Console.WriteLine(line);
            }
        }

        while ((line = process.StandardError.ReadLine()) != null)
        {
            _log.Enqueue(line);

            if (Verbose)
            {
                Console.WriteLine(line);
            }
        }
    }

    void CreateTag(ChangeLogEntry lastVersion, string currentDirectory)
    {
        var tagMessage = string.Join(Environment.NewLine, lastVersion.Bullets.Select(text => $"* {text}"));
        var tempFilePath = Path.GetTempFileName();

        File.WriteAllText(tempFilePath, tagMessage, Encoding.UTF8);

        Console.WriteLine($"Tag message written to {tempFilePath}");

        ShellExecute(currentDirectory, "git", $@"tag {lastVersion.Version} -a -F ""{tempFilePath}""");

        ShellExecute(currentDirectory, "git", @"push --tags");
    }

    static string ReadChangelog(string currentDirectory)
    {
        var path = Path.Combine(currentDirectory, "CHANGELOG.md");

        try
        {
            return File.ReadAllText(path);
        }
        catch (FileNotFoundException)
        {
            throw new GoCommandoException($@"Could not find changelog '{path}'.

Please create a changelog at the path shown above, and use a format where
versions are added like this:

    ## <version>

    * changelog line 1
    * changelog line 2

e.g. like this:

    ## 1.0.4

    * Fix subtle bug
    * Fix another thing

    ## 1.1.0

    * Add some function

etc.");
        }
    }

    static IEnumerable<ChangeLogEntry> ParseChangelog(string changelog)
    {
        #region Example
        /*
## 0.99.73

* Add GZIPping capability to data bus storage - can be enabled by attaching `.UseCompression()` in the data bus configuration builder
* Factor forwarding of failed messages to error queues out into `PoisonQueueErrorHandler` which implements `IErrorHandler`. Make room for customizing what to do about failed messages.

## 0.99.74

* Mark assemblies as CLS compliant becase VB.NET and F# programmers are most welcome too - thanks [NKnusperer]
* Update Serilog dependency to 2.1.0 - thanks [NKnusperer]
* Limit number of workers to match max parallelism
* Make thread pool-based workers default (old strategy can still be had by calling `o.UseClassicRebusWorkersMessageDispatch()`)
* Update NLog dependency to 4.3.7 - thanks [SvenVandenbrande]
* Update SimpleInjector dependency to 3.2.0 - thanks [SvenVandenbrande]
* Make adjustment to new thread pool-based workers that makes better use of async receive APIs of transports
* Update Wire dependency to 0.8.0
* Update Autofac dependency to 4.0.1
* Fix bug in Amazon SQS transport that would cause it to be unable to receive messages if the last created queue was not the transport's own input queue

## 2.0.0-a2

* Improve SQL transport expired messages cleanup to hit an index - thanks [xenoputtss]

## 2.0.0-a7

* Update to .NET 4.5.2 because it is the lowest framework version currently supported by Microsoft

## 2.0.0-a8

* Update NUnit dependency to 3.4.1

## 2.0.0-a9

* Fix file-based lock which was kept for longer than necessary (i.e. until GC would collect the `FileStream` that had not been properly disposed)

## 2.0.0-a10

* Experimentally multi-targeting .NET 4.5, 4.5.2, 4.6, and 4.6.1 (but it dit NOT work for 4.6 and 4.6.1)
         */
        #endregion

        var entriesText = TrimHeaderAndFooter(changelog);

        return entriesText.Split(new[] { "##" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(ParseEntryOrNull)
            .Where(entry => entry != null);
    }

    static ChangeLogEntry ParseEntryOrNull(string entriesText)
    {
        /*
2.0.0-a1

* Test release
        */

        try
        {
            var lines = entriesText.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            var versionString = lines.First().Trim();

            if (versionString.StartsWith("<")) return null;

            var version = SemVersion.Parse(versionString);
            var bulletLines = lines.Skip(1);
            var bullets = bulletLines
                .Select(text => text.Trim())
                .Where(text => text.StartsWith("*"))
                .Select(text => text.Substring(1).Trim())
                .Where(text => !string.IsNullOrWhiteSpace(text))
                .ToArray();

            if (bullets.Length == 0)
            {
                throw new FormatException($"No bullets for version {versionString}");
            }

            return new ChangeLogEntry(version, bullets);
        }
        catch (Exception exception)
        {
            throw new FormatException($@"Invalid changelog entry format:
<entry>
{entriesText}
</entry>
", exception);
        }

    }

    static string TrimHeaderAndFooter(string changelog)
    {
        var indexOfFirstEntry = changelog.IndexOf("##");
        if (indexOfFirstEntry < 0) throw new GoCommandoException("Could not find any entries in changelog");

        var withoutHeader = changelog.Substring(indexOfFirstEntry);
        var indexOfFooterMark = withoutHeader.IndexOf("---");
        if (indexOfFooterMark <= 0) return withoutHeader;

        var withoutFooter = withoutHeader.Substring(0, indexOfFooterMark - 1);
        return withoutFooter;
    }

    protected void Run(string scriptToRun, bool createTag = false)
    {
        var currentDirectory = Environment.CurrentDirectory;
        var projectName = currentDirectory.Split(Path.DirectorySeparatorChar).Last();
        var script = Path.Combine(currentDirectory, "scripts", scriptToRun);

        Run(script, projectName, currentDirectory, createTag);
    }

    class DummyDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}