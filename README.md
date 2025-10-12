# Compiler

Компилятор императивного языка программирования, разработанный как учебный проект.

## Структура проекта

```
compiler/
├── Compiler.sln              # Solution файл (включает все проекты)
├── src/
│   └── Compiler/             # Основной проект компилятора
│       ├── Compiler.csproj
│       ├── Program.cs        # Точка входа
│       └── Lexer/            # Лексический анализатор
│           ├── LexerClass.cs
│           ├── Token.cs
│           ├── TokenDefinitions.cs
│           └── TokenType.cs
├── tests/                    # Проект с тестами
│   ├── tests.csproj
│   ├── LexerTests.cs         # Unit-тесты лексера
│   ├── SimpleTestRunner.cs  # Кастомный test runner
│   └── test_files/           # Тестовые файлы
├── examples/                 # Примеры программ
│   └── test.imperative
└── docs/                     # Документация проекта
```

## Команды

### Использование Makefile (рекомендуется)

Для удобства в проекте есть Makefile с набором полезных команд:

```bash
# Показать все доступные команды
make help

# Основные команды
make build          # Собрать проект
make test           # Запустить тесты
make run            # Запустить компилятор
make clean          # Очистить артефакты сборки

# Команды для разработки
make dev            # Clean + Restore + Build + Test
make watch          # Автоматическая пересборка при изменениях
make format         # Форматировать код
make lint           # Проверить стиль кода

# Дополнительные команды
make release        # Собрать Release версию
make test-verbose   # Запустить тесты с подробным выводом
make rebuild        # Полная пересборка (clean + build)
```

### Прямое использование dotnet CLI

Если вы предпочитаете использовать dotnet напрямую:

```bash
# Сборка проекта
dotnet build Compiler.sln

# Запуск компилятора
dotnet run --project src/Compiler/Compiler.csproj

# Запуск тестов
dotnet test Compiler.sln

# Очистка проекта
dotnet clean Compiler.sln
```

## To-do's
- Fix invalid words (ex. 4fa should be one tkInvalid token, now is 3 different tkInvalid tokens)

## Требования
- .NET 9.0 SDK или выше

## Лицензия
См. файл LICENSE
