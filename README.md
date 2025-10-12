# Compiler

An imperative programming language compiler developed as an educational project.

## Features

- ✅ **Lexical Analysis** - Hand-written lexer for tokenization
- ✅ **Syntax Analysis** - GPPG-based parser with custom lexer integration
- ✅ **AST Generation** - Full Abstract Syntax Tree construction
- 🔄 Semantic Analysis (TODO)
- 🔄 Code Generation (TODO)

## Architecture

The compiler is structured in phases:

1. **Lexer** (`src/Compiler/Lexer/`) - Hand-written lexical analyzer
   - `LexerClass.cs` - Main lexer implementation
   - `Token.cs` - Token classes
   - `TokenType.cs` - Token type definitions
   - `TokenDefinitions.cs` - Keywords and operators

2. **Parser** (`src/Compiler/Parser/`) - GPPG-based syntax analyzer
   - `Grammar.y` - Grammar specification
   - `LexerAdapter.cs` - Bridge between custom lexer and GPPG
   - `ParserFacade.cs` - Public API for parsing
   - `Generated/` - Auto-generated parser code

3. **AST** (`src/Compiler/AST/`) - Abstract Syntax Tree
   - `AstNodes.cs` - Node definitions for all language constructs

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
make run-file FILE=examples/simple_valid.imperative

# Compile with detailed AST output (debug mode) - two ways:
make run-file FILE=examples/test_valid.imperative DEBUG=1
make run-debug FILE=examples/test_valid.imperative

# Run debug mode with default file
make run-debug

# Run lexer only
make run-lexer FILE=examples/test.imperative

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
dotnet run --project src/Compiler/Compiler.csproj -- examples/simple_valid.imperative
dotnet run --project src/Compiler/Compiler.csproj -- --help

# Run with detailed AST output (debug mode)
dotnet run --project src/Compiler/Compiler.csproj -- examples/test_valid.imperative --debug

# Run lexer only
dotnet run --project src/Compiler/Compiler.csproj -- --lexer-only examples/test.imperative

# Tests
dotnet test Compiler.sln
dotnet test --verbosity detailed

# Clean
dotnet clean Compiler.sln
```

## Examples

The `examples/` directory contains sample programs:
- `simple_valid.imperative` - Simple example with variables and a routine
- `test_valid.imperative` - Complex example with types, arrays, loops, and conditionals
- `complex_expressions.imperative` - Advanced example with complex expressions and nested operations
- `quick_demo.imperative` - Quick demo with loops and conditions

**Note:** File paths are relative to the current working directory.  
Run commands from the project root for best results.

## Output Modes

### Normal Mode
Shows compact AST summary:
```bash
dotnet run --project src/Compiler/Compiler.csproj -- examples/simple_valid.imperative
```

Output:
```
AST STRUCTURE:
Variable: x : integer = <expr>
Variable: y : real = <expr>
Routine: main(0 params)
  Body: 0 decls, 3 stmts
```

### Debug Mode (Detailed AST Tree)
Shows complete AST tree with all nodes and properties:
```bash
dotnet run --project src/Compiler/Compiler.csproj -- examples/complex_expressions.imperative --debug
```

Output shows full tree structure including:
- All expression details (operators, operands, values)
- Complete statement structure
- Type information
- Function parameters
- Record fields
- Array elements

### Lexer Only Mode
Shows only tokens without parsing:
```bash
dotnet run --project src/Compiler/Compiler.csproj -- --lexer-only examples/test.imperative
```

## Language Features

The imperative language supports:

- **Primitive Types**: `integer`, `real`, `boolean`
- **User-Defined Types**: Records and Arrays
- **Variables**: With optional initialization
- **Routines**: Functions with parameters and optional return type
- **Control Flow**: `if-then-else`, `while`, `for-in-range`
- **Expressions**: Arithmetic, logical, comparison operators
- **I/O**: `print` statement

### Example Program

```
type IntArray is array[5] integer

var x : integer is 42
var arr : IntArray is [1, 2, 3, 4, 5]

routine main()
    for i in 1 .. 5 loop
        print arr[i]
    end
    
    if (x > 10) then
        print x
    end
end
```

## Parser Integration

The parser is built using **GPPG** (Gardens Point Parser Generator) version 1.5.3.1. The integration includes:

- Custom lexer adapter that bridges the hand-written lexer to GPPG
- Complete grammar specification in `Grammar.y`
- Automatic parser generation during build
- Full AST construction with semantic actions

For more details on the parser integration, see `docs/parser_integration_status.md`.

## Testing

The project includes comprehensive test suites for both lexer and parser:

```bash
# Run all tests (65 tests)
make test

# Run only lexer tests (23 tests)
dotnet test --filter "FullyQualifiedName~LexerTests"

# Run only parser tests (42 tests)
dotnet test --filter "FullyQualifiedName~ParserTests"

# Verbose output
make test-verbose
```

### Test Coverage

**Lexer Tests (23):**
- Identifiers, keywords, literals
- Operators and delimiters
- Record access notation
- Error handling

**Parser Tests (42):**
- Variable declarations (with/without initialization)
- Type declarations (record, array, user-defined, nested records)
- Routine declarations (with/without parameters, return types)
- Statements (assignment, if-then-else, loops, return, print)
- Expressions (binary, unary, precedence, nested)
- Record access (simple and nested up to 4 levels)
- Array access
- Complex programs
- Error handling

All tests read code from external files in `tests/test_files/` (23 files) and `tests/parser_test_files/` (28 files), making it easy to add new test cases.

## To-do's

- [x] Implement Lexer
- [x] Implement Parser
- [x] Add comprehensive tests
- [ ] Implement Semantic Analyzer
- [ ] Implement Code Generator
- [ ] Add support for comments

## Requirements

- .NET 9.0 SDK or higher
- Make (for using Makefile)

## License

See LICENSE file
