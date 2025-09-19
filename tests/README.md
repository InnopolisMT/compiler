# Lexer Tests

This directory contains comprehensive tests for the LexerClass implementation.

## Test Structure

### Test Files Location
All test input files are located in the `test_files/` directory. Each test file contains specific lexemes or language constructions that are used to verify the lexer's behavior.

### Test Categories

#### 1. Basic Lexemes
- **basic_identifiers.txt**: Simple identifier tokens
- **integer_literals.txt**: Integer number literals
- **real_literals.txt**: Real (floating-point) number literals
- **boolean_literals.txt**: Boolean literals (true/false)
- **keywords.txt**: Language keywords
- **operators.txt**: Basic operators (+, -, *, /, :=, =, etc.)
- **delimiters.txt**: Delimiter symbols ((, ), [, ], ,, :)

#### 2. Language Constructions
- **type_declarations.txt**: Type definition with record structure
- **array_declarations.txt**: Array type declarations
- **control_structures.txt**: If statements and for loops
- **array_initialization.txt**: Array initialization with values
- **assignment_statements.txt**: Variable and record field assignments
- **conditional_expressions.txt**: Conditional expressions with comparisons
- **print_statements.txt**: Print statement examples

#### 3. Complex Cases
- **record_access.txt**: Simple record field access
- **nested_record_access.txt**: Nested record field access (person.addr.streetCode)
- **range_with_expressions.txt**: Range expressions with record access
- **complex_cases.txt**: Complex for loops with record access in ranges

#### 4. Error Cases
- **error_cases.txt**: Basic error cases (invalid numbers, invalid characters)
- **more_error_cases.txt**: Extended error cases (multiple dots in numbers, invalid characters)

#### 5. Complete Program
- **complete_program.txt**: A complete program demonstrating all language features

#### 6. Special Cases
- **advanced_operators.txt**: Advanced operators (%, and, or, not, xor)
- **whitespace_handling.txt**: Tests proper whitespace handling

## Test Implementation

The `LexerTests.cs` file contains the test implementation with the following key methods:

### Helper Methods
- `TokenizeFile(string fileName)`: Reads a test file and tokenizes it using LexerClass
- `AssertToken()`: Asserts basic token properties (type, lexeme, span)
- `AssertIntegerToken()`: Asserts integer token properties including value
- `AssertRealToken()`: Asserts real token properties including value
- `AssertBooleanToken()`: Asserts boolean token properties including value
- `AssertRecordAccessToken()`: Asserts record access token properties

### Test Methods
Each test method:
1. Loads a specific test file
2. Tokenizes the content
3. Verifies the expected number of tokens
4. Checks each token's type, lexeme, and span information
5. For specialized tokens, verifies additional properties (values, field paths, etc.)

## Running Tests

To run the tests:

```bash
cd tests
dotnet test
```

To run specific test categories:

```bash
# Run only basic lexeme tests
dotnet test --filter "TestBasicIdentifiers|TestIntegerLiterals|TestRealLiterals|TestBooleanLiterals|TestKeywords|TestOperators"

# Run only error case tests
dotnet test --filter "TestErrorCases|TestMoreErrorCases"

# Run only complex case tests
dotnet test --filter "TestComplexCases|TestRangeWithExpressions|TestNestedRecordAccess"
```

## Expected Token Output Format

The tests verify that tokens are created with the correct format:
- **TokenType**: The type of token (tkIdentifier, tkIntegerLiteral, etc.)
- **Lexeme**: The actual text that was tokenized
- **Span**: Line and column information (Line:Start-End)

Example output format:
```
tkIdentifier: variable,    span: 1:1-8
tkIntegerLiteral: 123,     span: 1:1-3
tkRecordAccess: person.name, span: 1:1-11
```

## Coverage

The test suite covers:
- ✅ All basic token types (identifiers, literals, keywords, operators)
- ✅ All language constructions (types, records, arrays, control structures)
- ✅ Complex cases (nested record access, ranges with expressions)
- ✅ Error handling (invalid tokens, malformed numbers)
- ✅ Edge cases (whitespace handling, mixed identifiers and numbers)
- ✅ Complete program tokenization

This comprehensive test suite ensures that the LexerClass correctly tokenizes all aspects of the universal language as described in the project specification.
