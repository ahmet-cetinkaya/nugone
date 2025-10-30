# 🤝 NuGone Contributing

Thank you for your interest in contributing to NuGone! This document explains how to set up your development environment, make changes, and submit contributions.

## 📋 Requirements

- .NET 9+ (primary target)
- Windows, macOS, or Linux
- .NET SDK ([Download here](https://dotnet.microsoft.com/download))
- Git

## 🚀 Getting Started

### 1. Fork the Repository

Click the "Fork" button on the top right of the [NuGone GitHub page](https://github.com/ahmet-cetinkaya/nugone) to create your own copy.

### 2. Clone Your Fork

```bash
git clone https://github.com/your-username/nugone.git
cd nugone
```

### 3. Create a Feature Branch

```bash
git checkout -b feat/your-feature-name
```

### 4. Build the Project

```bash
dotnet build -c Release
```

### 5. Run Tests (Optional)

```bash
dotnet test
```

### 6. Run the CLI Tool (Optional)

```bash
dotnet run --project src/presentation/NuGone.Cli --help
```

## 🛠️ Making Changes

- Make your changes in your feature branch.
- Follow the project's coding standards and best practices.
- Write or update tests as needed.

## 🔀 Submitting a Pull Request

1. Commit your changes:
   ```bash
   git commit -m "feat: describe your change"
   ```
2. Push your branch:
   ```bash
   git push origin feat/your-feature-name
   ```
3. Open a Pull Request from your branch to the `main` branch of the upstream repository.

## 👀 Code Review

- Your pull request will be reviewed by the maintainers.
- Please address any feedback and update your PR as needed.

## 💬 Need Help?

If you have any questions, feel free to open an issue or start a discussion on GitHub.

---

Thank you for helping make NuGone better!
