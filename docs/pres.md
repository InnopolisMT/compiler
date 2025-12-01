# Презентация проекта компилятора

## Команда
Наша команда: **Tourists**  
Участники: **Danila Khrankou & Tsimafei Kurstak**

---

## 1. Лексический анализатор (Lexer)

### 1.1. Общая архитектура

Лексический анализатор реализован вручную (hand-written) и состоит из 4 основных файлов:

- **`LexerClass.cs`** — основная логика лексического анализа
- **`Token.cs`** — классы токенов и информация о позиции
- **`TokenType.cs`** — перечисление всех типов токенов
- **`TokenDefinitions.cs`** — определения ключевых слов и операторов

### 1.2. Основной класс `LexerClass`

#### Поля и состояние
```csharp
private readonly string _source;  // Исходный код
private int _position;             // Текущая позиция в исходнике
private int _line;                 // Текущая строка
private int _column;               // Текущий столбец
private char _currentChar;         // Текущий символ
```

#### Основные методы

**`NextToken()`** — возвращает следующий токен из исходного кода.
**`ParseNumber()`** — разбирает числовые литералы.
**`ParseIdentifierOrKeyword()`** — разбирает идентификаторы и ключевые слова.
**`ReadOperator()`** — разбирает операторы.

**Вспомогательные методы:**
- `Move()` — переходит к следующему символу.
- `LookAhead()` — смотрит следующий символ без перехода.
- `SkipWhitespace()` — пропускает пробелы.
- `IsValidStartChar()` — проверяет допустимость начального символа токена.

### 1.3. Система токенов

#### Иерархия классов токенов

Базовый абстрактный класс **`Token`**:
```csharp
public abstract class Token(TokenType type, string lexeme, Span span)
{
    public TokenType Type { get; }      // Тип токена
    public Span Span { get; }           // Позиция в исходнике
    public string Lexeme { get; }       // Исходная строка
}
```

**Специализированные классы токенов:**

- **`IdentifierToken`** — идентификаторы (имена переменных, функций)
- **`IntegerToken`** — целочисленные литералы (хранит значение типа `long`)
- **`RealToken`** — вещественные литералы (хранит значение типа `double`)
- **`BooleanToken`** — булевы литералы (`true`/`false`, хранит `bool`)
- **`RecordAccessToken`** — доступ к полям записей (например, `record.field.subfield`)
- **`SimpleToken`** — ключевые слова и операторы

#### Класс `Span` — информация о позиции
```csharp
public class Span(int line, int start, int end)
{
    public int Line { get; }    // Номер строки
    public int Start { get; }   // Начальный столбец
    public int End { get; }     // Конечный столбец
}
```

Позволяет точно указывать, где находится токен, что критично для сообщений об ошибках.

### 1.4. Определения токенов

#### Примеры реализации

Перечисление типов токенов:
```csharp
public enum TokenType
{
    tkIntegerLiteral = 1,
    tkRealLiteral = 2,
    // ... остальные типы
    tkInvalid = 51
}
```

Словари ключевых слов и операторов:
```csharp
public static readonly Dictionary<string, TokenType> Keywords = new()
{
    {"var", TokenType.tkVar},
    {"integer", TokenType.tkIntegerKeyword},
    // ... остальные ключевые слова
};

public static readonly Dictionary<string, TokenType> Operators = new()
{
    {":=", TokenType.tkAssign},
    {"<=", TokenType.tkLessThanOrEqual},
    // ... остальные операторы
};
```

### 1.5. Обработка ошибок

- **Невалидные числа** — если после цифр идут буквы или несколько точек
- **Невалидные идентификаторы** — если после букв/цифр идут недопустимые символы
- **Неизвестные символы** — любые символы, не входящие в грамматику
- **Комментарии** — пропускаются (строки, начинающиеся с `#`)

Все ошибки возвращаются как токены типа `tkInvalid` с сохранением позиции.

---

## 2. Синтаксический анализатор (Parser)

### 2.1. Общая архитектура

Синтаксический анализатор построен на основе генератора парсеров **GPPG** (Gardens Point Parser Generator) версии 1.5.3.1. Это LR-парсер, который автоматически генерирует код на основе грамматики.

