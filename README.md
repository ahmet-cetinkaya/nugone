![NuGone icon](https://raw.githubusercontent.com/ahmet-cetinkaya/nugone/refs/heads/main/docs/icon/icon-100.webp)

# `NuGone` [![Buy Me A Coffee](https://img.shields.io/badge/Buy%20Me%20a%20Coffee-ffdd00?&logo=buy-me-a-coffee&logoColor=black)](https://ahmetcetinkaya.me/donate) [![GitHub license](https://img.shields.io/github/license/ahmet-cetinkaya/nugone)](LICENSE) [![GitHub stars](https://img.shields.io/github/stars/ahmet-cetinkaya/nugone?style=social)](https://github.com/ahmet-cetinkaya/nugone/stargazers) [![GitHub forks](https://img.shields.io/github/forks/ahmet-cetinkaya/nugone?style=social)](https://github.com/ahmet-cetinkaya/nugone/network/members)

Automatically detect and remove unused NuGet package references in your .NET projects. Keep your codebase lean, fast, and secure.

## ⚡ Getting Started

[![NuGet Version](https://img.shields.io/nuget/v/NuGone.svg?color=004880&logo=nuget&label=NuGet&style=flat-square)](https://www.nuget.org/packages/NuGone/) [![NuGet Downloads](https://img.shields.io/nuget/dt/NuGone.svg?color=004880&logo=nuget&label=Downloads&style=flat-square)](https://www.nuget.org/packages/NuGone/)

You can install NuGone as a .NET global tool:

```bash
dotnet tool install --global nugone
```

After installation, you can use the `nugone` command anywhere:

```bash
nugone analyze --project MySolution.sln
```

For more usage instructions, see future documentation updates.

## 🚀 Usage

To detect unused NuGet packages in your .NET projects, use the following command:

```
nugone analyze --project <SOLUTION_OR_PROJECT_PATH>
```

**Options:**

- `--output json` Show results in JSON format
- `--verbose` Show detailed analysis output

**Example:**

```
nugone analyze --project MySolution.sln --output json
```

For a list of all commands and parameters, use:

```
nugone --help
```

Only the `analyze` command is currently available. No changes are made; analysis is read-only.

## 📚 Documentation

For comprehensive documentation, see the **[Documentation Index](docs/DOCUMENTATION_INDEX.md)** which includes user guides, migration instructions, performance details, and more.

## 🤝 Contributing

If you'd like to contribute, please see [CONTRIBUTING.md](docs/CONTRIBUTING.md) for detailed instructions on forking, branching, building, and running the project locally.

## 📄 License

This project is licensed under the GNU General Public License v3.0. See the [LICENSE](LICENSE) file for details.
