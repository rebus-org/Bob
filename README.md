# Bob

Bob is a build tool that can help with invoking a couple of scripts, passing some sensible arguments to them.

## What does it do?

It has two arguments: `build` and `release`.

They both call scripts in the current repository, which is assumed to be a Git repository.

The scripts are `scripts/build.cmd` and `scripts/release.cmd` respectively.

## Example

![](/stuff/bob1.gif)

I `cd` my way to `C:\Projects-Rebus\Rebus.AutoScaling` which is where Rebus' auto-scaling plugin resides.

Then I 

	C:\Projects-Rebus\Rebus.AutoScaling>bob build

which outputs something like this:

	EXEC> C:\Projects-Rebus\Rebus.AutoScaling\scripts\build.cmd Rebus.AutoScaling 2.0.0

	OK :)

indicating that `scripts/build.cmd` was invoked with the name of the project and a version as arguments.

The name of the project is deduced from the name of the directory, and the version is extracted from a
`CHANGELOG.md` file which is assumed to reside in the root of the repository.

The changelog is assumed to follow this format:

	## <semver-version>

	* changelog line
	* changelog line

	## <semver-version>

	* changelog line
	* changelog line

etc.