Структура парсера:

- **`Grammar.y`** — спецификация грамматики (714 строк)
- **`LexerAdapter.cs`** — адаптер для интеграции лексера с GPPG
- **`ParserFacade.cs`** — адаптер для удобного использования парсера
- **`LexLocation.cs`** — класс для хранения позиции в исходном коде
- **`Generated/Parser.cs`** — автоматически сгенерированный парсер


### 2.3. Адаптер лексера `LexerAdapter`

`LexerAdapter` — это **мост** между hand-written лексером и GPPG-парсером. Он наследуется от `AbstractScanner<object, LexLocation>` из библиотеки GPPG.

#### Маппинг токенов

Ключевой компонент — словарь `TokenMap`, который отображает типы токенов лексера на числовые коды GPPG:

```csharp
private static readonly Dictionary<TokenType, int> TokenMap = new()
{
    { TokenType.tkIntegerLiteral, (int)Tokens.tkIntegerLiteral },
    { TokenType.tkRealLiteral, (int)Tokens.tkRealLiteral },
    // ... всего 51 маппинг
    { TokenType.tkInvalid, (int)Tokens.tkInvalid }
};
```

`Tokens` — это `enum`, автоматически сгенерированный GPPG на основе `%token` деклараций в `Grammar.y`.

#### Метод `yylex()`

Главный метод, который вызывает парсер для получения следующего токена:

```csharp
public override int yylex()
{
    _currentToken = _lexer.NextToken();

    // Устанавливаем позицию токена
    yylloc = new LexLocation(
        _currentToken.Span.Line,
        _currentToken.Span.Start,
        _currentToken.Span.Line,
        _currentToken.Span.End
    );

    // Пропускаем токены переноса строки
    if (_currentToken.Type == TokenType.tkEOL)
    {
        return yylex();
    }

    // Извлекаем значение токена
    yylval = GetTokenValue(_currentToken);

    // Преобразуем тип токена лексера в код GPPG
    if (TokenMap.TryGetValue(_currentToken.Type, out int parserToken))
    {
        return parserToken;
    }

    return (int)Tokens.error;
}
```


#### Метод `GetTokenValue()`

Извлекает значения из специализированных токенов:

```csharp
private object GetTokenValue(Token token)
{
    return token switch
    {
        IntegerToken intToken => intToken.Value,      // long
        RealToken realToken => realToken.Value,       // double
        BooleanToken boolToken => boolToken.Value,    // bool
        IdentifierToken idToken => idToken.Name,      // string
        RecordAccessToken recordToken => recordToken.Lexeme, // string
        _ => token.Lexeme                             // string по умолчанию
    };
}
```

Эти значения становятся доступны в семантических действиях грамматики через `$1`, `$2` и т.д.

### 2.4. Класс `LexLocation`

Хранит информацию о позиции конструкций в исходном коде:

```csharp
public class LexLocation : QUT.Gppg.IMerge<LexLocation>
{
    public int StartLine { get; set; }
    public int StartColumn { get; set; }
    public int EndLine { get; set; }
    public int EndColumn { get; set; }

    public LexLocation Merge(LexLocation last)
    {
        return new LexLocation(StartLine, StartColumn, 
                               last.EndLine, last.EndColumn);
    }
}
```

Метод `Merge()` позволяет объединять позиции нескольких токенов в одну (например, для всего выражения).

### 2.5. Фасад `ParserFacade`

Предоставляет простой API для использования парсера:

```csharp
public class ParserFacade
{
    private readonly LexerClass _lexer;

    public ParserFacade(LexerClass lexer)
    {
        _lexer = lexer;
    }

    public ProgramNode Parse()
    {
        var adapter = new LexerAdapter(_lexer);
        var parser = new Parser(adapter);

        if (!parser.Parse())
        {
            throw new ParseException("Parsing failed");
        }

        return parser.Result!;
    }
}
```

