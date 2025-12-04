# Презентация проекта компилятора

## Команда
Наша команда: **Tourists**  
Участники: **Danila Khrankou & Tsimafei Kurstak**

## Реализация
Язык: **ProjectI (Imperative)**
Язык реализации: **C#**
Инструмент: **gppg-based parser**
Целевая платформа: **WASM**

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

### 1.2. Система токенов

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

### 1.3. Определения токенов

#### Примеры реализации

Перечисление типов токенов:
```csharp
public enum TokenType
{
    tkIntegerLiteral = 1,
    tkRealLiteral = 2,
    // ...
}
```

Словари ключевых слов и операторов:
```csharp
public static readonly Dictionary<string, TokenType> Keywords = new()
{
    {"var", TokenType.tkVar},
    {"integer", TokenType.tkIntegerKeyword},
    // ...
};

public static readonly Dictionary<string, TokenType> Operators = new()
{
    {":=", TokenType.tkAssign},
    {"<=", TokenType.tkLessThanOrEqual},
    // ... 
};
```

### 1.4. Обработка ошибок

Все ошибки возвращаются списком токенов типа `tkInvalid` с сохранением позиции.

---

## 2. Синтаксический анализатор (Parser)

### 2.1. Общая архитектура

Синтаксический анализатор построен на основе генератора парсеров **GPPG**

Структура парсера:

- **`Grammar.y`** — спецификация грамматики
- **`LexerAdapter.cs`** — адаптер для интеграции лексера с GPPG
- **`ParserFacade.cs`** — адаптер для использования парсера
- **`LexLocation.cs`** — класс для хранения позиции в исходном коде (обычно поставляется с GPLEX, но у нас особый случай)
- **`Generated/Parser.cs`** — автоматически сгенерированный парсер


### 2.3. Адаптер лексера `LexerAdapter`

`LexerAdapter` — это мост между hand-written лексером и GPPG-парсером. Он наследуется от `AbstractScanner<object, LexLocation>` из библиотеки GPPG

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

### 2.5. Класс `ParserFacade`

Необходимый слой абстракции для использования 

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

