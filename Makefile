.PHONY: help build release release-linux release-mac release-win clean test test-verbose dev format run run-debug run-lexer install

help:
	@echo "🔧 Compiler Project - Available commands:"
	@echo ""
	@echo "  make build                    - Build the project (Debug)"
	@echo "  make release                  - Build release version (macOS)"
	@echo "  make release-linux            - Build release version (Linux)"
	@echo "  make release-win              - Build release version (Windows)"
	@echo "  make clean                    - Clean build artifacts"
	@echo "  make test                     - Run all tests"
	@echo "  make test-verbose             - Run tests with detailed output"
	@echo "  make dev                      - Restore + Build + Test"
	@echo "  make format                   - Format code"
	@echo "  make install                  - Install required tools"
	@echo "  make run f=<file>             - Run compiler with file"
	@echo "  make run-debug f=<file>       - Run compiler with debug output"
	@echo "  make run-lexer f=<file>       - Run lexer only with file"
	@echo ""
	@echo "Examples:"
	@echo "  make run f=examples/test.imperative"
	@echo "  make run-debug f=examples/test.imperative"
	@echo "  make run-lexer f=examples/test.imperative"
	@echo ""

build:
	@echo "🔨 Building project (Debug)..."
	dotnet build Compiler.sln

release:
	@echo "🚀 Building release version for macOS..."
	dotnet build Compiler.sln --configuration Release
	@echo "📦 Publishing single-file executable..."
	dotnet publish src/Compiler/Compiler.csproj --configuration Release --output ./release/macos --runtime osx-x64
	@echo "✅ macOS release build complete!"
	@echo "📁 Executable location: ./release/macos/Compiler"

release-linux:
	@echo "🚀 Building release version for Linux..."
	dotnet build Compiler.sln --configuration Release
	@echo "📦 Publishing single-file executable..."
	dotnet publish src/Compiler/Compiler.csproj --configuration Release --output ./release/linux --runtime linux-x64
	@echo "✅ Linux release build complete!"
	@echo "📁 Executable location: ./release/linux/Compiler"

release-win:
	@echo "🚀 Building release version for Windows..."
	dotnet build Compiler.sln --configuration Release
	@echo "📦 Publishing single-file executable..."
	dotnet publish src/Compiler/Compiler.csproj --configuration Release --output ./release/windows --runtime win-x64
	@echo "✅ Windows release build complete!"
	@echo "📁 Executable location: ./release/windows/Compiler.exe"

clean:
	@echo "🧹 Cleaning..."
	dotnet clean Compiler.sln
	@find . -type d -name "bin" -exec rm -rf {} + 2>/dev/null || true
	@find . -type d -name "obj" -exec rm -rf {} + 2>/dev/null || true
	@rm -rf ./release 2>/dev/null || true
	@echo "✅ Clean complete"

test:
	@echo "🧪 Running tests..."
	dotnet test Compiler.sln --verbosity minimal

test-verbose:
	@echo "🧪 Running tests (verbose)..."
	dotnet test Compiler.sln --verbosity detailed

dev:
	@echo "🔧 Development workflow..."
	@echo "📦 Restoring packages..."
	dotnet restore Compiler.sln
	@echo "🔨 Building project..."
	dotnet build Compiler.sln
	@echo "🧪 Running tests..."
	dotnet test Compiler.sln --verbosity minimal
	@echo "✅ Development workflow complete!"

format:
	@echo "✨ Formatting code..."
	dotnet format Compiler.sln

run:
	@if [ -z "$(f)" ]; then \
		echo "❌ Error: f parameter is required"; \
		echo "Usage: make run f=<file>"; \
		echo "Example: make run f=examples/test.imperative"; \
		exit 1; \
	fi
	@echo "🚀 Running compiler with file: $(f)"; \
	dotnet run --project src/Compiler/Compiler.csproj -- "$(f)"

run-debug:
	@if [ -z "$(f)" ]; then \
		echo "❌ Error: f parameter is required"; \
		echo "Usage: make run-debug f=<file>"; \
		echo "Example: make run-debug f=examples/test.imperative"; \
		exit 1; \
	fi
	@echo "🔍 Running compiler in debug mode: $(f)"; \
	dotnet run --project src/Compiler/Compiler.csproj -- "$(f)" --debug

# Run lexer only with file (usage: make run-lexer f=file.imperative)
run-lexer:
	@if [ -z "$(f)" ]; then \
		echo "❌ Error: f parameter is required"; \
		echo "Usage: make run-lexer f=<file>"; \
		echo "Example: make run-lexer f=examples/test.imperative"; \
		exit 1; \
	fi
	@echo "🔤 Running lexer only with file: $(f)"; \
	dotnet run --project src/Compiler/Compiler.csproj -- --lexer-only "$(f)"

# Install development tools
install:
	@echo "📦 Installing development tools..."
	dotnet tool install -g dotnet-format || dotnet tool update -g dotnet-format
	@echo "✅ Tools installed!"