**Процесс парсинга:**
1. Создаётся `LexerAdapter`, обёртывающий лексер
2. Создаётся `Parser` (сгенерированный GPPG), получающий адаптер
3. Вызывается `parser.Parse()`, запускающий процесс разбора
4. Возвращается корень AST (`ProgramNode`)

### 2.6. Построение AST

В грамматике для каждого правила указаны **семантические действия** в фигурных скобках:

```yacc
VariableDeclaration
    : tkVar tkIdentifier tkColon Type tkIs Expression
        {
            $$ = new VariableDeclarationNode
            {
                Name = (string)$2,           // значение tkIdentifier
                Type = (TypeNode)$4,          // результат разбора Type
                InitialValue = (ExpressionNode)$6  // результат разбора Expression
            };
        }
```

- `$1`, `$2`, ... — значения токенов/нетерминалов справа от `:`
- `$$` — результат данного правила
- Создаются узлы AST (`VariableDeclarationNode`, `ExpressionNode`, и т.д.)

### 2.7. Обработка ошибок

При ошибке парсинга вызывается метод `yyerror`:

```csharp
public override void yyerror(string format, params object[] args)
{
    var message = string.Format(format, args);
    var location = yylloc != null
        ? $" at line {yylloc.StartLine}, column {yylloc.StartColumn}"
        : "";
    throw new ParseException($"Parse error{location}: {message}");
}
```

Выбрасывается исключение `ParseException` с указанием позиции ошибки.

---

## 3. Семантический анализатор (Semantic Analyzer)

### 3.1. Общая архитектура

Семантический анализатор выполняет проверку корректности программы на уровне типов, областей видимости и семантических правил языка.

Структура семантического анализатора:

- **`SemanticAnalyzer.cs`** — основной класс анализа (1075 строк)
- **`SymbolTable.cs`** — таблица символов с поддержкой областей видимости
- **`Type.cs`** — система типов (примитивные, массивы, записи)
- **`Symbol.cs`** — представление символов (переменные, типы, процедуры)
- **`Scope.cs`** — управление областями видимости
- **`SemanticError.cs`** — представление семантических ошибок
- **`TypeNodeFactory.cs`** — утилиты создания типовых узлов

### 3.2. Двухпроходный анализ

Анализатор работает в **два прохода** для корректной обработки forward declarations и взаимных ссылок:

#### Первый проход (Pass1) — объявления и таблица символов

```csharp
private void Pass1_Declarations(ProgramNode program)
{
    foreach (var declaration in program.Declarations)
    {
        switch (declaration)
        {
            case TypeDeclarationNode typeDecl:
                ProcessTypeDeclaration(typeDecl);
                break;
            case RoutineDeclarationNode routineDecl:
                ProcessRoutineDeclaration(routineDecl);
                break;
        }
    }
    Pass1_CheckForwardDeclarations(program);
}
```

**Задачи первого прохода:**
- Регистрация всех типов в таблице символов
- Регистрация всех процедур (с поддержкой forward declarations)
- Обнаружение циклических зависимостей типов
- Проверка дублирования имён в одной области видимости
- Валидация forward declarations (наличие полной реализации)

#### Второй проход (Pass2) — проверка типов

```csharp
private void Pass2_TypeChecking(ProgramNode program)
{
    foreach (var declaration in program.Declarations)
    {
        switch (declaration)
        {
            case VariableDeclarationNode varDecl:
                CheckVariableDeclaration(varDecl);
                break;
            case RoutineDeclarationNode routineDecl:
                CheckRoutineDeclaration(routineDecl);
                break;
        }
    }
}
```

**Задачи второго прохода:**
- Проверка типов выражений
- Проверка совместимости типов при присваивании
- Проверка аргументов вызовов процедур
- Проверка условий в циклах и ветвлениях
- Анализ путей возврата из процедур
- Проверка доступа к полям записей и элементам массивов

### 3.3. Таблица символов и области видимости

#### Класс `SymbolTable`

Управляет иерархией областей видимости с использованием стека:

