# Compiler

An imperative programming language compiler developed as an educational project.

## Makefile Commands

The project includes a Makefile with convenient commands for common tasks.

### Basic Commands

```bash
make help          # Show all available commands
make build         # Build the project
make test          # Run tests
make run           # Run the compiler (with default file)
make clean         # Clean build artifacts
```

### Working with Files

```bash
# Compile a specific file
make run-file FILE=examples/simple.imperative

# Show compiler help
make run-help

# Run with example file
make run-example
```

### Development

```bash
make dev           # Clean + Restore + Build + Test
make watch         # Auto-rebuild on changes
make format        # Format code
make lint          # Check code style
make rebuild       # Full rebuild
```

### Additional Commands

```bash
make release       # Build Release version
make test-verbose  # Run tests with detailed output
make test-watch    # Run tests on changes
make install       # Install development tools
```

## Using dotnet CLI Directly

If you prefer using dotnet CLI directly:

```bash
# Build and run
dotnet build Compiler.sln
dotnet run --project src/Compiler/Compiler.csproj
dotnet run --project src/Compiler/Compiler.csproj -- examples/simple.imperative
dotnet run --project src/Compiler/Compiler.csproj -- --help

# Tests
dotnet test Compiler.sln
dotnet test --verbosity detailed

# Clean
dotnet clean Compiler.sln
```

## Examples

The `examples/` directory contains sample programs:
- `test.imperative` - Full example with types, arrays, and loops
- `simple.imperative` - Simple example with variables

**Note:** File paths are relative to the current working directory.  
Run commands from the project root for best results.

## To-do's

- [ ] Fix invalid words (e.g., `4fa` should be one `tkInvalid` token, currently produces 3 different `tkInvalid` tokens)
- [ ] Implement Parser
- [ ] Implement Semantic Analyzer
- [ ] Implement Code Generator

## Questions

- Should we add support for multi-line comments?
- What optimizations should be applied at the lexer level?

## Requirements

- .NET 9.0 SDK or higher
- Make (for using Makefile)

## License

See LICENSE file
