.PHONY: help build clean test test-verbose dev format run run-debug run-lexer install

# Default target
help:
	@echo "🔧 Compiler Project - Available commands:"
	@echo ""
	@echo "  make build                    - Build the project"
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

# Build the project
build:
	@echo "🔨 Building project..."
	dotnet build Compiler.sln

# Clean build artifacts
clean:
	@echo "🧹 Cleaning..."
	dotnet clean Compiler.sln
	@find . -type d -name "bin" -exec rm -rf {} + 2>/dev/null || true
	@find . -type d -name "obj" -exec rm -rf {} + 2>/dev/null || true
	@echo "✅ Clean complete"

# Run all tests
test:
	@echo "🧪 Running tests..."
	dotnet test Compiler.sln --verbosity minimal

# Run tests with verbose output
test-verbose:
	@echo "🧪 Running tests (verbose)..."
	dotnet test Compiler.sln --verbosity detailed

# Development workflow: restore, build, test
dev:
	@echo "🔧 Development workflow..."
	@echo "📦 Restoring packages..."
	dotnet restore Compiler.sln
	@echo "🔨 Building project..."
	dotnet build Compiler.sln
	@echo "🧪 Running tests..."
	dotnet test Compiler.sln --verbosity minimal
	@echo "✅ Development workflow complete!"

# Format code
format:
	@echo "✨ Formatting code..."
	dotnet format Compiler.sln

# Run compiler with file (usage: make run f=file.imperative)
run:
	@if [ -z "$(f)" ]; then \
		echo "❌ Error: f parameter is required"; \
		echo "Usage: make run f=<file>"; \
		echo "Example: make run f=examples/test.imperative"; \
		exit 1; \
	fi
	@echo "🚀 Running compiler with file: $(f)"; \
	dotnet run --project src/Compiler/Compiler.csproj -- "$(f)"

# Run compiler with debug output (usage: make run-debug f=file.imperative)
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