```csharp
public class SymbolTable
{
    private Scope _currentScope;
    private readonly Stack<Scope> _scopeStack;
    public Scope GlobalScope { get; private set; }

    public void PushScope(string? scopeName = null)  // Вход в область
    public bool PopScope()                            // Выход из области
    public bool Enter(string name, Symbol symbol)     // Регистрация символа
    public Symbol? Lookup(string name)                // Поиск по иерархии
    public Symbol? LookupLocal(string name)           // Поиск в текущей области
}
```

**Встроенные типы:**
При инициализации регистрируются базовые типы:
- `integer` — целые числа
- `real` — вещественные числа
- `boolean` — логические значения

#### Класс `Scope`

Представляет одну область видимости:

```csharp
public class Scope
{
    public Scope? Parent { get; }                     // Родительская область
    public int Level { get; }                         // Уровень вложенности
    private Dictionary<string, Symbol> _symbols;      // Символы в области

    public bool Define(string name, Symbol symbol)    // Добавить символ
    public Symbol? Resolve(string name)               // Найти с учётом родителей
    public Symbol? ResolveLocal(string name)          // Найти только локально
}
```

Области образуют **иерархию**: глобальная → процедура → вложенный блок.

### 3.4. Система типов

#### Абстрактный класс `Type`

```csharp
public abstract class Type
{
    public abstract string Name { get; }
    public abstract bool IsCompatibleWith(Type other);
    protected abstract bool EqualsType(Type other);
}
```

#### Типы в системе

**`PrimitiveType`** — примитивные типы:
```csharp
public enum PrimitiveKind { Integer, Real, Boolean }

public sealed class PrimitiveType : Type
{
    public PrimitiveKind Kind { get; }
    
    public override bool IsCompatibleWith(Type other)
    {
        // Поддержка неявного преобразования integer → real
        // Поддержка преобразования integer ↔ boolean
    }
}
```

**`ArrayType`** — массивы:
```csharp
public sealed class ArrayType : Type
{
    public Type ElementType { get; }
    public int Size { get; }
    
    public override bool IsCompatibleWith(Type other)
    {
        // Строгая совместимость: одинаковый размер и совместимые элементы
    }
}
```

**`RecordType`** — записи (структуры):
```csharp
public sealed class RecordType : Type
{
    private Dictionary<string, Type> _fields;
    public IReadOnlyDictionary<string, Type> Fields { get; }
    
    public override bool IsCompatibleWith(Type other)
    {
        // Структурная совместимость: одинаковые поля и типы
    }
}
```

### 3.5. Символы

```csharp
public enum SymbolKind
{
    Variable,    // Переменная
    Type,        // Пользовательский тип
    Routine,     // Процедура/функция
    Parameter    // Параметр процедуры
}

public class Symbol
{
    public string Name { get; set; }
    public SymbolKind Kind { get; set; }
    public Type? Type { get; set; }
    public Dictionary<string, object?> Attributes { get; set; }
    public object? DeclarationNode { get; set; }
    public Scope? Scope { get; set; }
}
```

Символы хранят всю информацию об идентификаторах: тип, вид, узел объявления и область видимости.

### 3.6. Ключевые семантические проверки

#### Проверка forward declarations

```csharp
private void ProcessRoutineDeclaration(RoutineDeclarationNode routineDecl)
{
    if (IsFullDeclaration(routineDecl))
    {
        // Полная декларация — проверяем совпадение с forward
        if (_forwardDeclarations.ContainsKey(routineDecl.Name))
        {
            var forward = _forwardDeclarations[routineDecl.Name];
            if (!SignaturesMatch(forward, routineDecl))
            {
                AddError(..., "Routine signature doesn't match forward declaration");
            }
            _forwardDeclarations.Remove(routineDecl.Name);
        }
    }
    else
    {
        // Forward declaration — сохраняем для последующей проверки
        _forwardDeclarations[routineDecl.Name] = routineDecl;
    }
}
```

#### Обнаружение циклических типов

```csharp
private void ProcessTypeDeclaration(TypeDeclarationNode typeDecl)
{
    if (_typeResolutionStack.Contains(typeDecl.Name))
    {
        AddError(..., "Circular type definition detected");
        return;
    }
    
    _typeResolutionStack.Add(typeDecl.Name);
    var type = ResolveTypeNode(typeDecl.Type);
    _typeResolutionStack.Remove(typeDecl.Name);
}
```

