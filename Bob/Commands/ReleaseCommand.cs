﻿using GoCommando;
// ReSharper disable ArgumentsStyleLiteral
// ReSharper disable UnusedMember.Global

namespace Bob.Commands;

[Command("release")]
[Description("Builds the Rebus project")]
public class ReleaseCommand : BaseCommand, ICommand
{
    public void Run()
    {
        const string scriptToRun = "release.cmd";

        Run(scriptToRun, createTag: true);
    }
}