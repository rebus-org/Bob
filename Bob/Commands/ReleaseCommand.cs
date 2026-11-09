// ReSharper disable ArgumentsStyleLiteral

namespace Bob.Commands;

public class ReleaseCommand : BaseCommand
{
    public override void Run() => Run("release", createTag: true);
}