#### Проверка совместимости типов

```csharp
private void CheckAssignment(AssignmentNode assign)
{
    var targetType = AnalyzeExpression(assign.Target);
    var valueType = AnalyzeExpression(assign.Value);
    
    if (!AreCompatible(targetType, valueType))
    {
        AddError(..., $"Cannot assign {valueType} to {targetType}");
    }
}
```

#### Анализ путей возврата

Проверяет, что все пути выполнения функции возвращают значение:
- Анализирует ветвления (`if-else`)
- Проверяет наличие `return` во всех ветках
- Сообщает об отсутствующих возвратах

#### Проверка границ массивов

- **Статическая проверка** — для константных индексов
- **Динамическая проверка** — для переменных индексов (генерация runtime-проверок)

### 3.7. Обработка ошибок

```csharp
public class SemanticError
{
    public int Line { get; }
    public int Column { get; }
    public string Message { get; }
    
    public string Format(string? fileName = null)
    {
        return $"{fileName}:{Line}:{Column}: {Message}";
    }
}
```

Все семантические ошибки собираются в список, что позволяет показать пользователю сразу все проблемы, а не только первую.

---

## 4. Оптимизации

### 4.1. Общая информация

Компилятор включает модуль оптимизации, который выполняет преобразования AST перед генерацией кода.

Структура:
- **`ConstantFolder.cs`** — свёртка констант (constant folding)

### 4.2. Свёртка констант (Constant Folding)

`ConstantFolder` выполняет вычисление константных выражений **на этапе компиляции**, заменяя их результатами.


7 = 7           → true
**Унарные** (`-`, `not`):
```
-42             → -42
not false       → true
```
**Условный оператор**:
```csharp
if (false) then       →  // заменяется на тело else
    print 1
**Цикл while**:
```csharp
while (false) loop    →  // полностью удаляется

#### Примеры преобразований

else
    print y
end
var y : boolean is true

print y
```

## 5. Генерация исполняемого файла (WASM)

### 5.1. Что генерируем
- Собираем секции: `type`, `import`, `func`, `memory`, `global`, `export`, `code`.
- Эмитим инструкции по AST (арифметика, логика, управление, память).
- Все индексы согласованы по таблицам типов/функций; длины секций и числа кодируются LEB128.

### 5.2. Как это реализовано
- `CodeGenerator` (Visitor) обходит оптимизированный AST и формирует сигнатуры, локальные переменные, тела функций.
- `WasmModuleBuilder` регистрирует типы, импорты (например, `env.print`), функции, память и экспорты.
- `WasmSerializer` пишет бинарник: магическое число/версию, затем секции по спецификации, со всеми оффсетами и индексами.

Мини-фрагмент сериализации секции (псевдокод):
```csharp
WriteByte(0x00);           // custom (если нужно)
WriteByte(0x01);           // type section id
WriteVarUint(length);      // LEB128 длина секции
WriteVarUint(typeCount);
// ... записи типов функций: params/result → кодируются как valtype (0x7F i32, 0x7E i64, 0x7D f32, 0x7C f64)
```

### 5.3. Пример результата (WAT для наглядности)
```wat
(module
  (memory (export "memory") 1)
  (import "env" "print" (func $print (param i32)))
  (func $main
    (local $x i32)
    (local $y i32)
    i32.const 50
    local.set $x
    i32.const 1
    local.set $y
    local.get $y
    call $print
  )
  (export "main" (func $main))
)
```

### 5.4. Как запускаем

- Браузер:
```js
const bytes = await fetch('out.wasm').then(r => r.arrayBuffer());
const { instance } = await WebAssembly.instantiate(bytes, {
  env: { print: (x) => console.log(x) }
});
instance.exports.main();
```
- Node.js:
```bash
node -e "const fs=require('fs');(async()=>{const wasm=await WebAssembly.instantiate(fs.readFileSync('out.wasm'),{env:{print:x=>console.log(x)}}); wasm.instance.exports.main();})();"
```

 

---
