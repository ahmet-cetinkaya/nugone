# `NuGone` [![Buy Me A Coffee](https://img.shields.io/badge/Buy%20Me%20a%20Coffee-ffdd00?&logo=buy-me-a-coffee&logoColor=black)](https://ahmetcetinkaya.me/donate) [![GitHub license](https://img.shields.io/github/license/ahmet-cetinkaya/nugone)](https://github.com/ahmet-cetinkaya/nugone/blob/main/LICENSE) [![GitHub stars](https://img.shields.io/github/stars/ahmet-cetinkaya/nugone?style=social)](https://github.com/ahmet-cetinkaya/nugone/stargazers) [![GitHub forks](https://img.shields.io/github/forks/ahmet-cetinkaya/nugone?style=social)](https://github.com/ahmet-cetinkaya/nugone/network/members)

Automatically detect and remove unused NuGet package references in your .NET projects. Keep your codebase lean, fast, and secure.

> This project will be developed using .NET 9 as the primary target framework.

## ⚡ Getting Started

You can install NuGone as a .NET global tool:

```bash
dotnet tool install --global nugone
```

After installation, you can use the `nugone` command anywhere:

```bash
nugone analyze --project MySolution.sln
```

For more usage instructions, see future documentation updates.

### 📋 Requirements

- .NET Core 3.1, .NET 5, .NET 6, .NET 7, .NET 8, .NET 9+ (primary target)
- Windows, macOS, or Linux

## 🤝 Contributing

If you'd like to contribute, please see [CONTRIBUTING.md](CONTRIBUTING.md) for detailed instructions on forking, branching, building, and running the project locally.

## 📄 License

This project is licensed under the GNU General Public License v3.0. See the [LICENSE](https://github.com/ahmet-cetinkaya/nugone/blob/main/LICENSE) file for details.
