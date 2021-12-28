using Semver;

namespace Bob.Model;

record ChangeLogEntry(SemVersion Version, IReadOnlyList<string> Bullets)
{
    public override string ToString() =>
        $@"===== {Version} =====
{string.Join(Environment.NewLine, Bullets.Select(bullet => $" * {bullet}"))}";
}