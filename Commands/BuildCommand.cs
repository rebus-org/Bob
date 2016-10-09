using GoCommando;

namespace Bob.Commands
{
    [Command("build")]
    [Description("Builds the Rebus project")]
    public class BuildCommand : BaseCommand, ICommand
    {
        public void Run()
        {
            const string scriptToRun = "build.cmd";

            Run(scriptToRun);
        }
    }
}