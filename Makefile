.PHONY: help build clean test run restore format lint watch dev release install

# Default target
help:
	@echo "🔧 Compiler Project - Available commands:"
	@echo ""
	@echo "  make build          - Build the project (Debug)"
	@echo "  make release        - Build the project (Release)"
	@echo "  make clean          - Clean build artifacts"
	@echo "  make restore        - Restore NuGet packages"
	@echo "  make test           - Run all tests"
	@echo "  make test-verbose   - Run tests with detailed output"
	@echo "  make run            - Run the compiler"
	@echo "  make run-example    - Run compiler with example file"
	@echo "  make watch          - Watch and rebuild on changes"
	@echo "  make format         - Format code"
	@echo "  make lint           - Check code style"
	@echo "  make dev            - Clean + Restore + Build + Test"
	@echo "  make install        - Install required tools"
	@echo "  make all            - Build everything"
	@echo ""

# Build the project
build:
	@echo "🔨 Building project..."
	dotnet build Compiler.sln

# Build in Release mode
release:
	@echo "🔨 Building project (Release)..."
	dotnet build Compiler.sln -c Release

# Clean build artifacts
clean:
	@echo "🧹 Cleaning..."
	dotnet clean Compiler.sln
	@find . -type d -name "bin" -exec rm -rf {} + 2>/dev/null || true
	@find . -type d -name "obj" -exec rm -rf {} + 2>/dev/null || true
	@echo "✅ Clean complete"

# Restore NuGet packages
restore:
	@echo "📦 Restoring packages..."
	dotnet restore Compiler.sln

# Run all tests
test:
	@echo "🧪 Running tests..."
	dotnet test Compiler.sln --verbosity minimal

# Run tests with verbose output
test-verbose:
	@echo "🧪 Running tests (verbose)..."
	dotnet test Compiler.sln --verbosity detailed

# Run tests with coverage (if you add coverage tool later)
test-coverage:
	@echo "🧪 Running tests with coverage..."
	dotnet test Compiler.sln --collect:"XPlat Code Coverage"

# Run the compiler
run:
	@echo "🚀 Running compiler..."
	dotnet run --project src/Compiler/Compiler.csproj

# Run with example file
run-example:
	@echo "🚀 Running compiler with example..."
	@cd src/Compiler && dotnet run

# Watch mode - rebuild on changes
watch:
	@echo "👀 Watching for changes..."
	dotnet watch --project src/Compiler/Compiler.csproj run

# Format code
format:
	@echo "✨ Formatting code..."
	dotnet format Compiler.sln

# Check code style without modifying
lint:
	@echo "🔍 Checking code style..."
	dotnet format Compiler.sln --verify-no-changes

# Development workflow: clean, restore, build, test
dev: clean restore build test
	@echo "✅ Development build complete!"

# Build everything
all: restore build
	@echo "✅ Build complete!"

# Install development tools
install:
	@echo "📦 Installing development tools..."
	dotnet tool install -g dotnet-format || dotnet tool update -g dotnet-format
	@echo "✅ Tools installed!"

# Quick rebuild
rebuild: clean build

# Run tests continuously
test-watch:
	@echo "👀 Watching tests..."
	dotnet watch --project tests/tests.csproj test

