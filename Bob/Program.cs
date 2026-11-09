using System.Reflection;
using Bob;
using Bob.Commands;

var commands = new CommandInfo[]
{
    new("build", "Builds the Rebus project", () => new BuildCommand()),
    new("release", "Builds the Rebus project", () => new ReleaseCommand()),
};

var options = new OptionInfo[]
{
    new("verbose", "v", "Toggles whether to print the full output when running the build"),
};

return Run(args);

int Run(string[] arguments)
{
    if (arguments.Length == 0)
    {
        PrintHelp();
        return 0;
    }

    var commandName = arguments[0];

    if (IsHelpSwitch(commandName) || commandName == "help")
    {
        PrintHelp();
        return 0;
    }

    if (commandName is "--version" or "-version")
    {
        Console.WriteLine(GetVersion());
        return 0;
    }

    var commandInfo = commands.FirstOrDefault(c => string.Equals(c.Name, commandName, StringComparison.OrdinalIgnoreCase));

    if (commandInfo == null)
    {
        return Fail($"Unknown command: '{commandName}'", printHelp: true);
    }

    var command = commandInfo.Factory();

    foreach (var argument in arguments.Skip(1))
    {
        if (IsHelpSwitch(argument))
        {
            PrintCommandHelp(commandInfo);
            return 0;
        }

        switch (GetOptionName(argument))
        {
            case "verbose":
                command.Verbose = true;
                break;

            default:
                return Fail($"Unknown argument for command '{commandInfo.Name}': '{argument}'", printHelp: false);
        }
    }

    try
    {
        command.Run();
        return 0;
    }
    catch (BobException exception)
    {
        return Fail(exception.Message, printHelp: false);
    }
    catch (Exception exception)
    {
        return Fail($"Unhandled error: {exception}", printHelp: false);
    }
}

// accepts "-v", "--verbose", and "-verbose" (the latter is what GoCommando used)
string GetOptionName(string argument)
{
    if (!argument.StartsWith('-')) return null;

    var name = argument.TrimStart('-');

    return options.FirstOrDefault(o => string.Equals(o.Name, name, StringComparison.OrdinalIgnoreCase)
                                       || string.Equals(o.ShortName, name, StringComparison.Ordinal))?.Name;
}

static bool IsHelpSwitch(string argument) => argument is "-h" or "--help" or "-help" or "-?" or "/?";

int Fail(string message, bool printHelp)
{
    var color = Console.ForegroundColor;
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Error.WriteLine(message);
    Console.ForegroundColor = color;

    if (printHelp)
    {
        Console.Error.WriteLine();
        PrintHelp(Console.Error);
    }

    return 1;
}

void PrintHelp(TextWriter writer = null)
{
    writer ??= Console.Out;

    writer.WriteLine($"Bob the Builder {GetVersion()}");
    writer.WriteLine();
    writer.WriteLine("Usage: bob <command> [options]");
    writer.WriteLine();
    writer.WriteLine("Commands:");

    var width = commands.Max(c => c.Name.Length) + 4;

    foreach (var command in commands)
    {
        writer.WriteLine($"  {command.Name.PadRight(width)}{command.Description}");
    }

    writer.WriteLine();
    writer.WriteLine("Run 'bob <command> --help' to see the options of a command.");
}

void PrintCommandHelp(CommandInfo command)
{
    Console.WriteLine($"Usage: bob {command.Name} [options]");
    Console.WriteLine();
    Console.WriteLine(command.Description);
    Console.WriteLine();
    Console.WriteLine("Options:");

    var switches = options.Select(o => (Text: $"-{o.ShortName}, --{o.Name}", o.Description)).ToList();
    var width = switches.Max(s => s.Text.Length) + 4;

    foreach (var (text, description) in switches)
    {
        Console.WriteLine($"  {text.PadRight(width)}{description}");
    }
}

static string GetVersion()
{
    var version = typeof(BaseCommand).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "";
    var plusIndex = version.IndexOf('+'); // strip source revision suffix
    return plusIndex < 0 ? version : version[..plusIndex];
}

record CommandInfo(string Name, string Description, Func<BaseCommand> Factory);

record OptionInfo(string Name, string ShortName, string Description);