```csharp
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

---

## 3. Семантический анализатор (Semantic Analyzer)

### 3.1. Общая архитектура

Семантический анализатор выполняет проверку корректности программы на уровне типов, областей видимости и семантических правил языка.

Структура семантического анализатора:

- **`SemanticAnalyzer.cs`** — основной класс анализа
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

### 3.4. Символы

```csharp
public enum SymbolKind
{
    Variable,    
    Type,        
    Routine,     
    Parameter    
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

### 3.5. Ключевые семантические проверки

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
```pascal
if (false) then       →  // заменяется на тело else
    print 1
```
**Цикл while**:
```pascal
while (false) loop    →  // полностью удаляется
```

## 5. Генерация исполняемого файла (WASM)

### 5.1. Общая архитектура

Генератор кода преобразует оптимизированное AST в бинарный формат WebAssembly. Процесс состоит из нескольких этапов: построение структуры модуля, генерация инструкций и сериализация в бинарный формат.

Структура генератора кода:

- **`WasmCodeGenerator.cs`** — основной класс, координирующий процесс генерации
- **`ExpressionCodeGen.cs`** — генерация кода для выражений
- **`StatementCodeGen.cs`** — генерация кода для операторов
- **`WasmModule.cs`** — представление WASM модуля в памяти
- **`WasmBinaryWriter.cs`** — запись бинарного формата WASM
- **`WasmInstructionBuilder.cs`** — построитель инструкций WASM
- **`LocalsManager.cs`** — управление локальными переменными и параметрами
- **`MemoryManager.cs`** — управление памятью (глобальные переменные, массивы, записи)
- **`WasmTestHarness.cs`** — генерация тестового JS-кода для запуска WASM

### 5.2. Основной класс `WasmCodeGenerator`

Метод `Generate()` выполняет генерацию в следующем порядке:

1. **Настройка импортов** — регистрация функций печати (`printInt`, `printReal`, `printBool`)
2. **Выделение глобальной памяти** — расчёт оффсетов для глобальных переменных
3. **Обработка процедур** — двухпроходный процесс:
   - Первый проход: регистрация сигнатур функций
   - Второй проход: генерация тел функций
4. **Расчёт памяти** — определение необходимого количества страниц памяти
5. **Экспорт памяти и функций**
6. **Запись бинарного файла** — сериализация модуля в `.wasm` формат

### 5.3. Управление памятью

#### Класс `MemoryManager`

Управляет размещением данных в линейной памяти WASM:
**Стратегия размещения:**
- Глобальные переменные размещаются в начале памяти с последовательными оффсетами
- Локальные переменные сложных типов (массивы, записи) размещаются после глобальных
- Примитивные локальные переменные хранятся в WASM locals (не в памяти)

```csharp
public class MemoryManager
{
    private int _globalOffset = 0;
    private readonly Dictionary<string, MemoryLocation> _globalVariables = new();
    
    public void AllocateGlobalVariable(string name, Semantic.Type type)
    {
        int size = CalculateTypeSize(type);
        _globalVariables[name] = new MemoryLocation
        {
            Offset = _globalOffset,
            Size = size,
            IsInMemory = true
        };
        _globalOffset += size;
    }
}
```

**Расчёт размеров типов:**
- Примитивные типы: `integer`/`boolean` → 4 байта, `real` → 8 байт
- Массивы: `размер_элемента × количество_элементов`
- Записи: сумма размеров всех полей

### 5.4. Управление локальными переменными

#### Класс `LocalsManager`

Управляет локальными переменными и параметрами функций:

```csharp
public class LocalsManager
{
    private readonly Dictionary<string, LocalVariable> _locals = new();
    private int _nextLocalIndex = 0;
    private int _localMemoryOffset = 0;
}
```

**Типы переменных:**

1. **Параметры примитивных типов** — хранятся в WASM locals
2. **Параметры сложных типов** — передаются по ссылке (i32 указатель)
3. **Локальные переменные примитивных типов** — хранятся в WASM locals
4. **Локальные переменные сложных типов** — размещаются в памяти

**Методы:**
- `AddParameter()` — регистрация параметра функции
- `AddLocal()` — регистрация локальной переменной
- `AllocateTemporary()` — выделение временной переменной для промежуточных вычислений

### 5.5. Генерация выражений

#### Класс `ExpressionCodeGen`

Генерирует WASM инструкции для различных типов выражений.

**Литералы:**
- `IntegerLiteralNode` → `i32.const <значение>`
- `RealLiteralNode` → `f64.const <значение>`
- `BooleanLiteralNode` → `i32.const 1` или `i32.const 0`

**Идентификаторы:**
- Локальные переменные в памяти → загрузка по оффсету (`i32.load` или `f64.load`)
- Локальные переменные в WASM locals → `local.get <индекс>`
- Глобальные переменные → загрузка по глобальному оффсету

**Особенности:**
- Деление всегда производит вещественный результат
- Автоматическая конвертация типов при смешанных операциях
- Поддержка всех арифметических, логических и операций сравнения

**Доступ к массивам и записям:**

- **Массивы:** вычисление адреса элемента с учётом размера элемента и 1-based индексации
- **Записи:** вычисление оффсета поля относительно начала записи

**Вызовы процедур:**

- Примитивные типы передаются по значению
- Сложные типы (массивы, записи) передаются по ссылке (адрес в памяти)

### 5.6. Генерация операторов

#### Класс `StatementCodeGen`

Генерирует WASM инструкции для операторов языка.

**Присваивание:**

```csharp
private void GenerateAssignment(AssignmentNode node)
{
    // Для сложных типов — побайтовое копирование через memory.copy
    if (targetType is ArrayType || targetType is RecordType)
    {
        GenerateAddress(node.Target);  // dest
        GenerateAddress(node.Value);    // src
        builder.I32Const(size);         // len
        builder.MemoryCopy();
    }
    // Для примитивных типов — обычное сохранение с конвертацией
    else
    {
        GenerateAddress(node.Target);
        Generate(node.Value);
        // Конвертация типов при необходимости
        if (IsRealType(targetType))
            builder.F64Store();
        else
            builder.I32Store();
    }
}
```

**Условные операторы:**

```csharp
private void GenerateIfStatement(IfStatementNode node)
{
    Generate(node.Condition);
    builder.If();
    
    // Генерация then-ветки
    foreach (var stmt in node.ThenBody)
        Generate(stmt);
    
    if (node.ElseBody.Count > 0)
    {
        builder.Else();
        // Генерация else-ветки
        foreach (var stmt in node.ElseBody)
            Generate(stmt);
    }
    
    builder.End();
}
```

**Циклы:**

- **`while`:** использует комбинацию `block` и `loop` с условным выходом через `br_if`
- **`for`:** инициализация переменной, проверка условия, инкремент/декремент, условный переход

**Возврат из функции:**

- Генерация возвращаемого значения (если есть)
- Автоматическая конвертация типов при необходимости
- Инструкция `return`

**Печать:**

Автоматический выбор функции печати в зависимости от типа:
- `integer` → `printInt`
- `real` → `printReal`
- `boolean` → `printBool`

### 5.7. Построение инструкций

#### Класс `WasmInstructionBuilder`

```csharp
public class WasmInstructionBuilder
{
    private readonly List<byte> _instructions = new();
    
    public void I32Const(int value) { ... }
    public void F64Const(double value) { ... }
    public void LocalGet(int index) { ... }
    public void LocalSet(int index) { ... }
    public void I32Add() { ... }
    public void F64Div() { ... }
    // ... и т.д.
}
```

**Особенности:**
- Все числовые значения кодируются в LEB128
- Поддержка всех необходимых WASM инструкций
- Управление структурой кода (`block`, `loop`, `if`, `else`, `end`)

### 5.8. Представление модуля

#### Класс `WasmModule`

Хранит структуру WASM модуля в памяти:

```csharp
public class WasmModule
{
    public List<WasmFunctionType> Types { get; set; }
    public List<WasmImport> Imports { get; set; }
    public List<WasmFunction> Functions { get; set; }
    public WasmMemory Memory { get; set; }
    public List<WasmExport> Exports { get; set; }
}
```

**Методы:**
- `AddType()` — добавление типа функции (с дедупликацией)
- `AddImport()` — регистрация импортируемой функции
- `AddFunction()` — регистрация функции модуля
- `AddExport()` — экспорт функции или памяти

### 5.9. Запись бинарного формата

#### Класс `WasmBinaryWriter`

Сериализует модуль в бинарный формат WASM согласно спецификации.

**Структура бинарного файла:**

1. **Магическое число и версия:** `0x00 0x61 0x73 0x6D` (ASCII "asm") + версия `0x01 0x00 0x00 0x00`
2. **Секция типов (ID=1):** сигнатуры всех функций
3. **Секция импортов (ID=2):** импортируемые функции (`env.printInt`, `env.printReal`, `env.printBool`)
4. **Секция функций (ID=3):** индексы типов для каждой функции
5. **Секция памяти (ID=5):** определение линейной памяти
6. **Секция экспортов (ID=7):** экспортируемые функции и память
7. **Секция кода (ID=10):** тела всех функций с локальными переменными и инструкциями

**Кодирование:**
- Все числа кодируются в LEB128 (Little Endian Base 128)
- Строки кодируются как длина (LEB128) + UTF-8 байты
- Локальные переменные сжимаются в группы одинаковых типов

### 5.10. Пример результата (WAT для наглядности)

```wasm
(module
  (memory (export "memory") 1)
  
  (import "env" "printInt" (func $printInt (param i32)))
  (import "env" "printReal" (func $printReal (param f64)))
  (import "env" "printBool" (func $printBool (param i32)))
  
  (func $main
    (local $x i32)
    (local $y i32)
    
    i32.const 50
    local.set $x
    
    i32.const 1
    local.set $y
    
    local.get $y
    call $printInt
  )
  
  (export "main" (func $main))
)
```

---
