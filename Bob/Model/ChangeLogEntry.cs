using Semver;

namespace Bob.Model;

class ChangeLogEntry
{
    public ChangeLogEntry(SemVersion version, string[] bullets)
    {
        Version = version;
        Bullets = bullets;
    }

    public SemVersion Version { get; }
    public string[] Bullets { get; }

    public override string ToString()
    {
        return $@"===== {Version} =====
{string.Join(Environment.NewLine, Bullets.Select(bullet => $" * {bullet}"))}";
    }
}